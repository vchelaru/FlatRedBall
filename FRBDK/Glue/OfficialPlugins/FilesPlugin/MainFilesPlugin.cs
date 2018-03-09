using FlatRedBall.Glue.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.FilesPlugin.Controls;
using OfficialPlugins.FilesPlugin.ViewModels;
using System.ComponentModel.Composition;

namespace OfficialPlugins.FilesPlugin
{
    [Export(typeof(PluginBase))]
    public class MainFilesPlugin : PluginBase
    {
        FileReferenceControl control;
        FileReferenceViewModel viewModel;

        public override string FriendlyName
        {
            get
            {
                return "Files Plugin";
            }
        }

        public override Version Version
        {
            get
            {
                return new Version();
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            this.ReactToItemSelectHandler += HandleItemSelected;

        }

        private void HandleItemSelected(TreeNode selectedTreeNode)
        {
            ReferencedFileSave rfs = null;
            if(selectedTreeNode != null)
            {
                rfs = selectedTreeNode.Tag as ReferencedFileSave;
            }

            if(rfs != null)
            {
                if(control == null)
                {
                    viewModel = new FileReferenceViewModel();
                    control = new FileReferenceControl();

                    control.DataContext = viewModel;

                    AddToTab(PluginManager.CenterTab, control, "Referenced Files");
                }
                else
                {
                    AddTab();
                }

            }
            else
            {
                base.RemoveTab();
            }

            if(viewModel != null)
            {
                viewModel.ReferencedFileSave = rfs;
            }

        }

    }
}
