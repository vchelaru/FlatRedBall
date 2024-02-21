using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Glue;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using System.Drawing;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using GlueSaveClasses;

namespace FlatRedBall.Glue.FormHelpers.PropertyGrids
{
    public static class PropertyGridRightClickHelper
    {
        #region Fields

        static System.Windows.Forms.ToolStripMenuItem mSetDefaultMenuItem;
        static System.Windows.Forms.ToolStripMenuItem mExposeVariable;

        static System.Windows.Forms.ToolStripMenuItem mUseCustomRectangle;
        static System.Windows.Forms.ToolStripMenuItem mUseFullScreen;

        static CustomVariable mHighlightedCustomVariable;
        #endregion

        public static void Initialize()
        {
            mSetDefaultMenuItem = new System.Windows.Forms.ToolStripMenuItem("Set to Default", null, SetDefaultClick);

            mExposeVariable = new ToolStripMenuItem("Expose Variable", null, ExposeVariableClick);

            mUseCustomRectangle = new ToolStripMenuItem("Use Custom Rectangle", null, UseCustomRectangleClick);
            mUseFullScreen = new ToolStripMenuItem("Use Full Screen", null, UseFullScreenClick);
        }

        public static void ReactToRightClick()
        {
            #region Get the context menu
            System.Windows.Forms.PropertyGrid propertyGrid = GlueCommands.Self.DialogCommands.PropertyGrid;
            mHighlightedCustomVariable = null;

            if (propertyGrid.ContextMenuStrip == null)
            {
                propertyGrid.ContextMenuStrip = new ContextMenuStrip();
            }


            var contextMenu = propertyGrid.ContextMenuStrip;
            contextMenu.Items.Clear();
            #endregion


            string label = propertyGrid.SelectedGridItem.Label;


            #region If there is a current StateSave

            if (GlueState.Self.CurrentStateSave != null)
            {
                // Assume that it's a variable
                contextMenu.Items.Add(mSetDefaultMenuItem);
            }

            #endregion

            #region If there is a current CustomVariable

            if (GlueState.Self.CurrentCustomVariable != null)
            {
                // Assume that it's a variable
                contextMenu.Items.Add(mSetDefaultMenuItem);
            }

            #endregion

            #region If there is a current NamedObject

            else if (GlueState.Self.CurrentNamedObjectSave != null)
            {
                NamedObjectSave namedObject = GlueState.Self.CurrentNamedObjectSave;

                // Is this a variable
                if (namedObject.GetCustomVariable(label) != null)
                {
                    contextMenu.Items.Add(mSetDefaultMenuItem);
                }

                else if (namedObject.IsLayer && label ==
                    "DestinationRectangle")
                {
                    if (namedObject.DestinationRectangle == null ||
                        !namedObject.DestinationRectangle.HasValue)
                    {
                        contextMenu.Items.Add(mUseCustomRectangle);
                    }
                    else
                    {
                        contextMenu.Items.Add(mUseFullScreen);
                    }
                }


            }

            #endregion

            #region If there is a current Entity Save (to be checked *after* the checks above)

            else if (GlueState.Self.CurrentElement != null)
            {
                if (GlueState.Self.CurrentTreeNode.IsRootCustomVariablesNode())
                {
                    CustomVariable customVariable = GlueState.Self.CurrentElement.GetCustomVariable(label);

                    if(customVariable != null)
                    {
                        mHighlightedCustomVariable = customVariable;
                        contextMenu.Items.Add(mSetDefaultMenuItem);
                    }
                }
                else if (GlueState.Self.CurrentEntitySave != null)
                {
                    
                    EntitySave sourceEntitySave = GlueState.Self.CurrentEntitySave;

                    if (label == "ImplementsIVisible" && sourceEntitySave != null && sourceEntitySave.ImplementsIVisible
                        && sourceEntitySave.GetCustomVariable("Visible") == null
                        )
                    {
                        contextMenu.Items.Add(mExposeVariable);
                    }
                    else if (label == "BaseEntity" && !string.IsNullOrEmpty(GlueState.Self.CurrentEntitySave.BaseEntity))
                    {
                        contextMenu.Items.Add("Go to definition", null, GoToDefinitionClick);

                    }
                }
            }

            #endregion



            PluginManager.ReactToPropertyGridRightClick(propertyGrid, contextMenu);
        }

