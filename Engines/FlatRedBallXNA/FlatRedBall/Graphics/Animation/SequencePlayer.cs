using System;
using System.Collections.Generic;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Content.Polygon;
using FlatRedBall.Math.Geometry;

namespace FlatRedBall.Graphics.Animation
{
    /// <summary>
    /// The sequences of a .seqj on one entity. Each sequence is a <see cref="SequenceInstance"/> you get by name, keep, and
    /// play or stop; each track of it has a Sprite that exists from the start (see <see cref="SequenceTrack"/>). The
    /// player runs everything that is playing and keeps the entity's shapes in step with it. A missing source or chain
    /// never throws: that track's Sprite is simply hidden.
    /// </summary>
    /// <remarks>
    /// Call <see cref="Activity"/> (or <see cref="Update"/>) every frame, <see cref="AddToManagers"/> once, and
    /// <see cref="RemoveFromManagers"/> before the entity removes its own shapes.
    /// <para>
    /// Shapes: the entity's own shapes (direct children) are matched by name to the shapes in the active frames' chains.
    /// A shape that a playing sequence names is enabled while an active frame names it and disabled otherwise; a disabled
    /// shape has left the owner's Collision. A name with no entity shape is ignored, and a shape no playing sequence names is
    /// never touched.
    /// </para>
    /// </remarks>
    public class SequencePlayer
    {
        // An entity shape that some playing sequence names. It is in Collision only while an active frame names it.
        private class ManagedShape
        {
            public PositionedObject Shape;
            public bool RemovedFromCollision;
        }

        private readonly List<SequenceInstance> mInstances = new List<SequenceInstance>();
        // The playing sequences, oldest start first. When two name the same shape, the newest start wins.
        private readonly List<SequenceInstance> mPlaying = new List<SequenceInstance>();
        private readonly List<ManagedShape> mManagedShapes = new List<ManagedShape>();
        private readonly HashSet<string> mDrivenNames = new HashSet<string>();
        private readonly HashSet<string> mDisabledNames = new HashSet<string>();

        private bool mIsInManagers;
        private bool mIsRemoved;

        /// <summary>The entity the sequences play on. Track offsets are relative to it.</summary>
        public PositionedObject Owner { get; }

        /// <summary>The loaded .seqj this player plays from. It can be shared by the players of many entities.</summary>
        public SequenceList Source { get; }

        /// <summary>Every sequence of the file, in file order.</summary>
        public IReadOnlyList<SequenceInstance> Sequences => mInstances;

        /// <param name="owner">The entity to play on.</param>
        /// <param name="source">The loaded .seqj (see <see cref="SequenceList.FromFile"/>).</param>
        public SequencePlayer(PositionedObject owner, SequenceList source)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Source = source ?? throw new ArgumentNullException(nameof(source));

            foreach (SequenceSave save in source.Save.Sequences)
            {
                mInstances.Add(new SequenceInstance(this, save));
            }
        }

        /// <summary>The sequence with this name (the first one, if a hand-edited file repeats a name).</summary>
        /// <exception cref="InvalidOperationException">The file has no sequence with this name.</exception>
        public SequenceInstance this[string sequenceName]
        {
            get
            {
                foreach (SequenceInstance instance in mInstances)
                {
                    if (instance.Name == sequenceName)
                    {
                        return instance;
                    }
                }
                throw new InvalidOperationException($"The sequence file has no sequence named {sequenceName}");
            }
        }

        /// <summary>Advances every playing sequence by TimeManager.SecondDifference. Call once per frame.</summary>
        public void Activity()
        {
            Update(TimeManager.SecondDifference);
        }

        /// <summary>Advances every playing sequence by this many seconds (scaled by each one's <see cref="SequenceInstance.Speed"/>).</summary>
        public void Update(double seconds)
        {
            foreach (SequenceInstance instance in mPlaying)
            {
                instance.Advance(seconds);
            }
            SyncShapes();
        }

        /// <summary>
        /// Whether the entity shape with this name is currently enabled. A shape that a playing sequence names is enabled
        /// while an active frame names it, and disabled otherwise; every other name is always enabled. A disabled shape has
        /// left the owner's Collision. A collision relationship on that shape alone (a sub-collision) calls the shape
        /// directly, so its delegate should return null while this is false:
        /// <c>item => item.Player.IsShapeEnabled("Hitbox") ? item.Hitbox : null</c>.
        /// </summary>
        public bool IsShapeEnabled(string shapeName)
        {
            return !mDisabledNames.Contains(shapeName);
        }

        /// <summary>Adds every track's Sprite and pivot to the managers.</summary>
        public void AddToManagers(Layer layer = null)
        {
            mIsInManagers = true;
            foreach (SequenceInstance instance in mInstances)
            {
                instance.AddToManagers(layer);
            }
        }

        /// <summary>Stops every sequence and removes the Sprites and pivots from the managers. The player can't be used afterwards.</summary>
        /// <remarks>
        /// Call this before the entity removes its own shapes: a shape taken out of Collision would otherwise stay in the
        /// ShapeManager when the entity clears its Collision. Stopping puts every disabled shape back first.
        /// </remarks>
        public void RemoveFromManagers()
        {
            foreach (SequenceInstance instance in new List<SequenceInstance>(mPlaying))
            {
                instance.Stop();
            }
            foreach (SequenceInstance instance in mInstances)
            {
                instance.Destroy(mIsInManagers);
            }
            mIsInManagers = false;
            mIsRemoved = true;
        }

        internal void ThrowIfRemoved()
        {
            if (mIsRemoved)
            {
                throw new InvalidOperationException("This SequencePlayer was removed from the managers and can't play again");
            }
        }

        // 0 for a missing source, chain or empty chain: it never plays but still cuts the chain before it.
        internal double CycleLength(SequenceChainReferenceSave reference)
        {
            AnimationChain chain = Source.FindChain(reference);
            return chain == null || chain.Count == 0 ? 0 : SequenceChainClock.CycleLength(chain);
        }

        internal void SequenceStarted(SequenceInstance instance)
        {
            mPlaying.Remove(instance);
            mPlaying.Add(instance);
            RefreshManagedShapes();
        }

        internal void SequenceStopped(SequenceInstance instance)
        {
            mPlaying.Remove(instance);
            RefreshManagedShapes();
            SyncShapes();
        }

        // Collects the entity's shapes (direct children) whose name some playing sequence names in a frame.
        private void RefreshManagedShapes()
        {
            RestoreShapes();

            HashSet<string> names = new HashSet<string>();
            foreach (SequenceInstance instance in mPlaying)
            {
                names.UnionWith(instance.GetShapeNames());
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

        internal static void AddShapeNames(ShapeCollectionSave save, HashSet<string> names)
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
        internal void SyncShapes()
        {
            if (mManagedShapes.Count == 0)
            {
                return;
            }

            // Oldest start first, so the newest sequence's values are applied last and win.
            foreach (SequenceInstance instance in mPlaying)
            {
                instance.ApplyFrameShapes(Owner);
            }

            mDrivenNames.Clear();
            foreach (SequenceInstance instance in mPlaying)
            {
                instance.AddDrivenShapeNames(mDrivenNames);
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
    }
}
