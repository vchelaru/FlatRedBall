using System;

namespace FlatRedBall.Glue.Diagnostics;

/// <summary>
/// Single funnel for every exception Glue's last-chance handlers see (WinForms thread, WPF dispatcher,
/// unobserved tasks, AppDomain). Writes the crash log first, then lets a misbehaving plugin take the blame,
/// otherwise prints to the output tab and shows a popup so the error is not only discoverable by someone
/// who happens to have the Output tab open.
/// </summary>
public class UnhandledExceptionReporter
{
    private readonly Func<Exception, string, bool> _tryHandleAsPluginError;
    private readonly Action<string> _printError;
    private readonly Action<string> _showPopup;

    // The popup is itself a WPF window; if showing it throws, the resulting exception comes back through
    // the same hooks. Without this the user gets an endless chain of failed popups.
    [ThreadStatic]
    private static bool _isReporting;

    public UnhandledExceptionReporter(
        Func<Exception, string, bool> tryHandleAsPluginError,
        Action<string> printError,
        Action<string> showPopup)
    {
        _tryHandleAsPluginError = tryHandleAsPluginError;
        _printError = printError;
        _showPopup = showPopup;
    }

    /// <param name="showPopup">False for failures the user can't act on, such as an unobserved background task:
    /// those still go to the log and the output tab, without interrupting the user.</param>
    public void Report(object exceptionObject, bool showPopup = true)
    {
        // An exception escaping an async void handler ends the process no matter what runs here, so the
        // file is the only record that survives. Write it first.
        var crashLogPath = CrashLog.Write(exceptionObject);

        if (_isReporting)
        {
            return;
        }

        _isReporting = true;
        try
        {
            if (_tryHandleAsPluginError(exceptionObject as Exception, crashLogPath))
            {
                return;
            }

            var message = exceptionObject?.ToString() +
                (crashLogPath != null ? $"{Environment.NewLine}Written to {crashLogPath}" : "");

            _printError(message);
            if (showPopup)
            {
                _showPopup("Glue hit an unexpected error:" + Environment.NewLine + Environment.NewLine + message);
            }
        }
        catch
        {
            // Reporting must never throw back into the handler that called it. The crash log is already on disk.
        }
        finally
        {
            _isReporting = false;
        }
    }
}
