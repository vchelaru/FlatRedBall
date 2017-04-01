using GlueView.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.ComponentModel.Composition;
using GlueView.Facades;

namespace GlueView.EmbeddedPlugins.CursorDisplayPlugin
{
    [Export(typeof(GlueViewPlugin))]
    class MainPlugin : GlueViewPlugin
    {
        CursorDisplayViewModel viewModel;
        public override string FriendlyName
        {
            get
            {
                return "Cursor Display Plugin";
            }
        }

        public override Version Version
        {
            get
            {
                return new Version(1, 0);
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            viewModel = new CursorDisplayPlugin.CursorDisplayViewModel();
            viewModel.ZValue = 0;
            viewModel.CursorText = "Getting values...";

            var control = new CursorDisplayControl();
            control.DataContext = viewModel;



            GlueViewCommands.Self.CollapsibleFormCommands.AddCollapsableForm(
                "Cursor Values", 80, control, this);

            this.Update += HandleUpdate;
        }

        private void HandleUpdate(object sender, EventArgs e)
        {
            var cursor = FlatRedBall.Gui.GuiManager.Cursor;

            var worldXAt = cursor.WorldXAt(viewModel.ZValue);
            var worldYAt = cursor.WorldYAt(viewModel.ZValue);

            viewModel.CursorText = ($"({worldXAt.ToString("n3")}, {worldYAt.ToString("n3")})");
        }
    }
}
