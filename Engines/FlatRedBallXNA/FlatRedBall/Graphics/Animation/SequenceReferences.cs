using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Content.AnimationChain;

namespace FlatRedBall.Graphics.Animation
{
    /// <summary>One step from a sequence into a sequence its reference track plays.</summary>
    public sealed class ReferenceHop
    {
        public ReferenceHop(SequenceTrackSave track, SequenceSave target)
        {
            Track = track;
            Target = target;
        }

        /// <summary>The reference track holding the clip.</summary>
        public SequenceTrackSave Track { get; }

        /// <summary>The reference clip.</summary>
        public SequenceReferenceSave Reference => Track.Reference;

        /// <summary>The sequence it plays.</summary>
        public SequenceSave Target { get; }
    }

    /// <summary>A chain track of a sequence with the reference tracks flattened away (<see cref="SequenceReferences.Inline"/>).</summary>
    public sealed class InlinedTrack
    {
        public InlinedTrack(string name, SequenceTrackSave track, bool visible, IReadOnlyList<ReferenceHop> hops)
        {
            Name = name;
            Track = track;
            Visible = visible;
            Hops = hops;
        }

        /// <summary>
        /// The track's name. Tracks inherited through a reference are prefixed with the reference track's name and <c>/</c>
        /// (<c>Earth/Body</c>, <c>Earth/Dust/Body</c> when nested), so they cannot collide with the holding sequence's own tracks.
        /// </summary>
        public string Name { get; }

        /// <summary>The chain track as written in its own sequence.</summary>
        public SequenceTrackSave Track { get; }

        /// <summary>Whether the track and every reference track on its way in are visible.</summary>
        public bool Visible { get; }

        /// <summary>The reference clips between the holding sequence and <see cref="Track"/>, outermost first; empty for the sequence's own track.</summary>
        public IReadOnlyList<ReferenceHop> Hops { get; }
    }

    /// <summary>
    /// The rules for tracks that play another sequence (<see cref="SequenceTrackSave.Reference"/>): cycle detection,
    /// flattening into chain tracks, and the time mapping into the target. A reference is resolved by sequence name within
    /// the one file; the first sequence with the name wins. A port of the FlatRedBall2 AnimationEditor's SequenceReferences,
    /// so both engines agree.
    /// </summary>
    public static class SequenceReferences
    {
        /// <summary>
        /// A cycle among the sequences' references as the names along it, ending where it started (<c>A, B, A</c>), or null
        /// when there is none. A reference to a missing sequence is not a cycle.
        /// </summary>
        public static IReadOnlyList<string> FindCycle(IReadOnlyList<SequenceSave> sequences)
        {
            HashSet<SequenceSave> done = new HashSet<SequenceSave>();
            foreach (SequenceSave sequence in sequences)
            {
                List<string> cycle = Visit(sequences, sequence, new List<SequenceSave>(), done);
                if (cycle != null)
                {
                    return cycle;
                }
            }
            return null;
        }

        private static List<string> Visit(IReadOnlyList<SequenceSave> sequences, SequenceSave sequence, List<SequenceSave> stack, HashSet<SequenceSave> done)
        {
            int at = stack.IndexOf(sequence);
            if (at >= 0)
            {
                return stack.Skip(at).Concat(new[] { sequence }).Select(s => s.Name).ToList();
            }
            if (!done.Add(sequence))
            {
                return null;
            }
            stack.Add(sequence);
            foreach (SequenceTrackSave track in sequence.Tracks)
            {
                SequenceSave target = track.Reference == null ? null : Find(sequences, track.Reference.Sequence);
                List<string> cycle = target == null ? null : Visit(sequences, target, stack, done);
                if (cycle != null)
                {
                    return cycle;
                }
            }
            stack.RemoveAt(stack.Count - 1);
            return null;
        }

        /// <summary>
        /// Whether giving <paramref name="host"/> a reference to <paramref name="target"/> would close a cycle: the target is
        /// the host or, through its own references, plays it.
        /// </summary>
        public static bool WouldCycle(IReadOnlyList<SequenceSave> sequences, string host, string target)
        {
            if (host == target)
            {
                return true;
            }
            HashSet<string> seen = new HashSet<string>();
            return Reaches(sequences, host, target, seen);
        }

        private static bool Reaches(IReadOnlyList<SequenceSave> sequences, string host, string from, HashSet<string> seen)
        {
            if (from == host)
            {
                return true;
            }
            SequenceSave sequence = seen.Add(from) ? Find(sequences, from) : null;
            if (sequence == null)
            {
                return false;
            }
            return sequence.Tracks.Any(track => track.Reference != null && Reaches(sequences, host, track.Reference.Sequence, seen));
        }

