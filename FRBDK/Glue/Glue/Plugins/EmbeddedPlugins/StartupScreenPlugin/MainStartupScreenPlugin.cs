using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GlueFormsCore.Plugins.EmbeddedPlugins.StartupScreenPlugin.Errors;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.StartupScreenPlugin;

[Export(typeof(PluginBase))]
class MainStartupScreenPlugin : EmbeddedPlugin
{
    ErrorReporter _errorReporter;

    public override void StartUp()
    {
        _errorReporter = new ErrorReporter();
        this.AddErrorReporter(_errorReporter);

        AssignEvents();
    }

    private void AssignEvents()
    {
        this.ReactToChangedStartupScreen += HandleStartupScreenChanged;
    }

    private void HandleStartupScreenChanged()
    {
        GlueCommands.Self.RefreshCommands.RefreshErrorsFor(_errorReporter);
    }
}
