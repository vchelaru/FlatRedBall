using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using GameCommunicationPlugin.GlueControl.Managers;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GameCommunicationPlugin;

/// <summary>
/// Covers the Glue-side half of the embedded input gate: EmbeddedInputAllowedService decides whether
/// the embedded game window is the topmost window under the cursor, and pushes the answer to the game.
///
/// The Win32 test below matters more than the decision table. The four bug reports this feature
/// replaced (#2183, #2205, #2214) were all the same failure: the OS calls behind the decision never
/// ran (they were behind a `#if WINDOWS` that game projects don't define), while the decision logic
/// itself was tested thoroughly with synthetic booleans and stayed green through every regression.
/// Testing only the pure function reproduces exactly that blind spot, so ReadIsAllowed is exercised
/// against a real window with the real cursor.
/// </summary>
public class EmbeddedInputAllowedServiceTests
{
    [Fact]
    public void ComputeIsAllowed_NoEmbeddedGame_IsFalse()
    {
        EmbeddedInputAllowedService.ComputeIsAllowed(IntPtr.Zero, cursorPositionKnown: true,
            topmostWindowAtCursor: new IntPtr(1), topmostBelongsToEmbeddedGame: true)
            .ShouldBeFalse("no game is embedded, so there is nothing for a click to reach");
    }

    [Fact]
    public void ComputeIsAllowed_CursorPositionUnknown_FailsClosed()
    {
        EmbeddedInputAllowedService.ComputeIsAllowed(new IntPtr(1), cursorPositionKnown: false,
            topmostWindowAtCursor: new IntPtr(1), topmostBelongsToEmbeddedGame: true)
            .ShouldBeFalse("GetCursorPos failed, so the answer can't be confirmed and must fail closed (#2154)");
    }

    [Fact]
    public void ComputeIsAllowed_NoWindowAtCursor_FailsClosed()
    {
        EmbeddedInputAllowedService.ComputeIsAllowed(new IntPtr(1), cursorPositionKnown: true,
            topmostWindowAtCursor: IntPtr.Zero, topmostBelongsToEmbeddedGame: false)
            .ShouldBeFalse("nothing is drawn at the cursor (e.g. off-screen), so fail closed");
    }

    [Fact]
    public void ComputeIsAllowed_AnotherWindowIsTopmost_IsFalse()
    {
        EmbeddedInputAllowedService.ComputeIsAllowed(new IntPtr(1), cursorPositionKnown: true,
            topmostWindowAtCursor: new IntPtr(2), topmostBelongsToEmbeddedGame: false)
            .ShouldBeFalse("something else is drawn over the embedded game at the cursor - the click is " +
                "that window's, not the game's (#2154, #2214)");
    }

    [Fact]
    public void ComputeIsAllowed_EmbeddedGameIsTopmost_IsTrue()
    {
        EmbeddedInputAllowedService.ComputeIsAllowed(new IntPtr(1), cursorPositionKnown: true,
            topmostWindowAtCursor: new IntPtr(1), topmostBelongsToEmbeddedGame: true)
            .ShouldBeTrue("the game window is what the OS reports as topmost under the cursor");
    }

    [Fact]
    public void ReadIsAllowed_NoEmbeddedGame_IsFalse() =>
        EmbeddedInputAllowedService.ReadIsAllowed(IntPtr.Zero, isBlockedByUiInteraction: false).ShouldBeFalse();

    /// <summary>
    /// The real thing: a real window, the real cursor, real GetCursorPos/WindowFromPoint. Proves the OS
    /// calls actually happen and actually answer - which is the exact assurance the previous
    /// implementation never had.
    /// </summary>
    [StaFact]
    public void ReadIsAllowed_RealWindow_TracksWhetherTheCursorIsOverIt()
    {
        GetCursorPos(out var originalCursorPosition).ShouldBeTrue(
            "GetCursorPos must work in Glue's own process - the whole design depends on it");

        var window = new Window
        {
            Width = 400,
            Height = 300,
            Left = 100,
            Top = 100,
            Topmost = true,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None,
        };

        try
        {
            window.Show();
            PumpDispatcher();

            var handle = new WindowInteropHelper(window).Handle;
            handle.ShouldNotBe(IntPtr.Zero);

            // Read the window's real screen rect rather than trusting Left/Top: those are WPF DIPs, and
            // the cursor is positioned in physical pixels.
            GetWindowRect(handle, out var rect).ShouldBeTrue();
            var centerX = (rect.Left + rect.Right) / 2;
            var centerY = (rect.Top + rect.Bottom) / 2;

            SetCursorPos(centerX, centerY).ShouldBeTrue();
            PumpDispatcher();

            EmbeddedInputAllowedService.ReadIsAllowed(handle, isBlockedByUiInteraction: false).ShouldBeTrue(
                "the cursor is over the window and nothing covers it, so input belongs to it");

            // A splitter drag (or other blocking UI interaction) in progress must win even though the
            // window genuinely is topmost under the cursor - this is what a resizing embedded game
            // window looks like mid-drag (#2226), and no amount of Win32 Z-order truth should let a
            // drag gesture reach the game.
            EmbeddedInputAllowedService.ReadIsAllowed(handle, isBlockedByUiInteraction: true).ShouldBeFalse(
                "a blocking UI interaction (e.g. a splitter drag) is in progress, so input must not reach the game");

            // Move the cursor off the window (its top-left corner is at least 100px in from the screen
            // edge, so this lands outside it) and the same call must flip.
            SetCursorPos(rect.Left - 50, rect.Top - 50).ShouldBeTrue();
            PumpDispatcher();

            EmbeddedInputAllowedService.ReadIsAllowed(handle, isBlockedByUiInteraction: false).ShouldBeFalse(
                "the cursor is no longer over the window, so a click there isn't the window's");
        }
        finally
        {
            SetCursorPos(originalCursorPosition.X, originalCursorPosition.Y);
            window.Close();
            PumpDispatcher();
        }
    }

    static void PumpDispatcher() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool GetCursorPos(out EmbeddedInputAllowedService.Win32Point point);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool GetWindowRect(IntPtr hWnd, out Win32Rect rect);

    [StructLayout(LayoutKind.Sequential)]
    struct Win32Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
