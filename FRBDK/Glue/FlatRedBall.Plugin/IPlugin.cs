using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    }
}
