using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.ProjectExclusionPlugin
{
    [Export(typeof (PluginBase))]
    public class MainPlugin : EmbeddedPlugin
    {
        ExclusionControl control;
        PluginTab pluginTab;

        public override void StartUp()
        {
            this.ReactToItemSelectHandler += HandleItemSelected;
        }

        private void HandleItemSelected(System.Windows.Forms.TreeNode selectedTreeNode)
        {
            var file = GlueState.Self.CurrentReferencedFileSave;

            if (file == null)
            {
                base.RemoveTab();
            }
            else if(GlueState.Self.SyncedProjects.Count() != 0)
            {
                if (control == null)
                {
                    control = new ExclusionControl();
                    pluginTab = base.AddToTab(PluginManager.LeftTab, control, "Platform Inclusions");
                }
                else
                {
                    base.AddTab();
                }

                FileExclusionViewModel viewModel = new FileExclusionViewModel();
                viewModel.PropertyChanged += HandlePropertyChanged;
                viewModel.SetFrom(file);

                UpdateTabTitle(file);

                control.DataContext = viewModel;

            }
        }

        private void HandlePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var file = GlueState.Self.CurrentReferencedFileSave;
            if (file != null)
            {
                UpdateTabTitle(file);
            }
        }

        private void UpdateTabTitle(SaveClasses.ReferencedFileSave file)
        {
            int count = file.ProjectsToExcludeFrom.Count;

            if (count == 0)
            {
                pluginTab.Text = "Platform Inclusions";
            }
            else
            {
                pluginTab.Text = "Excluded from " + count + " platforms";
            }
        }
    }
}
