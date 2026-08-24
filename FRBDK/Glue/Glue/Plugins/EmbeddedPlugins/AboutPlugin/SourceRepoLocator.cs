using FlatRedBall.Glue.VSHelpers.Projects;
using System;
using System.IO;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.AboutPlugin
{
    /// <summary>
    /// Resolves the absolute folder of the FRB/Gum source repos a loaded game project is linked
    /// against, by finding the project's evaluated ProjectReference to a known engine csproj and
    /// stripping off the known relative suffix.
    /// </summary>
    /// <remarks>
    /// This mirrors <c>OfficialPlugins\FrbSourcePlugin\Managers\AddSourceManager</c>'s and
    /// <c>Frb2AddSourceManager</c>'s relative-path lists rather than reusing them: Glue.csproj can't
    /// reference OfficialPlugins.csproj (the dependency runs the other way, through PluginLibraries).
    /// </remarks>
    internal static class SourceRepoLocator
    {
        static readonly (string FrbRelative, string GumRelative)[] Frb1EngineRelativePaths = new[]
        {
            (@"Engines\FlatRedBallXNA\FlatRedBallDesktopGLNet6\FlatRedBallDesktopGLNet6.csproj", @"GumCore\GumCoreXnaPc\GumCore.DesktopGlNet6\GumCore.DesktopGlNet6.csproj"),
            (@"Engines\FlatRedBallXNA\FlatRedBall.FNA\FlatRedBall.FNA.csproj", @"GumCore\GumCoreXnaPc\GumCore.FNA\GumCore.FNA.csproj"),
            (@"Engines\FlatRedBallXNA\KniWeb\FlatRedBallKniWeb.csproj", @"GumCore\GumCoreXnaPc\GumCore.Kni.Web\GumCore.Kni.Web.csproj"),
            (@"Engines\FlatRedBallXNA\FlatRedBallAndroid\FlatRedBallAndroid.csproj", @"GumCore\GumCoreXnaPc\GumCoreAndroid\GumCoreAndroid.csproj"),
            (@"Engines\FlatRedBallXNA\FlatRedBalliOS\FlatRedBalliOS.csproj", @"GumCore\GumCoreXnaPc\GumCoreiOS\GumCoreiOS.csproj"),
        };

        const string Frb2EngineRelativePath = @"src\FlatRedBall2.csproj";

        public static bool TryGetFrb1SourceRoots(VisualStudioProject project, out string frbRoot, out string gumRoot)
        {
            frbRoot = null;
            gumRoot = null;

            var projectDirectory = project.FullFileName.GetDirectoryContainingThis().FullPath;

            foreach (var item in project.EvaluatedItems)
            {
                if (item.ItemType != "ProjectReference")
                {
                    continue;
                }

                var fullPath = ResolveFullPath(projectDirectory, item.EvaluatedInclude);

                foreach (var (frbRelative, gumRelative) in Frb1EngineRelativePaths)
                {
                    if (frbRoot == null && EndsWithPath(fullPath, frbRelative))
                    {
                        frbRoot = fullPath.Substring(0, fullPath.Length - frbRelative.Length).TrimEnd('\\', '/');
                    }
                    if (gumRoot == null && EndsWithPath(fullPath, gumRelative))
                    {
                        gumRoot = fullPath.Substring(0, fullPath.Length - gumRelative.Length).TrimEnd('\\', '/');
                    }
                }
            }

            return frbRoot != null;
        }

        public static bool TryGetFrb2SourceRoot(VisualStudioProject project, out string frbRoot)
        {
            frbRoot = null;

            var projectDirectory = project.FullFileName.GetDirectoryContainingThis().FullPath;

            foreach (var item in project.EvaluatedItems)
            {
                if (item.ItemType != "ProjectReference")
                {
                    continue;
                }

                var fullPath = ResolveFullPath(projectDirectory, item.EvaluatedInclude);

                if (EndsWithPath(fullPath, Frb2EngineRelativePath))
                {
                    frbRoot = fullPath.Substring(0, fullPath.Length - Frb2EngineRelativePath.Length).TrimEnd('\\', '/');
                    return true;
                }
            }

            return false;
        }

        static string ResolveFullPath(string projectDirectory, string evaluatedInclude)
        {
            var combined = Path.IsPathRooted(evaluatedInclude)
                ? evaluatedInclude
                : Path.Combine(projectDirectory, evaluatedInclude);
            return Path.GetFullPath(combined);
        }

        static bool EndsWithPath(string fullPath, string relativeSuffix) =>
            fullPath.EndsWith(relativeSuffix, StringComparison.OrdinalIgnoreCase);
    }
}
