using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Interfaces;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;

namespace FlatRedBall.Glue.MVVM;

/// <summary>
/// What follows a change to a value in a Glue object's <see cref="IPropertyListContainer.Properties"/>: code
/// generation for its element (or Global Content), the named-object change reaction (live edit restarts), and a
/// save. <see cref="PropertyListContainerViewModel.SetAndPersist"/> runs this for every view model edit; code
/// that changes such a value without a view model (a plugin's command class) calls it too.
/// </summary>
public static class PropertyListChangeFollowUp
{
    public static void QueueCodegenReactionAndSave(IPropertyListContainer glueObject, string propertyName, object oldValue)
    {
        GlueElement element = null;
        bool isGlobalContent = false;

        if (glueObject is GlueElement glueElement)
        {
            element = glueElement;
        }
        else if (glueObject is NamedObjectSave nos)
        {
            element = ObjectFinder.Self.GetElementContaining(nos);
        }
        else if (glueObject is ReferencedFileSave referencedFile)
        {
            element = ObjectFinder.Self.GetElementContaining(referencedFile);

            if (element == null)
            {
                isGlobalContent = GlueState.Self.CurrentGlueProject.GlobalFiles?.Contains(referencedFile) == true;
            }
        }

        if (element != null)
        {
            TaskManager.Self.Add(() =>
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
            }, $"Generating code for {element}", TaskExecutionPreference.AddOrMoveToEnd);
        }
        else if (isGlobalContent)
        {
            TaskManager.Self.Add(() =>
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateGlobalContentCode();
            }, "Generating Global Content Code", TaskExecutionPreference.AddOrMoveToEnd);
        }

        // Do this in a task after the codegen so that the compiler can pick up on it and restart
        if (glueObject is NamedObjectSave namedObject)
        {
            TaskManager.Self.Add(() =>
            {
                TaskManager.Self.OnUiThread(() =>
                    EditorObjects.IoC.Container.Get<NamedObjectSetVariableLogic>().ReactToNamedObjectChangedValue(
                        propertyName, oldValue, namedObjectSave: namedObject));
            },
            "Restarting due to change " + namedObject.InstanceName + "." + propertyName, TaskExecutionPreference.AddOrMoveToEnd);
        }

        if (element != null)
        {
            // add or move to end makes things a little faster, especially when dealing with sliders
            GlueCommands.Self.GluxCommands.SaveElementAsync(element, TaskExecutionPreference.AddOrMoveToEnd);
        }
        else if (glueObject is ReferencedFileSave)
        {
            // add or move to end makes things a little faster, especially when dealing with sliders
            GlueCommands.Self.GluxCommands.SaveGlujFile(TaskExecutionPreference.AddOrMoveToEnd);
        }
        else
        {
            // December 26, 2024 - Not sure if this case can get hit anymore, but keeping it here just in case:
            GlueCommands.Self.GluxCommands.SaveProjectAndElements();
        }
    }
}
