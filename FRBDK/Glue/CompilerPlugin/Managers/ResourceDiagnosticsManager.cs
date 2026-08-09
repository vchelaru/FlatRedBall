using CompilerLibrary.Diagnostics;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CompilerPlugin.Managers
{
    /// <summary>
    /// Watches the editor's USER/GDI/handle/thread counts and how long the UI thread has been quiet,
    /// writing every sample to a file and the notable ones to the Build tab.
    /// </summary>
    /// <remarks>
    /// Sampling runs on a background thread on purpose. The failures this exists to catch either block
    /// the UI thread or exhaust the resources it needs to run, so anything driven by a DispatcherTimer
    /// stops producing data exactly when the data matters. The UI thread's only job here is to stamp a
    /// heartbeat that the background loop reads.
    /// </remarks>
    public class ResourceDiagnosticsManager
    {
        #region Fields/Properties

        static readonly TimeSpan SampleInterval = TimeSpan.FromSeconds(2);
        static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(1);

        readonly ResourceDiagnosticsReporter reporter = new ResourceDiagnosticsReporter();
        readonly Action<string> printOutput;

        System.Windows.Threading.DispatcherTimer heartbeatTimer;
        CancellationTokenSource samplingCancellation;

        /// <summary>
        /// Last time the UI thread ran, as a monotonic tick count. Written by the UI thread, read by the
        /// sampling thread; long reads and writes are atomic on 64-bit so no lock is needed.
        /// </summary>
        long lastUiHeartbeatTicks;

        string logFilePath;

        public bool IsEnabled { get; private set; }

        #endregion

        public ResourceDiagnosticsManager(Action<string> printOutput)
        {
            this.printOutput = printOutput;
        }

        /// <summary>
        /// Starts or stops sampling. Off by default: this is opt-in diagnostics, not always-on overhead.
        /// </summary>
        public void SetEnabled(bool isEnabled)
        {
            if (isEnabled == IsEnabled)
            {
                return;
            }
            IsEnabled = isEnabled;

            if (isEnabled)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }

        void Start()
        {
            logFilePath = BuildLogFilePath();
            printOutput($"Resource diagnostics on. Every sample is being appended to:{Environment.NewLine}{logFilePath}");

            lastUiHeartbeatTicks = Environment.TickCount64;

            heartbeatTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = HeartbeatInterval
            };
            heartbeatTimer.Tick += (_, __) => lastUiHeartbeatTicks = Environment.TickCount64;
            heartbeatTimer.Start();

            samplingCancellation = new CancellationTokenSource();
            _ = SampleLoop(samplingCancellation.Token);
        }

        void Stop()
        {
            heartbeatTimer?.Stop();
            heartbeatTimer = null;

            try { samplingCancellation?.Cancel(); } catch { }
            samplingCancellation = null;

            printOutput("Resource diagnostics off.");
        }

        async Task SampleLoop(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                try
                {
                    var sample = ResourceSampler.Take(Environment.TickCount64 - lastUiHeartbeatTicks);

                    AppendToLogFile(ResourceDiagnosticsReporter.FormatForFile(DateTime.UtcNow, sample));

                    foreach (var line in reporter.Observe(sample))
                    {
                        printOutput(line);
                    }
                }
                catch (Exception e)
                {
                    // Diagnostics must never be the thing that breaks the editor.
                    Debug.WriteLine($"Resource diagnostics sample failed: {e}");
                }

                try
                {
                    await Task.Delay(SampleInterval, cancellation).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        void AppendToLogFile(string line)
        {
            try
            {
                File.AppendAllText(logFilePath, line + Environment.NewLine);
            }
            catch
            {
                // A locked or unwritable log file is not worth interrupting the user over.
            }
        }

        static string BuildLogFilePath()
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FlatRedBall",
                "Glue",
                "Diagnostics");

            Directory.CreateDirectory(directory);

            return Path.Combine(directory, $"resources-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        }
    }
}
