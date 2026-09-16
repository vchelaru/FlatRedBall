namespace FlatRedBall.Glue.Controls
{
    /// <summary>
    /// Seam over Glue.csproj's <c>DialogService.ShowMessage</c>, covering only the one member the
    /// GlueCommon extractions in #2276 need. GlueCommon can't reference <c>DialogService</c> directly
    /// (wrong direction - Glue.csproj references GlueCommon, not the reverse), so the real
    /// <c>DialogService</c> implements this interface and wires itself into
    /// <see cref="ErrorReportingCore.Self"/> from its own static constructor. Same pattern as
    /// <c>IObjectFinderCore</c>/<c>ObjectFinderCore</c> - see that interface's doc comment and #2276's
    /// third comment.
    /// </summary>
    public interface IErrorReportingCore
    {
        void ShowMessage(string text);
    }

    public static class ErrorReportingCore
    {
        public static IErrorReportingCore Self { get; set; }
    }
}
