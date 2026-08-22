using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    /// <summary>
    /// Decides whether a .csproj is a FlatRedBall 2 game project. FRB2 games consume Glue's
    /// .gluj/.glsj/.glej JSON directly at runtime, so Glue neither generates code for them nor
    /// maintains their .csproj - see <see cref="Frb2Project"/>.
    /// </summary>
    /// <remarks>
    /// This runs before <see cref="ProjectCreator"/>'s DefineConstants cascade, because an FRB2
    /// .csproj sets none of the preprocessor constants that cascade keys off of.
    ///
    /// Rules are a list rather than a single check because there is more than one way to reference
    /// FRB2: games inside the FRB2 repo reference the engine by source, and games created by
    /// <c>dotnet new frb2-desktop</c> reference the published nuget packages.
    /// </remarks>
    public static class Frb2ProjectDetector
    {
        const string Frb2SourceProjectFileName = "FlatRedBall2.csproj";
        const string Frb2PackagePrefix = "FlatRedBall2";

        static readonly Func<ProjectItemInfo, bool>[] Rules = new Func<ProjectItemInfo, bool>[]
        {
            item => item.ItemType == "ProjectReference" &&
                string.Equals(GetFileName(item.Include), Frb2SourceProjectFileName, StringComparison.OrdinalIgnoreCase),

            item => item.ItemType == "PackageReference" && IsFrb2Package(item.Include),
        };

        /// <summary>
        /// Matches the engine packages a template-created game takes - FlatRedBall2.MonoGame,
        /// FlatRedBall2.Kni, and anything else published under that prefix. Public so the
        /// FRB-source-linking feature can find the same package references to remove when swapping
        /// them for a ProjectReference into a sibling FlatRedBall2 checkout.
        /// </summary>
        /// <remarks>
        /// The trailing dot matters: FRB1's packages are FlatRedBall.*, and treating one of those as
        /// FRB2 would silently stop Glue generating that project's code. "FlatRedBall2" cannot be a
        /// prefix of "FlatRedBall." so the two families cannot be confused.
        /// </remarks>
        public static bool IsFrb2Package(string include) =>
            !string.IsNullOrEmpty(include) &&
            (string.Equals(include, Frb2PackagePrefix, StringComparison.OrdinalIgnoreCase) ||
             include.StartsWith(Frb2PackagePrefix + ".", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// The one thing every rule needs from a project item, so callers can test this without an
        /// MSBuild evaluation and <see cref="ProjectCreator"/> can pass a real one.
        /// </summary>
        public readonly struct ProjectItemInfo
        {
            public ProjectItemInfo(string itemType, string include)
            {
                ItemType = itemType;
                Include = include;
            }

            public string ItemType { get; }
            public string Include { get; }
        }

        public static bool IsFrb2Project(IEnumerable<ProjectItemInfo> projectItems)
        {
            if (projectItems == null)
            {
                return false;
            }

            return projectItems.Any(item => Rules.Any(rule => rule(item)));
        }

        /// <summary>
        /// The FRB2 game project the user means by picking <paramref name="csprojPath"/>: the file
        /// itself when it is one, otherwise a project it directly references that is. Null when neither
        /// applies, which must leave the caller's original choice alone.
        /// </summary>
        /// <remarks>
        /// `dotnet new frb2-desktop` splits a game into MyGame.Common - Game1, content, and the engine
        /// reference - and MyGame.Desktop, a launcher holding only Program.cs and the content pipeline.
        /// Common is what Glue edits, but Desktop is the runnable one and just as likely to be picked.
        ///
        /// The .csproj files are read as XML rather than evaluated by MSBuild: this runs on a file the
        /// user may have picked by mistake, and an SDK or import that fails to resolve should produce
        /// "no redirect", not an exception on the load path.
        ///
        /// One hop only. Following the whole reference graph would eventually reach the engine from
        /// almost anywhere and start redirecting people to projects they never meant to open.
        /// </remarks>
        public static string FindFrb2GameProjectFor(string csprojPath)
        {
            if (string.IsNullOrEmpty(csprojPath) || !File.Exists(csprojPath))
            {
                return null;
            }

            if (IsFrb2Project(ReadProjectItems(csprojPath)))
            {
                return csprojPath;
            }

            var directory = Path.GetDirectoryName(csprojPath);

            foreach (var item in ReadProjectItems(csprojPath))
            {
                if (item.ItemType != "ProjectReference" || string.IsNullOrEmpty(item.Include))
                {
                    continue;
                }

                string referencedPath;
                try
                {
                    referencedPath = Path.GetFullPath(
                        Path.Combine(directory, item.Include.Replace('\\', Path.DirectorySeparatorChar)));
                }
                catch (ArgumentException)
                {
                    continue;
                }

                if (File.Exists(referencedPath) && IsFrb2Project(ReadProjectItems(referencedPath)))
                {
                    return referencedPath;
                }
            }

            return null;
        }

        static IEnumerable<ProjectItemInfo> ReadProjectItems(string csprojPath)
        {
            try
            {
                return System.Xml.Linq.XDocument.Load(csprojPath)
                    .Descendants()
                    .Where(element => element.Attribute("Include") != null)
                    .Select(element => new ProjectItemInfo(
                        element.Name.LocalName, element.Attribute("Include").Value))
                    .ToArray();
            }
            catch (Exception)
            {
                // Unreadable or malformed - no redirect, and let whatever opens it report the problem.
                return Array.Empty<ProjectItemInfo>();
            }
        }

        public static bool IsFrb2Project(Project coreVisualStudioProject) =>
            IsFrb2Project(coreVisualStudioProject?.AllEvaluatedItems
                .Select(item => new ProjectItemInfo(item.ItemType, item.EvaluatedInclude)));

        // Path.GetFileName only splits on the running platform's separators, and a .csproj authored on
        // either platform can use either.
        static string GetFileName(string include) =>
            string.IsNullOrEmpty(include)
                ? include
                : include.Replace('\\', '/').Split('/').Last();
    }
}
