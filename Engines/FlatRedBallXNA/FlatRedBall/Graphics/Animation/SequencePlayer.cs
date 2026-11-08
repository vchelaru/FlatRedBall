using System;
using System.Collections.Generic;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.IO;

namespace FlatRedBall.Graphics.Animation
{
    /// <summary>
    /// Plays the sequences of a .seqj on an entity. Each track of the playing sequence gets a Sprite, parented to a
    /// pivot object that carries the track's offset; the Sprite keeps applying its own frame offset, texture, flip and
    /// keyframes. The player decides which chain each Sprite shows (and whether it shows at all) from the sequence time,
    /// following <see cref="SequenceTimeline"/> and <see cref="SequenceChainClock"/>. A missing source or chain never
    /// throws: that track's Sprite is simply hidden.
    /// </summary>
    /// <remarks>
    /// Call <see cref="Activity"/> (or <see cref="Update"/>) every frame. The Sprites exist only while a sequence is
    /// playing: <see cref="Play"/> destroys the previous sequence's Sprites and creates the new ones.
    /// </remarks>
    public class SequencePlayer
    {
        private class TrackRuntime
        {
            public SequenceTrackSave Save;
            public List<SequenceChainReferenceSave> Ordered;
            public PositionedObject Pivot;
            public Sprite Sprite;
            public AnimationChainList ActiveList;
            public AnimationChain ActiveChain;
        }

        private readonly SequenceListSave mFile;
        private readonly Func<string, AnimationChainList> mSourceResolver;
        private readonly Dictionary<string, AnimationChainList> mSources =
            new Dictionary<string, AnimationChainList>(StringComparer.OrdinalIgnoreCase);
        private readonly List<TrackRuntime> mTracks = new List<TrackRuntime>();
        private readonly List<Sprite> mSprites = new List<Sprite>();
        private readonly List<PositionedObject> mPivots = new List<PositionedObject>();

        private SequenceSave mCurrent;
        private double mLength;
        private bool mIsInManagers;
        private Layer mLayer;

        /// <summary>The entity the sequence plays on. Track offsets are relative to it.</summary>
        public PositionedObject Owner { get; }

        /// <summary>One Sprite per track of the playing sequence, in track order. Empty when nothing is playing.</summary>
        public IReadOnlyList<Sprite> Sprites => mSprites;

        /// <summary>The pivot each track's Sprite is attached to, carrying the track offset and draw order.</summary>
        public IReadOnlyList<PositionedObject> Pivots => mPivots;

        /// <summary>The name of the playing sequence, or null when none is.</summary>
        public string CurrentSequenceName => mCurrent?.Name;

        /// <summary>Seconds into the playing sequence.</summary>
        public double Time { get; private set; }

        /// <summary>Seconds the playing sequence lasts: until its last chain stops playing.</summary>
        public double Length => mLength;

        /// <summary>True once a sequence that does not loop has reached its end.</summary>
        public bool IsFinished => mCurrent != null && !mCurrent.Loop && Time >= mLength;

        /// <summary>Multiplier on elapsed time. 2 plays twice as fast.</summary>
        public float Speed { get; set; } = 1;

        /// <summary>
        /// Distance in Z between draw-order ranks of tracks. A track with a higher Z gets a higher RelativeZ (in front for
        /// the default camera); equal Z keeps track order, a later track in front.
        /// </summary>
        public float ZSpacing { get; set; } = 0.001f;

        /// <param name="owner">The entity to play on.</param>
        /// <param name="file">The loaded .seqj.</param>
        /// <param name="sourceResolver">Returns the chains of a source as written in the .seqj's source list, or null when
        /// it can't be loaded. Called once per source.</param>
        public SequencePlayer(PositionedObject owner, SequenceListSave file, Func<string, AnimationChainList> sourceResolver)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            mFile = file ?? throw new ArgumentNullException(nameof(file));
            mSourceResolver = sourceResolver ?? throw new ArgumentNullException(nameof(sourceResolver));
        }

