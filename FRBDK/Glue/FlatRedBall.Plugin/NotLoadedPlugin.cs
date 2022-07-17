using FlatRedBall.Glue.Plugins.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public string GithubRepoOwner => null;
        public string GithubRepoName => null;
        public bool CheckGithubForNewRelease => false;

        public event Action<IPlugin, string, string> ReactToPluginEventAction;
        public event Action<IPlugin, string, string> ReactToPluginEventWithReturnAction;

        public void StartUp()
        {

        }

        public bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public void HandleEvent(string eventName, string payload)
        {
        }

        public Task<string> HandleEventWithReturn(string eventName, string payload)
        {
            return Task.FromResult((string)null);
        }

        public void HandleEventResponseWithReturn(string payload)
        {
        }
    }
}
