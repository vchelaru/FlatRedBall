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
    /// Rules are a list rather than a single check so more ways of referencing FRB2 can be added
    /// without restructuring. Today the only shipping FRB2 games reference the engine by source; a
    /// PackageReference rule gets added once FRB2 publishes a nuget package.
    /// </remarks>
    public static class Frb2ProjectDetector
    {
        const string Frb2SourceProjectFileName = "FlatRedBall2.csproj";

        static readonly Func<ProjectItemInfo, bool>[] Rules = new Func<ProjectItemInfo, bool>[]
        {
            item => item.ItemType == "ProjectReference" &&
                string.Equals(GetFileName(item.Include), Frb2SourceProjectFileName, StringComparison.OrdinalIgnoreCase),
        };

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
