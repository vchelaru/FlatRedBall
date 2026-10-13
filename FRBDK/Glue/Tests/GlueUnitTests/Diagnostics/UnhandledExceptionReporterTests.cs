using System;
using System.IO;
using FlatRedBall.Glue.Diagnostics;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Diagnostics;

// Issue #2357: Glue died from an exception in a dialog's Loaded handler and the reporter had no log file,
// no popup and no idea where to look. Non-plugin errors only went to the Output tab.
public class UnhandledExceptionReporterTests : IDisposable
{
    private readonly string _originalDirectory = CrashLog.Directory;
    private readonly TempDir _tempDir = new();

    private string _printed;
    private string _popup;
    private int _popupCount;

    public UnhandledExceptionReporterTests()
    {
        CrashLog.Directory = Path.Combine(_tempDir.Root, "Diagnostics");
    }

    public void Dispose()
    {
        CrashLog.Directory = _originalDirectory;
        _tempDir.Dispose();
    }

    private UnhandledExceptionReporter Create(
        Func<Exception, string, bool> tryHandleAsPluginError = null,
        Action<string> showPopup = null) =>
        new UnhandledExceptionReporter(
            tryHandleAsPluginError ?? ((_, _) => false),
            text => _printed = text,
            showPopup ?? (text => { _popup = text; _popupCount++; }));

    [Fact]
    public void Report_ShouldWriteCrashLog_PrintAndShowPopupWithLogPath_ForNonPluginError()
    {
        Create().Report(new ArgumentNullException("v"));

        var logFile = Directory.GetFiles(CrashLog.Directory).ShouldHaveSingleItem();
        _printed.ShouldContain(logFile);
        _popup.ShouldContain("ArgumentNullException");
        _popup.ShouldContain(logFile);
    }

    [Fact]
    public void Report_ShouldNotShowPopup_WhenAPluginTookTheBlame()
    {
        Create(tryHandleAsPluginError: (_, _) => true).Report(new Exception("plugin broke"));

        Directory.GetFiles(CrashLog.Directory).ShouldHaveSingleItem();
        _popupCount.ShouldBe(0);
    }

    [Fact]
    public void Report_ShouldLogAndPrintButNotShowPopup_WhenPopupIsSuppressed()
    {
        Create().Report(new Exception("background task failed"), showPopup: false);

        Directory.GetFiles(CrashLog.Directory).ShouldHaveSingleItem();
        _printed.ShouldContain("background task failed");
        _popupCount.ShouldBe(0);
    }

    [Fact]
    public void Report_ShouldNotRecurse_WhenShowingThePopupRaisesAnotherUnhandledException()
    {
        UnhandledExceptionReporter reporter = null;
        reporter = Create(showPopup: _ =>
        {
            _popupCount++;
            // What the real hooks do when the popup window itself throws.
            reporter.Report(new InvalidOperationException("popup failed"));
        });

        Should.NotThrow(() => reporter.Report(new Exception("first")));

        _popupCount.ShouldBe(1);
        // Both exceptions still reach disk.
        Directory.GetFiles(CrashLog.Directory).Length.ShouldBe(2);
    }

    [Fact]
    public void Report_ShouldNotThrow_WhenThePopupThrows()
    {
        var reporter = Create(showPopup: _ => throw new InvalidOperationException("no UI"));

        Should.NotThrow(() => reporter.Report(new Exception("x")));
    }
}
