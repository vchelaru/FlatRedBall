using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins;


namespace GluePluginTemplate
{
    [Export(typeof(PluginBase))]
    public class MyPlugin : PluginBase
    {
        [Import("GlueCommands")]
        public IGlueCommands GlueCommands
        {
            get;
            set;
        }
		
		[Import("GlueState")]
		public IGlueState GlueState
		{
		    get;
		    set;
        }


        public override Version Version
        {
            get { return new Version(1, 0); }
        }

        public override string FriendlyName
        {
            get { return "My plugin"; }
        }

        public override bool ShutDown(PluginShutDownReason reason)
        {
            // Do anything your plugin needs to do to shut down
            // or don't shut down and return false

            return true;
        }

        public override void StartUp()
        {
            // Do anything your plugin needs to do when it first starts up

        }

    }
}
