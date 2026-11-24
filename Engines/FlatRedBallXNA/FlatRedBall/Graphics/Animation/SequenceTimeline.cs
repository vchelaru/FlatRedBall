using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Content.AnimationChain;

namespace FlatRedBall.Graphics.Animation
{
    /// <summary>
    /// The timing rules of a sequence, matching the FlatRedBall2 AnimationEditor's SequenceTimeline so both agree.
    /// <list type="bullet">
    /// <item>A chain reference lasts its Length (one cycle of the chain when unset), or until the next chain on its track starts, whichever is first.
    /// The cut part never plays and is not resumed.</item>
    /// <item>The next chain is the one with the next later start. On a tie, list order decides: of two chains starting
    /// together, the earlier one in the track's list plays for zero seconds.</item>
    /// <item>A missing chain (cycle length 0) never plays, but it still cuts the chain before it.</item>
    /// <item>Outside every chain, the track's sprite is hidden, including before the first chain starts.</item>
    /// <item>A <see cref="SequenceTrackSave.Continuous"/> reference track carries its target's phase across the wrap of a looping holder (<see cref="CarriesAcrossWrap"/>): the target keeps running instead of restarting.</item>
    /// </list>
    /// Cycle lengths come from the caller, so this class needs no chain data.
    /// </summary>
    public static class SequenceTimeline
    {
        /// <summary>
        /// Authored length in seconds: length when set, else one cycleLength. Never negative, and 0 when cycleLength is not
        /// positive (a missing chain or target never plays).
        /// </summary>
        public static double FullLength(double? length, double cycleLength)
        {
            return cycleLength > 0 ? System.Math.Max(0, length ?? cycleLength) : 0;
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
            return PlayedLength(reference.Start, FullLength(reference.Length, cycleLength), NextStart(track, reference));
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
                double played = PlayedLength(reference.Start, FullLength(reference.Length, cycleLength(reference)), next);
                if (time >= reference.Start + played)
                {
                    return null;
                }

                localTime = time - reference.Start;
                return reference;
            }
            return null;
        }

        /// <summary>
        /// Seconds of playback a <see cref="SequenceTrackSave.Continuous"/> track's first block has already advanced when
        /// pass <paramref name="pass"/> (0 = the first run through, 1 = after the first wrap) begins: 0 for pass 0 and for a
        /// track that restarts at the wrap. At each wrap the carry becomes the playback time of the block playing at the wrap
        /// (sequenceLength minus its start, plus its own carry when it is the first block), if that block is the same chain
        /// (source and name) as the first block and the first block starts at 0; otherwise 0.
        /// </summary>
        public static double CarryOver(SequenceTrackSave track, double sequenceLength, int pass,
            Func<SequenceChainReferenceSave, double> cycleLength)
        {
            if (!track.Continuous || pass <= 0 || sequenceLength <= 0 || track.Chains.Count == 0)
            {
                return 0;
            }
            SequenceChainReferenceSave first = Ordered(track)[0];
            if (first.Start > Slack(0))
            {
                return 0;
            }

            // The block playing just before the end of a pass: the wrap instant itself belongs to the next pass.
            double endTime = sequenceLength - System.Math.Min(HoldEpsilon, sequenceLength);
            double carry = 0;
            for (int i = 0; i < pass; i++)
            {
                SequenceChainReferenceSave atWrap = ActiveAt(track, endTime, cycleLength, out _);
                if (atWrap == null || atWrap.Source != first.Source || atWrap.Chain != first.Chain)
                {
                    carry = 0;
                    continue;
                }
                carry = (ReferenceEquals(atWrap, first) ? carry : 0) + (sequenceLength - atWrap.Start);
            }
            return carry;
        }

        /// <summary>
        /// <see cref="ActiveAt(SequenceTrackSave, double, Func{SequenceChainReferenceSave, double}, out double)"/> for pass
        /// <paramref name="pass"/> of a looping sequence, where <paramref name="time"/> is the time inside that pass. A
        /// <see cref="SequenceTrackSave.Continuous"/> track's first block does not restart at the wrap: its
        /// <paramref name="localTime"/> includes <see cref="CarryOver"/>. Every other block and track is exactly ActiveAt.
        /// </summary>
        public static SequenceChainReferenceSave ActiveAtPass(SequenceTrackSave track, double time, int pass, double sequenceLength,
            Func<SequenceChainReferenceSave, double> cycleLength, out double localTime)
        {
            SequenceChainReferenceSave active = ActiveAt(track, time, cycleLength, out localTime);
            if (active != null && track.Continuous && pass > 0 && ReferenceEquals(active, Ordered(track)[0]))
            {
                localTime += CarryOver(track, sequenceLength, pass, cycleLength);
            }
            return active;
        }

