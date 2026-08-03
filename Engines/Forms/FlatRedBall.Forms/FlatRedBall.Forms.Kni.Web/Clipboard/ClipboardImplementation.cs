using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextCopy;

namespace FlatRedBall.Forms.Clipboard
{
    internal class ClipboardImplementation
    {
        // The callback is accepted but unused, matching the DesktopGL implementation. Gum's shared
        // Forms source calls GetText(HandlePaste) unconditionally under #if FRB, so every platform
        // has to accept the parameter even where the clipboard read is synchronous.
        internal static string GetText(Action? callback = null)
        {
            return ClipboardService.GetText();
        }

        internal static void PushStringToClipboard(string text)
        {
            ClipboardService.SetText(text);
        }
    }
}
