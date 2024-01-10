using FlatRedBall;
using FlatRedBall.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Math.Collision
{
    public abstract class PartitionedValuesBase
    {
        public abstract object PartitionedObject { get; }
        public bool SortEveryFrame { get; protected set; }

        public float MaxWidthOrHeight { get; set; }
        public Axis axis;

        public abstract void Sort();

        public override string ToString()
        {
            return $"{PartitionedObject} on {axis}";
        }

    }

    public class PartitionedEntity<T> : PartitionedValuesBase where T : PositionedObject
    {
        T entity;
        public override object PartitionedObject => entity;

        public override void Sort()
        {
            // do nothing
        }

        public PartitionedEntity(T entity, Axis axis)
        {
            this.entity = entity;
            this.axis = axis;
        }
    }

    public class PartitionedListValues<T> : PartitionedValuesBase where T : PositionedObject
    {
        PositionedObjectList<T> list;
        public override object PartitionedObject => list;

        public PartitionedListValues(PositionedObjectList<T> list, Axis axis, bool sortEveryFrame)
        {
            this.list = list;
            this.axis = axis;
            this.SortEveryFrame = sortEveryFrame;
        }

        public override void Sort()
        {
            switch (axis)
            {
                case Axis.X:
                    list.SortXInsertionAscending();
                    break;
                case Axis.Y:
                    list.SortYInsertionAscending();
                    break;
                case Axis.Z:
                    throw new Exception();
            }
        }
    }
}
