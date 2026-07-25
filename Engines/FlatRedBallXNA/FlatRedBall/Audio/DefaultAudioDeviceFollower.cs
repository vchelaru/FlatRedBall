// =============================================================================
// DefaultAudioDeviceFollower  --  TEMPORARY WORKAROUND, DELETE WHEN FIXED UPSTREAM
// =============================================================================
//
// WHY THIS EXISTS
// ---------------
// MonoGame opens the OpenAL audio device exactly once at startup and never
// re-points it. If the user changes the Windows default playback device while
// the game is running (unplugs headphones, switches to Bluetooth, changes the
// default in the sound control panel, etc.) the game keeps playing to the now
// stale device -- which usually means audio goes silent until the game restarts.
//
// This is MonoGame issue #9467:
//     https://github.com/MonoGame/MonoGame/issues/9467
//
// WHAT THIS DOES
// --------------
// OpenAL-Soft 1.21+ exports the ALC_SOFT_reopen_device extension, whose
// entry point alcReopenDeviceSOFT() re-points an already-open ALCdevice onto a
// new physical output WITHOUT tearing down any sources, buffers or contexts --
// currently-playing sounds simply continue on the new device. MonoGame 3.8.4.1
// bundles OpenAL-Soft 1.24.3 (as MonoGame.Library.OpenAL / openal.dll), so the
// extension is available at runtime.
//
// This class:
//   1. Polls the Windows Core Audio default *render* endpoint on a low-frequency
//      (~1s) background timer.
//   2. When the default endpoint id changes, it grabs the live ALCdevice from
//      OpenAL itself and calls alcReopenDeviceSOFT(device, NULL, NULL), which
//      re-points onto whatever the *current* system default now is.
//
// WHY IT LOOKS THE WAY IT DOES (so it stays easy to delete)
// ---------------------------------------------------------
//   * It is a single, self-contained file. Deleting this file and its one call
//     site in FlatRedBallServices.FinishInitialization() fully removes it.
//   * It gets the ALCdevice handle from OpenAL (alcGetCurrentContext ->
//     alcGetContextsDevice) rather than from MonoGame's private fields, so it
//     does not depend on MonoGame internals and survives MonoGame upgrades.
//   * It resolves alcReopenDeviceSOFT dynamically via alcGetProcAddress (an
//     extension symbol is not guaranteed to be a hard export), and feature-
//     detects ALC_SOFT_reopen_device first -- if the game ever ships an older
//     MonoGame / OpenAL-Soft, this quietly no-ops forever.
//   * It polls the default-endpoint id instead of registering an
//     IMMNotificationClient COM callback. Polling is a handful of lines, has no
//     callback-lifetime / threading pitfalls, and is trivial to rip out later.
//   * Everything is wrapped in try/catch and disables itself on first failure,
//     so a broken assumption can never throw into the game loop.
//
// SCOPE: Windows + DesktopGL (OpenAL) only. The whole file is compiled out on
// every other target via the DESKTOP_GL guard below, and additionally gated on
// a runtime Windows check. It is intentionally NOT used on WindowsDX (XAudio),
// which is a different audio backend and out of scope for #9467.
//
// =============================================================================

#if DESKTOP_GL

using System;
using System.Runtime.InteropServices;
using System.Threading;

// CA1416: the Core Audio COM calls below are Windows-only. We gate every entry
// point on a runtime RuntimeInformation.IsOSPlatform(Windows) check (see Start),
// which the analyzer can't see, so suppress the platform-compatibility warning
// for this self-contained shim.
#pragma warning disable CA1416

namespace FlatRedBall.Audio
{
    /// <summary>
    /// Temporary shim that makes DesktopGL/OpenAL audio follow the Windows default
    /// output device when it is switched mid-game. Works around MonoGame issue
    /// #9467; delete once MonoGame re-points the audio device itself.
    /// </summary>
    internal static class DefaultAudioDeviceFollower
    {
        // -------------------------------------------------------------------------
        // OpenAL (ALC) interop.
        //
        // Target dll is "openal.dll" -- the name OpenAL-Soft ships under on Windows
        // since MonoGame 3.8.3 (it was "soft_oal.dll" before that). These four
        // entry points are stable, standard ALC functions present in every
        // OpenAL-Soft build, so we can hard-DllImport them. The extension entry
        // point (alcReopenDeviceSOFT) is NOT imported this way -- see below.
        // OpenAL-Soft uses the C calling convention.
        // -------------------------------------------------------------------------
        private const string OpenAlLibrary = "openal.dll";