        /// <summary>
        /// Loads a .seqj from disk. Each source is loaded from the .achx/.achj next to it (paths in the file are relative
        /// to the .seqj); a source that can't be loaded is treated as missing.
        /// </summary>
        public static SequencePlayer FromFile(PositionedObject owner, string seqjFileName, string contentManagerName)
        {
            string absolute = FileManager.IsRelative(seqjFileName) ? FileManager.MakeAbsolute(seqjFileName) : seqjFileName;
            string directory = FileManager.GetDirectory(absolute);

            return new SequencePlayer(owner, SequenceListSave.FromFile(absolute), source =>
            {
                try
                {
                    return Content.AnimationChain.AnimationChainListSave
                        .FromFile(directory + source)
                        .ToAnimationChainList(contentManagerName, throwError: false);
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }

        /// <summary>
        /// Starts the sequence with this name from time 0, replacing the one playing. Returns false, changing nothing,
        /// when the file has no such sequence.
        /// </summary>
        public bool Play(string sequenceName)
        {
            SequenceSave sequence = mFile.Sequences.Find(item => item.Name == sequenceName);
            if (sequence == null)
            {
                return false;
            }

            DestroyTracks();
            mCurrent = sequence;
            Time = 0;
            mLength = SequenceTimeline.Length(sequence, CycleLength);
            CreateTracks();
            SetTime(0);
            return true;
        }

        /// <summary>Advances the playing sequence by TimeManager.SecondDifference. Call once per frame.</summary>
        public void Activity()
        {
            Update(TimeManager.SecondDifference);
        }

        /// <summary>Advances the playing sequence by this many seconds (scaled by <see cref="Speed"/>).</summary>
        public void Update(double seconds)
        {
            if (mCurrent == null)
            {
                return;
            }
            SetTime(Time + seconds * Speed);
        }

        /// <summary>Shows the playing sequence at this time: wrapped when it loops, held at its end when it does not.</summary>
        public void SetTime(double time)
        {
            if (mCurrent == null)
            {
                return;
            }

            // Time keeps counting past the end for a non-looping sequence, so IsFinished stays true.
            Time = System.Math.Max(0, time);
            double shown = SequenceTimeline.WrapTime(Time, mLength, mCurrent.Loop);

            foreach (TrackRuntime track in mTracks)
            {
                ApplyTrack(track, shown);
            }
        }

        /// <summary>Adds the Sprites and pivots to the managers. Sprites created by later calls to Play are added too.</summary>
        public void AddToManagers(Layer layer = null)
        {
            mIsInManagers = true;
            mLayer = layer;
            foreach (TrackRuntime track in mTracks)
            {
                AddTrackToManagers(track);
            }
        }

        /// <summary>Removes the Sprites and pivots from the managers and stops the sequence.</summary>
        public void RemoveFromManagers()
        {
            DestroyTracks();
            mIsInManagers = false;
            mCurrent = null;
        }

        private void ApplyTrack(TrackRuntime track, double time)
        {
            Sprite sprite = track.Sprite;

            double local = 0;
            SequenceChainReferenceSave reference = track.Save.Visible
                ? SequenceTimeline.ActiveAt(track.Ordered, time, CycleLength, out local)
                : null;

            AnimationChain chain = reference == null ? null : FindChain(reference);
            if (chain == null || chain.Count == 0)
            {
                sprite.Visible = false;
                return;
            }

            sprite.Visible = true;
            AnimationChainList list = GetSource(reference.Source);
            if (track.ActiveList != list)
            {
                sprite.AnimationChains = list;
                track.ActiveList = list;
            }
            if (track.ActiveChain != chain)
            {
                sprite.CurrentChain = chain;
                track.ActiveChain = chain;
            }

            sprite.TimeIntoAnimation = SequenceChainClock.TimeIntoCycle(chain, local);

            if (track.Save.FlipHorizontal)
            {
                // The Sprite was told not to apply frame flips; mirror the frame's own across the track instead.
                AnimationFrame frame = chain[sprite.CurrentFrameIndex];
                sprite.FlipHorizontal = !frame.FlipHorizontal;
                sprite.FlipVertical = frame.FlipVertical;
                sprite.FlipDiagonal = frame.FlipDiagonal;
                if (sprite.UseAnimationRelativePosition)
                {
                    sprite.RelativeX = -sprite.RelativeX;
                }
            }
        }

        private void CreateTracks()
        {
            // Draw order: by Z, equal Z in track order. Rank r gets RelativeZ r * ZSpacing.
            int[] byDrawOrder = new int[mCurrent.Tracks.Count];
            for (int i = 0; i < byDrawOrder.Length; i++)
            {
                byDrawOrder[i] = i;
            }
            Array.Sort(byDrawOrder, (a, b) =>
            {
                int byZ = mCurrent.Tracks[a].Z.CompareTo(mCurrent.Tracks[b].Z);
                return byZ != 0 ? byZ : a.CompareTo(b);
            });
            int[] rank = new int[byDrawOrder.Length];
            for (int r = 0; r < byDrawOrder.Length; r++)
            {
                rank[byDrawOrder[r]] = r;
            }

            for (int i = 0; i < mCurrent.Tracks.Count; i++)
            {
                SequenceTrackSave save = mCurrent.Tracks[i];

                PositionedObject pivot = new PositionedObject { Name = save.Name };
                pivot.AttachTo(Owner);
                pivot.RelativeX = save.X;
                pivot.RelativeY = save.Y;
                pivot.RelativeZ = rank[i] * ZSpacing;

                Sprite sprite = new Sprite { Name = save.Name, TextureScale = 1, Animate = false, Visible = false };
                sprite.AttachTo(pivot);
                // A flipped track applies frame flips itself, mirrored across the track.
                sprite.UseAnimationTextureFlip = !save.FlipHorizontal;

                TrackRuntime track = new TrackRuntime
                {
                    Save = save,
                    Ordered = SequenceTimeline.Ordered(save),
                    Pivot = pivot,
                    Sprite = sprite,
                };
                mTracks.Add(track);
                mSprites.Add(sprite);
                mPivots.Add(pivot);

                if (mIsInManagers)
                {
                    AddTrackToManagers(track);
                }
            }
        }

        private void AddTrackToManagers(TrackRuntime track)
        {
            SpriteManager.AddPositionedObject(track.Pivot);
            if (mLayer != null)
            {
                SpriteManager.AddToLayer(track.Sprite, mLayer);
            }
            else
            {
                SpriteManager.AddSprite(track.Sprite);
            }
        }

        private void DestroyTracks()
        {
            foreach (TrackRuntime track in mTracks)
            {
                if (mIsInManagers)
                {
                    SpriteManager.RemoveSprite(track.Sprite);
                    SpriteManager.RemovePositionedObject(track.Pivot);
                }
                track.Sprite.Detach();
                track.Pivot.Detach();
            }
            mTracks.Clear();
            mSprites.Clear();
            mPivots.Clear();
        }

        // 0 for a missing source, chain or empty chain: it never plays but still cuts the chain before it.
        private double CycleLength(SequenceChainReferenceSave reference)
        {
            AnimationChain chain = FindChain(reference);
            return chain == null || chain.Count == 0 ? 0 : SequenceChainClock.CycleLength(chain);
        }

        private AnimationChain FindChain(SequenceChainReferenceSave reference)
        {
            AnimationChainList list = GetSource(reference.Source);
            if (list == null)
            {
                return null;
            }
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Name == reference.Chain)
                {
                    return list[i];
                }
            }
            return null;
        }

        private AnimationChainList GetSource(string source)
        {
            if (!mSources.TryGetValue(source, out AnimationChainList list))
            {
                try
                {
                    list = mSourceResolver(source);
                }
                catch (Exception)
                {
                    // A source that can't be loaded is a missing source, not a crash.
                    list = null;
                }
                mSources[source] = list;
            }
            return list;
        }
    }
}
