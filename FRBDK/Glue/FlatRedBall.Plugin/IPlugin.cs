using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.Interfaces
{
    public enum PluginShutDownReason
    {
        UserDisabled,
        PluginException,
        PluginInitiated,
        GluxUnload,
        GlueShutDown
    }

    public interface IPlugin
    {
        string FriendlyName {get;}
        Version Version {get;}
        string GithubRepoOwner { get; }
        string GithubRepoName { get; }
        bool CheckGithubForNewRelease { get; }
        void StartUp();
        bool ShutDown(PluginShutDownReason shutDownReason);
        
        event Action<IPlugin, string, string> ReactToPluginEventAction;
        event Action<IPlugin, string, string> ReactToPluginEventWithReturnAction;
        void HandleEvent(string eventName, string payload);
        Task<string> HandleEventWithReturn(string eventName, string payload);
        void HandleEventResponseWithReturn(string payload);
    }
}
