namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Seam over Glue.csproj's <c>GlueState.Self</c>, covering only the members the GlueCommon
    /// extractions in #2276 need. GlueCommon can't reference <c>GlueState</c> directly (wrong
    /// direction - Glue.csproj references GlueCommon, not the reverse), so the real <c>GlueState</c>
    /// implements this interface and wires itself into <see cref="GlueStateCore.Self"/> the first time
    /// its own <c>Self</c> is accessed. Same pattern as <c>IObjectFinderCore</c>/<c>ObjectFinderCore</c>
    /// - see that interface's doc comment and #2276's third comment.
    /// </summary>
    public interface IGlueStateCore
    {
        string ProjectNamespace { get; }
        string CurrentMainProjectDirectory { get; }
        EntitySave CurrentEntitySave { get; }
        ScreenSave CurrentScreenSave { get; }
    }

    public static class GlueStateCore
    {
        public static IGlueStateCore Self { get; set; }
    }
}
