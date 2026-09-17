namespace FlatRedBall.Glue.Controls
{
    /// <summary>
    /// Seam over Glue.csproj's <c>DialogService</c>, covering only the members the GlueCommon
    /// extractions in #2276 need (<c>ShowMessage</c>, <c>ShowConfirm</c>). GlueCommon can't reference
    /// <c>DialogService</c> directly (wrong direction - Glue.csproj references GlueCommon, not the
    /// reverse), so the real <c>DialogService</c> implements this interface and wires itself into
    /// <see cref="ErrorReportingCore.Self"/> from its own static constructor. Same pattern as
    /// <c>IObjectFinderCore</c>/<c>ObjectFinderCore</c> - see that interface's doc comment and #2276's
    /// third comment.
    /// </summary>
    public interface IErrorReportingCore
    {
        void ShowMessage(string text);

        /// <summary>
        /// Mirrors <c>DialogService.ShowConfirm</c>: which button was clicked, or null if the dialog
        /// was closed without clicking one (also what headless Glue answers).
        /// </summary>
        DialogButton? ShowConfirm(string text, DialogButton primary = DialogButton.Yes, DialogButton secondary = DialogButton.No);
    }

    public static class ErrorReportingCore
    {
        public static IErrorReportingCore Self { get; set; }
    }
}
