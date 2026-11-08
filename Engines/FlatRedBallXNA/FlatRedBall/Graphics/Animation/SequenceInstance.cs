using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlatRedBall.Content.AnimationChain;

namespace FlatRedBall.Graphics.Animation
{
    /// <summary>
    /// One sequence of a .seqj on one entity. Get it from <see cref="SequencePlayer"/> by name, keep it, and
    /// <see cref="Play"/> or <see cref="Stop"/> it. Sequences run independently: playing one never stops another, so
    /// a body and its wings can play at once. Starting a sequence that overlaps another (two that both draw the body)
    /// is the caller's job to avoid by stopping the first.
    /// </summary>
    public class SequenceInstance
    {
        private readonly SequencePlayer mPlayer;
        private int mPlayVersion;
        private HashSet<string> mShapeNames;

        internal SequenceInstance(SequencePlayer player, SequenceSave save)
        {
            mPlayer = player;
            Save = save;
            Tracks = new SequenceTrackList(save.Name);

            // The track's Z (draw order) is not applied yet: FRB1 orders sprites by layer and sort type, not by a
            // Z the player can set safely. Tracks are created in file order.
            foreach (SequenceTrackSave trackSave in save.Tracks)
            {
                PositionedObject pivot = new PositionedObject { Name = trackSave.Name };
                pivot.AttachTo(player.Owner);
                pivot.RelativeX = trackSave.X;
                pivot.RelativeY = trackSave.Y;

                Sprite sprite = new Sprite { Name = trackSave.Name, TextureScale = 1, Animate = false, Visible = false };
                sprite.AttachTo(pivot);
                // A flipped track applies frame flips itself, mirrored across the track.
                sprite.UseAnimationTextureFlip = !trackSave.FlipHorizontal;

                Tracks.Add(new SequenceTrack(this, trackSave, pivot, sprite));
            }
        }

        /// <summary>The sequence's data from the file.</summary>
        public SequenceSave Save { get; }

        /// <summary>The sequence's name in the file.</summary>
        public string Name => Save.Name;

        /// <summary>The sequence's tracks, in file order, by position or by name.</summary>
        public SequenceTrackList Tracks { get; }

        /// <summary>True from <see cref="Play"/> until <see cref="Stop"/>. A sequence that has reached its end is still playing.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>Seconds into the sequence.</summary>
        public double Time { get; private set; }

        /// <summary>Seconds the sequence lasts: until its last chain stops playing. Set by <see cref="Play"/>.</summary>
        public double Length { get; private set; }

        /// <summary>True once a sequence that does not loop has reached its end.</summary>
        public bool IsFinished => IsPlaying && !Save.Loop && Time >= Length;

        /// <summary>Multiplier on elapsed time. 2 plays twice as fast.</summary>
        public float Speed { get; set; } = 1;

        /// <summary>Starts the sequence from time 0. Playing it again restarts it. Tracks keep their <see cref="SequenceTrack.Visible"/>.</summary>
        public void Play()
        {
            mPlayer.ThrowIfRemoved();
            mPlayVersion++;
            Length = SequenceTimeline.Length(Save, mPlayer.CycleLength);
            Time = 0;
            IsPlaying = true;
            mPlayer.SequenceStarted(this);
            ApplyTracks();
            mPlayer.SyncShapes();
        }

        /// <summary>
        /// Stops the sequence: its Sprites hide and any shapes it was disabling come back. A <see cref="PlayAsync"/> still
        /// waiting on it is canceled.
        /// </summary>
        public void Stop()
        {
            if (!IsPlaying)
            {
                return;
            }

            mPlayVersion++;
            IsPlaying = false;
            foreach (SequenceTrack track in Tracks)
            {
                track.Sprite.Visible = false;
                track.ActiveFrame = null;
            }
            mPlayer.SequenceStopped(this);
        }

