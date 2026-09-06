using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Timers;
using GameCommunicationPlugin.GlueControl.CommandSending;
using GameCommunicationPlugin.GlueControl.Dtos;

namespace GameCommunicationPlugin.GlueControl.Managers;

/// <summary>
/// Answers "may the embedded game act on mouse input right now?" and pushes the answer to the game
/// (<see cref="SetEmbeddedInputAllowedDto"/>) whenever it changes. Mirrors
/// <see cref="ModalReportingService"/>: poll on a timer, send only on a change.
///
/// This decision used to live inside the game, where it was an OR-chain of Win32 focus guesses that
/// grew a clause per bug report (#2183, #2205, #2214) and could never be right in both directions.
/// Worse, all of those calls were compile-time stubs: GlueControl/Embedded compiles into the user's
/// game project, which defines MONOGAME;DESKTOP_GL;MONOGAME_381 and never WINDOWS, so the
/// `#if WINDOWS` P/Invokes there always returned IntPtr.Zero/false. Glue is a real Windows-only
/// WPF/WinForms assembly, so the same APIs work here, and one precise question replaces the OR-chain:
/// is the window the OS itself reports as topmost under the cursor our embedded game window?
///
/// That single check is Z-order-aware (so a window dragged over the panel blocks the game - #2154,
/// #2214) and activation-independent (so the first click that returns focus to Glue still lands -
/// #2183, #2187), which is exactly the pair the old heuristics kept trading against each other.
///
/// Z-order alone can't answer one thing, though: who a gesture already in progress belongs to. A drag
/// that starts anywhere in Glue and passes over the game reads as "the game is topmost" at every
/// instant it is. <see cref="GlueMouseGestureWatcher"/> supplies that missing half (#2226).
/// </summary>
class EmbeddedInputAllowedService
{
    Timer pollTimer;
    private readonly ISynchronizeInvoke _synchronizingObject;
    private readonly CommandSender _commandSender;
    private readonly Func<IntPtr> _getEmbeddedGameWindowHandle;

    public EmbeddedInputAllowedService(ISynchronizeInvoke synchronizingObject, CommandSender commandSender,
        Func<IntPtr> getEmbeddedGameWindowHandle)
    {
        _synchronizingObject = synchronizingObject;
        _commandSender = commandSender;
        _getEmbeddedGameWindowHandle = getEmbeddedGameWindowHandle;
    }

    readonly GlueMouseGestureWatcher _gestureWatcher = new();

    public void Initialize()
    {
        // Watches Glue's own message loop, so any mouse gesture that starts on any Glue surface blocks
        // the game for its duration - no per-control wiring, and nothing the user does inside the game
        // (a separate process) reaches it. Re-check immediately when a gesture starts or ends rather
        // than waiting for the next tick: a splitter drag resizes the embedded game window live, so its
        // edge can slide under the cursor well inside one poll interval.
        System.Windows.Forms.Application.AddMessageFilter(_gestureWatcher);
        _gestureWatcher.GestureChanged += () => _ = CheckAndSendIfChanged();

        // Occlusion and app switching change on human timescales (dragging a window off the panel,
        // alt-tabbing), not per-frame, so this doesn't need to keep up with the mouse. The game
        // combines this with its own per-frame FlatRedBall.Input.Mouse.IsInGameWindow() for the
        // cursor's actual position, so a poll interval can never misplace a click.
        var frequency = 100; // ms
        pollTimer = new Timer(frequency);
        pollTimer.Elapsed += UpdateTimer;
        pollTimer.SynchronizingObject = _synchronizingObject;
        pollTimer.Start();
    }

    bool? lastAcknowledged = null;
    bool isSending = false;

    private async void UpdateTimer(object sender, ElapsedEventArgs e)
    {
        // Ends a gesture whose button-up Glue never saw because it happened over the game's window.
        _gestureWatcher.Refresh();
        await CheckAndSendIfChanged();
    }

