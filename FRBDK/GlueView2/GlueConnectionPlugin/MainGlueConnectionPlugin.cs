using GlueView2.Plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace GlueConnectionPlugin
{
    [Export(typeof(PluginBase))]
    public class MainGlueConnectionPlugin : PluginBase
    {
        public override string FriendlyName
        {
            get
            {
                return "Glue Connection Plugin";
            }
        }

        public override Version Version
        {
            get
            {
                return new Version(1, 0, 0);
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {

        }
    }
}
