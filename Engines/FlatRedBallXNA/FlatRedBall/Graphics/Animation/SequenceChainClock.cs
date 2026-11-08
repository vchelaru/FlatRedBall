using System;

namespace FlatRedBall.Graphics.Animation
{
    /// <summary>
    /// Which frame a chain shows a given time after its block starts. A looping chain wraps; a chain with Loop off plays
    /// one cycle and holds its end. Ping-pong and reverse follow the chain's own playback rules. Matches the
    /// FlatRedBall2 AnimationEditor's ChainClock.
    /// </summary>
    public static class SequenceChainClock
    {
        // Frame lengths are floats, so a boundary like 0.3 s sums to a hair off in double. Nudging the sample forward
        // keeps a time that sits on a boundary in the later frame.
        private const double BoundaryTolerance = 1e-6;

        /// <summary>Seconds of one playback cycle: the sum of the chain's frames, twice that for ping-pong. 0 for an empty chain.</summary>
        public static double CycleLength(AnimationChain chain)
        {
            return chain.PlaybackLength;
        }

        /// <summary>
        /// How many showings of a frame one cycle has, in playback order ("visits"): one per frame, two under ping-pong
        /// (once each way). Frame events are raised per visit.
        /// </summary>
        public static int VisitCount(AnimationChain chain)
        {
            return chain.PingPong ? 2 * chain.Count : chain.Count;
        }

        /// <summary>The visit being shown cycleTime seconds into the first cycle. Time past the cycle gives the last visit.</summary>
        public static int VisitAt(AnimationChain chain, double cycleTime)
        {
            int count = chain.Count;
            double local = cycleTime + BoundaryTolerance;
            if (local >= CycleLength(chain))
            {
                return VisitCount(chain) - 1;
            }

            chain.GetPlaybackFrame(local, out int frame, out _);
            bool backward = chain.Reverse != (chain.PingPong && local >= chain.TotalLength);
            bool firstHalf = !chain.PingPong || (chain.Reverse ? backward : !backward);
            int visit = backward ? count - 1 - frame : frame;
            return firstHalf ? visit : visit + count;
        }

        /// <summary>The frame of this visit, in the order <see cref="VisitAt"/> numbers them.</summary>
        public static int FrameOfVisit(AnimationChain chain, int visit)
        {
            int count = chain.Count;
            if (!chain.PingPong)
            {
                return chain.Reverse ? count - 1 - visit : visit;
            }
            if (!chain.Reverse)
            {
                return visit < count ? visit : 2 * count - 1 - visit;
            }
            return visit < count ? count - 1 - visit : visit - count;
        }

        /// <summary>
        /// Whether reaching this visit enters a frame, so its events are raised. Always for a chain that is not a ping-pong
        /// (a plain loop re-enters its first frame on each wrap); for a ping-pong, only when the shown frame changes, so the
        /// turnaround and the loop point raise nothing.
        /// </summary>
        public static bool EntersNewFrame(AnimationChain chain, int visit)
        {
            if (!chain.PingPong)
            {
                return true;
            }
            int visits = VisitCount(chain);
            return FrameOfVisit(chain, visit) != FrameOfVisit(chain, (visit - 1 + visits) % visits);
        }

        /// <summary>
        /// The time into the chain's playback cycle to give a Sprite for a moment time seconds after the block starts:
        /// wrapped when the chain loops, held just short of the cycle's end when it does not. The Sprite's own frame
        /// lookup then lands on the frame <see cref="Locate"/> reports.
        /// </summary>
        public static double TimeIntoCycle(AnimationChain chain, double time)
        {
            double total = CycleLength(chain);
            double local = time + BoundaryTolerance;
            if (local >= total)
            {
                local = chain.Loop ? local % total : total - BoundaryTolerance;
            }
            return local;
        }

        /// <summary>
        /// The frame showing time seconds after the block starts, and the forward seconds into that frame to sample
        /// keyframes at. The chain must have at least one frame.
        /// </summary>
        public static (int Frame, double Elapsed) Locate(AnimationChain chain, double time)
        {
            double total = CycleLength(chain);
            double local = time + BoundaryTolerance;
            bool atEnd = local >= total && !chain.Loop;
            if (local >= total)
            {
                local = chain.Loop ? local % total : total;
            }

            chain.GetPlaybackFrame(local, out int frame, out double elapsed);

            // GetPlaybackFrame reports forward elapsed time; undo the nudge in the direction the timeline plays.
            bool backward = chain.Reverse != (chain.PingPong && local >= chain.TotalLength);
            if (!atEnd)
            {
                elapsed += backward ? BoundaryTolerance : -BoundaryTolerance;
            }
            return (frame, System.Math.Min(System.Math.Max(elapsed, 0), chain[frame].FrameLength));
        }
    }
}
