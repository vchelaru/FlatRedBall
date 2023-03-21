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
using FlatRedBall.Glue.Elements;
using OfficialPluginsCore.PropertyGrid.Views;
using OfficialPluginsCore.PropertyGrid.ViewModels;
using OfficialPlugins.PropertyGrid.Managers;

namespace OfficialPlugins.VariableDisplay
{
    [Export(typeof(PluginBase))]
    public class MainPropertyGridPlugin : EmbeddedPlugin
    {
        #region Fields

        DataUiGrid settingsGrid;
        public static VariableView VariableGrid;

        VariableViewModel variableViewModel;

        PluginTab settingsTab;
        PluginTab variableTab;

        const bool showSettings = false;

        public override string FriendlyName => "Main Property Grid Plugin";

        #endregion

        public override void StartUp()
        {
            VariableDisplayerTypeManager.FillTypeNameAssociations();

            this.ReactToItemSelectHandler += HandleItemSelect;

            this.ReactToLoadedGlux += HandleLoadedGlux;

            this.ReactToChangedPropertyHandler += HandleRefreshProperties;

        }

        private void HandleLoadedGlux()
        {
            HandleItemSelect( GlueState.Self.CurrentTreeNode);
        }

        private void HandleRefreshProperties(string changedMember, object oldValue, GlueElement glueElement)
        {
            if (VariableGrid != null)
            {
                RefreshLogic.RefreshGrid(VariableGrid.DataUiGrid);
            }
        }

        //private void HandleRefreshProperties()
        //{
        //    if(this.variableGrid != null)
        //    {
        //        VariableShowingLogic.RefreshGrid(variableGrid);
        //    }
        //}

        public void RefreshVariables()
        {
            if(GlueState.Self.CurrentNamedObjectSave != null)
            {
                HandleNamedObjectSelect(GlueState.Self.CurrentNamedObjectSave);
            }
            else if(GlueState.Self.CurrentElement != null)
            {
                ShowVariablesForCurrentElement();
            }
            //RefreshLogic.RefreshGrid(variableGrid.DataUiGrid);
        }

