using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace FlatRedBall.Glue.Managers
{
    /// <summary>
    /// Copies text to the Windows clipboard, tolerating the fact that another process can be holding
    /// it at that instant.
    /// </summary>
    /// <remarks>
    /// The clipboard is a single system-wide resource that any process can lock. When one does,
    /// <c>Clipboard.SetText</c> throws <c>COMException 0x800401D0 (CLIPBRD_E_CANT_OPEN)</c> - commonly
    /// while a remote desktop client, a password manager or a clipboard manager is mid-poll. Nothing
    /// about it indicates a bug in Glue, and the hold clears in milliseconds, so a copy button must
    /// retry rather than surface a stack trace.
    /// </remarks>
    public static class ClipboardService
    {
        /// <summary>
        /// Transitional injection point - see REFACTORING.md. The real clipboard needs an STA thread
        /// and a live desktop, neither of which a test host has.
        /// </summary>
        internal static Action<string> SetTextImpl = text => System.Windows.Clipboard.SetText(text);

        internal static Action<int> SleepImpl = Thread.Sleep;

        internal const int AttemptCount = 5;
        internal const int DelayBetweenAttemptsMs = 50;

        /// <summary>
        /// Copies <paramref name="text"/>, returning whether it made it. Never throws: a failed copy
        /// is worth reporting, not worth taking a click down with it.
        /// </summary>
        public static bool SetText(string text)
        {
            for (var attempt = 1; attempt <= AttemptCount; attempt++)
            {
                try
                {
                    SetTextImpl(text);
                    return true;
                }
                catch (COMException exception) when (exception.HResult == ClipboardCantOpenHResult)
                {
                    // Only this one HResult is retried. Any other failure is a real bug and must keep
                    // throwing rather than being reported as a copy that quietly did not happen.
                    if (attempt == AttemptCount)
                    {
                        return false;
                    }

                    SleepImpl(DelayBetweenAttemptsMs);
                }
            }

            return false;
        }

        /// <summary>CLIPBRD_E_CANT_OPEN - another process currently holds the clipboard.</summary>
        internal const int ClipboardCantOpenHResult = unchecked((int)0x800401D0);
    }
}
