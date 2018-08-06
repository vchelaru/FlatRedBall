using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.RuntimeObjects;
using GlueView.Plugin;
using GumPlugin.RuntimeObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumPlugin
{
    [Export(typeof(GlueViewPlugin))]
    class MainGumPlugin : GlueViewPlugin
    {
        GumRuntimeFileManager gumRuntimeFileManager;

        public override string FriendlyName
        {
            get { return "Gum GlueView Plugin"; }
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
            // remove it
            //ReferencedFileRuntimeList.FileManagers.Add(new object);
            return true;
        }

        public override void StartUp()
        {
            gumRuntimeFileManager = new GumRuntimeFileManager();
            gumRuntimeFileManager.SubscribeToFrbWindowResize();
            ReferencedFileRuntimeList.FileManagers.Add(gumRuntimeFileManager);

        }
    }
}
