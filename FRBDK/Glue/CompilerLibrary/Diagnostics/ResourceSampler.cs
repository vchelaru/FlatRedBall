using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CompilerLibrary.Diagnostics
{
    /// <summary>
    /// Reads the current process's window-system and thread resource counts.
    /// </summary>
    public static class ResourceSampler
    {
        [DllImport("user32.dll")]
        static extern int GetGuiResources(IntPtr hProcess, int uiFlags);

        const int GR_GDIOBJECTS = 0;
        const int GR_USEROBJECTS = 1;

        /// <summary>
        /// Takes a reading. <paramref name="uiThreadStallMilliseconds"/> is supplied by the caller since
        /// only the caller knows when the UI thread last ran.
        /// </summary>
        public static ResourceSample Take(long uiThreadStallMilliseconds)
        {
            using var process = Process.GetCurrentProcess();
            var handle = process.Handle;

            return new ResourceSample
            {
                UserObjectCount = GetGuiResources(handle, GR_USEROBJECTS),
                GdiObjectCount = GetGuiResources(handle, GR_GDIOBJECTS),
                HandleCount = process.HandleCount,
                ThreadCount = process.Threads.Count,
                UiThreadStallMilliseconds = uiThreadStallMilliseconds
            };
        }
    }
}
