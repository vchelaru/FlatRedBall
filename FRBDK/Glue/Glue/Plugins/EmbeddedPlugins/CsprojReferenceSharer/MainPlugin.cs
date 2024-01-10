using System;
using System.ComponentModel.Composition;
using L = Localization;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CsprojReferenceSharer
{
    [Export(typeof(Plugins.PluginBase))]
    public class MainPlugin : EmbeddedPlugins.EmbeddedPlugin
    {
        ReferenceCopierControl control;
        PluginTab tab;

        public override void StartUp()
        {
            this.AddMenuItemTo(L.Texts.ProjectCopyFileLinks, L.MenuIds.ProjectCopyFileLinksId, HandleCopyProjectLinks, L.MenuIds.ProjectId);
        }

        private void HandleCopyProjectLinks(object sender, EventArgs e)
        {
            if (control == null)
            {
                control = new ReferenceCopierControl();
                tab = CreateAndAddTab(control, L.Texts.ReferenceSharing, TabLocation.Left);

                var viewModel = new ReferenceCopierViewModel();
                control.DataContext = viewModel;
            }
            tab.Show();
        }   
    }
}
