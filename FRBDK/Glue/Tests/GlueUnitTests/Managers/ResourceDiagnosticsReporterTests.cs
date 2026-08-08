using CompilerLibrary.Diagnostics;
using Shouldly;
using System;
using System.Linq;
using Xunit;

namespace GlueUnitTests.Managers
{
    public class ResourceDiagnosticsReporterTests
    {
        static ResourceSample Sample(
            int userObjects = 100,
            int gdiObjects = 100,
            int handles = 100,
            int threads = 10,
            long uiStallMilliseconds = 0) => new ResourceSample
            {
                UserObjectCount = userObjects,
                GdiObjectCount = gdiObjects,
                HandleCount = handles,
                ThreadCount = threads,
                UiThreadStallMilliseconds = uiStallMilliseconds
            };

        [Fact]
        public void FirstSample_ReportsABaseline()
        {
            var reporter = new ResourceDiagnosticsReporter();

            var lines = reporter.Observe(Sample(userObjects: 250, gdiObjects: 300, handles: 900, threads: 42));

            lines.ShouldHaveSingleItem();
            lines[0].ShouldContain("250");
            lines[0].ShouldContain("300");
            lines[0].ShouldContain("900");
            lines[0].ShouldContain("42");
        }

        /// <summary>
        /// The point of reporting only new peaks: a healthy editor settles at a ceiling and stops
        /// producing output, so continuous output means something is actually growing.
        /// </summary>
        [Fact]
        public void SamplesAtOrBelowThePeak_ReportNothing()
        {
            var reporter = new ResourceDiagnosticsReporter();
            reporter.Observe(Sample(userObjects: 500));

            reporter.Observe(Sample(userObjects: 500)).ShouldBeEmpty();
            reporter.Observe(Sample(userObjects: 499)).ShouldBeEmpty();
            reporter.Observe(Sample(userObjects: 200)).ShouldBeEmpty();
        }

        [Fact]
        public void NewPeak_ReportsTheValueAndTheIncrease()
        {
            var reporter = new ResourceDiagnosticsReporter();
            reporter.Observe(Sample(userObjects: 500));

            var lines = reporter.Observe(Sample(userObjects: 530));

            lines.ShouldHaveSingleItem();
            lines[0].ShouldContain("USER objects");
            lines[0].ShouldContain("530");
            lines[0].ShouldContain("30");
        }

        [Fact]
        public void EachCounter_IsTrackedSeparately()
        {
            var reporter = new ResourceDiagnosticsReporter();
            reporter.Observe(Sample());

            var lines = reporter.Observe(Sample(userObjects: 101, gdiObjects: 101, handles: 101, threads: 11));

            lines.Count.ShouldBe(4);
            lines.ShouldContain(x => x.Contains("USER objects"));
            lines.ShouldContain(x => x.Contains("GDI objects"));
            lines.ShouldContain(x => x.Contains("handles"));
            lines.ShouldContain(x => x.Contains("threads"));
        }

        [Fact]
        public void ApproachingTheUserObjectLimit_WarnsOncePerThreshold()
        {
            var reporter = new ResourceDiagnosticsReporter();
            reporter.Observe(Sample(userObjects: 100));

            var atHalf = reporter.Observe(Sample(userObjects: ResourceDiagnosticsReporter.UserObjectLimit / 2));
            atHalf.ShouldContain(x => x.Contains("50%"));

            // Still over half, but the 50% warning has already been given.
            var stillOverHalf = reporter.Observe(Sample(userObjects: ResourceDiagnosticsReporter.UserObjectLimit / 2 + 1));
            stillOverHalf.ShouldNotContain(x => x.Contains("50%"));
        }

        [Fact]
        public void UiThreadStall_IsWarnedAboutOnceItPassesTheThreshold()
        {
            var reporter = new ResourceDiagnosticsReporter { StallWarningMilliseconds = 2000 };
            reporter.Observe(Sample());

            reporter.Observe(Sample(uiStallMilliseconds: 1500)).ShouldBeEmpty();

            var lines = reporter.Observe(Sample(uiStallMilliseconds: 2500));
            lines.ShouldContain(x => x.Contains("UI thread has not run for 2500ms"));
        }

        /// <summary>
        /// A freeze lasting minutes must not produce a warning per sample.
        /// </summary>
        [Fact]
        public void OngoingStall_IsReportedOnlyOnEachDoubling()
        {
            var reporter = new ResourceDiagnosticsReporter { StallWarningMilliseconds = 2000 };
            reporter.Observe(Sample());

            reporter.Observe(Sample(uiStallMilliseconds: 2000)).ShouldNotBeEmpty();
            reporter.Observe(Sample(uiStallMilliseconds: 2500)).ShouldBeEmpty();
            reporter.Observe(Sample(uiStallMilliseconds: 3900)).ShouldBeEmpty();
            reporter.Observe(Sample(uiStallMilliseconds: 4000)).ShouldNotBeEmpty();
            reporter.Observe(Sample(uiStallMilliseconds: 7000)).ShouldBeEmpty();
            reporter.Observe(Sample(uiStallMilliseconds: 8000)).ShouldNotBeEmpty();
        }

        [Fact]
        public void RecoveringFromAStall_IsReported()
        {
            var reporter = new ResourceDiagnosticsReporter { StallWarningMilliseconds = 2000 };
            reporter.Observe(Sample());
            reporter.Observe(Sample(uiStallMilliseconds: 5000));

            var lines = reporter.Observe(Sample(uiStallMilliseconds: 10));

            lines.ShouldContain(x => x.Contains("responded again after 5000ms"));
        }

        [Fact]
        public void RecoveryIsReportedOnlyOnce()
        {
            var reporter = new ResourceDiagnosticsReporter { StallWarningMilliseconds = 2000 };
            reporter.Observe(Sample());
            reporter.Observe(Sample(uiStallMilliseconds: 5000));
            reporter.Observe(Sample(uiStallMilliseconds: 10));

            reporter.Observe(Sample(uiStallMilliseconds: 10)).ShouldBeEmpty();
        }

        [Fact]
        public void FormatForFile_WritesEverySampleAsOneParseableLine()
        {
            var line = ResourceDiagnosticsReporter.FormatForFile(
                new DateTime(2026, 8, 8, 13, 5, 6, 700, DateTimeKind.Utc),
                Sample(userObjects: 1, gdiObjects: 2, handles: 3, threads: 4, uiStallMilliseconds: 5));

            line.ShouldBe("2026-08-08 13:05:06.700\tuser=1\tgdi=2\thandles=3\tthreads=4\tuiStallMs=5");
        }
    }
}
