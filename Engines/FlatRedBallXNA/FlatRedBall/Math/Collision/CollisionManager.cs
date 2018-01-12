using FlatRedBall;
using FlatRedBall.Managers;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Math.Collision
{
    public class CollisionManager : IManager
    {
        #region Fields/Properties

        /// <summary>
        /// The relationships which are currently part of the collision manager. This should not be added to
        /// by regular game code. This is exposed so that collision manager extension methods can add new relationships.
        /// </summary>
        public List<CollisionRelationship> Relationships { get; private set; } = new List<CollisionRelationship>();

        List<PartitionedValuesBase> partitions = new List<PartitionedValuesBase>();
        public IEnumerable<PartitionedValuesBase> Partitions => partitions;

        public int DeepCollisionsThisFrame
        {
            get
            {
                int toReturn = 0;
                foreach(var relationship in Relationships)
                {
                    toReturn += relationship.DeepCollisionsThisFrame;
                }

                return toReturn;
            }
        }

        #endregion

        #region CreateRelationship Methods
        // Entity vs. Entity
        public PositionedObjectVsPositionedObjectRelationship<FirstCollidableT, SecondCollidableT> CreateRelationship<FirstCollidableT, SecondCollidableT>(
            FirstCollidableT first, SecondCollidableT second)
            where FirstCollidableT : PositionedObject, ICollidable where SecondCollidableT : PositionedObject, ICollidable
        {
            var relationship = new PositionedObjectVsPositionedObjectRelationship<FirstCollidableT, SecondCollidableT>(first, second);
            relationship.Partitions = Partitions;
            this.Relationships.Add(relationship);

            return relationship;
        }

        // Entity vs. List
        public PositionedObjectVsListRelationship<FirstCollidableT, SecondCollidableT> CreateRelationship<FirstCollidableT, SecondCollidableT>(
            FirstCollidableT first, PositionedObjectList<SecondCollidableT> second)
            where FirstCollidableT : PositionedObject, ICollidable where SecondCollidableT : PositionedObject, ICollidable
        {
            var relationship = new PositionedObjectVsListRelationship<FirstCollidableT, SecondCollidableT>(first, second);
            relationship.Partitions = Partitions;
            this.Relationships.Add(relationship);
            return relationship;
        }


        // List vs. Entity
        public ListVsPositionedObjectRelationship<FirstCollidableT, SecondCollidableT> CreateRelationship<FirstCollidableT, SecondCollidableT>(
            PositionedObjectList<FirstCollidableT> first, SecondCollidableT second)
            where FirstCollidableT : PositionedObject, ICollidable where SecondCollidableT : PositionedObject, ICollidable
        {
            var relationship = new ListVsPositionedObjectRelationship<FirstCollidableT, SecondCollidableT>(first, second);
            relationship.Partitions = Partitions;
            this.Relationships.Add(relationship);
            return relationship;
        }

        // List vs. List
        public ListVsListRelationship<FirstCollidableT, SecondCollidableT> CreateRelationship<FirstCollidableT, SecondCollidableT>(
            PositionedObjectList<FirstCollidableT> first, PositionedObjectList<SecondCollidableT> second)
            where FirstCollidableT : PositionedObject, ICollidable where SecondCollidableT : PositionedObject, ICollidable
        {
            var relationship = new ListVsListRelationship<FirstCollidableT, SecondCollidableT>(first, second);
            relationship.Partitions = Partitions;
            this.Relationships.Add(relationship);
            return relationship;
        }


        #endregion


        public void Partition<T>(PositionedObjectList<T> list, Axis axis, float maxWidthOrHeight, bool sortEveryFrame = false) where T : PositionedObject
        {
            var partitionedValues = new PartitionedListValues<T>(list, axis, sortEveryFrame);
            partitionedValues.MaxWidthOrHeight = maxWidthOrHeight;
            partitions.Add(partitionedValues);
        }

        public void Partition<T>(T entity, Axis axis, float maxWidthOrHeight) where T : PositionedObject
        {
            var partitionedValues = new PartitionedEntity<T>(entity, axis);
            partitionedValues.MaxWidthOrHeight = maxWidthOrHeight;
            partitions.Add(partitionedValues);
        }

        public void Update()
        {
            foreach(var partition in this.Partitions)
            {
                if (partition.SortEveryFrame)
                {
                    partition.Sort();
                }
            }

            foreach (var relationship in Relationships)
            {
                relationship.DeepCollisionsThisFrame = 0;

                if(relationship.IsActive)
                {
                    relationship.DoCollisions();
                }
            }
        }

        public void UpdateDependencies()
        {
        }

    }
}
