using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins
{
    /// <summary>
    /// Seam over Glue.csproj's <c>PluginManager</c>, covering only the member the GlueCommon
    /// extractions in #2276 need: letting plugins turn a <see cref="VariableDefinition.PreferredDisplayerName"/>
    /// into a displayer type during <c>CustomVariable.FixAllTypes</c>, which only the plugin system
    /// (Glue.csproj, net8.0-windows) can do since the displayer types belong to plugins. GlueCommon can't
    /// reference <c>PluginManager</c> directly (wrong direction - Glue.csproj references GlueCommon, not
    /// the reverse), so the real <c>PluginManager</c> implements this interface and wires itself into
    /// <see cref="PluginManagerCore.Self"/> from its own static constructor. Same "extract an interface for
    /// just the members a class calls" pattern as <see cref="IObjectFinderCore"/>.
    /// Lives under <c>SaveClasses/</c> next to the other seams rather than <c>Plugins/</c> because
    /// <c>GlueCommon.csproj</c> has <c>&lt;Compile Remove="Plugins\**" /&gt;</c>.
    /// </summary>
    public interface IPluginManagerCore
    {
        void TryAssignPreferredDisplayerFromName(CustomVariable customVariable);
    }

    public static class PluginManagerCore
    {
        public static IPluginManagerCore Self { get; set; }
    }
}
