using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;

namespace GumPlugin.Managers;

/// <summary>The Gum Plugin's <see cref="IGumCommands"/>.</summary>
internal class GumCommands : IGumCommands
{
    public bool HasGum() => AppState.Self.GumProjectSave != null;
}
