using System;
using System.Globalization;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.AboutPlugin;

internal static class DailyBuildVersionLogic
{
    internal static Version GetLatestVersion(DateTimeOffset lastModified)
    {
        return Version.Parse(lastModified.UtcDateTime.ToString("yyyy.M.d", CultureInfo.InvariantCulture));
    }
}
