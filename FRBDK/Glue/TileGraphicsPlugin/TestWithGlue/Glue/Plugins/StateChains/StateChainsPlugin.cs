using System;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;

namespace PluginTestbed.StateChains
{
    public partial class StateChainsPlugin
    {
        public const string PropertyName = "StateChainCollection";

        public StateChainsPlugin()
        {
            InitCodeGen();
        }

        [Import("GlueCommands")]
        public IGlueCommands GlueCommands
        {
            get;
            set;
        }

        public string FriendlyName
        {
            get { return "State Chains Plugin"; }
        }

        public Version Version
        {
            get { return new Version(1, 0);}
        }

        public void StartUp()
        {
        }

        public bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }
    }
}
