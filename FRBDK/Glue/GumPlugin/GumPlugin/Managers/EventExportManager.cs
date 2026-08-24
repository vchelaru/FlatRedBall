using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using GlueFormsCore.Managers;
using GumPlugin.CodeGeneration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumPlugin.Managers
{
    #region Events

    public enum GumEventTypes
    {
        ElementAdded,
        ElementDeleted,
        ElementRenamed,
        StateRenamed,
        StateCategoryRenamed,
        InstanceAdded,
        InstanceDeleted,
        InstanceRenamed,
    }

    #endregion

    #region Classes

    public class ExportedEventCollection
    {
        public Dictionary<string, List<ExportedEvent>> UserEvents { get; set; }
    }

    public class ExportedEvent
    {
        public string NewName { get; set; }
        public string OldName { get; set; }
        public string ElementType { get; set; }
        public GumEventTypes EventType { get; set; }
        public DateTime TimestampUtc { get; set; }

        public override string ToString()
        {
            string toReturn = EventType.ToString();

            if(EventType == GumEventTypes.ElementRenamed || 
                EventType == GumEventTypes.InstanceRenamed ||
                EventType == GumEventTypes.StateCategoryRenamed ||
                EventType == GumEventTypes.StateRenamed )
            {
                toReturn += $" {OldName} -> {NewName}";
            }
            return toReturn;
        }
    }

    #endregion

    public class EventExportManager : Singleton<EventExportManager>
    {
        /// <summary>
        /// The location where the file storing the last Gum response date.
        /// This way, FRB editor doesn't re-apply the same changes over and over
        /// from the gum_events.json file.
        /// </summary>
        public FilePath GumLastChangeFilePath
        {
            get
            {
                var glueProject = GlueState.Self.CurrentGlueProject;

                if(glueProject == null)
                {
                    return null;
                }
                else
                {
                    return GlueState.Self.ContentDirectory + "\\GumLastChangeFilePath.txt";
                }
            }
        }

        DateTime? LastTimeChangesHandledUtc;

        public async Task HandleEventExportFileChanged(string fileName)
        {
            string contents = null;
            try
            {
                contents = System.IO.File.ReadAllText(fileName);
            }
            catch
            {
                // do nothing, could have gotten deleted last minute...
            }

            if(!string.IsNullOrEmpty(contents))
            {
                ExportedEventCollection deserialized = null;
                try
                {
                    deserialized = 
                        JsonConvert.DeserializeObject<ExportedEventCollection>(contents);
                }
                catch
                {
                    // oh well
                }

                if(deserialized != null)
                {
                    var eventArray = deserialized.UserEvents
                        .SelectMany(item => item.Value)
                        .OrderBy(item => item.TimestampUtc)
                        .ToArray();

                    if(eventArray.Length > 0)
                    {
                        var file = GumLastChangeFilePath;
                        if(LastTimeChangesHandledUtc == null)
                        {
                            try
                            {
                                if (file.Exists())
                                {
                                    var text = System.IO.File.ReadAllText(file.FullPath);
                                    LastTimeChangesHandledUtc = DateTime.Parse(text, null, System.Globalization.DateTimeStyles.RoundtripKind);
                                }
                                else
                                {
                                    LastTimeChangesHandledUtc = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);
                                }
                            }
                            catch { }// do nothing
                        }

                        var eventsToReactTo = eventArray
                            .Where(eventInstance => eventInstance.TimestampUtc > LastTimeChangesHandledUtc && !string.IsNullOrEmpty(eventInstance.ElementType))
                            .ToArray();

                        foreach (var eventInstance in eventsToReactTo)
                        {
                            await ReactToExportedEvent(eventInstance);
                        }

                        try
                        {
                            var newTime = DateTime.UtcNow;
                            System.IO.File.WriteAllText(file.FullPath, newTime.ToString("O"));
                            LastTimeChangesHandledUtc = newTime;
                        }
                        catch(Exception e)
                        {
                            // it's okay, I think? 
                            // Update November 17, 2023 
                            // Ya, this is a warning, but
                            // we should not print an error
                            // here. We'll just output a warning:
                            GlueCommands.Self.PrintOutput($"Warning: Could not save Gum last change file {file}." +
                                $"You can continue to work normally, but you may want to see if something is " +
                                $"blocking access to this file if your team relies on this being part of the repository:\n{e}");
                        }
                    }
                }

            }
        }

        private async Task ReactToExportedEvent(ExportedEvent deserialized)
        {
            switch (deserialized.EventType)
            {
                case GumEventTypes.ElementDeleted:
                    HandleElementDeleted(deserialized);
                    break;
                case GumEventTypes.ElementRenamed:
                    await HandleElementRenamed(deserialized);
                    break;
            }
        }

        private async Task HandleElementRenamed(ExportedEvent exportedEvent)
        {
            var elementType = exportedEvent.ElementType;
            
            RemoveGumFilesFromProject(elementType, exportedEvent.OldName);

            var newCodeFileName = GetCustomCodeFileFor(exportedEvent.NewName);
            var oldCodeFileName = GetCustomCodeFileFor(exportedEvent.OldName);

            if(Gum.Managers.ObjectFinder.Self.GumProjectSave != null)
            {
                var gumProject = Gum.Managers.ObjectFinder.Self.GumProjectSave;

                Gum.DataTypes.ElementSave element = null;
                if(exportedEvent.ElementType == "Screens")
                {
                    element = gumProject.Screens
                        .FirstOrDefault(item => item.Name == exportedEvent.NewName);
                }
                else if(exportedEvent.ElementType == "Components")
                {
                    element = gumProject.Components
                        .FirstOrDefault(item => item.Name == exportedEvent.NewName);
                }

                if(element != null)
                {
                    // make sure it's updated:
                    CodeGeneratorManager.Self.GenerateCodeForcedTask(element, saveProjects:true);

                    await TaskManager.Self.AddAsync(async () =>
                    {
                        try
                        {
                            if(oldCodeFileName.Exists())
                            {
                                GlueCommands.Self.TryMultipleTimes(() =>
                                {
                                    System.IO.File.Copy(oldCodeFileName.FullPath, newCodeFileName.FullPath, overwrite: true);

                                    var contents = System.IO.File.ReadAllText(newCodeFileName.FullPath);
                                    RefactorManager.Self.RenameClassInCode(
                                        oldCodeFileName.NoPathNoExtension,
                                        newCodeFileName.NoPathNoExtension,
                                        ref contents);

                                    var oldNamespace = GueDerivingClassCodeGenerator.Self.GetFullRuntimeNamespaceFor(false, exportedEvent.OldName);
                                    var newNamespace = GueDerivingClassCodeGenerator.Self.GetFullRuntimeNamespaceFor(false, exportedEvent.NewName);

                                    if (oldNamespace != newNamespace)
                                    {
                                        RefactorManager.Self.RenameNamespaceInCode(
                                            oldNamespace,
                                            newNamespace,
                                            ref contents);
                                    }

                                    FlatRedBall.IO.FileManager.SaveText(contents, newCodeFileName.FullPath );
                                });

                                await GlueCommands.Self.ProjectCommands.TryAddCodeFileToProjectAsync(newCodeFileName, saveOnAdd:true);
                            }

                            // Same copy-and-rename for the paired Forms code-behind - previously only the
                            // GumRuntimes file was migrated, leaving the old XForms.cs behind with no
                            // defining Generated.cs once regeneration stopped targeting the old name
                            // (CS0759 - GitHub issue #2195).
                            var newFormsCodeFileName = GetCustomFormsCodeFileFor(elementType, exportedEvent.NewName);
                            var oldFormsCodeFileName = GetCustomFormsCodeFileFor(elementType, exportedEvent.OldName);

                            if (oldFormsCodeFileName.Exists())
                            {
                                GlueCommands.Self.TryMultipleTimes(() =>
                                {
                                    System.IO.File.Copy(oldFormsCodeFileName.FullPath, newFormsCodeFileName.FullPath, overwrite: true);

                                    var formsContents = System.IO.File.ReadAllText(newFormsCodeFileName.FullPath);
                                    RefactorManager.Self.RenameClassInCode(
                                        oldFormsCodeFileName.NoPathNoExtension,
                                        newFormsCodeFileName.NoPathNoExtension,
                                        ref formsContents);

                                    var oldFormsNamespace = FormsClassCodeGenerator.Self.GetFullRuntimeNamespaceFor(exportedEvent.OldName, elementType, prefixGlobal: false);
                                    var newFormsNamespace = FormsClassCodeGenerator.Self.GetFullRuntimeNamespaceFor(exportedEvent.NewName, elementType, prefixGlobal: false);

                                    if (oldFormsNamespace != newFormsNamespace)
                                    {
                                        RefactorManager.Self.RenameNamespaceInCode(
                                            oldFormsNamespace,
                                            newFormsNamespace,
                                            ref formsContents);
                                    }

                                    FlatRedBall.IO.FileManager.SaveText(formsContents, newFormsCodeFileName.FullPath);
                                });

                                await GlueCommands.Self.ProjectCommands.TryAddCodeFileToProjectAsync(newFormsCodeFileName, saveOnAdd:true);
                            }

                            var allNoses = FlatRedBall.Glue.Elements.ObjectFinder.Self.GetAllNamedObjects().ToArray();

                            var prefix = GlueState.Self.ProjectNamespace + ".GumRuntimes.";

                            var oldQualifiedType = prefix + exportedEvent.OldName.Replace('\\', '.').Replace("/", ".") + "Runtime";
                            var newQualifiedType = prefix + exportedEvent.NewName.Replace('\\', '.').Replace("/", ".") + "Runtime";

                            var elementsToRegen = new HashSet<GlueElement>();

                            foreach(var nos in allNoses)
                            {
                                if(nos.SourceClassType == oldQualifiedType || nos.SourceClassType == oldCodeFileName.NoPathNoExtension)
                                {
                                    // add this here:
                                    nos.SourceClassType = newQualifiedType;
                                    elementsToRegen.Add(ObjectFinder.Self.GetElementContaining(nos));
                                }
                            }

                            foreach(var element in elementsToRegen)
                            {
                                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
                            }

                            if(elementsToRegen.Count > 0)
                            {
                                GlueCommands.Self.GluxCommands.SaveProjectAndElements();
                            }
                        }
                        catch (Exception e)
                        {
                            GlueCommands.Self.PrintError($"Could not copy custom Gum file:\n{e}");
                        }

                    }, $"Copying {oldCodeFileName} to {newCodeFileName}");
                

                }

            }
        }

        private void HandleElementDeleted(ExportedEvent exportedEvent)
        {
            var elementType = exportedEvent.ElementType;
            var oldName = exportedEvent.OldName;
            RemoveGumFilesFromProject(elementType, oldName);
        }

        static FilePath GetCustomCodeFileFor(string elementName) =>
            $"{GlueState.Self.CurrentGlueProjectDirectory}GumRuntimes/{elementName}Runtime.cs";

        // Forms code lives under Forms/Screens or Forms/Components - "elementType" here is already
        // Gum's own "Screens"/"Components" string (see ExportedEvent.ElementType), matching
        // CodeGeneratorManager.FormsSubfolderFor's mapping for those two element kinds.
        static FilePath GetCustomFormsCodeFileFor(string elementType, string elementName) =>
            $"{GlueState.Self.CurrentGlueProjectDirectory}Forms/{elementType}/{elementName}Forms.cs";

        static FilePath GetGeneratedFormsCodeFileFor(string elementType, string elementName) =>
            $"{GlueState.Self.CurrentGlueProjectDirectory}Forms/{elementType}/{elementName}Forms.Generated.cs";

        private static void RemoveGumFilesFromProject(string elementType, string oldName)
        {
            switch (elementType)
            {
                case "Screens":
                case "Components":
                    // screen/component was deleted, so remove the runtime:
                    var customFileToRemove = GetCustomCodeFileFor(oldName);
                    var generatedFile =
                        $"{GlueState.Self.CurrentGlueProjectDirectory}GumRuntimes/{oldName}Runtime.Generated.cs";

                    // ...and the paired Forms code, which used to be left behind here - see GitHub issue #2195.
                    var customFormsFileToRemove = GetCustomFormsCodeFileFor(elementType, oldName);
                    var generatedFormsFile = GetGeneratedFormsCodeFileFor(elementType, oldName);

                    TaskManager.Self.Add(() =>
                    {
                        GlueCommands.Self.ProjectCommands.RemoveFromProjects(customFileToRemove, false);
                        GlueCommands.Self.ProjectCommands.RemoveFromProjects(generatedFile, true);

                        GlueCommands.Self.ProjectCommands.RemoveFromProjects(customFormsFileToRemove, false);
                        GlueCommands.Self.ProjectCommands.RemoveFromProjects(generatedFormsFile, true);

                    },
                    $"Removing Gum object {oldName}");

                    break;
            }
        }
    }
}