        /// <summary>
        /// Whether <paramref name="host"/>'s reference to <paramref name="target"/> plays: false when no sequence has the
        /// target's name, or when the target plays the host back (a cycle), which a hand-edited file can hold. A reference
        /// that does not play adds nothing wherever it is inlined, sampled or drawn.
        /// </summary>
        public static bool Plays(IReadOnlyList<SequenceSave> sequences, string host, string target)
        {
            return Find(sequences, target) != null && !WouldCycle(sequences, host, target);
        }

        /// <summary>The first sequence called <paramref name="name"/>, or null.</summary>
        public static SequenceSave Find(IReadOnlyList<SequenceSave> sequences, string name)
        {
            return sequences.FirstOrDefault(sequence => sequence.Name == name);
        }

        /// <summary>
        /// Every chain track <paramref name="sequence"/> plays, in track order, with each reference track replaced by the
        /// tracks of its target (recursively, see <see cref="InlinedTrack.Name"/>). A reference that does not
        /// <see cref="Plays"/> (a missing target, or one that plays its holder back) adds nothing.
        /// </summary>
        public static IReadOnlyList<InlinedTrack> Inline(IReadOnlyList<SequenceSave> sequences, SequenceSave sequence)
        {
            List<InlinedTrack> result = new List<InlinedTrack>();
            Collect(sequences, sequence, "", true, new List<ReferenceHop>(), new List<SequenceSave> { sequence }, result);
            return result;
        }

        private static void Collect(IReadOnlyList<SequenceSave> sequences, SequenceSave sequence, string prefix, bool visible,
            List<ReferenceHop> hops, List<SequenceSave> stack, List<InlinedTrack> result)
        {
            foreach (SequenceTrackSave track in sequence.Tracks)
            {
                SequenceReferenceSave reference = track.Reference;
                if (reference == null)
                {
                    result.Add(new InlinedTrack(prefix + track.Name, track, visible && track.Visible, hops.ToArray()));
                    continue;
                }
                // The stack check is a backstop for duplicate names, which Plays (by name) cannot tell apart.
                SequenceSave target = Find(sequences, reference.Sequence);
                if (!Plays(sequences, sequence.Name, reference.Sequence) || target == null || stack.Contains(target))
                {
                    continue;
                }
                hops.Add(new ReferenceHop(track, target));
                stack.Add(target);
                Collect(sequences, target, prefix + track.Name + "/", visible && track.Visible, hops, stack, result);
                stack.RemoveAt(stack.Count - 1);
                hops.RemoveAt(hops.Count - 1);
            }
        }

        /// <summary>
        /// The time to sample <paramref name="track"/>'s own sequence at when the outermost sequence is at
        /// <paramref name="time"/>: the time mapped through each hop (<see cref="SequenceTimeline.ReferenceTime"/>), or null
        /// when any clip on the way is not playing. A track of the outermost sequence returns <paramref name="time"/> unchanged.
        /// </summary>
        /// <param name="track">A track from <see cref="Inline"/>.</param>
        /// <param name="time">Seconds on the outermost sequence's clock.</param>
        /// <param name="length">A sequence's length in seconds (<see cref="SequenceTimeline.Length(IReadOnlyList{SequenceSave}, SequenceSave, Func{SequenceChainReferenceSave, double})"/>).</param>
        public static double? InlinedTime(InlinedTrack track, double time, Func<SequenceSave, double> length)
        {
            return InlinedTime(track, time, 0, 0, length, out _);
        }

        /// <summary>
        /// <see cref="InlinedTime(InlinedTrack, double, Func{SequenceSave, double})"/> for pass <paramref name="pass"/> of the
        /// outermost sequence, <paramref name="sequenceLength"/> seconds long, where <paramref name="time"/> is the time inside
        /// that pass. Each hop maps the time and pass through <see cref="SequenceTimeline.ReferenceTimeAtPass"/>, so a
        /// continuous reference track carries its target across the wrap. <paramref name="innerPass"/> is the pass of
        /// <paramref name="track"/>'s own sequence, for its continuous chain tracks; 0 when any hop on the way restarts.
        /// </summary>
        public static double? InlinedTime(InlinedTrack track, double time, int pass, double sequenceLength,
            Func<SequenceSave, double> length, out int innerPass)
        {
            double? current = time;
            innerPass = pass;
            foreach (ReferenceHop hop in track.Hops)
            {
                double targetLength = length(hop.Target);
                current = SequenceTimeline.ReferenceTimeAtPass(hop.Track, hop.Target, targetLength, current.Value, innerPass, sequenceLength, out innerPass);
                if (current == null)
                {
                    innerPass = 0;
                    return null;
                }
                sequenceLength = targetLength;
            }
            return current;
        }
    }
}
