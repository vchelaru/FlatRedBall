using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OfficialPlugins.GlueView;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    public class GlueViewCommands
    {
        public void SendScript(string script)
        {
            GlueViewPlugin.Self.SendScript(script);
        }
    }
}
