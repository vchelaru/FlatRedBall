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
