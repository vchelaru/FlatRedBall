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
        PluginTab tab;

        public override void StartUp()
        {
            this.AddMenuItemTo("Copy project file links", HandleCopyProjectLinks, "Project");
        }

        private void HandleCopyProjectLinks(object sender, EventArgs e)
        {
            if (control == null)
            {
                control = new ReferenceCopierControl();
                tab = CreateAndAddTab(control, "Reference Sharing", TabLocation.Left);

                var viewModel = new ReferenceCopierViewModel();
                control.DataContext = viewModel;
            }
            tab.Show();
        }   
    }
}
