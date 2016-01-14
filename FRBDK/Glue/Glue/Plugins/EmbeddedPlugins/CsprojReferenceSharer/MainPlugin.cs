using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CsprojReferenceSharer
{
    [Export(typeof(Plugins.PluginBase))]
    public class MainPlugin : EmbeddedPlugins.EmbeddedPlugin
    {
        ReferenceCopierControl control;

        public override void StartUp()
        {
            this.AddMenuItemTo("Copy project file links", HandleCopyProjectLinks, "Project");
        }

        private void HandleCopyProjectLinks(object sender, EventArgs e)
        {
            if (control == null)
            {
                control = new ReferenceCopierControl();
                this.AddToTab(PluginManager.LeftTab, control, "Reference Sharing");

                var viewModel = new ReferenceCopierViewModel();
                control.DataContext = viewModel;
            }
            else
            {
                AddTab();
            }

            
        }   
    }
}
