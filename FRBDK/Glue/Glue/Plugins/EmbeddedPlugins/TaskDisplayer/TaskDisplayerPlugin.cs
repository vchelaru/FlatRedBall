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
        PluginTabPage tab;
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
            this.tab = base.AddToTab(PluginManager.BottomTab, control, "Tasks");
        }

        private void HandlePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TaskManager.Self.OnUiThread(() =>
                {
                    string desiredText = " " + vm.StatusText;
                    if (tab.Text != desiredText)
                    {
                        tab.Text = desiredText;
                    }
                }
            );
        }
    }
}
