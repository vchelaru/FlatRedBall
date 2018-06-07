using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.StateDataPlugin.Controls;
using OfficialPlugins.StateDataPlugin.ViewModels;
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

        public override string FriendlyName { get { return "State Data Plugin"; } }

        public override Version Version
        {
            get { return new Version(0, 1, 0); }
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
            this.RemoveTab();
        }

        private void ShowCategory(StateSaveCategory currentStateSaveCategory)
        {
            if(control == null)
            {
                control = new StateDataControl();

                // continue here by either creating a new VM or modifying the existing
                // to have columns set from the element's variables

                this.AddToTab(PluginManager.CenterTab, control, "State Data");
            }
            else
            {
                this.AddTab();
            }

            var viewModel = new StateCategoryViewModel(currentStateSaveCategory, GlueState.Self.CurrentElement);

            foreach(var variable in GlueState.Self.CurrentElement.CustomVariables)
            {
                viewModel.Columns.Add(variable.Name);
            }

            control.DataContext = viewModel;
            control.RefreshColumns();
        }
    }
}