        private static void SetDefaultClick(object sender, EventArgs e)
        {
            // set default, reset default, set to default, reset to default
            if (mHighlightedCustomVariable != null)
            {
                mHighlightedCustomVariable.DefaultValue = null;
                GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
            }
            else if (GlueState.Self.CurrentStateSave != null)
            {
                StateSave stateSave = GlueState.Self.CurrentStateSave;

                string valueToChange = GlueCommands.Self.DialogCommands.PropertyGrid.SelectedGridItem.Label;
                if (valueToChange.Contains(" set"))
                {
                    valueToChange = valueToChange.Substring(0, valueToChange.IndexOf(" set"));
                }
                for (int i = stateSave.InstructionSaves.Count - 1; i > -1; i--)
                {
                    if (stateSave.InstructionSaves[i].Member == valueToChange)
                    {
                        stateSave.InstructionSaves.RemoveAt(i);
                        break;
                    }
                }
            }
            else if (GlueState.Self.CurrentCustomVariable != null)
            {
                GlueState.Self.CurrentCustomVariable.DefaultValue = null;
                GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
            }
            else
            {
                NamedObjectSave currentNamedObject = GlueState.Self.CurrentNamedObjectSave;

                string variableToSet = GlueCommands.Self.DialogCommands.PropertyGrid.SelectedGridItem.Label;

                SetVariableToDefault(currentNamedObject, variableToSet);
            }

            GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

            GluxCommands.Self.SaveProjectAndElements();

            GlueCommands.Self.DialogCommands.PropertyGrid.Refresh();
        }

        public static void SetVariableToDefault(NamedObjectSave currentNamedObject, string variableToSet)
        {
            // July 13, 2014
            // This used to simply set the value to null, but why don't we remove it if it exists?
            // This way if an error is introduced by some plugin that sets the type to something invalid
            // the user can still remove it through this option and recover the type later.
            //currentNamedObject.SetPropertyValue(variableToSet, null);
            object oldValue = currentNamedObject.InstructionSaves
                .FirstOrDefault(item => item.Member == variableToSet)?.Value;


            currentNamedObject.InstructionSaves.RemoveAll(item => item.Member == variableToSet);

            var foundCustomVariable = currentNamedObject.GetCustomVariable(variableToSet);
            if (foundCustomVariable != null)
            {
                oldValue = oldValue ?? foundCustomVariable?.Value;
                // See if this variable is tunneled into in this element.
                // If so, set that value too.
                CustomVariableInNamedObject cvino = currentNamedObject.GetCustomVariable(variableToSet);
                object value = cvino.Value;

                var currentElement = GlueState.Self.CurrentElement;
                foreach (CustomVariable customVariable in currentElement.CustomVariables)
                {
                    if (customVariable.SourceObject == currentNamedObject.InstanceName &&
                        customVariable.SourceObjectProperty == variableToSet)
                    {
                        customVariable.DefaultValue = value;
                        break;
                    }
                }

                GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
            }

            PluginManager.ReactToNamedObjectChangedValue(variableToSet, oldValue, currentNamedObject);

        }

        private static void ExposeVariableClick(object sender, EventArgs e)
        {
            System.Windows.Forms.PropertyGrid propertyGrid = GlueCommands.Self.DialogCommands.PropertyGrid;

            string label = propertyGrid.SelectedGridItem.Label;

            if (label == "ImplementsIVisible")
            {
                // We're going to make a bool Visible for this now.

                CustomVariable newVariable = new CustomVariable();
                newVariable.Name = "Visible";
                newVariable.Type = "bool";

                GlueCommands.Self.GluxCommands.ElementCommands.AddCustomVariableToCurrentElement(newVariable);

            }
        }

        private static void GoToDefinitionClick(object sender, EventArgs e)
        {
            string baseName = GlueState.Self.CurrentElement.BaseElement;
            GlueState.Self.CurrentElement = ObjectFinder.Self.GetElement(baseName);
        }

        private static void UseCustomRectangleClick(object sender, EventArgs e)
        {

            FloatRectangle rectangle = new FloatRectangle(0, 0,
                ProjectManager.GlueProjectSave.ResolutionWidth,
                ProjectManager.GlueProjectSave.ResolutionHeight);

            GlueState.Self.CurrentNamedObjectSave.DestinationRectangle = rectangle;

            GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
            GluxCommands.Self.SaveProjectAndElements();
            GlueCommands.Self.DialogCommands.PropertyGrid.Refresh();
        }

        private static void UseFullScreenClick(object sender, EventArgs e)
        {
            GlueState.Self.CurrentNamedObjectSave.DestinationRectangle = null;

            GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
            GluxCommands.Self.SaveProjectAndElements();
            GlueCommands.Self.DialogCommands.PropertyGrid.Refresh();
        }


    }
}