        /// <summary>
        /// Starts the sequence and returns a task that completes when it reaches its end. The task is canceled, not
        /// completed, if the sequence is stopped or played again first, so code after the await never runs as though it had
        /// finished.
        /// </summary>
        /// <param name="cancellationToken">Cancels the wait; the sequence keeps playing.</param>
        /// <exception cref="InvalidOperationException">The sequence loops, so it never finishes (use <see cref="Play"/>).</exception>
        public async Task PlayAsync(CancellationToken cancellationToken = default)
        {
            if (Save.Loop)
            {
                throw new InvalidOperationException($"Could not await {Name} because it has Loop turned on, so it never finishes. Use Play for a looping sequence");
            }

            Play();
            int playVersion = mPlayVersion;

            await TimeManager.DelayUntil(() => mPlayVersion != playVersion || IsFinished, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (mPlayVersion != playVersion)
            {
                throw new OperationCanceledException($"The sequence {Name} was stopped or restarted before it finished");
            }
        }

        /// <summary>Advances the sequence by this many seconds (scaled by <see cref="Speed"/>). Does nothing unless it is playing.</summary>
        public void Update(double seconds)
        {
            if (IsPlaying)
            {
                Advance(seconds);
                mPlayer.SyncShapes();
            }
        }

        /// <summary>Shows the sequence at this time: wrapped when it loops, held at its end when it does not. Does nothing unless it is playing.</summary>
        public void SetTime(double time)
        {
            if (IsPlaying)
            {
                // Time keeps counting past the end for a non-looping sequence, so IsFinished stays true.
                Time = System.Math.Max(0, time);
                ApplyTracks();
                mPlayer.SyncShapes();
            }
        }

        internal void Advance(double seconds)
        {
            Time = System.Math.Max(0, Time + seconds * Speed);
            ApplyTracks();
        }

        internal void TrackVisibilityChanged()
        {
            if (IsPlaying)
            {
                ApplyTracks();
                mPlayer.SyncShapes();
            }
        }

        // The shape names any chain of this sequence puts in a frame. Computed once.
        internal HashSet<string> GetShapeNames()
        {
            if (mShapeNames == null)
            {
                mShapeNames = new HashSet<string>();
                foreach (SequenceTrackSave track in Save.Tracks)
                {
                    foreach (SequenceChainReferenceSave reference in track.Chains)
                    {
                        AnimationChain chain = mPlayer.Source.FindChain(reference);
                        for (int i = 0; chain != null && i < chain.Count; i++)
                        {
                            SequencePlayer.AddShapeNames(chain[i].ShapeCollectionSave, mShapeNames);
                        }
                    }
                }
            }
            return mShapeNames;
        }

        // The first track wins when several name the same shape, so apply from the last track to the first.
        internal void ApplyFrameShapes(PositionedObject owner)
        {
            for (int i = Tracks.Count - 1; i >= 0; i--)
            {
                Tracks[i].ActiveFrame?.ShapeCollectionSave?.SetValuesOn(owner, false);
            }
        }

        internal void AddDrivenShapeNames(HashSet<string> names)
        {
            foreach (SequenceTrack track in Tracks)
            {
                SequencePlayer.AddShapeNames(track.ActiveFrame?.ShapeCollectionSave, names);
            }
        }

        internal void Destroy(bool inManagers)
        {
            foreach (SequenceTrack track in Tracks)
            {
                if (inManagers)
                {
                    SpriteManager.RemoveSprite(track.Sprite);
                    SpriteManager.RemovePositionedObject(track.Pivot);
                }
                track.Sprite.Detach();
                track.Pivot.Detach();
            }
        }

        internal void AddToManagers(Layer layer)
        {
            foreach (SequenceTrack track in Tracks)
            {
                SpriteManager.AddPositionedObject(track.Pivot);
                if (layer != null)
                {
                    SpriteManager.AddToLayer(track.Sprite, layer);
                }
                else
                {
                    SpriteManager.AddSprite(track.Sprite);
                }
            }
        }

        private void ApplyTracks()
        {
            double shown = SequenceTimeline.WrapTime(Time, Length, Save.Loop);
            foreach (SequenceTrack track in Tracks)
            {
                ApplyTrack(track, shown);
            }
        }

        private void ApplyTrack(SequenceTrack track, double time)
        {
            Sprite sprite = track.Sprite;

            track.ActiveFrame = null;
            double local = 0;
            SequenceChainReferenceSave reference = track.Visible
                ? SequenceTimeline.ActiveAt(track.Ordered, time, mPlayer.CycleLength, out local)
                : null;

            AnimationChain chain = reference == null ? null : mPlayer.Source.FindChain(reference);
            if (chain == null || chain.Count == 0)
            {
                sprite.Visible = false;
                return;
            }

            sprite.Visible = true;
            AnimationChainList list = mPlayer.Source.GetSource(reference.Source);
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
            track.ActiveFrame = chain[sprite.CurrentFrameIndex];

            if (track.Save.FlipHorizontal)
            {
                // The Sprite was told not to apply frame flips; mirror the frame's own across the track instead.
                AnimationFrame frame = track.ActiveFrame;
                sprite.FlipHorizontal = !frame.FlipHorizontal;
                sprite.FlipVertical = frame.FlipVertical;
                sprite.FlipDiagonal = frame.FlipDiagonal;
                if (sprite.UseAnimationRelativePosition)
                {
                    sprite.RelativeX = -sprite.RelativeX;
                }
            }
        }
    }
}
