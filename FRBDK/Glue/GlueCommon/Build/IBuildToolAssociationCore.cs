using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Managers
{
    /// <summary>
    /// Seam over Glue.csproj's <c>BuildToolAssociationManager.Self</c>, covering only the member the
    /// GlueCommon extraction in #2276 needs - given a <see cref="ReferencedFileSave"/>, the processed
    /// path of whatever tool builds it (or null when none is associated). GlueCommon can't reference
    /// <c>BuildToolAssociationManager</c>/<c>BuildToolAssociation</c> directly (wrong direction -
    /// Glue.csproj references GlueCommon, not the reverse; <c>BuildToolAssociation</c> also carries WPF
    /// dialog code GlueCommon can't build against), so the real <c>BuildToolAssociationManager</c>
    /// implements this interface and wires itself into <see cref="BuildToolAssociationCore.Self"/> the
    /// first time its own <c>Self</c> is accessed. Same pattern as <c>IObjectFinderCore</c>/
    /// <c>ObjectFinderCore</c> - see that interface's doc comment and #2276's third comment.
    /// </summary>
    public interface IBuildToolAssociationCore
    {
        string GetBuildToolProcessed(ReferencedFileSave referencedFileSave);
    }

    public static class BuildToolAssociationCore
    {
        public static IBuildToolAssociationCore Self { get; set; }
    }
}
