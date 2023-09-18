using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Managers;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.TaskDisplayer
{
    [Export(typeof(PluginBase))]
    class TaskDisplayerPlugin : EmbeddedPlugin
    {
        PluginTab tab;
        TaskDisplayerViewModel vm;

        public override void StartUp()
        {
            HandleInitializeBottomTab();
        }

        private void HandleInitializeBottomTab()
        {
            TaskDisplayerControl control = new TaskDisplayerControl();

            this.vm = new TaskDisplayerViewModel();
            control.DataContext = this.vm;
            this.vm.PropertyChanged += HandlePropertyChanged;
            this.tab = CreateAndAddTab(control, "Tasks", TabLocation.Bottom);
        }

        private void HandlePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(!ProjectManager.WantsToCloseProject)
            {
                string desiredText = " " + TaskDisplayerViewModel.StatusText;
                TaskManager.Self.BeginOnUiThread(() =>
                    {
                        if (tab.Title != desiredText)
                        {
                            tab.Title = desiredText;
                        }
                    }
                );
            }
        }
    }
}