        /// <summary>
        /// Seconds until a sequence with no reference tracks ends. A reference track adds nothing here, since its target
        /// can't be resolved without the rest of the file; use the overload that takes every sequence.
        /// </summary>
        public static double Length(SequenceSave sequence, Func<SequenceChainReferenceSave, double> cycleLength)
        {
            return Length(new[] { sequence }, sequence, cycleLength);
        }

        /// <summary>
        /// Seconds until the sequence ends: the latest end of any clip on any track. A chain ends where it stops playing; a
        /// reference clip ends its <see cref="SequenceReferenceSave.Length"/> (one run of its target when unset) after its start,
        /// measured the same way, through every level. A reference that does not <see cref="SequenceReferences.Plays"/> (a
        /// missing target or a cycle) adds nothing. 0 when nothing plays.
        /// </summary>
        /// <param name="sequences">Every sequence of the file, to resolve references by name.</param>
        /// <param name="sequence">The sequence to measure.</param>
        /// <param name="cycleLength">A chain reference's cycle length in seconds; 0 for a missing chain.</param>
        public static double Length(IReadOnlyList<SequenceSave> sequences, SequenceSave sequence, Func<SequenceChainReferenceSave, double> cycleLength)
        {
            return LengthWithin(sequences, sequence, cycleLength, new List<SequenceSave> { sequence });
        }

        private static double LengthWithin(IReadOnlyList<SequenceSave> sequences, SequenceSave sequence,
            Func<SequenceChainReferenceSave, double> cycleLength, List<SequenceSave> stack)
        {
            double end = 0;
            foreach (SequenceTrackSave track in sequence.Tracks)
            {
                foreach (SequenceChainReferenceSave reference in track.Chains)
                {
                    end = System.Math.Max(end, reference.Start + PlayedLength(track, reference, cycleLength(reference)));
                }

                // The stack check is a backstop for duplicate names, which Plays (by name) cannot tell apart.
                SequenceReferenceSave clip = track.Reference;
                if (clip == null || !SequenceReferences.Plays(sequences, sequence.Name, clip.Sequence))
                {
                    continue;
                }
                SequenceSave target = SequenceReferences.Find(sequences, clip.Sequence);
                if (target == null || stack.Contains(target))
                {
                    continue;
                }
                stack.Add(target);
                double targetLength = LengthWithin(sequences, target, cycleLength, stack);
                stack.RemoveAt(stack.Count - 1);
                end = System.Math.Max(end, clip.Start + FullLength(clip.Length, targetLength));
            }
            return end;
        }

        /// <summary>
        /// Slack in seconds (at least this, and proportional to the time beyond 1 s) when comparing a time to a clip's start
        /// or end. A start is inclusive and an end exclusive, within this slack.
        /// </summary>
        public const double BoundaryTolerance = 1e-6;

        // Float error in a summed clip end grows with the number of cycles, so the slack scales with the time.
        private static double Slack(double time)
        {
            return BoundaryTolerance * System.Math.Max(1, System.Math.Abs(time));
        }

        /// <summary>How far before the end of a non-looping sequence its held final pose is sampled, in seconds.</summary>
        public const double HoldEpsilon = 1e-4;

        /// <summary>
        /// The time to sample a sequence's pose at for a playhead at <paramref name="time"/>. A sequence with
        /// <paramref name="loop"/> off holds its final pose: at or past <paramref name="length"/> the pose is the one at the
        /// last instant before the end (<see cref="HoldEpsilon"/> earlier), so a track whose chains already finished stays
        /// hidden and the rest keep their last frame. Otherwise, and for an empty sequence, the time is unchanged.
        /// </summary>
        public static double PoseTime(double time, double length, bool loop)
        {
            return !loop && length > 0 && time >= length ? length - System.Math.Min(HoldEpsilon, length) : time;
        }