        // ALCcontext* alcGetCurrentContext(void);
        [DllImport(OpenAlLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr alcGetCurrentContext();

        // ALCdevice* alcGetContextsDevice(ALCcontext* context);
        [DllImport(OpenAlLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr alcGetContextsDevice(IntPtr context);

        // ALCboolean alcIsExtensionPresent(ALCdevice* device, const ALCchar* extName);
        // ALCboolean is a single byte (ALC_TRUE == 1, ALC_FALSE == 0).
        [DllImport(OpenAlLibrary, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern byte alcIsExtensionPresent(IntPtr device, [MarshalAs(UnmanagedType.LPStr)] string extName);

        // void* alcGetProcAddress(ALCdevice* device, const ALCchar* funcName);
        [DllImport(OpenAlLibrary, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr alcGetProcAddress(IntPtr device, [MarshalAs(UnmanagedType.LPStr)] string funcName);

        // The extension entry point, resolved at runtime via alcGetProcAddress and
        // marshalled to this delegate. Signature:
        //   ALCboolean alcReopenDeviceSOFT(ALCdevice* device,
        //                                  const ALCchar* deviceName,
        //                                  const ALCint* attribs);
        // We pass deviceName = NULL and attribs = NULL, which re-points the device
        // onto the *current* system default output without tearing anything down.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte AlcReopenDeviceSoftDelegate(IntPtr device, IntPtr deviceName, IntPtr attribs);

        // -------------------------------------------------------------------------
        // Windows Core Audio (MMDevice) interop -- minimal, just enough to read the
        // id of the current default render (playback) endpoint.
        //
        // We use ComImport interface declarations. Only the methods we actually
        // call are given real signatures; earlier vtable slots are declared as
        // stubs purely to preserve the vtable layout (COM dispatches by slot order).
        // -------------------------------------------------------------------------

        // CLSID_MMDeviceEnumerator
        private static readonly Guid CLSID_MMDeviceEnumerator = new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");

        // eRender = 0 (playback endpoints), eConsole = 0 (default "games/system sounds" role).
        private const int EDataFlow_eRender = 0;
        private const int ERole_eConsole = 0;

        [ComImport]
        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")] // IID_IMMDeviceEnumerator
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            // Slot 3 (vtable): EnumAudioEndpoints -- declared only to hold its slot.
            [PreserveSig] int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);

            // Slot 4: GetDefaultAudioEndpoint(dataFlow, role, out IMMDevice).
            [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
        }

        [ComImport]
        [Guid("D666063F-1587-4E43-81F1-B948E807363F")] // IID_IMMDevice
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            // Slot 3: Activate -- stub to hold its slot.
            [PreserveSig] int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object instance);

            // Slot 4: OpenPropertyStore -- stub to hold its slot.
            [PreserveSig] int OpenPropertyStore(int stgmAccess, out IntPtr properties);

            // Slot 5: GetId -- returns the unique endpoint id string (CoTaskMem allocated;
            // the LPWStr marshaller frees it for us).
            [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        }

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        private static Timer _pollTimer;
        private static string _lastEndpointId;
        private static AlcReopenDeviceSoftDelegate _reopenDevice;
        private static bool _reopenResolved;      // have we attempted to resolve the extension yet?
        private static bool _disabled;            // set true after any failure or if unsupported -> permanent no-op
        private const int PollIntervalMs = 1000;

        /// <summary>
        /// Starts following the Windows default output device. Safe to call more than
        /// once (subsequent calls are ignored). No-ops on non-Windows platforms.
        /// </summary>
        public static void Start()
        {
            try
            {
                if (_pollTimer != null || _disabled)
                {
                    return;
                }

                // Windows-only: openal.dll's ALC_SOFT_reopen_device + Core Audio are
                // both Windows concepts here. On Linux/macOS DesktopGL builds this
                // simply does nothing.
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    _disabled = true;
                    return;
                }

                // Poll on a background timer. Feature detection and the first device
                // read happen on the first tick, by which point OpenAL's context has
                // typically been created lazily by MonoGame.
                _pollTimer = new Timer(OnPoll, null, PollIntervalMs, PollIntervalMs);
            }
            catch (Exception e)
            {
                DisableWithLog("failed to start", e);
            }
        }

        /// <summary>
        /// Stops and disposes the poll timer. Wired to the game's Exiting event.
        /// </summary>
        public static void Stop()
        {
            try
            {
                _pollTimer?.Dispose();
                _pollTimer = null;
            }
            catch
            {
                // Nothing useful to do on shutdown; never throw out of teardown.
            }
        }

        private static void OnPoll(object state)
        {
            if (_disabled)
            {
                return;
            }

            try
            {
                var currentId = GetDefaultRenderEndpointId();
                if (currentId == null)
                {
                    // Couldn't read the endpoint this tick; try again next tick.
                    return;
                }

                if (_lastEndpointId == null)
                {
                    // First successful observation -- just record it, nothing to do.
                    _lastEndpointId = currentId;
                    return;
                }

                if (!string.Equals(currentId, _lastEndpointId, StringComparison.Ordinal))
                {
                    // The Windows default output device changed under us.
                    _lastEndpointId = currentId;
                    ReopenOntoCurrentDefault();
                }
            }
            catch (Exception e)
            {
                DisableWithLog("poll failed", e);
            }
        }

        /// <summary>
        /// Reads the id of the current Windows default render (playback) endpoint,
        /// or null if it can't be determined.
        /// </summary>
        private static string GetDefaultRenderEndpointId()
        {
            IMMDeviceEnumerator enumerator = null;
            IMMDevice device = null;
            try
            {
                var comType = Type.GetTypeFromCLSID(CLSID_MMDeviceEnumerator);
                if (comType == null)
                {
                    return null;
                }

                enumerator = (IMMDeviceEnumerator)Activator.CreateInstance(comType);

                // hr < 0 means failure (e.g. no audio endpoints available at all).
                if (enumerator.GetDefaultAudioEndpoint(EDataFlow_eRender, ERole_eConsole, out device) < 0 || device == null)
                {
                    return null;
                }

                return device.GetId(out var id) < 0 ? null : id;
            }
            finally
            {
                if (device != null)
                {
                    Marshal.ReleaseComObject(device);
                }
                if (enumerator != null)
                {
                    Marshal.ReleaseComObject(enumerator);
                }
            }
        }

        /// <summary>
        /// Re-points the live OpenAL device onto the current system default output.
        /// </summary>
        private static void ReopenOntoCurrentDefault()
        {
            // Grab the live ALCdevice straight from OpenAL. If audio hasn't actually
            // been initialized yet (no context), there's nothing to re-point.
            var context = alcGetCurrentContext();
            if (context == IntPtr.Zero)
            {
                return;
            }

            var device = alcGetContextsDevice(context);
            if (device == IntPtr.Zero)
            {
                return;
            }

            if (!_reopenResolved)
            {
                ResolveReopenDelegate(device);
            }

            if (_reopenDevice == null)
            {
                // Extension not present -> permanently disabled (already logged once).
                return;
            }

            // deviceName = NULL, attribs = NULL -> re-point onto the current default.
            var result = _reopenDevice(device, IntPtr.Zero, IntPtr.Zero);
            if (result == 0)
            {
                Log("alcReopenDeviceSOFT returned ALC_FALSE; audio may not have followed the device change.");
            }
        }

        /// <summary>
        /// Feature-detects ALC_SOFT_reopen_device and, if present, resolves the
        /// alcReopenDeviceSOFT entry point. Runs at most once.
        /// </summary>
        private static void ResolveReopenDelegate(IntPtr device)
        {
            _reopenResolved = true;

            try
            {
                if (alcIsExtensionPresent(device, "ALC_SOFT_reopen_device") == 0)
                {
                    // Older MonoGame / OpenAL-Soft < 1.21. Nothing we can do -- no-op forever.
                    Log("ALC_SOFT_reopen_device not present; default-device following disabled.");
                    _disabled = true;
                    return;
                }

                var procAddress = alcGetProcAddress(device, "alcReopenDeviceSOFT");
                if (procAddress == IntPtr.Zero)
                {
                    Log("alcGetProcAddress(\"alcReopenDeviceSOFT\") returned null; disabled.");
                    _disabled = true;
                    return;
                }

                _reopenDevice = Marshal.GetDelegateForFunctionPointer<AlcReopenDeviceSoftDelegate>(procAddress);
            }
            catch (Exception e)
            {
                DisableWithLog("failed to resolve alcReopenDeviceSOFT", e);
            }
        }

        private static void DisableWithLog(string what, Exception e)
        {
            _disabled = true;
            Stop();
            Log($"{what}: {e.GetType().Name}: {e.Message}. Default-device following disabled.");
        }

        private static void Log(string message)
        {
            // Intentionally lightweight and dependency-free so this file stays trivial
            // to delete. Shows up in the debugger output window / attached console.
            System.Diagnostics.Debug.WriteLine("[DefaultAudioDeviceFollower] " + message);
        }
    }
}

#pragma warning restore CA1416

#endif
