using FlatRedBall.Glue.Plugins;
using Shouldly;

namespace GlueUnitTests.Plugins;

// The Tiled plugin removes its toolbar on glux unload. With no toolbar tray (a headless host) that threw,
// and PluginManager disabled the plugin for the rest of the process (#2429).
public class PluginBaseToolbarTests
{
    // StaFact: constructs a WPF UserControl.
    [StaFact]
    public void RemoveFromToolbar_WithNoToolbarTray_ReturnsFalseInsteadOfThrowing()
    {
        PluginManager.ToolBarTray.ShouldBeNull();

        new ToolbarPlugin().Remove(new System.Windows.Controls.UserControl()).ShouldBeFalse();
    }

    class ToolbarPlugin : PluginBase
    {
        public override string FriendlyName => "Toolbar test plugin";
        public override System.Version Version => new(1, 0);
        public override void StartUp() { }
        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason) => true;

        public bool Remove(System.Windows.Controls.UserControl control) => RemoveFromToolbar(control, "Tools");
    }
}
