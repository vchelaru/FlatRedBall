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

        internal SequenceTrack(SequenceInstance instance, SequenceTrackSave save, PositionedObject pivot, Sprite sprite)
        {
            mInstance = instance;
            Save = save;
            Pivot = pivot;
            Sprite = sprite;
            mVisible = save.Visible;
            Ordered = SequenceTimeline.Ordered(save);
        }

        /// <summary>The track's name in the file.</summary>
        public string Name => Save.Name;

        /// <summary>The track's data from the file.</summary>
        public SequenceTrackSave Save { get; }

        /// <summary>True when the Sprite belongs to the entity (see <see cref="SequencePlayer"/>'s existingSprites). A bound track has no <see cref="Pivot"/>.</summary>
        public bool IsBound => Pivot == null;

        /// <summary>The object between the entity and the Sprite. It carries the track's X and Y offset. Null for a bound track.</summary>
        public PositionedObject Pivot { get; }

        /// <summary>The Sprite that shows this track. It is hidden whenever no chain of the track is playing.</summary>
        public Sprite Sprite { get; }

        /// <summary>
        /// Whether the track is on. It starts as the file's <c>visible</c> value and stays whatever it is set to. A track that
        /// is off shows nothing and its frame shapes drive nothing, even while the sequence plays.
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

        internal List<SequenceChainReferenceSave> Ordered { get; }

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
