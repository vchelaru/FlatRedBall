using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using WpfDataUi;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;

namespace OfficialPlugins.VariableDisplay
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : EmbeddedPlugin
    {
        #region Fields

        DataUiGrid settingsGrid;
        DataUiGrid variableGrid;

        PluginTab settingsTab;
        PluginTab variableTab;

        const bool showSettings = false;

        #endregion

        public override void StartUp()
        {
            this.ReactToItemSelectHandler += HandleItemSelect;

            this.ReactToLoadedGlux += HandleLoadedGlux;

            this.ReactToChangedPropertyHandler += HandleRefreshProperties;

        }

        private void HandleLoadedGlux()
        {
            HandleItemSelect(GlueState.Self.CurrentTreeNode);
        }

        private void HandleRefreshProperties(string changedMember, object oldValue)
        {
            if (this.variableGrid != null)
            {
                RefreshLogic.RefreshGrid(variableGrid);
            }
        }

        //private void HandleRefreshProperties()
        //{
        //    if(this.variableGrid != null)
        //    {
        //        VariableShowingLogic.RefreshGrid(variableGrid);
        //    }
        //}

        private void HandleItemSelect(System.Windows.Forms.TreeNode selectedTreeNode)
        {
            if(GlueState.Self.CurrentNamedObjectSave != null)
            {
                HandleNamedObjectSelect(GlueState.Self.CurrentNamedObjectSave);
            }
            else if(GlueState.Self.CurrentStateSave != null || GlueState.Self.CurrentStateSaveCategory != null)
            {
                // For now we don't handle showing states, so let's hide it so the user doens't think
                // they are editing states
                RemoveTab(variableTab);
            }
            else if(GlueState.Self.CurrentElement != null && selectedTreeNode.IsRootCustomVariablesNode())
            {
                ShowVariablesForCurrentElement();
            }
            else
            {
                RemoveTab(variableTab);

                if (showSettings)
                {
                    RemoveTab(settingsTab);
                }
            }
        }

        private void ShowVariablesForCurrentElement()
        {
            if (showSettings)
            {
                AddOrShowSettingsGrid();
                settingsGrid.Instance = GlueState.Self.CurrentElement;
            }

            AddOrShowVariableGrid();
            variableGrid.Instance = GlueState.Self.CurrentElement;
            ElementVariableShowingLogic.UpdateShownVariables(variableGrid, GlueState.Self.CurrentElement);
        }

        private void HandleNamedObjectSelect(NamedObjectSave namedObject)
        {

            if (showSettings)
            {
                AddOrShowSettingsGrid();
                settingsGrid.Instance = namedObject;
            }


            // If we are showing a NOS that comes from a file, don't show the grid.
            // Old Glue used to show the grid, but this introduces problems:
            // 1. This means that the variable showing code has to look at the file to
            //    get current values, rather than just at the ATI. This means we need new
            //    functionality in the plugin class for pulling values from files. 
            // 2. The plugin class has to do some kind of intelligent caching to prevent
            //    hitting the disk for every property (which would be slow). 
            // 3. The plugin will also have to respond to file changes and refresh the grid.
            // 4. This may confuse users who are expecting the changes in Glue to modify the original
            //    file, instead of them understanding that Glue overrides the file. 
            // I think I'm going to keep it simple and only show the grid if it doesn't come from file:
            bool shouldShowVariables = namedObject.SourceType != SourceType.File;

            if(shouldShowVariables == false)
            {
                if(variableGrid != null)
                {
                    variableGrid.Visibility = System.Windows.Visibility.Collapsed;
                }
                if(variableTab != null)
                {
                    RemoveTab(variableTab);
                }
            }
            else
            {
                AddOrShowVariableGrid();
                variableGrid.Instance = namedObject;
                variableGrid.Visibility = System.Windows.Visibility.Visible;

                NamedObjectVariableShowingLogic.UpdateShownVariables(variableGrid, namedObject,
                    GlueState.Self.CurrentElement);
            }
        }

        private void AddOrShowSettingsGrid()
        {
            if(settingsGrid == null)
            {

                settingsGrid = new DataUiGrid();
                settingsTab = this.AddToTab(PluginManager.CenterTab, settingsGrid, "Settings");
                settingsTab.DrawX = false;
            }
            else
            {
                this.ShowTab(settingsTab);
            }
        }

        private void AddOrShowVariableGrid()
        {
            if(variableGrid == null)
            {
                variableGrid = new DataUiGrid();
                
                var tabControl = PluginManager.CenterTab;
                
                variableTab = this.AddToTab(tabControl, variableGrid, "Variables");
                variableTab.DrawX = false;

                // let's make this the first item and have it be focused:
                tabControl.SelectedTab = variableTab;
                // This makes it the last tab clicked, which gives it priority:
                variableTab.LastTimeClicked = DateTime.Now;
            }
            else
            {
                this.ShowTab(variableTab);
            }
        }
    }
}
