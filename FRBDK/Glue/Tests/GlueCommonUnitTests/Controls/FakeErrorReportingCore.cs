using FlatRedBall.Glue.Controls;

namespace GlueCommonUnitTests.Controls;

/// <summary>
/// Hand-rolled test double for <see cref="IErrorReportingCore"/> - the real implementation
/// (<c>DialogService</c>) lives in Glue.csproj and isn't reachable from this net8.0 test project.
/// </summary>
public class FakeErrorReportingCore : IErrorReportingCore
{
    public List<string> Messages { get; } = new();

    public List<string> Confirms { get; } = new();

    /// <summary>
    /// What <see cref="ShowConfirm"/> answers. Null is what the real dialog returns when closed
    /// without clicking a button (and what headless Glue always returns).
    /// </summary>
    public DialogButton? ConfirmResult { get; set; }

    public void ShowMessage(string text) => Messages.Add(text);

    public DialogButton? ShowConfirm(string text, DialogButton primary = DialogButton.Yes, DialogButton secondary = DialogButton.No)
    {
        Confirms.Add(text);
        return ConfirmResult;
    }
}
