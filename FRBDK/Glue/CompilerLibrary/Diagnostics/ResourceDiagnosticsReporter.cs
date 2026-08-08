using System;
using System.Collections.Generic;
using System.Globalization;

namespace CompilerLibrary.Diagnostics
{
    /// <summary>
    /// Decides which resource samples are worth telling the user about.
    /// </summary>
    /// <remarks>
    /// Deliberately holds no timers, no P/Invoke and no file access, so the reporting rules can be
    /// tested directly. <see cref="Observe"/> reports a counter only when it sets a new high-water mark:
    /// a healthy editor reaches its working ceiling and then goes quiet, while a leak keeps producing
    /// lines. Continuous output is therefore the signal, not noise to be tuned away.
    /// </remarks>
    public class ResourceDiagnosticsReporter
    {
        /// <summary>
        /// Per-process cap on USER and GDI objects. Hitting it is what makes PostMessage fail with
        /// ERROR_NOT_ENOUGH_QUOTA (Win32 1816).
        /// </summary>
        public const int UserObjectLimit = 10000;

        /// <summary>
        /// A UI thread quiet for longer than this is not merely busy.
        /// </summary>
        public long StallWarningMilliseconds { get; set; } = 2000;

        bool hasBaseline;

        int highestUserObjects;
        int highestGdiObjects;
        int highestHandles;
        int highestThreads;

        long lastReportedStall;

        readonly List<int> reportedLimitPercentages = new List<int>();

        static readonly int[] LimitPercentagesToWarnAt = { 50, 75, 90 };

        /// <summary>
        /// Returns the lines worth surfacing for this sample, which is usually none.
        /// </summary>
        public IReadOnlyList<string> Observe(ResourceSample sample)
        {
            var lines = new List<string>();

            if (!hasBaseline)
            {
                hasBaseline = true;
                highestUserObjects = sample.UserObjectCount;
                highestGdiObjects = sample.GdiObjectCount;
                highestHandles = sample.HandleCount;
                highestThreads = sample.ThreadCount;

                lines.Add(
                    $"Resource baseline: {sample.UserObjectCount} USER objects, {sample.GdiObjectCount} GDI objects, " +
                    $"{sample.HandleCount} handles, {sample.ThreadCount} threads.");

                AddLimitWarnings(sample.UserObjectCount, lines);

                return lines;
            }

            ReportNewPeak("USER objects", sample.UserObjectCount, ref highestUserObjects, lines);
            ReportNewPeak("GDI objects", sample.GdiObjectCount, ref highestGdiObjects, lines);
            ReportNewPeak("handles", sample.HandleCount, ref highestHandles, lines);
            ReportNewPeak("threads", sample.ThreadCount, ref highestThreads, lines);

            AddLimitWarnings(sample.UserObjectCount, lines);
            AddStallWarnings(sample.UiThreadStallMilliseconds, lines);

            return lines;
        }

        static void ReportNewPeak(string name, int value, ref int highest, List<string> lines)
        {
            if (value <= highest)
            {
                return;
            }

            lines.Add($"New peak {name}: {value} (up {value - highest}).");
            highest = value;
        }

        void AddLimitWarnings(int userObjectCount, List<string> lines)
        {
            foreach (var percentage in LimitPercentagesToWarnAt)
            {
                if (reportedLimitPercentages.Contains(percentage))
                {
                    continue;
                }

                if (userObjectCount * 100L / UserObjectLimit >= percentage)
                {
                    reportedLimitPercentages.Add(percentage);
                    lines.Add(
                        $"WARNING: USER objects at {userObjectCount} of the {UserObjectLimit} per-process limit " +
                        $"({percentage}%). At the limit the editor stops drawing and stops responding to input.");
                }
            }
        }

        void AddStallWarnings(long stallMilliseconds, List<string> lines)
        {
            if (stallMilliseconds < StallWarningMilliseconds)
            {
                if (lastReportedStall > 0)
                {
                    lines.Add($"UI thread responded again after {lastReportedStall}ms.");
                    lastReportedStall = 0;
                }
                return;
            }

            // Only on the first breach and each doubling after it, so a long freeze produces a handful
            // of lines rather than one per sample.
            if (lastReportedStall != 0 && stallMilliseconds < lastReportedStall * 2)
            {
                return;
            }

            lines.Add($"WARNING: UI thread has not run for {stallMilliseconds}ms.");
            lastReportedStall = stallMilliseconds;
        }

        /// <summary>
        /// One fixed-shape line per sample for the log file, which records every sample rather than only
        /// the notable ones. A frozen editor cannot be read or copied from on screen, so the file is the
        /// only copy that survives the failure being diagnosed.
        /// </summary>
        public static string FormatForFile(DateTime timestampUtc, ResourceSample sample) =>
            string.Format(
                CultureInfo.InvariantCulture,
                "{0:yyyy-MM-dd HH:mm:ss.fff}\tuser={1}\tgdi={2}\thandles={3}\tthreads={4}\tuiStallMs={5}",
                timestampUtc,
                sample.UserObjectCount,
                sample.GdiObjectCount,
                sample.HandleCount,
                sample.ThreadCount,
                sample.UiThreadStallMilliseconds);
    }
}
