using System;
using System.Windows.Forms;
using GameCommunicationPlugin.GlueControl.Managers;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GameCommunicationPlugin;

/// <summary>
/// The embedded game runs in its own process, so the mouse messages for its window are posted to that
/// process's queue and never reach Glue's message loop. Every button press this filter sees therefore
/// started on a Glue surface, and the game must not act on the same gesture for as long as it lasts -
/// however far the cursor travels in the meantime.
/// </summary>
public class GlueMouseGestureWatcherTests
{
    const int WM_MOUSEMOVE = 0x0200;
    const int WM_LBUTTONDOWN = 0x0201;
    const int WM_LBUTTONUP = 0x0202;
    const int WM_RBUTTONDOWN = 0x0204;
    const int WM_RBUTTONUP = 0x0205;
    const int WM_NCLBUTTONDOWN = 0x00A1;

    bool isAnyMouseButtonPhysicallyDown;
    readonly GlueMouseGestureWatcher watcher;

    public GlueMouseGestureWatcherTests() =>
        watcher = new GlueMouseGestureWatcher(() => isAnyMouseButtonPhysicallyDown);

    bool Send(int message)
    {
        var windowsMessage = Message.Create(IntPtr.Zero, message, IntPtr.Zero, IntPtr.Zero);
        return watcher.PreFilterMessage(ref windowsMessage);
    }

    [Fact]
    public void NothingHappened_NoGestureInProgress() =>
        watcher.IsGestureInProgress.ShouldBeFalse("no button has been pressed anywhere in Glue");

    [Fact]
    public void ButtonDownInGlue_StartsGesture()
    {
        isAnyMouseButtonPhysicallyDown = true;
        Send(WM_LBUTTONDOWN);

        watcher.IsGestureInProgress.ShouldBeTrue(
            "Glue's message loop saw the press, so the gesture started on a Glue surface");
    }

    [Fact]
    public void NonClientButtonDown_StartsGesture()
    {
        isAnyMouseButtonPhysicallyDown = true;
        Send(WM_NCLBUTTONDOWN);

        watcher.IsGestureInProgress.ShouldBeTrue(
            "a press on a non-client area (title bar, window border) is still a Glue gesture");
    }

    [Fact]
    public void MouseMoveWithButtonHeld_DoesNotStartGesture()
    {
        isAnyMouseButtonPhysicallyDown = true;
        Send(WM_MOUSEMOVE);

        watcher.IsGestureInProgress.ShouldBeFalse(
            "the cursor merely passing over Glue while a button is held elsewhere is not a Glue gesture");
    }

    [Fact]
    public void ButtonUp_EndsGesture()
    {
        isAnyMouseButtonPhysicallyDown = true;
        Send(WM_LBUTTONDOWN);

        isAnyMouseButtonPhysicallyDown = false;
        Send(WM_LBUTTONUP);

        watcher.IsGestureInProgress.ShouldBeFalse("the button came back up, so the gesture is over");
    }

    [Fact]
    public void ButtonUp_WhileAnotherButtonIsStillHeld_KeepsGesture()
    {
        isAnyMouseButtonPhysicallyDown = true;
        Send(WM_RBUTTONDOWN);
        Send(WM_LBUTTONDOWN);

        Send(WM_RBUTTONUP);

        watcher.IsGestureInProgress.ShouldBeTrue(
            "a button is still held, so the gesture that started in Glue is still in progress");
    }

    /// <summary>
    /// The case that makes the physical-state check necessary rather than nice to have: a drag that
    /// starts in Glue and is released over the embedded game delivers its button-up to the game's
    /// process, so Glue's message loop never sees the end of the gesture. Without this the gate would
    /// stay shut for good and every later click in the game would be swallowed.
    /// </summary>
    [Fact]
    public void ButtonReleasedOverTheGame_EndsGestureOnRefresh()
    {
        isAnyMouseButtonPhysicallyDown = true;
        Send(WM_LBUTTONDOWN);

        isAnyMouseButtonPhysicallyDown = false;
        watcher.Refresh();

        watcher.IsGestureInProgress.ShouldBeFalse(
            "no button is physically down any more, so the gesture ended even though Glue never saw the up");
    }

    [Fact]
    public void Refresh_WhileButtonStillHeld_KeepsGesture()
    {
        isAnyMouseButtonPhysicallyDown = true;
        Send(WM_LBUTTONDOWN);

        watcher.Refresh();

        watcher.IsGestureInProgress.ShouldBeTrue("the button is still down, so the drag is still going");
    }

    [Fact]
    public void GestureChanged_RaisedOnlyOnTransitions()
    {
        var raiseCount = 0;
        watcher.GestureChanged += () => raiseCount++;

        isAnyMouseButtonPhysicallyDown = true;
        Send(WM_LBUTTONDOWN);
        Send(WM_LBUTTONDOWN);
        raiseCount.ShouldBe(1, "the gesture only started once");

        isAnyMouseButtonPhysicallyDown = false;
        Send(WM_LBUTTONUP);
        watcher.Refresh();
        raiseCount.ShouldBe(2, "the gesture only ended once");
    }

    [Fact]
    public void FilterNeverConsumesMessages()
    {
        isAnyMouseButtonPhysicallyDown = true;

        Send(WM_LBUTTONDOWN).ShouldBeFalse("Glue's own controls must still receive the press");
        Send(WM_MOUSEMOVE).ShouldBeFalse();
        Send(WM_LBUTTONUP).ShouldBeFalse("Glue's own controls must still receive the release");
    }
}
