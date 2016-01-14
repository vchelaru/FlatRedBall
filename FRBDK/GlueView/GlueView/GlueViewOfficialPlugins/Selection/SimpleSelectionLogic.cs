using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall;
using FlatRedBall.Glue;
using GlueView.Plugin;

namespace GlueViewOfficialPlugins.Selection
{
    public class SimpleSelectionLogic : GlueViewPlugin
    {
        #region Fields

        protected ElementRuntime mHighlightedElementRuntime;
        protected ElementRuntimeHighlight mHighlight;
        protected ContextMenuStrip mContextMenuStrip;

        protected bool mIgnoreNextClick = false;
        #endregion

        public SimpleSelectionLogic()
        {
            mHighlight = new ElementRuntimeHighlight();
            mHighlight.FadeInAndOut = true;

            var control = Control.FromHandle(FlatRedBallServices.WindowHandle);

            mContextMenuStrip = new ContextMenuStrip();
            mContextMenuStrip.Items.Add("-");

            control.ContextMenuStrip = mContextMenuStrip;
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
