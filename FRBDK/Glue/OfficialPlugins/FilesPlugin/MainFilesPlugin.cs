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
using FlatRedBall.Glue.Controls;
using OfficialPlugins.FilesPlugin.Managers;
using FlatRedBall.Glue.FormHelpers;

namespace OfficialPlugins.FilesPlugin
{
    [Export(typeof(PluginBase))]
    public class MainFilesPlugin : PluginBase
    {
        #region Fields/Properties

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


        PluginTab referencedFileTab;
        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            this.ReactToItemSelectHandler += HandleItemSelected;
            this.ReactToTreeViewRightClickHandler +=
                RightClickManager.HandleRightClick;
        }

        private void HandleItemSelected(ITreeNode selectedTreeNode)
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

                    referencedFileTab = CreateTab(control, "Referenced Files");

                }
                referencedFileTab.Show();
            }
            else
            {
                referencedFileTab?.Hide();
            }

            if(viewModel != null)
            {
                viewModel.ReferencedFileSave = rfs;
            }

        }

    }
}
