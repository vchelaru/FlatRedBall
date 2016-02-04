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

            this.ReactToChangedPropertyHandler += HandleRefreshProperties;

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
                HandleNamedObjectSelect();
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
            AddOrShowGrid();


            if (showSettings)
            {
                settingsGrid.Instance = GlueState.Self.CurrentElement;
            }

            variableGrid.Instance = GlueState.Self.CurrentElement;

            ElementVariableShowingLogic.UpdateShownVariables(variableGrid, GlueState.Self.CurrentElement);
        }

        private void HandleNamedObjectSelect()
        {
            AddOrShowGrid();

            if (showSettings)
            {
                settingsGrid.Instance = GlueState.Self.CurrentNamedObjectSave;
            }
            variableGrid.Instance = GlueState.Self.CurrentNamedObjectSave;

            NamedObjectVariableShowingLogic.UpdateShownVariables(variableGrid, GlueState.Self.CurrentNamedObjectSave,
                GlueState.Self.CurrentElement);
        }

        private void AddOrShowGrid()
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

                if (showSettings)
                {
                    settingsGrid = new DataUiGrid();
                    settingsTab = this.AddToTab(PluginManager.CenterTab, settingsGrid, "Settings");
                    settingsTab.DrawX = false;
                }
            }
            else
            {
                this.ShowTab(variableTab);
                if (showSettings)
                {
                    this.ShowTab(settingsTab);
                }
            }
        }
    }
}
