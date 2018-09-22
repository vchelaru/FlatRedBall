using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueView2.Plugin
{

    public enum PluginShutDownReason
    {
        UserDisabled,
        PluginException,
        PluginInitiated,
        ProjectUnload,
        AppShutDown
    }

    public interface IPlugin
    {
        string FriendlyName { get; }
        string UniqueId { get; set; }
        Version Version { get; }
        void StartUp();
        bool ShutDown(PluginShutDownReason shutDownReason);
    }
}
