using System;
using System.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;
using Gum.DataTypes;
using GumPlugin.Managers;

namespace GumPlugin.ErrorReporting
{
    /// <summary>
    /// Detection logic backing <see cref="RectangleFillStrokeRuntimeVersionError"/> - split out so
    /// both the error's construction (message) and its GetIfIsFixed() re-check call the exact same
    /// resolution path, and so it's unit-testable without going through ErrorReporter's full scan.
    /// </summary>
    internal static class RectangleFillStrokeRuntimeVersionCheck
    {
        internal const int RequiredRuntimeSyntaxVersion = 4;

        private static readonly string[] DllReferenceNames =
        {
            "GumCore.DesktopGlNet6", "GumCore.FNA", "GumCore.Kni.DesktopGL", "GumCore.Kni.Web",
            "GumCoreAndroid", "GumCoreiOS",
        };

        private static readonly string[] NuGetPackageNames =
        {
            "FlatRedBall.GumCore.DesktopGlNet6", "FlatRedBall.GumCore.FNA",
            "FlatRedBall.GumCore.Kni.DesktopGL", "FlatRedBall.GumCore.Kni.Web",
            "FlatRedBall.GumCoreAndroid", "FlatRedBall.GumCoreiOS",
        };

        /// <summary>
        /// True if a v3 gumx Rectangle Fill/Stroke project's referenced engine build actually supports
        /// it - source-linked projects are trusted (established codebase convention, see
        /// IsFrbSourceLinked() call sites throughout StandardsCodeGenerator), everyone else needs the
        /// referenced GumCore.*.dll to declare GumSyntaxVersion &gt;= 4.
        /// </summary>
        public static bool GetIfCurrentlyFixed()
        {
            var gumProject = AppState.Self.GumProjectSave;
            if (gumProject == null ||
                gumProject.Version < (int)GumProjectSave.GumxVersions.ShapeVariableExpansion)
            {
                return true;
            }

            // GitHub issue #2172: this check only knows how to find an FRB1-shaped engine reference
            // (a committed GumCore.*.dll, or a FlatRedBall.GumCore.* NuGet package) that can drift
            // behind the gumx format it's stamped against. FRB2 has no such reference to go stale -
            // its engine comes from either a Directory.Packages.props-pinned NuGet package or a live
            // source ProjectReference, both always current with the rest of the engine - so there is
            // nothing for this check to catch there, and DetectedRuntimeSyntaxVersion's FRB1-only name
            // lists made it misfire as "not present" on every FRB2 project regardless.
            if (GlueState.Self.CurrentMainProject is Frb2Project)
            {
                return true;
            }

            if (GlueState.Self.CurrentMainProject?.IsFrbSourceLinked() == true)
            {
                return true;
            }

            return DetectedRuntimeSyntaxVersion() is int version && version >= RequiredRuntimeSyntaxVersion;
        }

        /// <summary>
        /// Resolves and reads the referenced GumCore.*.dll's GumSyntaxVersion, or null if no such
        /// reference could be found/read (treated as "too old" by <see cref="GetIfCurrentlyFixed"/>,
        /// per the attribute's own "absent means pre-unification" contract).
        /// </summary>
        internal static int? DetectedRuntimeSyntaxVersion()
        {
            var mainProject = GlueState.Self.CurrentMainProject;
            var csprojPath = mainProject?.FullFileName?.FullPath;
            if (string.IsNullOrEmpty(csprojPath) || !File.Exists(csprojPath))
            {
                return null;
            }

            string csprojContents;
            try
            {
                csprojContents = File.ReadAllText(csprojPath);
            }
            catch
            {
                return null;
            }

            var csprojDirectory = Path.GetDirectoryName(csprojPath);

            foreach (var referenceName in DllReferenceNames)
            {
                var hintPath = GumRuntimeSyntaxVersionReader.TryFindHintPath(csprojContents, csprojDirectory, referenceName);
                if (hintPath != null && File.Exists(hintPath))
                {
                    return GumRuntimeSyntaxVersionReader.ReadVersion(hintPath);
                }
            }

            var nuGetCacheRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

            foreach (var packageName in NuGetPackageNames)
            {
                var dllPath = GumRuntimeSyntaxVersionReader.TryFindPackageReferenceDll(csprojContents, packageName, nuGetCacheRoot);
                if (dllPath != null)
                {
                    return GumRuntimeSyntaxVersionReader.ReadVersion(dllPath);
                }
            }

            // Neither a recognized DLL reference nor NuGet package was found. This is not necessarily
            // an error on its own (could be an unusual project setup) - GetIfCurrentlyFixed already
            // returned true for source-linked projects above, so anything reaching here without a
            // resolvable reference is treated the same as "too old" rather than silently passing.
            return null;
        }
    }
}
