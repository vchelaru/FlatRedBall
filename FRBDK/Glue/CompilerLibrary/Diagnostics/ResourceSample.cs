namespace CompilerLibrary.Diagnostics
{
    /// <summary>
    /// One reading of the editor process's window-system and thread resource usage.
    /// </summary>
    public struct ResourceSample
    {
        /// <summary>
        /// USER objects held by the process (windows, menus, hooks, timers). Capped per process at
        /// <see cref="ResourceDiagnosticsReporter.UserObjectLimit"/>; exhausting it makes PostMessage
        /// fail with ERROR_NOT_ENOUGH_QUOTA and leaves the UI unable to draw or take input.
        /// </summary>
        public int UserObjectCount;

        /// <summary>
        /// GDI objects held by the process (brushes, pens, bitmaps, device contexts). Same per-process
        /// cap as <see cref="UserObjectCount"/>.
        /// </summary>
        public int GdiObjectCount;

        /// <summary>
        /// All kernel handles held by the process. Grows with leaked files, sockets and processes, none
        /// of which show up as memory.
        /// </summary>
        public int HandleCount;

        public int ThreadCount;

        /// <summary>
        /// How long since the UI thread last ran anything. Stays near the sampling interval while the
        /// editor is healthy and climbs without bound once the UI thread is blocked or deadlocked.
        /// </summary>
        public long UiThreadStallMilliseconds;
    }
}
