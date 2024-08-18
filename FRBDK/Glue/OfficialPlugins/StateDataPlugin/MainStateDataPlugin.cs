using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.StateDataPlugin.Controls;
using OfficialPlugins.StateDataPlugin.StateError;
using OfficialPlugins.StateDataPlugin.ViewModels;
using OfficialPluginsCore.StateDataPlugin.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OfficialPlugins.StateDataPlugin
{
    [Export(typeof(PluginBase))]
    public class MainStateDataPlugin : PluginBase
    {
        #region Fields/Properties
        StateDataControl control;
        PluginTab tab;

        StateErrorReporter stateErrorReporter;

        public override string FriendlyName { get { return "State Data Plugin"; } }

        // 0.1.0 - Initial creation
        // 0.2.0 - Fixed static variables showing up when they shouldn't
        public override Version Version
        {
            get { return new Version(0, 2, 0); }
        }

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            HideCategoryUi();
            return true;
        }

        public override void StartUp()
        {
            this.ReactToItemSelectHandler += HandleReactToItemSelect;
            this.ReactToStateRemovedHandler += HandleStateRemoved;
            this.ReactToUnloadedGlux += HandleGluxUnloaded;
            this.ReactToStateCategoryExcludedVariablesChanged += HandleStateCategoryVariableChange;

            stateErrorReporter = new StateErrorReporter();
            this.AddErrorReporter(stateErrorReporter);
        }

        private void HandleGluxUnloaded()
        {
            HideCategoryUi();
        }

        private void HandleStateRemoved(IElement element, string stateName)
        {
            GlueCommands.Self.RefreshCommands.RefreshErrorsFor(stateErrorReporter);
        }

        private void HandleReactToItemSelect(ITreeNode selectedTreeNode)
        {
            if(GlueState.Self.CurrentTreeNode?.IsStateCategoryNode() == true)
            {
                ShowCategory(GlueState.Self.CurrentStateSaveCategory);
            }
            else
            {
                HideCategoryUi();
            }
        }

        private void HideCategoryUi()
        {
            tab?.Hide();
        }

        private void HandleStateCategoryVariableChange(StateSaveCategory stateSaveCategory, string arg2, StateCategoryVariableAction arg3)
        {
            if(stateSaveCategory == GlueState.Self.CurrentStateSaveCategory && tab.IsShown)
            {
                ShowCategory(stateSaveCategory);
            }
        }

        private void ShowCategory(StateSaveCategory currentStateSaveCategory)
        {
            if(control == null)
            {
                control = new StateDataControl();

                // continue here by either creating a new VM or modifying the existing
                // to have columns set from the element's variables

                tab = this.CreateTab(control, "State Data");
                tab.IsPreferredDisplayerForType = (type) => type == nameof(StateSaveCategory);
            }
            tab.Show();

            var existing = control.DataContext as StateCategoryViewModel;

            var viewModel = new StateCategoryViewModel(currentStateSaveCategory, GlueState.Self.CurrentElement);

            if(existing != null)
            {
                // This code can trigger as a result of the state changing in the UI itself, so we need to make sure 
                // we preserve variables beteween vm recreation.
                viewModel.VariableManagementVisibility = existing.VariableManagementVisibility;
                viewModel.TopSectionHeight = existing.TopSectionHeight;
                viewModel.SelectedIndex = existing.SelectedIndex;
                viewModel.SelectedIncludedVariable = existing.SelectedIncludedVariable;
                viewModel.SelectedExcludedVariable = existing.SelectedExcludedVariable;
                viewModel.SelectedState = existing.SelectedState;
            }

            var variablesToConsider = GlueState.Self.CurrentElement.CustomVariables
                .Where(item=> VariableInclusionManager.ShouldIncludeVariable(item, currentStateSaveCategory))
                .ToList() ;

            foreach (var variable in variablesToConsider)
            {
                viewModel.Columns.Add(variable.Name);
            }

            control.DataContext = viewModel;
            control.RefreshColumns();
        }
    }
}
