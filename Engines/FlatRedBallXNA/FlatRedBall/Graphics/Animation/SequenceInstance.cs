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
        private double mLastShown;
        private HashSet<string> mShapeNames;

        internal SequenceInstance(SequencePlayer player, SequenceSave save)
        {
            mPlayer = player;
            Save = save;
            Tracks = new SequenceTrackList(save.Name);

            // Tracks are created in file order, which is also their draw order (see SequencePlayer.AddToManagers).
            foreach (SequenceTrackSave trackSave in save.Tracks)
            {
                PositionedObject pivot = new PositionedObject { Name = trackSave.Name };
                pivot.AttachTo(player.Owner);
                pivot.RelativeX = trackSave.X;
                pivot.RelativeY = trackSave.Y;

                // The Sprite animates itself once a block switches its chain (Animate turns on then).
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

        /// <summary>
        /// Raised with an event's name and data when playback enters a frame that has it. Events fire in the first cycle of a
        /// chain's block only, never from a cut-off tail or a track that is off, and a big step raises every skipped frame's
        /// events in order. <see cref="Play"/> raises the first frames' events at once. A handler that stops or restarts the
        /// sequence ends the rest of that update.
        /// </summary>
        public event Action<string, string> EventRaised;

        /// <summary>Starts the sequence from time 0. Playing it again restarts it. Tracks keep their <see cref="SequenceTrack.Visible"/>.</summary>
        public void Play()
        {
            mPlayer.ThrowIfRemoved();
            mPlayVersion++;
            Length = SequenceTimeline.Length(Save, mPlayer.CycleLength);
            Time = 0;
            mLastShown = 0;
            IsPlaying = true;
            foreach (SequenceTrack track in Tracks)
            {
                track.EventReference = null;
                track.EventVisit = -1;
                track.ActiveReference = null;
            }
            mPlayer.SequenceStarted(this);
            ApplyTracks(true);
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
                track.ActiveReference = null;
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
                ApplyTracks(true);
                mPlayer.SyncShapes();
            }
        }

        internal void Advance(double seconds)
        {
            Time = System.Math.Max(0, Time + seconds * Speed);
            ApplyTracks(false);
        }

        internal void TrackVisibilityChanged()
        {
            if (IsPlaying)
            {
                ApplyTracks(true);
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

        // Raises the events of the frames playback has entered since the last pass, in the first cycle of the block only.
        // Frames passed while the time moved backward, while the track was off, or after the first cycle raise nothing.
        private void RaiseEvents(SequenceTrack track, SequenceChainReferenceSave reference, double local)
        {
            AnimationChain chain = reference == null ? null : mPlayer.Source.FindChain(reference);
            if (chain == null || chain.Count == 0)
            {
                track.EventReference = null;
                track.EventVisit = -1;
                return;
            }

            if (!ReferenceEquals(track.EventReference, reference))
            {
                track.EventReference = reference;
                track.EventVisit = -1;
            }

            int visit = SequenceChainClock.VisitAt(chain, local);
            if (visit < track.EventVisit)
            {
                // Time moved backward (a seek); nothing is raised for frames already seen.
                track.EventVisit = visit;
                return;
            }

            int playVersion = mPlayVersion;
            int from = track.EventVisit + 1;
            track.EventVisit = visit;
            if (!track.Visible || EventRaised == null)
            {
                return;
            }

            for (int next = from; next <= visit; next++)
            {
                // Visit 0 is the block starting, which always enters its first frame; for a ping-pong it only looks like the
                // loop point of an earlier cycle.
                if (next != 0 && !SequenceChainClock.EntersNewFrame(chain, next))
                {
                    continue;
                }
                List<AnimationFrameEvent> events = chain[SequenceChainClock.FrameOfVisit(chain, next)].Events;
                for (int i = 0; i < events.Count; i++)
                {
                    EventRaised?.Invoke(events[i].Name, events[i].Data);
                    if (playVersion != mPlayVersion)
                    {
                        return;
                    }
                }
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

        // A seek (play, SetTime, a track turned on or off, a loop wrapping) writes every Sprite's time into its chain.
        // A plain advance only switches a Sprite's chain when a new block starts; between switches the Sprite animates
        // itself in the manager's update, as it does for any Sprite.
        private void ApplyTracks(bool seek)
        {
            double shown = SequenceTimeline.WrapTime(Time, Length, Save.Loop);
            if (shown < mLastShown)
            {
                seek = true;
            }
            mLastShown = shown;

            int playVersion = mPlayVersion;
            foreach (SequenceTrack track in Tracks)
            {
                ApplyTrack(track, shown, seek);

                // An event handler may have stopped or restarted the sequence; the rest of this pass is stale.
                if (playVersion != mPlayVersion)
                {
                    return;
                }
            }
        }

        private void ApplyTrack(SequenceTrack track, double time, bool seek)
        {
            Sprite sprite = track.Sprite;

            track.ActiveFrame = null;
            SequenceChainReferenceSave reference = SequenceTimeline.ActiveAt(track.Ordered, time, mPlayer.CycleLength, out double local);

            // Events are tracked even while the track is off, so turning it on midway doesn't replay what went by.
            RaiseEvents(track, reference, local);
            if (!track.Visible)
            {
                reference = null;
            }

            AnimationChain chain = reference == null ? null : mPlayer.Source.FindChain(reference);
            if (chain == null || chain.Count == 0)
            {
                sprite.Visible = false;
                track.ActiveReference = null;
                return;
            }

            sprite.Visible = true;
            AnimationChainList list = mPlayer.Source.GetSource(reference.Source);
            if (track.ActiveList != list)
            {
                sprite.AnimationChains = list;
                track.ActiveList = list;
            }

            if (track.Save.FlipHorizontal)
            {
                ApplyFlippedTrack(track, chain, local);
                return;
            }

            sprite.AnimationSpeed = Speed;
            if (seek || !ReferenceEquals(track.ActiveReference, reference))
            {
                track.ActiveReference = reference;
                if (track.ActiveChain != chain)
                {
                    sprite.CurrentChain = chain;
                    track.ActiveChain = chain;
                }
                sprite.TimeIntoAnimation = SequenceChainClock.TimeIntoCycle(chain, local);
                sprite.Animate = true;
                track.Holding = false;

                // A chain with Loop off plays one cycle and holds its last frame.
                if (!chain.Loop && local + 1e-6 >= SequenceChainClock.CycleLength(chain))
                {
                    Hold(track, chain);
                }
            }
            else if (!chain.Loop && !track.Holding && sprite.JustCycled)
            {
                // The Sprite's own animation just wrapped past the end of the cycle.
                Hold(track, chain);
            }

            track.ActiveFrame = chain[sprite.CurrentFrameIndex];
        }

        private static void Hold(SequenceTrack track, AnimationChain chain)
        {
            track.Sprite.TimeIntoAnimation = SequenceChainClock.TimeIntoCycle(chain, SequenceChainClock.CycleLength(chain));
            track.Sprite.Animate = false;
            track.Holding = true;
        }

        // A flipped track can't use the Sprite's own animation: every frame's offset has to be mirrored after the Sprite
        // applies it. So the player sets the time on every update and puts the Sprite at its new world position itself.
        private void ApplyFlippedTrack(SequenceTrack track, AnimationChain chain, double local)
        {
            Sprite sprite = track.Sprite;
            track.ActiveReference = null;
            if (track.ActiveChain != chain)
            {
                sprite.CurrentChain = chain;
                track.ActiveChain = chain;
            }

            sprite.Animate = false;
            sprite.TimeIntoAnimation = SequenceChainClock.TimeIntoCycle(chain, local);
            track.ActiveFrame = chain[sprite.CurrentFrameIndex];

            // The Sprite was told not to apply frame flips; mirror the frame's own across the track instead.
            AnimationFrame frame = track.ActiveFrame;
            sprite.FlipHorizontal = !frame.FlipHorizontal;
            sprite.FlipVertical = frame.FlipVertical;
            sprite.FlipDiagonal = frame.FlipDiagonal;
            if (sprite.UseAnimationRelativePosition)
            {
                sprite.RelativeX = -sprite.RelativeX;
            }

            // UpdateDependencies skips a Sprite the managers already updated at the current time; clear that so the new
            // offset reaches the world position now instead of one frame late.
            sprite.LastDependencyUpdate = -1;
            sprite.UpdateDependencies(TimeManager.CurrentTime);
        }
    }
}
