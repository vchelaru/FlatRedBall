using GlueView.Plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;

namespace GlueView.EmbeddedPlugins.EditingPlugin
{
    [Export(typeof(GlueViewPlugin))]
    public class MainPlugin : GlueViewPlugin
    {
        public override string FriendlyName
        {
            get
            {
                return "Editing Plugin";
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
            this.Push += HandlePush;
            this.Click += HandleClick;
            this.MouseMove += HandleMouseMove;
        }

        private void HandleMouseMove(object sender, EventArgs e)
        {
            EditingLogic.HandleMouseMove();
        }

        private void HandleClick(object sender, EventArgs e)
        {
            EditingLogic.HandleClick();
        }

        private void HandlePush(object sender, EventArgs e)
        {
            EditingLogic.HandlePush();
        }
    }
}
