using FlatRedBall.Glue.Plugins.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Plugins
{
    public enum LoadedState
    {
        NotLoaded,
        LoadedNextTime
    }

    public class NotLoadedPlugin : IPlugin
    {
        public LoadedState LoadedState { get; set; }

        public string FriendlyName
        {
            get;
            set;
        }

        public Version Version
        {
            get { return new Version(); }
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
