using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GameCommunicationPlugin.GlueControl.Managers;

/// <summary>
/// Answers "is a mouse gesture that began on a Glue surface still in progress?" by watching Glue's own
/// message loop. Registered with <see cref="Application.AddMessageFilter"/>, so it sees every message
/// that loop pumps - WinForms controls and the WPF content in Glue's ElementHost alike.
///
/// The embedded game is a separate process whose window is reparented into Glue, so its mouse messages
/// go to that process's queue and never reach this filter. A button press seen here therefore started
/// somewhere in Glue: a panel splitter, the tree view, the output window, a tab header. Such a gesture
/// belongs to Glue until the button comes back up, no matter where the cursor travels in between,
/// which is what keeps a drag out of Glue and over the Game tab from also registering inside the game
/// as a click or a rectangle-select (#2226).
///
/// Asking where the gesture started replaces the earlier approach of hooking each control that was
/// reported broken - originally only the panel GridSplitters. Every drag source in Glue hits the same
/// defect, so the gate can't be a list of controls somebody remembered to wire up.
/// </summary>
class GlueMouseGestureWatcher : IMessageFilter
{
    const int WM_NCLBUTTONDOWN = 0x00A1;
    const int WM_NCLBUTTONUP = 0x00A2;
    const int WM_NCRBUTTONDOWN = 0x00A4;
    const int WM_NCRBUTTONUP = 0x00A5;
    const int WM_NCMBUTTONDOWN = 0x00A7;
    const int WM_NCMBUTTONUP = 0x00A8;
    const int WM_NCXBUTTONDOWN = 0x00AB;
    const int WM_NCXBUTTONUP = 0x00AC;
    const int WM_LBUTTONDOWN = 0x0201;
    const int WM_LBUTTONUP = 0x0202;
    const int WM_RBUTTONDOWN = 0x0204;
    const int WM_RBUTTONUP = 0x0205;
    const int WM_MBUTTONDOWN = 0x0207;
    const int WM_MBUTTONUP = 0x0208;
    const int WM_XBUTTONDOWN = 0x020B;
    const int WM_XBUTTONUP = 0x020C;

    const int VK_LBUTTON = 0x01;
    const int VK_RBUTTON = 0x02;
    const int VK_MBUTTON = 0x04;
    const int VK_XBUTTON1 = 0x05;
    const int VK_XBUTTON2 = 0x06;

    readonly Func<bool> _getIsAnyMouseButtonDown;

    public GlueMouseGestureWatcher() : this(IsAnyMouseButtonPhysicallyDown) { }

    internal GlueMouseGestureWatcher(Func<bool> getIsAnyMouseButtonDown) =>
        _getIsAnyMouseButtonDown = getIsAnyMouseButtonDown;

    public bool IsGestureInProgress { get; private set; }

    /// <summary>
    /// Raised when <see cref="IsGestureInProgress"/> flips, so the embedded input gate can be
    /// re-evaluated the moment a drag starts rather than on its next poll tick. A splitter drag resizes
    /// the embedded game window live, so the game's edge can slide under the cursor well inside one
    /// poll interval.
    /// </summary>
    public event Action GestureChanged;

    public bool PreFilterMessage(ref Message m)
    {
        switch (m.Msg)
        {
            case WM_LBUTTONDOWN:
            case WM_RBUTTONDOWN:
            case WM_MBUTTONDOWN:
            case WM_XBUTTONDOWN:
            case WM_NCLBUTTONDOWN:
            case WM_NCRBUTTONDOWN:
            case WM_NCMBUTTONDOWN:
            case WM_NCXBUTTONDOWN:
                SetIsGestureInProgress(true);
                break;
            case WM_LBUTTONUP:
            case WM_RBUTTONUP:
            case WM_MBUTTONUP:
            case WM_XBUTTONUP:
            case WM_NCLBUTTONUP:
            case WM_NCRBUTTONUP:
            case WM_NCMBUTTONUP:
            case WM_NCXBUTTONUP:
                Refresh();
                break;
        }

        // Only ever observes; Glue's own controls still get every message.
        return false;
    }

    /// <summary>
    /// Ends a gesture once no mouse button is physically down. Glue does not always see the button-up
    /// itself: a drag released over the embedded game delivers it to the game's process instead, so
    /// without this the gate would stay shut for good and every later click in the game would be
    /// swallowed. Called both on the button-up messages Glue does see and on every poll tick.
    /// </summary>
    public void Refresh()
    {
        if (IsGestureInProgress && !_getIsAnyMouseButtonDown())
        {
            SetIsGestureInProgress(false);
        }
    }

    void SetIsGestureInProgress(bool value)
    {
        if (IsGestureInProgress == value)
        {
            return;
        }

        IsGestureInProgress = value;
        GestureChanged?.Invoke();
    }

    /// <summary>
    /// GetAsyncKeyState rather than Control.MouseButtons: this is called from inside PreFilterMessage,
    /// before the button-up message has been dispatched, and the message-queue-synchronous key state
    /// GetKeyState reports still says "down" at that point.
    /// </summary>
    static bool IsAnyMouseButtonPhysicallyDown() =>
        IsPhysicallyDown(VK_LBUTTON) || IsPhysicallyDown(VK_RBUTTON) || IsPhysicallyDown(VK_MBUTTON) ||
        IsPhysicallyDown(VK_XBUTTON1) || IsPhysicallyDown(VK_XBUTTON2);

    static bool IsPhysicallyDown(int virtualKey) => (GetAsyncKeyState(virtualKey) & 0x8000) != 0;

    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int virtualKey);
}
