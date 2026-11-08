using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Content.AnimationChain;

namespace FlatRedBall.Graphics.Animation
{
    /// <summary>
    /// The timing rules of a sequence, matching the FlatRedBall2 AnimationEditor's SequenceTimeline so both agree.
    /// <list type="bullet">
    /// <item>A chain reference lasts Loops cycles, or until the next chain on its track starts, whichever is first.
    /// The cut part never plays and is not resumed.</item>
    /// <item>The next chain is the one with the next later start. On a tie, list order decides: of two chains starting
    /// together, the earlier one in the track's list plays for zero seconds.</item>
    /// <item>A missing chain (cycle length 0) never plays, but it still cuts the chain before it.</item>
    /// <item>Outside every chain, the track's sprite is hidden, including before the first chain starts.</item>
    /// </list>
    /// Cycle lengths come from the caller, so this class needs no chain data.
    /// </summary>
    public static class SequenceTimeline
    {
        /// <summary>Authored length in seconds: loops cycles of cycleLength. Never negative.</summary>
        public static double FullLength(float loops, double cycleLength)
        {
            return System.Math.Max(0, loops) * cycleLength;
        }

        /// <summary>Seconds a chain actually plays: fullLength, cut where nextStart begins. Pass null when it is last on its track.</summary>
        public static double PlayedLength(double start, double fullLength, double? nextStart)
        {
            if (nextStart.HasValue)
            {
                return System.Math.Min(System.Math.Max(nextStart.Value - start, 0), fullLength);
            }
            return fullLength;
        }

        /// <summary>Seconds reference plays on track after the cut rule.</summary>
        public static double PlayedLength(SequenceTrackSave track, SequenceChainReferenceSave reference, double cycleLength)
        {
            return PlayedLength(reference.Start, FullLength(reference.Loops, cycleLength), NextStart(track, reference));
        }

        /// <summary>The track's references in playback order: by start, ties kept in list order.</summary>
        public static List<SequenceChainReferenceSave> Ordered(SequenceTrackSave track)
        {
            // OrderBy is stable, so ties keep list order.
            return track.Chains.OrderBy(reference => reference.Start).ToList();
        }

        /// <summary>When the chain after reference on track starts, or null when reference is last or not on the track.</summary>
        public static double? NextStart(SequenceTrackSave track, SequenceChainReferenceSave reference)
        {
            List<SequenceChainReferenceSave> ordered = Ordered(track);
            for (int i = 0; i < ordered.Count - 1; i++)
            {
                if (ReferenceEquals(ordered[i], reference))
                {
                    return ordered[i + 1].Start;
                }
            }
            return null;
        }

        /// <summary>The reference playing at time on track, or null when the sprite is hidden.</summary>
        /// <param name="track">The track to sample.</param>
        /// <param name="time">Seconds from the sequence start.</param>
        /// <param name="cycleLength">A reference's cycle length in seconds; 0 for a missing chain.</param>
        /// <param name="localTime">Seconds since the returned reference's start; 0 when none plays.</param>
        public static SequenceChainReferenceSave ActiveAt(SequenceTrackSave track, double time,
            Func<SequenceChainReferenceSave, double> cycleLength, out double localTime)
        {
            return ActiveAt(Ordered(track), time, cycleLength, out localTime);
        }

        /// <summary>Same as the track overload, for a list already in playback order (see <see cref="Ordered"/>), so a caller can cache it.</summary>
        public static SequenceChainReferenceSave ActiveAt(IList<SequenceChainReferenceSave> ordered, double time,
            Func<SequenceChainReferenceSave, double> cycleLength, out double localTime)
        {
            localTime = 0;
            // The latest chain that has started is the only candidate: any earlier one is cut at its start.
            for (int i = ordered.Count - 1; i >= 0; i--)
            {
                SequenceChainReferenceSave reference = ordered[i];
                if (reference.Start > time)
                {
                    continue;
                }

                double? next = i + 1 < ordered.Count ? ordered[i + 1].Start : (double?)null;
                double played = PlayedLength(reference.Start, FullLength(reference.Loops, cycleLength(reference)), next);
                if (time >= reference.Start + played)
                {
                    return null;
                }

                localTime = time - reference.Start;
                return reference;
            }
            return null;
        }

        /// <summary>Seconds until the sequence ends: the end of the last chain to finish playing, across all tracks. 0 when it has no chains.</summary>
        public static double Length(SequenceSave sequence, Func<SequenceChainReferenceSave, double> cycleLength)
        {
            double end = 0;
            foreach (SequenceTrackSave track in sequence.Tracks)
            {
                foreach (SequenceChainReferenceSave reference in track.Chains)
                {
                    end = System.Math.Max(end, reference.Start + PlayedLength(track, reference, cycleLength(reference)));
                }
            }
            return end;
        }

        /// <summary>The sequence time to show for an elapsed time: wrapped when loop, otherwise held at length. Negative time shows 0.</summary>
        public static double WrapTime(double time, double length, bool loop)
        {
            if (length <= 0)
            {
                return 0;
            }
            if (time < length)
            {
                return System.Math.Max(0, time);
            }
            return loop ? time % length : length;
        }
    }
}
