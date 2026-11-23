using System;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;

namespace OfficialPlugins.PropertyGrid.Managers
{
    /// <summary>The Main Property Grid Plugin's <see cref="IPropertyGridCommands"/>. The grid is the plugin's view,
    /// so the plugin passes in its refresh.</summary>
    internal class PropertyGridCommands : IPropertyGridCommands
    {
        readonly Action refreshVariables;

        public PropertyGridCommands(Action refreshVariables) => this.refreshVariables = refreshVariables;

        public void RefreshVariables() => refreshVariables();
    }
}
