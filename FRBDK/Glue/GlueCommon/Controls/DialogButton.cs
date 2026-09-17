namespace FlatRedBall.Glue.Controls
{
    /// <summary>
    /// The result of a simple confirm/choice dialog. Used both as the return type of
    /// <c>DialogService.ShowConfirm</c> / <see cref="IErrorReportingCore.ShowConfirm"/> and as the
    /// button-identity vocabulary for the default WPF implementations in Glue.csproj. Lives here
    /// (net8.0) rather than next to <c>DialogService</c> so GlueCommon code can read a confirm result
    /// through the <see cref="IErrorReportingCore"/> seam. Same namespace as before, so no call site
    /// changed when it moved (#2276).
    /// </summary>
    public enum DialogButton
    {
        Ok,
        Yes,
        No,
        Cancel,
        Retry
    }
}
