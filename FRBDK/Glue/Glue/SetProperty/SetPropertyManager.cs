using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses.Helpers;
using Glue;
using FlatRedBall.Glue.SaveClasses;
using EditorObjects.IoC;

namespace FlatRedBall.Glue.SetVariable
{
    /// <summary>
    /// Initial responder to when an object property (not variable) get changed.  This handles determing the type 
    /// of change that occurred and calling methods on object-based handlers - like 
    /// NamedObjectSave vs. ReferencedFileSave.
    /// </summary>
    public class SetPropertyManager
    {

        public void PropertyValueChanged(PropertyValueChangedEventArgs e, System.Windows.Forms.PropertyGrid mPropertyGrid)
        {
            UnreferencedFilesManager.Self.ProcessRefreshOfUnreferencedFiles();

            #region Check for Errors

            if (mPropertyGrid == null)
            {
                System.Windows.Forms.MessageBox.Show("There has been an internal error in Glue related to updating the PropertyGrid.  This likely happens if there has been an earlier error in Glue.  You should probably restart Glue.");
                MainGlueWindow.Self.HasErrorOccurred = true;
            }

            #endregion

            string changedMember = e.ChangedItem.PropertyDescriptor.Name;
            object oldValue = e.OldValue;
            string variableName = e.ChangedItem.Label;
            string parentGridItemName = null;
            if (e.ChangedItem != null && e.ChangedItem.Parent != null)
            {
                parentGridItemName = e.ChangedItem.Parent.Label;
            }

            ReactToPropertyChanged(changedMember, oldValue, variableName, parentGridItemName);
        }

        /// <summary>
        /// Reacts to the changing of a property by name considering the current GlueElement, NamedObject, ReferencedFile, StateSave, StateSaveCategory, Event, or Variable.
        /// Note that this should not be called if a property change occurs on an object which is not selected. In that case, the
        /// specific Logic object should be invoked.
        /// </summary>
        /// <param name="variableNameAsDisplayed">The variable name as displayed in the property grid.</param>
        /// <param name="oldValue">The value before the change</param>
        /// <param name="variableName">The variable name as defined in Glue (no spaces)</param>
        /// <param name="parentGridItemName">The parent PropertyGridItem, usually null unless the value being changed is a component of a larger property grid.</param>
        public async void ReactToPropertyChanged(string variableNameAsDisplayed, object oldValue, 
            string variableName, string parentGridItemName)
        {
            var mPropertyGrid = MainGlueWindow.Self.PropertyGrid;

            bool pushReactToChangedProperty = true;
            bool updateTreeView = true;

            #region EventResponseSave
            if (GlueState.Self.CurrentEventResponseSave != null)
            {
                Container.Get<EventResponseSaveSetVariableLogic>().ReactToChange(
                    variableNameAsDisplayed, oldValue, GlueState.Self.CurrentEventResponseSave, GlueState.Self.CurrentElement);
            }

            #endregion

            #region State

            else if (GlueState.Self.CurrentStateSave != null)
            {
                Container.Get<StateSaveSetVariableLogic>().ReactToStateSaveChangedValue(
                    GlueState.Self.CurrentStateSave, GlueState.Self.CurrentStateSaveCategory, variableNameAsDisplayed, oldValue,
                    GlueState.Self.CurrentElement, ref updateTreeView);


            }

            #endregion

            #region NamedObject

            else if (GlueState.Self.CurrentNamedObjectSave != null)
            {
                await Container.Get<NamedObjectSetVariableLogic>().ReactToNamedObjectChangedValue(
                    variableNameAsDisplayed, oldValue, parentGridItemName, GlueState.Self.CurrentNamedObjectSave);
            }

            #endregion

            #region ReferencedFile

            else if (GlueState.Self.CurrentReferencedFileSave != null)
            {
                Container.Get<ReferencedFileSaveSetPropertyManager>().ReactToChangedReferencedFile(
                    variableNameAsDisplayed, oldValue, ref updateTreeView);
            }

            #endregion

            #region CustomVariable

            else if (GlueState.Self.CurrentCustomVariable != null)
            {
                await Container.Get<CustomVariableSaveSetPropertyLogic>().ReactToCustomVariableChangedValue(
                    variableNameAsDisplayed, GlueState.Self.CurrentCustomVariable, oldValue);
            }
            else if (mPropertyGrid.SelectedObject != null && mPropertyGrid.SelectedObject is PropertyGridDisplayer &&
                GlueState.Self.CurrentElement != null && GlueState.Self.CurrentElement.GetCustomVariableRecursively(variableName) != null)
            {
                await Container.Get<CustomVariableSaveSetPropertyLogic>().ReactToCustomVariableChangedValue(
                    variableName, GlueState.Self.CurrentElement.GetCustomVariableRecursively(variableName), oldValue);
            }
            #endregion

            // Check Entities and Screens after checking variables and objects
            #region Entity
            else if (GlueState.Self.CurrentEntitySave != null)
            {
                Container.Get<EntitySaveSetPropertyLogic>().ReactToEntityChangedProperty(variableNameAsDisplayed, oldValue, GlueState.Self.CurrentEntitySave);
                pushReactToChangedProperty = false;
            }

            #endregion

            #region ScreenSave

            else if (GlueState.Self.CurrentScreenSave != null)
            {
                Container.Get<ScreenSaveSetVariableLogic>().ReactToScreenChangedValue(variableNameAsDisplayed, oldValue);
            }

            #endregion

            #region Global content container node

            else if (GlueState.Self.CurrentTreeNode.Root.IsGlobalContentContainerNode())
            {
                Container.Get<GlobalContentSetVariableLogic>().ReactToGlobalContentChangedValue(
                    variableNameAsDisplayed, oldValue, ref updateTreeView);
            }

            #endregion


            if(parentGridItemName == "State Variable")
            {
                PluginManager.ReactToStateVariableChanged(GlueState.Self.CurrentStateSave,
                    GlueState.Self.CurrentStateSaveCategory,
                    variableName);
            }
            else
            {
                if(pushReactToChangedProperty)
                {
                    PluginManager.ReactToChangedProperty(variableNameAsDisplayed, oldValue, GlueState.Self.CurrentElement, null);
                }
            }

            if (GlueState.Self.CurrentElement != null)
            {
                GlueCommands.Self.GenerateCodeCommands
                    .GenerateElementAndReferencedObjectCode(GlueState.Self.CurrentElement);
            }
            else if (GlueState.Self.CurrentReferencedFileSave != null)
            {
                await TaskManager.Self.AddAsync(() => GlobalContentCodeGenerator.UpdateLoadGlobalContentCode(), "Updating load global content code");
            }

            // UpdateCurrentObjectReferencedTreeNodes
            // kicks off a save by default.  Therefore
            // we don't need to call SaveProjects if UpdateCurrentObjectReferencedTreeNodes
            // is called.
            if (updateTreeView)
            {
                if (GlueState.Self.CurrentElement != null)
                {
                    GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();
                }
                else if (GlueState.Self.CurrentReferencedFileSave != null)
                {
                    GlueCommands.Self.RefreshCommands.RefreshGlobalContent();
                }
            }
            else
            {
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }

            mPropertyGrid.Refresh();

            GluxCommands.Self.SaveGlux();

            // Vic says:  This was intented to refresh the variables at one point
            // but this is a messy feature.  I think we should just refresh the entire
            // glux whenever a change is made now that it's async
            //RemotingManager.RefreshVariables(false); 
        }

    }
}
