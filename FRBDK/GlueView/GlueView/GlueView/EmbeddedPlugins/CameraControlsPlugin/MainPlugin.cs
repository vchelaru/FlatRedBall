using GlueView.Plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;
using GlueView.Facades;
using GlueView.Forms;

namespace GlueView.EmbeddedPlugins.CameraControlsPlugin
{
    [Export(typeof(GlueViewPlugin))]
    public class MainPlugin : GlueViewPlugin
    {
        BoundsLogic boundsLogic;

        public override string FriendlyName
        {
            get
            {
                return "Camera Plugin";
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
            boundsLogic = new CameraControlsPlugin.BoundsLogic();


            GlueViewCommands.Self.CollapsibleFormCommands.AddCollapsableForm(
                "Camera", -1, new CameraControl(), this);

            this.ElementLoaded += HandleElementLoaded;
        }

        private void HandleElementLoaded(object sender, EventArgs e)
        {
            boundsLogic.HandleElementLoaded();
        }
    }
}