        private void HandleItemSelect(ITreeNode selectedTreeNode)
        {
            if(GlueState.Self.CurrentNamedObjectSave != null)
            {
                HandleNamedObjectSelect(GlueState.Self.CurrentNamedObjectSave);
            }
            else if(GlueState.Self.CurrentStateSave != null || GlueState.Self.CurrentStateSaveCategory != null)
            {
                // For now we don't handle showing states, so let's hide it so the user doens't think
                // they are editing states
                variableTab?.Hide();
            }
            else if(GlueState.Self.CurrentElement != null && 
                (selectedTreeNode.IsRootCustomVariablesNode() 
                // Feb 18, 2021 - It's annoying to have to select the Variables
                // node. The user should be able to see variables just by selecting
                // the entity itself.
                || selectedTreeNode.IsElementNode()))
            {
                ShowVariablesForCurrentElement();
            }
            else
            {
                variableTab?.Hide();

                if (showSettings)
                {
                    settingsTab?.Hide();
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

            variableViewModel.CanAddVariable = true;
            VariableGrid.DataUiGrid.Instance = GlueState.Self.CurrentElement;
            ElementVariableShowingLogic.UpdateShownVariables(VariableGrid.DataUiGrid, GlueState.Self.CurrentElement);
        }

        private void HandleNamedObjectSelect(NamedObjectSave namedObject)
        {
            // Update August 17, 2021
            // If it's a list, don't show the Variables tab. It's never got anything:
            var hide = namedObject.IsList;

            if(hide)
            {
                variableTab?.Hide();
                return;
            }


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
            // Update Jan 24, 2019
            // I thought about this problem for a bit and realized that this can be solved in two steps.
            // 1. The problem is that if we show the values that come from the AssetTypeInfo on a from-file
            //    object, the default values in the ATI may not match the values of the object in the file, which
            //    has mislead developers in the past. That is why I opted to just not show a property grid at all.
            //    However, if we just didn't show any defaults at all, that would solve the problem! To do this, we
            //    just need to take the AssetTypeInfo, clone it, and make the default values for all variables be null.
            //    This should result in a property grid that shows the variables with blank boxes/UI so that the user can
            //    fill it in.
            // 2. Plugins could be given the copied ATI to fill in with values from-file. If plugins don't, then the values
            //    would be blank, but if the plugin does want to, then the values from-file would show up.
            // I'm not going to implement this solution just yet, but I thought of how it could be done and thought it would
            // be good to document it here for future reference.
            // Update August 16, 2020
            // 1. I like the solution above to avoid confusion; but there are times when an object has properties which don't
            //    come from file - like the X/Y values of a TileMap. Therefore, some properties should have defaults that stick
            //    around. Eventually this probably means a new property on the VariableDefinition object.
            var isFile = namedObject.SourceType == SourceType.File;

            var ati = namedObject.GetAssetTypeInfo();

            if(isFile && ati != null)
            {
                try
                {
                    var oldTag = ati.Tag;
                    ati = FlatRedBall.IO.FileManager.CloneObject<AssetTypeInfo>(ati);
                    // The tag isn't cloned through serialization since that may cause 
                    // exceptions. But at runtime we may need the tag so we'll copy it over.
                    // The purpose of cloning is to wipe the variables, but we still want the
                    // rest of the ATI (including its tag) to be the same.
                    ati.Tag = oldTag;
                }
                catch(Exception e)
                {
                    int m = 3;
                }
                foreach(var variable in ati.VariableDefinitions)
                {
                    variable.DefaultValue = null;
                }
            }

            AddOrShowVariableGrid();
            // can't add variables on the instance:
            variableViewModel.CanAddVariable = false;

            // Setting the instance resets all categories. Categories get replaced
            // in the UpdateShownVariables method, so do we even need the instance set here?
            //VariableGrid.DataUiGrid.Instance = namedObject;
            VariableGrid.Visibility = System.Windows.Visibility.Visible;

            NamedObjectVariableShowingLogic.UpdateShownVariables(VariableGrid.DataUiGrid, namedObject,
                GlueState.Self.CurrentElement, ati);

        }

        private void AddOrShowSettingsGrid()
        {
            if(settingsGrid == null)
            {

                settingsGrid = new DataUiGrid();
                settingsTab = this.CreateTab(settingsGrid, "Settings");
                settingsTab.CanClose = false;
            }
            settingsTab.Show();
        }

        private void AddOrShowVariableGrid()
        {
            if(VariableGrid == null)
            {
                VariableGrid = new VariableView();

                variableViewModel = new VariableViewModel();
                VariableGrid.DataContext = variableViewModel;

                variableTab = this.CreateTab(VariableGrid, "Variables");
                variableTab.IsPreferredDisplayerForType = (typeName) =>
                {
                    if (typeName == "Variables") return true;
                    if (typeName == nameof(NamedObjectSave)) return true;

                    return false;

                };

                variableTab.TabShown += () =>
                {
                    var currentElement = GlueState.Self.CurrentElement;
                    var nos = GlueState.Self.CurrentNamedObjectSave;
                    if(nos != null)
                    {
                        HandleNamedObjectSelect(nos);
                    }
                    else if(currentElement != null)
                    {
                        ElementVariableShowingLogic.UpdateShownVariables(VariableGrid.DataUiGrid, currentElement);
                    }

                };
                
                //variableTab = this.AddToTab(tabControl, variableGrid, "Variables");
                variableTab.CanClose = false;

                // let's make this the first item and have it be focused:
                //tabControl.SelectedTab = variableTab;
                // Update May 4, 2021
                // We now have lots of tabs
                // that take priority over Variables, 
                // like collision tabs. We should rely on
                // the last clicked and not force this anymore:
                //GlueCommands.Self.DialogCommands.FocusTab("Variables");
                // This makes it the last tab clicked, which gives it priority:
                //variableTab.Focus();
            }
            variableTab.Show();
        }
    }
}