    private async System.Threading.Tasks.Task CheckAndSendIfChanged()
    {
        if (!_commandSender.IsConnected)
        {
            // A freshly launched game starts with IsInputAllowedFromGlue false and hasn't been told
            // otherwise, so forget what the previous game was told - otherwise the first push after a
            // restart is suppressed as "no change" and the new game's gate stays shut for good.
            lastAcknowledged = null;
            return;
        }

        if (isSending)
        {
            // A send is still in flight; skip rather than queue up. The next tick re-reads current
            // state anyway, so nothing is lost by dropping this one.
            return;
        }

        var isAllowed = ReadIsAllowed(_getEmbeddedGameWindowHandle(), _gestureWatcher.IsGestureInProgress);

        if (lastAcknowledged == isAllowed)
        {
            return;
        }

        isSending = true;
        try
        {
            var response = await _commandSender.Send(new SetEmbeddedInputAllowedDto
            {
                IsAllowed = isAllowed
            });

            // Only remember it once the game confirms it arrived. A game that is connected but not yet
            // able to dispatch DTOs (GlueControlManager.Self still null during Game1.Initialize) answers
            // unsuccessfully, and recording that as sent would leave the game's gate shut until the
            // value happened to change again - the exact "clicks do nothing" symptom this replaces.
            if (response?.Succeeded == true)
            {
                lastAcknowledged = isAllowed;
            }
        }
        catch
        {
            // Game went away mid-send; the next tick re-evaluates from scratch.
        }
        finally
        {
            isSending = false;
        }
    }

    /// <summary>
    /// The real OS half: where is the cursor, and who owns the window actually drawn there. Split from
    /// <see cref="ComputeIsAllowed"/> so the decision can be unit tested with synthetic inputs and this
    /// can be tested against a real window (see EmbeddedInputAllowedServiceTests) - the previous
    /// version of this feature had thorough tests of its decision logic and none of its Win32 calls,
    /// which is precisely why nobody noticed the calls weren't happening.
    /// </summary>
    /// <param name="isGlueGestureInProgress">
    /// True while a mouse gesture that started on a Glue surface is still held down - see
    /// <see cref="GlueMouseGestureWatcher"/>.
    /// </param>
    internal static bool ReadIsAllowed(IntPtr embeddedGameWindowHandle, bool isGlueGestureInProgress)
    {
        var cursorPositionKnown = GetCursorPos(out var cursorPosition);
        var topmostWindowAtCursor = cursorPositionKnown ? WindowFromPoint(cursorPosition) : IntPtr.Zero;

        var topmostBelongsToEmbeddedGame =
            topmostWindowAtCursor != IntPtr.Zero &&
            embeddedGameWindowHandle != IntPtr.Zero &&
            (topmostWindowAtCursor == embeddedGameWindowHandle ||
                IsChild(embeddedGameWindowHandle, topmostWindowAtCursor));

        return ComputeIsAllowed(embeddedGameWindowHandle, cursorPositionKnown, topmostWindowAtCursor,
            topmostBelongsToEmbeddedGame, isGlueGestureInProgress);
    }

    /// <summary>
    /// Pure decision logic. Fails closed whenever the answer can't be positively confirmed, matching
    /// #2154's "when in doubt, block."
    /// </summary>
    /// <param name="embeddedGameWindowHandle">IntPtr.Zero when no game is currently embedded.</param>
    /// <param name="cursorPositionKnown">False when GetCursorPos failed.</param>
    /// <param name="topmostWindowAtCursor">IntPtr.Zero when WindowFromPoint found no window there.</param>
    /// <param name="topmostBelongsToEmbeddedGame">
    /// Whether that topmost window is the embedded game window or a child of it. SDL/MonoGame uses a
    /// single HWND today, but a child is still the game as far as this question goes.
    /// </param>
    /// <param name="isGlueGestureInProgress">
    /// Whether a mouse gesture that started on a Glue surface is still held down. It wins over the
    /// Z-order answer, which can't tell "the game is genuinely topmost under the cursor" from "the user
    /// is dragging out of Glue and happens to be over the game right now" - and, while a splitter drag
    /// resizes the game window live, can't tell it from "the game's window is mid-resize and transiently
    /// still covers the cursor" either (#2226).
    /// </param>
    internal static bool ComputeIsAllowed(IntPtr embeddedGameWindowHandle, bool cursorPositionKnown,
        IntPtr topmostWindowAtCursor, bool topmostBelongsToEmbeddedGame, bool isGlueGestureInProgress)
    {
        if (embeddedGameWindowHandle == IntPtr.Zero)
        {
            return false;
        }

        if (isGlueGestureInProgress)
        {
            return false;
        }

        if (!cursorPositionKnown || topmostWindowAtCursor == IntPtr.Zero)
        {
            return false;
        }

        return topmostBelongsToEmbeddedGame;
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool GetCursorPos(out Win32Point point);

    [DllImport("user32.dll")]
    static extern IntPtr WindowFromPoint(Win32Point point);

    [DllImport("user32.dll")]
    static extern bool IsChild(IntPtr hWndParent, IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Win32Point
    {
        public int X;
        public int Y;
    }
}
