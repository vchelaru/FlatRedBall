using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Content.AnimationChain;

namespace FlatRedBall.Graphics.Animation
{
    /// <summary>One step from a sequence into a sequence one clip of its reference track plays.</summary>
    public sealed class ReferenceHop
    {
        public ReferenceHop(SequenceSave host, SequenceTrackSave track, SequenceReferenceSave reference, SequenceSave target)
        {
            Host = host;
            Track = track;
            Reference = reference;
            Target = target;
        }

        /// <summary>The sequence holding <see cref="Track"/>.</summary>
        public SequenceSave Host { get; }

        /// <summary>The reference track holding the clip. Its other clips cut this one.</summary>
        public SequenceTrackSave Track { get; }

        /// <summary>The reference clip.</summary>
        public SequenceReferenceSave Reference { get; }

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
    /// The rules for tracks that play other sequences (<see cref="SequenceTrackSave.References"/>): cycle detection,
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
                if (track.References == null)
                {
                    continue;
                }
                foreach (SequenceReferenceSave reference in track.References)
                {
                    SequenceSave target = Find(sequences, reference.Sequence);
                    List<string> cycle = target == null ? null : Visit(sequences, target, stack, done);
                    if (cycle != null)
                    {
                        return cycle;
                    }
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
            return sequence.Tracks.Any(track => track.References != null
                && track.References.Any(reference => Reaches(sequences, host, reference.Sequence, seen)));
        }

        /// <summary>
        /// Whether <paramref name="host"/>'s reference to <paramref name="target"/> plays: false when no sequence has the
        /// target's name, or when the target plays the host back (a cycle), which a hand-edited file can hold. A reference
        /// that does not play adds nothing wherever it is inlined, sampled or drawn, but still cuts the clip before it.
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
        /// tracks of each clip's target (recursively, see <see cref="InlinedTrack.Name"/>), clips in playback order. Clips of
        /// one track share the name prefix, so the result can hold several tracks of one name, each reached through a
        /// different clip; the cut rule lets only one of them play at a time, and the player shows them on one sprite. A clip
        /// that does not <see cref="Plays"/> (a missing target, or one that plays its holder back) adds nothing.
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
                if (track.References == null)
                {
                    result.Add(new InlinedTrack(prefix + track.Name, track, visible && track.Visible, hops.ToArray()));
                    continue;
                }
                foreach (SequenceReferenceSave reference in SequenceTimeline.OrderedClips(track))
                {
                    // The stack check is a backstop for duplicate names, which Plays (by name) cannot tell apart.
                    SequenceSave target = Find(sequences, reference.Sequence);
                    if (!Plays(sequences, sequence.Name, reference.Sequence) || target == null || stack.Contains(target))
                    {
                        continue;
                    }
                    hops.Add(new ReferenceHop(sequence, track, reference, target));
                    stack.Add(target);
                    Collect(sequences, target, prefix + track.Name + "/", visible && track.Visible, hops, stack, result);
                    stack.RemoveAt(stack.Count - 1);
                    hops.RemoveAt(hops.Count - 1);
                }
            }
        }

        /// <summary>
        /// The time to sample <paramref name="track"/>'s own sequence at when the outermost sequence is at
        /// <paramref name="time"/>: the time mapped through each hop (<see cref="SequenceTimeline.ReferenceTime"/>, the next
        /// clip on the hop's track cutting it), or null when any clip on the way is not playing. A track of the outermost
        /// sequence returns <paramref name="time"/> unchanged.
        /// </summary>
        /// <param name="sequences">Every sequence of the file, to resolve the other clips on each hop's track.</param>
        /// <param name="track">A track from <see cref="Inline"/>.</param>
        /// <param name="time">Seconds on the outermost sequence's clock.</param>
        /// <param name="length">A sequence's length in seconds (<see cref="SequenceTimeline.Length(IReadOnlyList{SequenceSave}, SequenceSave, Func{SequenceChainReferenceSave, double})"/>).</param>
        public static double? InlinedTime(IReadOnlyList<SequenceSave> sequences, InlinedTrack track, double time, Func<SequenceSave, double> length)
        {
            return InlinedTime(sequences, track, time, 0, 0, length, out _);
        }

        /// <summary>
        /// <see cref="InlinedTime(IReadOnlyList{SequenceSave}, InlinedTrack, double, Func{SequenceSave, double})"/> for pass
        /// <paramref name="pass"/> of the outermost sequence, <paramref name="sequenceLength"/> seconds long, where
        /// <paramref name="time"/> is the time inside that pass. Each hop maps the time and pass through
        /// <see cref="SequenceTimeline.ReferenceTimeAtPass"/>, so a continuous reference track carries its first clip's target
        /// across the wrap. <paramref name="innerPass"/> is the pass of <paramref name="track"/>'s own sequence, for its
        /// continuous chain tracks; 0 when any hop on the way restarts.
        /// </summary>
        public static double? InlinedTime(IReadOnlyList<SequenceSave> sequences, InlinedTrack track, double time, int pass,
            double sequenceLength, Func<SequenceSave, double> length, out int innerPass)
        {
            double? current = time;
            innerPass = pass;
            foreach (ReferenceHop hop in track.Hops)
            {
                double targetLength = length(hop.Target);
                current = SequenceTimeline.ReferenceTimeAtPass(hop.Track, hop.Reference, hop.Target, targetLength, current.Value, innerPass,
                    sequenceLength, clip => ClipTargetLength(sequences, hop.Host, clip, length), out innerPass);
                if (current == null)
                {
                    innerPass = 0;
                    return null;
                }
                sequenceLength = targetLength;
            }
            return current;
        }

        // A clip's target length, or 0 when the clip does not play.
        private static double ClipTargetLength(IReadOnlyList<SequenceSave> sequences, SequenceSave host, SequenceReferenceSave clip,
            Func<SequenceSave, double> length)
        {
            SequenceSave target = Find(sequences, clip.Sequence);
            return target != null && Plays(sequences, host.Name, clip.Sequence) ? length(target) : 0;
        }
    }
}