        /// <summary>
        /// The time to sample <paramref name="target"/> at when the sequence holding <paramref name="reference"/> is at
        /// <paramref name="parentTime"/>, or null when the clip is not playing: before its start, after
        /// its <see cref="SequenceReferenceSave.Length"/> (one <paramref name="targetLength"/> when unset), or when the target has no
        /// length. The target restarts with the holder (its clock is the holder's minus the clip start). A looping target
        /// wraps at its length; one with Loop off plays once and then holds its final pose (<see cref="PoseTime"/>) until the
        /// clip ends.
        /// </summary>
        public static double? ReferenceTime(SequenceReferenceSave reference, SequenceSave target, double targetLength, double parentTime)
        {
            if (targetLength <= 0)
            {
                return null;
            }
            if (reference.Start > parentTime + Slack(parentTime))
            {
                return null;
            }
            double local = System.Math.Max(0, parentTime - reference.Start);
            if (parentTime + Slack(parentTime) >= reference.Start + FullLength(reference.Length, targetLength))
            {
                return null;
            }
            return PoseTime(WrapTime(local, targetLength, target.Loop), targetLength, target.Loop);
        }

        /// <summary>
        /// Whether a <see cref="SequenceTrackSave.Continuous"/> reference track carries its target across the wrap of a looping
        /// holder of <paramref name="sequenceLength"/> seconds. All of these must hold, otherwise the flag is ignored and the
        /// target restarts at each wrap: the track is continuous, the clip starts at 0, the target loops
        /// (<see cref="SequenceSave.Loop"/>), and the clip is still playing at the wrap: it ends (its start plus
        /// <see cref="SequenceReferenceSave.Length"/>, or one <paramref name="targetLength"/> when unset) no earlier than the
        /// holder's length. A clip of the default length therefore carries only when the target is as long as the holder; to
        /// carry a shorter looping target, set the clip's length to the holder's length.
        /// </summary>
        public static bool CarriesAcrossWrap(SequenceTrackSave track, SequenceSave target, double targetLength, double sequenceLength)
        {
            SequenceReferenceSave clip = track.Reference;
            if (!track.Continuous || clip == null || sequenceLength <= 0 || targetLength <= 0 || !target.Loop)
            {
                return false;
            }
            if (clip.Start > Slack(0))
            {
                return false;
            }
            return FullLength(clip.Length, targetLength) >= sequenceLength - Slack(sequenceLength);
        }

        /// <summary>
        /// <see cref="ReferenceTime"/> for pass <paramref name="pass"/> of a looping holder of <paramref name="sequenceLength"/>
        /// seconds (0 = first run through, 1 = after the first wrap, counted by the player), where <paramref name="parentTime"/>
        /// is the time inside that pass. The clip's start and end are checked against that in-pass time, as in pass 0. When the
        /// track <see cref="CarriesAcrossWrap"/>, the target's clock does not restart at the wrap: it is
        /// <c>pass * sequenceLength + parentTime</c> (the clip starts at 0), so the target keeps its phase. The target is then a
        /// looping sequence playing continuously: its own wraps count as passes, returned in <paramref name="targetPass"/> and
        /// used for the target's own continuous tracks (chain or reference), and the returned time is inside the current one.
        /// Otherwise <paramref name="targetPass"/> is 0 and this is exactly <see cref="ReferenceTime"/>: a played sequence runs
        /// at pass 0. Null when the clip is not playing or the track has no reference.
        /// </summary>
        public static double? ReferenceTimeAtPass(SequenceTrackSave track, SequenceSave target, double targetLength, double parentTime,
            int pass, double sequenceLength, out int targetPass)
        {
            targetPass = 0;
            SequenceReferenceSave clip = track.Reference;
            if (clip == null)
            {
                return null;
            }
            double? plain = ReferenceTime(clip, target, targetLength, parentTime);
            if (plain == null || !CarriesAcrossWrap(track, target, targetLength, sequenceLength))
            {
                return plain;
            }

            double elapsed = System.Math.Max(0, pass) * sequenceLength + parentTime;
            targetPass = (int)(elapsed / targetLength);
            return elapsed % targetLength;
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
