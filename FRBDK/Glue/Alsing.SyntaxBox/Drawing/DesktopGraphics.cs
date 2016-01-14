using System;
using Alsing.Windows;
using System.Drawing;

namespace Alsing.Drawing
{
    public class DesktopGraphics : IDisposable
    {
        public readonly Graphics Graphics;
        protected IntPtr handle = new IntPtr(0);
        protected IntPtr hdc = new IntPtr(0);

        public DesktopGraphics()
        {
            handle = NativeMethods.GetDesktopWindow();
            hdc = NativeMethods.GetWindowDC(hdc);
            Graphics = Graphics.FromHdc(hdc);
        }

        public void Dispose()
        {
            NativeMethods.ReleaseDC(handle, hdc);
        }
    }
}
