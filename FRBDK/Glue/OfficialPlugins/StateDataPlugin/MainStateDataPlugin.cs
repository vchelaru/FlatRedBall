using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.StateDataPlugin.Controls;
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
        StateDataControl control;
        PluginTab tab;
        public override string FriendlyName { get { return "State Data Plugin"; } }

        // 0.1.0 - Initial creation
        // 0.2.0 - Fixed static variables showing up when they shouldn't
        public override Version Version
        {
            get { return new Version(0, 2, 0); }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            HideCategoryUi();
            return true;
        }

        public override void StartUp()
        {
            this.ReactToItemSelectHandler += HandleReactToItemSelect;
        }

        private void HandleReactToItemSelect(TreeNode selectedTreeNode)
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

        private void ShowCategory(StateSaveCategory currentStateSaveCategory)
        {
            if(control == null)
            {
                control = new StateDataControl();

                // continue here by either creating a new VM or modifying the existing
                // to have columns set from the element's variables

                tab = this.CreateTab(control, "State Data");
            }
            tab.Show();

            var viewModel = new StateCategoryViewModel(currentStateSaveCategory, GlueState.Self.CurrentElement);

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
