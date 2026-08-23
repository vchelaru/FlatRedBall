using System;
using System.Collections.Generic;
using System.Linq;

namespace FlatRedBall.Glue.VSHelpers
{
    /// <summary>
    /// A csproj &lt;Compile Include&gt; entry to consider for orphan removal, described with just the data
    /// needed to decide (not a live MSBuild <c>ProjectItem</c>), so the decision logic below is testable
    /// without a loaded project or any Glue bootstrap.
    /// </summary>
    public struct CandidateCompileItem
    {
        public string UnevaluatedInclude;
        public bool IsWildcard;
        public bool IsConditional;
    }

    /// <summary>
    /// Decides which &lt;Compile Include&gt; entries for Glue-generated files are safe to remove because
    /// their backing file no longer exists and no current element owns that path anymore - see GitHub issue
    /// #2103. Deliberately narrow: only files matching Glue's own generated-file naming (".Generated.cs",
    /// ".Generated.Event.cs") are candidates. Hand-authored files such as "X.cs" or "X.Event.cs" are never
    /// touched here, even when orphaned - that's what the explicit delete flow
    /// (<see cref="FlatRedBall.Glue.Elements.DeletionPlanner"/>) is for, since it asks the user first.
    /// </summary>
    public static class OrphanedGeneratedCompileItemFinder
    {
        /// <summary>
        /// Top-level folders a Screen/Entity's or a Gum element's owned generated files can ever live under -
        /// see <see cref="FlatRedBall.Glue.Elements.DeletionPlanner.GetOwnedGeneratedRelativePaths"/> (element
        /// names are always "Screens\..."/"Entities\...", factories always live under "Factories\", and Gum
        /// Forms code always lives under "Forms\" - see GumPlugin's
        /// CodeGeneratorManager.GetFormsGeneratedRelativePaths, GitHub issue #2185). Reconciliation is scoped
        /// to only these folders - see GitHub issue #2170 - because the
        /// ownership model doesn't understand every generator's output: a ".Generated.cs" written by some
        /// other generator into any other folder (e.g. Performance\PoolList.Generated.cs, written once by
        /// FactoryElementCodeGenerator.AddGeneratedPerformanceTypes) will never appear "owned" and would
        /// otherwise look orphaned - and get removed - on any load where its backing file happens to be
        /// missing when this check runs.
        /// </summary>
        static readonly HashSet<string> ElementOwnedGeneratedFolders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Screens",
            "Entities",
            "Factories",
            "Forms",
        };

        public static bool IsGlueGeneratedFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            return fileName.EndsWith(".Generated.cs", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".Generated.Event.cs", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns the <see cref="CandidateCompileItem.UnevaluatedInclude"/> of every entry that should be
        /// removed: a Glue-generated file name, not a wildcard/conditional entry, whose file is missing on
        /// disk and whose relative path isn't in <paramref name="ownedRelativePaths"/>.
        /// </summary>
        public static List<string> FindOrphanedIncludes(
            IEnumerable<CandidateCompileItem> compileItems,
            Func<string, bool> fileExists,
            HashSet<string> ownedRelativePaths)
        {
            var toReturn = new List<string>();

            foreach (var item in compileItems)
            {
                if (item.IsWildcard || item.IsConditional)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(item.UnevaluatedInclude))
                {
                    continue;
                }

                var segments = item.UnevaluatedInclude.Replace('\\', '/').Split('/');
                var fileName = segments.Last();

                if (!IsGlueGeneratedFileName(fileName))
                {
                    continue;
                }

                var topFolder = segments.Length > 1 ? segments[0] : string.Empty;
                if (!ElementOwnedGeneratedFolders.Contains(topFolder))
                {
                    continue;
                }

                if (ownedRelativePaths.Contains(item.UnevaluatedInclude))
                {
                    continue;
                }

                if (fileExists(item.UnevaluatedInclude))
                {
                    continue;
                }

                toReturn.Add(item.UnevaluatedInclude);
            }

            return toReturn;
        }
    }
}
