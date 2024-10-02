using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Controls;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins;
using Glue;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.FormHelpers;
using OfficialPlugins.GlobalContentManagerPlugin.Views;

namespace PluginTestbed.GlobalContentManagerPlugins
{
    [Export(typeof(PluginBase))]
    public class GlobalContentManagerHelperPlugin : PluginBase
    {
        PluginTab GlobalContentPropertiesTab;

        GlobalContentPropertiesView GlobalContentPropertiesView;

        public override string FriendlyName
        {
            get { return "Global ContentManager Helper Plugin"; }
        }

        public override void StartUp()
        {
            this.AddMenuItemTo("GlobalContent Membership", "GlobalContent Membership", mMenuItem_Click, "Content");

            this.ReactToItemsSelected += HandleItemsSelected;
        }

        private void HandleItemsSelected(List<ITreeNode> list)
        {
            if(list.Any(item => item.IsGlobalContentContainerNode()))
            {
                ShowGlobalContentPropertiesTab();
            }
            else
            {
                GlobalContentPropertiesTab?.Hide();
            }
        }

        private void ShowGlobalContentPropertiesTab()
        {
            if(GlobalContentPropertiesView== null)
            {
                GlobalContentPropertiesView = new GlobalContentPropertiesView();
                GlobalContentPropertiesTab = this.CreateAndAddTab(GlobalContentPropertiesView, "GlobalContent Properties");
            }
            this.GlobalContentPropertiesView.Refresh();
            GlobalContentPropertiesTab.Show();
        }

        void mMenuItem_Click()
        {
            PluginForm pluginForm = new PluginForm();
            pluginForm.GlueCommands = GlueCommands.Self;
            pluginForm.RefreshElements();

            pluginForm.ShowDialog(MainGlueWindow.Self);

        }
    }
}
