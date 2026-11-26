using System;
using System.Collections;
using System.Collections.Generic;
using FlatRedBall.Content.AnimationChain;

namespace FlatRedBall.Graphics.Animation
{
    /// <summary>
    /// One lane of a <see cref="SequenceInstance"/>: a Sprite (parented to a pivot that carries the track's offset) that
    /// shows the track's chains while the sequence plays. The Sprite exists as soon as the <see cref="SequencePlayer"/>
    /// does and stays the same object, so it can be handed to other code (a hit flash, for example) once.
    /// </summary>
    public class SequenceTrack
    {
        private readonly SequenceInstance mInstance;
        private bool mVisible;

        // variants: the inlined tracks of this name, one per clip on the way in (see SequenceReferences.Inline); a track of
        // the sequence itself has one. Only one plays at a time, so they share the Sprite.
        internal SequenceTrack(SequenceInstance instance, IReadOnlyList<InlinedTrack> variants, PositionedObject pivot, Sprite sprite)
        {
            mInstance = instance;
            Variants = variants;
            Pivot = pivot;
            Sprite = sprite;
            mVisible = variants[0].Visible;
            mOrdered = new List<SequenceChainReferenceSave>[variants.Count];
            UseVariant(0);
        }

        private readonly List<SequenceChainReferenceSave>[] mOrdered;
        private int mVariant = -1;

        internal IReadOnlyList<InlinedTrack> Variants { get; }

        // Points the track at one variant: its chains, offset and flip.
        internal void UseVariant(int index)
        {
            if (index == mVariant)
            {
                return;
            }
            mVariant = index;
            Inlined = Variants[index];
            if (mOrdered[index] == null)
            {
                mOrdered[index] = SequenceTimeline.Ordered(Inlined.Track);
            }
            Ordered = mOrdered[index];
            if (Pivot != null)
            {
                Pivot.RelativeX = Inlined.Track.X;
                Pivot.RelativeY = Inlined.Track.Y;
                // A flipped track applies frame flips itself, mirrored across the track.
                Sprite.UseAnimationTextureFlip = !Inlined.Track.FlipHorizontal;
            }
            // The blocks of the old variant mean nothing on the new one.
            EventReference = null;
            EventVisit = -1;
            ActiveReference = null;
            LastClock = null;
        }

        /// <summary>
        /// The track's name. A track played through a reference track is named after it and <c>/</c>
        /// (<c>Earth/Body</c>, <c>Earth/Dust/Body</c> when nested).
        /// </summary>
        public string Name => Inlined.Name;

        /// <summary>
        /// The track's data as written in its own sequence, which is the referenced one for a track played through a reference.
        /// When clips of a reference track play different sequences with a track of this name, it is the one last played.
        /// </summary>
        public SequenceTrackSave Save => Inlined.Track;

        /// <summary>True when the track is played through one or more reference tracks, so it runs on the referenced sequence's clock.</summary>
        public bool IsReferenced => Inlined.Hops.Count > 0;

        internal InlinedTrack Inlined { get; private set; }

        // The time on the track's own sequence clock at the last pass, or null when no clip on the way was playing.
        internal double? LastClock;

        /// <summary>True when the Sprite belongs to the entity (see <see cref="SequencePlayer"/>'s existingSprites). A bound track has no <see cref="Pivot"/>.</summary>
        public bool IsBound => Pivot == null;

        /// <summary>The object between the entity and the Sprite. It carries the track's X and Y offset. Null for a bound track.</summary>
        public PositionedObject Pivot { get; }

        /// <summary>The Sprite that shows this track. It is hidden whenever no chain of the track is playing.</summary>
        public Sprite Sprite { get; }

        /// <summary>
        /// Whether the track is on. It starts as the file's <c>visible</c> value (false when it or any reference track it is
        /// played through is hidden) and stays whatever it is set to. A track that is off shows nothing and its frame shapes
        /// drive nothing, even while the sequence plays.
        /// </summary>
        public bool Visible
        {
            get { return mVisible; }
            set
            {
                if (mVisible != value)
                {
                    mVisible = value;
                    mInstance.TrackVisibilityChanged();
                }
            }
        }

        internal List<SequenceChainReferenceSave> Ordered { get; private set; }

        // Event bookkeeping: the block the track was last in and the last visit whose events were raised there.
        internal SequenceChainReferenceSave EventReference;
        internal int EventVisit = -1;

        internal AnimationChainList ActiveList;
        internal AnimationChain ActiveChain;

        // The block the Sprite was last switched to, and whether it is holding the last frame of a chain with Loop off.
        internal SequenceChainReferenceSave ActiveReference;
        internal bool Holding;

        // The frame the track shows right now, or null while its Sprite is hidden. Its shapes drive the entity's shapes.
        internal AnimationFrame ActiveFrame;
    }

    /// <summary>The tracks of one sequence, by position or by name.</summary>
    public class SequenceTrackList : IReadOnlyList<SequenceTrack>
    {
        private readonly List<SequenceTrack> mTracks = new List<SequenceTrack>();
        private readonly string mSequenceName;

        internal SequenceTrackList(string sequenceName)
        {
            mSequenceName = sequenceName;
        }

        internal void Add(SequenceTrack track)
        {
            mTracks.Add(track);
        }

        public int Count => mTracks.Count;

        public SequenceTrack this[int index] => mTracks[index];

        /// <summary>The track with this name (the first one, if a hand-edited file repeats a name).</summary>
        /// <exception cref="InvalidOperationException">The sequence has no track with this name.</exception>
        public SequenceTrack this[string trackName]
        {
            get
            {
                foreach (SequenceTrack track in mTracks)
                {
                    if (track.Name == trackName)
                    {
                        return track;
                    }
                }
                throw new InvalidOperationException($"The sequence {mSequenceName} has no track named {trackName}");
            }
        }

        public IEnumerator<SequenceTrack> GetEnumerator()
        {
            return mTracks.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return mTracks.GetEnumerator();
        }
    }
}
