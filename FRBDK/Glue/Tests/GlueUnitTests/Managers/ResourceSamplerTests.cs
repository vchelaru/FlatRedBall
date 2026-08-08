using CompilerLibrary.Diagnostics;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Managers
{
    /// <summary>
    /// The reporting rules are covered by <see cref="ResourceDiagnosticsReporterTests"/>. This only
    /// checks that the actual GetGuiResources P/Invoke and process counters return usable numbers, since
    /// a wrong signature or flag would silently report zeros forever and look like "nothing is leaking".
    /// </summary>
    public class ResourceSamplerTests
    {
        [Fact]
        public void Take_ReturnsRealCountersForThisProcess()
        {
            var sample = ResourceSampler.Take(uiThreadStallMilliseconds: 1234);

            sample.HandleCount.ShouldBeGreaterThan(0);
            sample.ThreadCount.ShouldBeGreaterThan(0);

            // A test host is not a windowed app, so these can legitimately be low, but a negative value
            // means the P/Invoke failed rather than the process being frugal.
            sample.UserObjectCount.ShouldBeGreaterThanOrEqualTo(0);
            sample.GdiObjectCount.ShouldBeGreaterThanOrEqualTo(0);

            sample.UiThreadStallMilliseconds.ShouldBe(1234);
        }

        [Fact]
        public void Take_DoesNotLeakHandlesEachTimeItIsCalled()
        {
            var first = ResourceSampler.Take(0);

            for (int i = 0; i < 200; i++)
            {
                ResourceSampler.Take(0);
            }

            var last = ResourceSampler.Take(0);

            // Sampling opens a Process object every call. If it were not disposed, 200 calls would show
            // up plainly here - which would make the leak detector itself a leak.
            (last.HandleCount - first.HandleCount).ShouldBeLessThan(100);
        }
    }
}
