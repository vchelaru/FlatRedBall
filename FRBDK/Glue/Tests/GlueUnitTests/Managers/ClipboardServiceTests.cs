using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FlatRedBall.Glue.Managers;
using Xunit;

namespace GlueUnitTests.Managers;

/// <summary>
/// Copying to the clipboard when another process is holding it. Any process can lock the clipboard,
/// and while one does, Clipboard.SetText throws COMException 0x800401D0 (CLIPBRD_E_CANT_OPEN) - which
/// took the error list's "Copy All" button down with a stack trace.
/// </summary>
public class ClipboardServiceTests : IDisposable
{
    readonly Action<string> _originalSetText = ClipboardService.SetTextImpl;
    readonly Action<int> _originalSleep = ClipboardService.SleepImpl;
    readonly List<int> _delaysSlept = new();

    public ClipboardServiceTests()
    {
        // Real sleeping would make the retry tests take as long as the real backoff.
        ClipboardService.SleepImpl = ms => _delaysSlept.Add(ms);
    }

    public void Dispose()
    {
        // Process-wide, so it has to go back even when the test fails.
        ClipboardService.SetTextImpl = _originalSetText;
        ClipboardService.SleepImpl = _originalSleep;
    }

    static COMException ClipboardBusy() => new("OpenClipboard Failed", unchecked((int)0x800401D0));

    [Fact]
    public void SetText_WhenTheClipboardIsFree_WritesOnceAndSucceeds()
    {
        var written = new List<string>();
        ClipboardService.SetTextImpl = text => written.Add(text);

        var succeeded = ClipboardService.SetText("hello");

        Assert.True(succeeded);
        Assert.Equal(new[] { "hello" }, written);
    }

    [Fact]
    public void SetText_WhenTheClipboardIsBrieflyHeld_RetriesUntilItSucceeds()
    {
        // The real case: a clipboard manager or remote desktop client holds it for a few ms.
        var attempts = 0;
        ClipboardService.SetTextImpl = _ =>
        {
            attempts++;
            if (attempts < 3)
            {
                throw ClipboardBusy();
            }
        };

        var succeeded = ClipboardService.SetText("hello");

        Assert.True(succeeded);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public void SetText_WhenTheClipboardIsNeverFree_GivesUpWithoutThrowing()
    {
        // A failed copy is worth reporting, not worth taking the click down with it.
        var attempts = 0;
        ClipboardService.SetTextImpl = _ =>
        {
            attempts++;
            throw ClipboardBusy();
        };

        var succeeded = ClipboardService.SetText("hello");

        Assert.False(succeeded);
        Assert.Equal(ClipboardService.AttemptCount, attempts);
    }

    [Fact]
    public void SetText_DoesNotSwallowAnUnrelatedException()
    {
        // Only clipboard contention is worth retrying past. Anything else is a real bug and must not
        // be turned into a silent "false".
        ClipboardService.SetTextImpl = _ => throw new InvalidOperationException("something else");

        Assert.Throws<InvalidOperationException>(() => ClipboardService.SetText("hello"));
    }
}
