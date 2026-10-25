using System.Collections.Generic;
using FlatRedBall.Glue.SaveClasses;

namespace OfficialPlugins.VariableDisplay
{
    /// <summary>
    /// Decides which <see cref="ReferencedFileSave"/> members the WPF "Settings (Preview)" grid hides.
    /// Split out of <c>MainPropertyGridPlugin</c> so it can be tested without a UI.
    /// </summary>
    public static class ReferencedFileSaveSettingsLogic
    {
        /// <summary>
        /// The members to hide for this file: everything that doesn't apply to it (the rules shared with the
        /// legacy WinForms grid, see <see cref="ReferencedFileSaveElementExtensions.GetHiddenMemberNames"/>)
        /// plus the members only this grid hides.
        /// </summary>
        public static HashSet<string> GetMembersToIgnore(ReferencedFileSave referencedFileSave)
        {
            var toIgnore = referencedFileSave.GetHiddenMemberNames();

            toIgnore.Add(nameof(ReferencedFileSave.FilePath));
            toIgnore.Add(nameof(ReferencedFileSave.CachedInstanceName));
            toIgnore.Add(nameof(ReferencedFileSave.OpensWith));
            toIgnore.Add(nameof(ReferencedFileSave.IsCreatedByWildcard));

            // todo - removing this until we get the dropdown working....
            toIgnore.Add(nameof(ReferencedFileSave.RuntimeType));

            return toIgnore;
        }

        /// <summary>
        /// Whether changing this member can change which other members apply (see
        /// <see cref="ReferencedFileSaveElementExtensions.GetHiddenMemberNames"/>), so the grid has to
        /// rebuild its ignore list. Without this, ticking Treat As Csv would never reveal Csv Delimiter.
        /// </summary>
        public static bool ChangeAffectsShownMembers(string changedMember) =>
            changedMember is nameof(ReferencedFileSave.Name)
                or nameof(ReferencedFileSave.TreatAsCsv)
                or nameof(ReferencedFileSave.LoadedAtRuntime)
                or nameof(ReferencedFileSave.UseContentPipeline)
                or nameof(ReferencedFileSave.SourceFile)
                or nameof(ReferencedFileSave.RuntimeType);
    }
}
