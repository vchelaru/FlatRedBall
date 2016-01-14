using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlueView.Plugin;
using System.ComponentModel.Composition;

namespace GlueViewOfficialPlugins.Selection
{

    [Export(typeof(GlueViewPlugin))]
    public class SimpleSelectionPlugin : GlueViewPlugin
    {


        public SimpleSelectionPlugin()
        {
            this.MouseMove += new EventHandler(OnMouseMove);
            
        }

        void OnMouseMove(object sender, EventArgs e)
        {
            // do something:
        }

        public override string FriendlyName
        {
            get { return "Selection Plugin"; }
        }

        public override Version Version
        {
            get { return new Version(); }
        }

        public override void StartUp()
        {
            // do nothing;

        }

        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason)
        {
            return true;
        }
    }
}
