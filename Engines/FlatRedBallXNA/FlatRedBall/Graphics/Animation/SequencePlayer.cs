using System;
using System.Collections.Generic;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Content.Polygon;
using FlatRedBall.Math.Geometry;

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
            // The frame the track shows right now, or null while its Sprite is hidden. Its shapes drive the entity's shapes.
            public AnimationFrame ActiveFrame;
        }

        // An entity shape that some chain of the playing sequence names. It is in Collision only while an active frame
        // names it.
        private class ManagedShape
        {
            public PositionedObject Shape;
            public bool RemovedFromCollision;
        }

        private readonly List<TrackRuntime> mTracks = new List<TrackRuntime>();
        private readonly List<Sprite> mSprites = new List<Sprite>();
        private readonly List<PositionedObject> mPivots = new List<PositionedObject>();
        private readonly List<ManagedShape> mManagedShapes = new List<ManagedShape>();
        private readonly HashSet<string> mDrivenNames = new HashSet<string>();
        private readonly HashSet<string> mDisabledNames = new HashSet<string>();

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

        /// <summary>The loaded .seqj this player plays from. It can be shared by the players of many entities.</summary>
        public SequenceList Sequences { get; }

        /// <param name="owner">The entity to play on.</param>
        /// <param name="sequences">The loaded .seqj (see <see cref="SequenceList.FromFile"/>).</param>
        public SequencePlayer(PositionedObject owner, SequenceList sequences)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Sequences = sequences ?? throw new ArgumentNullException(nameof(sequences));
        }

        /// <summary>
        /// Starts the sequence with this name from time 0, replacing the one playing. Returns false, changing nothing,
        /// when the file has no such sequence.
        /// </summary>
        public bool Play(string sequenceName)
        {
            SequenceSave sequence = Sequences.Find(sequenceName);
            if (sequence == null)
            {
                return false;
            }

            RestoreShapes();
            DestroyTracks();
            mCurrent = sequence;
            Time = 0;
            mLength = SequenceTimeline.Length(sequence, CycleLength);
            CreateTracks();
            FindManagedShapes();
            SetTime(0);
            return true;
        }

        /// <summary>
        /// Whether the entity shape with this name is currently enabled. A shape that some chain of the playing sequence
        /// names is enabled while an active frame names it, and disabled otherwise; every other name is always enabled.
        /// A disabled shape has left the owner's Collision. A collision relationship on that shape alone (a sub-collision)
        /// calls the shape directly, so its delegate should return null while this is false:
        /// <c>item => item.Player.IsShapeEnabled("Hitbox") ? item.Hitbox : null</c>.
        /// </summary>
        public bool IsShapeEnabled(string shapeName)
        {
            return !mDisabledNames.Contains(shapeName);
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

            SyncShapes();
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
        /// <remarks>
        /// Call this before the entity removes its own shapes: a shape taken out of Collision would otherwise stay in the
        /// ShapeManager when the entity clears its Collision. This puts every disabled shape back first.
        /// </remarks>
        public void RemoveFromManagers()
        {
            RestoreShapes();
            DestroyTracks();
            mIsInManagers = false;
            mCurrent = null;
        }

        private void ApplyTrack(TrackRuntime track, double time)
        {
            Sprite sprite = track.Sprite;

            track.ActiveFrame = null;
            double local = 0;
            SequenceChainReferenceSave reference = track.Save.Visible
                ? SequenceTimeline.ActiveAt(track.Ordered, time, CycleLength, out local)
                : null;

            AnimationChain chain = reference == null ? null : Sequences.FindChain(reference);
            if (chain == null || chain.Count == 0)
            {
                sprite.Visible = false;
                return;
            }

            sprite.Visible = true;
            AnimationChainList list = Sequences.GetSource(reference.Source);
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

        private void CreateTracks()
        {
            // The track's Z (draw order) is not applied yet: FRB1 orders sprites by layer and sort type, not by a
            // Z the player can set safely. Tracks are created in file order.
            for (int i = 0; i < mCurrent.Tracks.Count; i++)
            {
                SequenceTrackSave save = mCurrent.Tracks[i];

                PositionedObject pivot = new PositionedObject { Name = save.Name };
                pivot.AttachTo(Owner);
                pivot.RelativeX = save.X;
                pivot.RelativeY = save.Y;

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

        // Collects the entity's shapes (direct children) whose name some chain of the playing sequence names in a frame.
        private void FindManagedShapes()
        {
            HashSet<string> names = new HashSet<string>();
            foreach (SequenceTrackSave track in mCurrent.Tracks)
            {
                foreach (SequenceChainReferenceSave reference in track.Chains)
                {
                    AnimationChain chain = Sequences.FindChain(reference);
                    if (chain == null)
                    {
                        continue;
                    }
                    for (int i = 0; i < chain.Count; i++)
                    {
                        AddShapeNames(chain[i].ShapeCollectionSave, names);
                    }
                }
            }

            foreach (PositionedObject child in Owner.Children)
            {
                bool isShape = child is AxisAlignedRectangle || child is Circle || child is Polygon;
                if (isShape && names.Contains(child.Name))
                {
                    mManagedShapes.Add(new ManagedShape { Shape = child });
                }
            }
        }

        private static void AddShapeNames(ShapeCollectionSave save, HashSet<string> names)
        {
            if (save == null)
            {
                return;
            }
            foreach (AxisAlignedRectangleSave rectangle in save.AxisAlignedRectangleSaves)
            {
                names.Add(rectangle.Name);
            }
            foreach (CircleSave circle in save.CircleSaves)
            {
                names.Add(circle.Name);
            }
            foreach (PolygonSave polygon in save.PolygonSaves)
            {
                names.Add(polygon.Name);
            }
        }

        // Applies the active frames' shape values to the entity's shapes, then enables the managed shapes an active
        // frame names and disables the rest. Matching is by name; a frame shape with no entity shape is ignored.
        private void SyncShapes()
        {
            if (mManagedShapes.Count == 0)
            {
                return;
            }

            // The first track wins when several name the same shape, so apply from the last track to the first.
            for (int i = mTracks.Count - 1; i >= 0; i--)
            {
                mTracks[i].ActiveFrame?.ShapeCollectionSave?.SetValuesOn(Owner, false);
            }

            mDrivenNames.Clear();
            foreach (TrackRuntime track in mTracks)
            {
                AddShapeNames(track.ActiveFrame?.ShapeCollectionSave, mDrivenNames);
            }

            ShapeCollection collision = (Owner as ICollidable)?.Collision;
            foreach (ManagedShape managed in mManagedShapes)
            {
                string name = managed.Shape.Name;
                if (mDrivenNames.Contains(name))
                {
                    mDisabledNames.Remove(name);
                    if (managed.RemovedFromCollision)
                    {
                        AddToCollision(collision, managed.Shape);
                        managed.RemovedFromCollision = false;
                    }
                }
                else
                {
                    mDisabledNames.Add(name);
                    if (!managed.RemovedFromCollision && collision != null && RemoveFromCollision(collision, managed.Shape))
                    {
                        managed.RemovedFromCollision = true;
                    }
                }
            }
        }

        // Puts every shape this player took out of Collision back and forgets the managed set.
        private void RestoreShapes()
        {
            ShapeCollection collision = (Owner as ICollidable)?.Collision;
            foreach (ManagedShape managed in mManagedShapes)
            {
                if (managed.RemovedFromCollision)
                {
                    AddToCollision(collision, managed.Shape);
                }
            }
            mManagedShapes.Clear();
            mDisabledNames.Clear();
        }

        private static void AddToCollision(ShapeCollection collision, PositionedObject shape)
        {
            if (shape is AxisAlignedRectangle rectangle)
            {
                collision.Add(rectangle);
            }
            else if (shape is Circle circle)
            {
                collision.Add(circle);
            }
            else if (shape is Polygon polygon)
            {
                collision.Add(polygon);
            }
        }

        // Returns whether the shape was in the collection and has been taken out.
        private static bool RemoveFromCollision(ShapeCollection collision, PositionedObject shape)
        {
            if (shape is AxisAlignedRectangle rectangle && collision.Contains(rectangle))
            {
                collision.AxisAlignedRectangles.Remove(rectangle);
                return true;
            }
            if (shape is Circle circle && collision.Contains(circle))
            {
                collision.Circles.Remove(circle);
                return true;
            }
            if (shape is Polygon polygon && collision.Contains(polygon))
            {
                collision.Polygons.Remove(polygon);
                return true;
            }
            return false;
        }

        // 0 for a missing source, chain or empty chain: it never plays but still cuts the chain before it.
        private double CycleLength(SequenceChainReferenceSave reference)
        {
            AnimationChain chain = Sequences.FindChain(reference);
            return chain == null || chain.Count == 0 ? 0 : SequenceChainClock.CycleLength(chain);
        }
    }
}
