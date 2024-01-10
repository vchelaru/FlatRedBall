using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.CustomVariablePlugin.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using WpfDataUi;

namespace OfficialPlugins.CustomVariablePlugin
{
    [Export(typeof(PluginBase))]
    internal class MainCustomVariablePlugin : PluginBase
    {
        PluginTab tab;
        CustomVariablePropertiesView view;

        public override void StartUp()
        {
            this.ReactToItemsSelected += HandleItemsSelected;
            this.TryAssignPreferredDisplayerFromName += PreferredDisplayerManager.TryAssignPreferredDisplayerFromName;
        }

        private void HandleItemsSelected(List<ITreeNode> list)
        {
            var customVariable = list.FirstOrDefault()?.Tag as CustomVariable;

            if(customVariable != null)
            {
                ShowInTab(customVariable);
            }
            else
            {
                tab?.Hide();
            }
        }

        private void ShowInTab(CustomVariable customVariable)
        {
            if(tab == null)
            {
                view = new CustomVariablePropertiesView();
                tab = this.CreateTab(view, "Properties (experimental)");
            }

            view.RefreshAll(customVariable);

            tab.Show();
        }
    }
}
