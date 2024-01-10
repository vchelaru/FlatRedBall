using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Graphics;

#if FRB_MDX
using Microsoft.DirectX;
#else
using Microsoft.Xna.Framework;
#endif

namespace FlatRedBall.Math
{
    public class PositionedObjectList<T> : AttachableList<T>, IEquatable<PositionedObjectList<T>> where T : PositionedObject
    {
        #region Properties

        public T Last
        {
            get
            {
                if (Count == 0)
                    return null;
                else
                    return this[Count - 1];
            }
        }

        #endregion

        #region Methods

        #region Constructor

        public PositionedObjectList() : base() 
        { 
        }

        public PositionedObjectList(int capacity) : base(capacity) 
        { 
        }

        #endregion

        #region Public Methods

        public void AddSortedZAscending(T attachableToAdd)
        {
            if (this.Count == 0)
            {
                Add(attachableToAdd);
            }
            else
            {
                int index = GetFirstAfter(attachableToAdd.Z, Axis.Z, 0, this.Count);
                this.Insert(index, attachableToAdd);
            }
        }

		public void AttachTo(PositionedObject newParent, bool changeRelative)
		{
			for (int i = 0; i < Count; i++)
			{
				this[i].AttachTo(newParent, changeRelative);
			}
		}

        /// <summary>
        /// Detaches all contained PositionedObjects from their parents.
        /// </summary>
		public void Detach() 
		{
			for ( int i = 0; i < Count; i++ )
				this[i].Detach();
		}

		public void AttachAllDetachedTo(PositionedObject newParent, bool changeRelative)
		{
			for (int i = 0; i < Count; i++)
			{
                T thisAtI = this[i];
                if (thisAtI.Parent == null)
                {
                    thisAtI.AttachTo(newParent, changeRelative);
                }
			}
		}

        public void CopyAbsoluteToRelative()
        {
            CopyAbsoluteToRelative(true);
        }

        public void CopyAbsoluteToRelative(bool includeItemsWithParent)
        {
            for(int i = 0; i < this.Count; i++)
            {
                if (this[i].Parent == null || includeItemsWithParent == true)
                {
                    this[i].CopyAbsoluteToRelative();
                }
            }
        }

        public int CountOf(PositionedObject positionedObjectToSearchFor)
        {
            int count = 0;

            for (int i = 0; i < Count; i++)
            {
                if (this[i] == positionedObjectToSearchFor)
                {
                    count++;
                }
            }
            return count;
        }

        #region XML Docs
        /// <summary>
        /// Gets the first object found after the argument "value" on the argument "axis".  Lists
        /// must be sorted for this method to work effectively.  WARNING:  This method uses an inclusive upper bound.  Use GetFirstAfter instead which uses an exclusive upper bound.
        /// </summary>
        /// <param name="value">The value to search after.  For example, this method will return objects with their position values greater than
        /// the argument value.  In other words, if 0 is passed as the value, then objects with position values greater than (not equal to) will be returned.</param>
        /// <param name="axis">The axis representing the value to use (x, y, or z)</param>
        /// <param name="lowBound">The lower (inclusive) bound.</param>
        /// <param name="highBound">The upper (inclusive) bound.  This argument is why GetFirstAfterPosition is obsolete.</param>
        /// <returns>The index of the first object after the given value.</returns>
        #endregion
        [Obsolete("Use GetFirstAfter.  GetFirstAfterPosition uses an inclusive highBound.  GetFirstAfter uses an exclusive highBound")]
        public int GetFirstAfterPosition(float value, Axis axis, int lowBound, int highBound)
        {
            // October 20, 2011
            // GetFirstAfterPosition
            // was made obsolete and the
            // method GetFirstAfter was added
            // instead because most methods in
            // C# uses an exclusive upper bound.
            // This change makes GetFirstAfter work
            // more as users might expect.  It also simplifies
            // code because the user will no longer have to subtract
            // one when using list.Count as the argument.
            return GetFirstAfter(value, axis, lowBound, highBound + 1);
        }

        /// <summary>
        /// Gets the first object found after the argument "value" on the argument "axis".  Lists
        /// must be sorted ascending for this method to work effectively.
        /// </summary>
        /// <remarks>
        /// This method is useful when searching for items in a list after a given value.  
        /// </remarks>
        /// <param name="value">The value to search after.  For example, this method will return objects with their position values greater than
        /// the argument value.  In other words, if 0 is passed as the value, then objects with position values greater than (not equal to) will be returned.</param>
        /// <param name="axis">The axis representing the value to use (x, y, or z)</param>
        /// <param name="lowBoundIndex">The lower (inclusive) bound.</param>
        /// <param name="highBoundIndexExclusive">The upper (exclusive) bound.</param>
        /// <returns>The index of the first object after the given value. If low bound equals high bound, then the low bound is returned.</returns>
        public int GetFirstAfter(float value, Axis axis, int lowBoundIndex, int highBoundIndexExclusive)
        {
#if DEBUG
            if(float.IsNaN(value))
            {
                throw new ArgumentException("value cannot be float.NaN");
            }
#endif
            if (lowBoundIndex == highBoundIndexExclusive)
            {
                return lowBoundIndex;
            }

            // We want it inclusive
            highBoundIndexExclusive -= 1;
            int current = 0;  
          
            switch(axis)
            {
                #region X axis
                case Axis.X:
                    while (true)
                    {
                        current = (lowBoundIndex + highBoundIndexExclusive) >> 1;
                        if (highBoundIndexExclusive - lowBoundIndex < 2)
                        {
                            if (this[highBoundIndexExclusive].Position.X <= value)
                            {
                                return highBoundIndexExclusive + 1;
                            }
                            else if (this[lowBoundIndex].Position.X <= value)
                            {
                                return lowBoundIndex + 1;
                            }
                            else if (this[lowBoundIndex].Position.X > value)
                            {
                                return lowBoundIndex;
                            }
                        }

                        if (this[current].Position.X >= value)
                        {
                            highBoundIndexExclusive = current;
                        }
                        else if (this[current].Position.X < value)
                        {
                            lowBoundIndex = current;
                        }
                    }
                //break;
                #endregion

                #region Y Axis
                case Axis.Y:
                    while (true)
                    {
                        current = (lowBoundIndex + highBoundIndexExclusive) >> 1;
                        if (highBoundIndexExclusive - lowBoundIndex < 2)
                        {
                            if (this[highBoundIndexExclusive].Position.Y <= value)
                            {
                                return highBoundIndexExclusive + 1;
                            }
                            else if (this[lowBoundIndex].Position.Y <= value)
                            {
                                return lowBoundIndex + 1;
                            }
                            else if (this[lowBoundIndex].Position.Y > value)
                            {
                                return lowBoundIndex;
                            }
                        }

                        if (this[current].Position.Y >= value)
                        {
                            highBoundIndexExclusive = current;
                        }
                        else if (this[current].Position.Y < value)
                        {
                            lowBoundIndex = current;
                        }
                    }
                //break;
                #endregion

                #region Z Axis
                case Axis.Z:
                    while (true)
                    {
                        current = (lowBoundIndex + highBoundIndexExclusive) >> 1;
                        if (highBoundIndexExclusive - lowBoundIndex < 2)
                        {
                            if (this[highBoundIndexExclusive].Position.Z <= value)
                            {
                                return highBoundIndexExclusive + 1;
                            }
                            else if (this[lowBoundIndex].Position.Z <= value)
                            {
                                return lowBoundIndex + 1;
                            }
                            else if (this[lowBoundIndex].Position.Z > value)
                            {
                                return lowBoundIndex;
                            }
                        }

                        if (this[current].Position.Z >= value)
                        {
                            highBoundIndexExclusive = current;
                        }
                        else if (this[current].Position.Z < value)
                        {
                            lowBoundIndex = current;
                        }
                    }
                //break;
                #endregion
            }

            throw new Exception("Shouldn't have gotten to this code");
            //return 0;
        }

        public PositionedObject GetFirstDuplicatePositionedObject()
        {
            for (int i = 0; i < Count; i++)
            {
                if (CountOf(this[i]) != 1)
                {
                    return this[i];
                }
            }

            return null;
        }

        #region XML Docs
        /// <summary>
        /// Returns a one-way List containing the TopParents of all items in the list without duplication.
        /// </summary>
        /// <returns>The list of parents.</returns>
        #endregion
        public PositionedObjectList<T> GetTopParents()
        {
            PositionedObjectList<T> listToReturn = new PositionedObjectList<T>();

            foreach (PositionedObject positionedObject in this)
            {
                PositionedObject parent = positionedObject.TopParent;

                if (listToReturn.Contains(parent as T) == false)
                {
                    listToReturn.AddOneWay(((T)parent));
                }
            }

            return listToReturn;
        }

        public List<int> GetZBreaks()
        {
            List<int> zBreaks = new List<int>();

            GetZBreaks(zBreaks);

            return zBreaks;

        }

        /// <summary>
        /// Fills a list of indexes containing where the Z values change. 
        /// </summary>
        /// <param name="zBreaks">The list of indexes to fill.</param>
        public void GetZBreaks(List<int> zBreaks)
        {
            zBreaks.Clear();

            if (Count == 0 || Count == 1)
                return;

            for (int i = 1; i < Count; i++)
            {
                if (this[i].Position.Z != this[i - 1].Position.Z)
                    zBreaks.Add(i);
            }
        }

        /// <summary>
        /// Performs the argument action on each instance in this list. This performs a forward loop,
        /// so it may skip instances if Destroy is called in the argument Action.
        /// </summary>
        /// <seealso cref="ForEachReverse(Action{T})"/>
        /// <param name="action">The action to perform on each instance in this.</param>
        public void ForEach(Action<T> action)
        {
            for(int i = 0; i < this.Count; i++)
            {
                action(this[i]);
            }
        }

        /// <summary>
        /// Performans the argument action on each instance in this list. This performs a reverse loop,
        /// allowing Destroy to be called.
        /// </summary>
        /// <param name="action">The action to perform on each instance in this list.</param>
        public void ForEachReverse(Action<T> action)
        {
            for(int i = this.Count - 1; i > -1; i--)
            {
                action(this[i]);
            }
        }

        #region Shift methods (move all obects by a set amount)

        /// <summary>
        /// Shifts all contained objects' Position by the arguments x,y,z.
        /// </summary>
        /// <param name="x">Amount to move on the X axis.</param>
        /// <param name="y">Amount to move on the Y axis.</param>
        /// <param name="z">Amount to move on the Z axis.</param>
        public void Shift(float x, float y, float z)
        {
            for (int i = 0; i < this.Count; i++)
            {
                this[i].Position.X += x;
                this[i].Position.Y += y;
                this[i].Position.Z += z;
            }
        }

        /// <summary>
        /// Shifts all contained objects' Position by the argument vector.
        /// </summary>
        /// <param name="vector">The amount to change Position by.</param>
        public void Shift(Vector3 vector)
        {
            for (int i = 0; i < this.Count; i++)
            {
                this[i].Position += vector;
            }
        }

        /// <summary>
        /// Shifts all contained objects' RelativePosition by the argument vector.
        /// </summary>
        /// <param name="vector">The amount to change RelativePosition by.</param>
        public void ShiftRelative(Vector3 vector)
        {
            for (int i = 0; i < this.Count; i++)
            {
                this[i].RelativePosition += vector;
            }
        }

        #endregion

        #region Swap methods (swap object1 and object2 in the list)

        public void Swap(T object1, T object2)
        {
            T temp = object1;
            int index1 = IndexOf(object1);
            int index2 = IndexOf(object2);
            this[index1] = object2;
            this[index2] = temp;
        }

        public void Swap(int index1, int index2)
        {
            T temp = this[index1];
            this[index1] = this[index2];
            this[index2] = temp;
        }

        #endregion

        #region Shuffle/Sorting Methods
        /*
        public void KeepObjectSortedAscending(int positionedObject, Axis axis)
        {
            switch (axis)
            {
                #region Axis.X
                case Axis.X:

                    

                    break;
                #endregion

                #region Axis.Y
                case Axis.Y:

                    break;
                #endregion

                #region Axis.Z
                case Axis.Z:

                    break;
                #endregion
            }
        }
        */

        // Currently sorting methods call Insert and Remove internally.  This is a performance hit
        // which could be resolved by writing a Swap method which shifts only the two objects
        // which need to be swapped and all elements inbetween the indexes of the two.

        /// <summary>
        /// Shuffles all elements in the list such that the are in random order afer the call finishes.
        /// </summary>
        /// <remarks>
        /// The Shuffle method can be used for elements which should be presented in a random order. For example,
        /// a list of players may be shuffled before a game begins to randomize the turn order to prevent one player from
        /// always going first.
        /// </remarks>
        public void Shuffle()
        {
            int count = Count;
            int index = 0;
            for (int i = count - 1; i > -1; i--)
            {
                T objectAtI = this[i];
                mInternalList.RemoveAt(i);
                // count - 1 because we removed an object.
                index = FlatRedBallServices.Random.Next(count - 1);
                mInternalList.Insert(index, objectAtI);
            }

            // We do this twice because this will essentially start at the last index
            // and skip every other one. Doing it twice gives us much better results
            for (int i = count - 1; i > -1; i--)
            {
                T objectAtI = this[i];
                mInternalList.RemoveAt(i);
                // count - 1 because we removed an object.
                index = FlatRedBallServices.Random.Next(count - 1);
                mInternalList.Insert(index, objectAtI);
            }

        }

        public void SortAlongForwardVectorDescending(PositionedObject positionedObjectRelativeTo)
        {
            Dictionary<PositionedObject, Vector3> oldPositions = new Dictionary<PositionedObject, Vector3>(this.Count);

            Matrix inverseRotationMatrix = positionedObjectRelativeTo.RotationMatrix;

            Matrix.Invert(ref inverseRotationMatrix, out inverseRotationMatrix);

            int temporaryCount = this.Count;

            for(int i = 0; i < temporaryCount; i++)
            {
                oldPositions.Add(this[i], this[i].Position);

                this[i].Position -= positionedObjectRelativeTo.Position;

                Vector3.Transform(ref this[i].Position,
                    ref inverseRotationMatrix, out this[i].Position);

            }

            this.SortZInsertionAscending();

            foreach (KeyValuePair<PositionedObject, Vector3> kvp in oldPositions)
            {
                kvp.Key.Position = kvp.Value;
            }
        }

        public void SortCameraDistanceInsersionDescending(Camera camera)
        {// Biggest first
            if (Count == 1 || Count == 0)
                return;

            int whereCameraBelongs;

            float cameraDistanceSquared;

            for (int i = 1; i < Count; i++)
            {
                // Creating new vectors here for distance is slow.  
                // For optimization just do the length suared calculations
                // by hand.
                cameraDistanceSquared = ((Vector3)(this[i]).Position - camera.Position).LengthSquared();

                if (cameraDistanceSquared > ((this[i - 1]).Position - camera.Position).LengthSquared())
                {
                    if (i == 1)
                    {
                        mInternalList.Insert(0, this[i]);
                        mInternalList.RemoveAt(i + 1);
                        continue;
                    }

                    for (whereCameraBelongs = i - 2; whereCameraBelongs > -1; whereCameraBelongs--)
                    {
                        // Optimize here by not calling LengthSq/LengthSquared
#if FRB_MDX
#else
                        if (cameraDistanceSquared <= ((this[whereCameraBelongs]).Position - camera.Position).LengthSquared())
#endif
                        {
                            mInternalList.Insert(whereCameraBelongs + 1, this[i]);
                            mInternalList.RemoveAt(i + 1);
                            break;
                        }
                        else if (whereCameraBelongs == 0 && cameraDistanceSquared > ((this[0]).Position - camera.Position).LengthSquared())
                        {
                            mInternalList.Insert(0, this[i]);
                            mInternalList.RemoveAt(i + 1);
                            break;
                        }
                    }
                }
            }
        }

        public void SortXInsertionAscending()
        {
            SortXInsertionAscending(0, this.Count);
        }

        public void SortXInsertionAscending(int firstObject, int lastObjectExclusive)
        {
            int whereObjectBelongs;
            for (int i = firstObject + 1; i < lastObjectExclusive; i++)
            {
                if ((this[i]).Position.X < (this[i - 1]).Position.X)
                {
                    if (i == firstObject + 1)
                    {
                        mInternalList.Insert(firstObject, this[i]);
                        mInternalList.RemoveAt(i + 1);
                        continue;
                    }

                    for (whereObjectBelongs = i - 2; whereObjectBelongs > firstObject - 1; whereObjectBelongs--)
                    {
                        if ((this[i]).Position.X >= (this[whereObjectBelongs]).Position.X)
                        {
                            mInternalList.Insert(whereObjectBelongs + 1, this[i]);
                            mInternalList.RemoveAt(i + 1);
                            break;
                        }
                        else if (whereObjectBelongs == firstObject && (this[i]).Position.X < (this[firstObject]).Position.X)
                        {
                            mInternalList.Insert(firstObject, this[i]);
                            mInternalList.RemoveAt(i + 1);
                            break;
                        }
                    }
                }
            }
        }

        public void SortXInsertionDescending()
        {
            SortXInsertionDescending(0, this.Count);
        }

        public void SortXInsertionDescending(int firstObject, int lastObjectExclusive)
        {
            int whereObjectBelongs;
            for (int i = firstObject + 1; i < lastObjectExclusive; i++)
            {
                if ((this[i]).Position.X > (this[i - 1]).Position.X)
                {
                    if (i == firstObject + 1)
                    {
                        mInternalList.Insert(firstObject, this[i]);
                        mInternalList.RemoveAt(i + 1);
                        continue;
                    }

                    for (whereObjectBelongs = i - 2; whereObjectBelongs > firstObject - 1; whereObjectBelongs--)
                    {
                        if ((this[i]).Position.X <= (this[whereObjectBelongs]).Position.X)
                        {
                            mInternalList.Insert(whereObjectBelongs + 1, this[i]);
                            mInternalList.RemoveAt(i + 1);
                            break;
                        }
                        else if (whereObjectBelongs == firstObject && (this[i]).Position.X > (this[firstObject]).Position.X)
                        {
                            mInternalList.Insert(firstObject, this[i]);
                            mInternalList.RemoveAt(i + 1);
                            break;
                        }
                    }
                }
            }
        }

        public void SortYInsertionAscending()
        {
            SortYInsertionAscending(0, this.Count);
        }

        public void SortYInsertionAscending(int firstObject, int lastObjectExclusive)
        {
            int whereObjectBelongs;
            for (int i = firstObject + 1; i < lastObjectExclusive; i++)
            {
                if ((this[i]).Position.Y < (this[i - 1]).Position.Y)
                {
                    if (i == firstObject + 1)
                    {
                        mInternalList.Insert(firstObject, this[i]);
                        mInternalList.RemoveAt(i + 1);
                        continue;
                    }

                    for (whereObjectBelongs = i - 2; whereObjectBelongs > firstObject - 1; whereObjectBelongs--)
                    {
                        if ((this[i]).Position.Y >= (this[whereObjectBelongs]).Position.Y)
                        {
                            mInternalList.Insert(whereObjectBelongs + 1, this[i]);
                            mInternalList.RemoveAt(i + 1);
                            break;
                        }
                        else if (whereObjectBelongs == firstObject && (this[i]).Position.Y < (this[firstObject]).Position.Y)
                        {
                            mInternalList.Insert(firstObject, this[i]);
                            mInternalList.RemoveAt(i + 1);
                            break;
                        }
                    }
                }
            }
        }

        public void SortYInsertionDescending()
        {
            SortYInsertionDescending(0, this.Count);
        }

        public void SortYInsertionDescending(int firstObject, int lastObjectExclusive)
        {
            int whereObjectBelongs;
            for (int i = firstObject + 1; i < lastObjectExclusive; i++)
            {
                if ((this[i]).Position.Y > (this[i - 1]).Position.Y)
                {
                    if (i == firstObject + 1)
                    {
                        mInternalList.Insert(firstObject, this[i]);
                        mInternalList.RemoveAt(i + 1);
                        continue;
                    }

                    for (whereObjectBelongs = i - 2; whereObjectBelongs > firstObject - 1; whereObjectBelongs--)
                    {
                        if ((this[i]).Position.Y <= (this[whereObjectBelongs]).Position.Y)
                        {
                            mInternalList.Insert(whereObjectBelongs + 1, this[i]);
                            mInternalList.RemoveAt(i + 1);
                            break;
                        }
                        else if (whereObjectBelongs == firstObject && (this[i]).Position.Y > (this[firstObject]).Position.Y)
                        {
                            mInternalList.Insert(firstObject, this[i]);
                            mInternalList.RemoveAt(i + 1);
                            break;
                        }
                    }
                }
            }
        }

        public void SortParentYInsertionDescending(int firstObject, int lastObjectExclusive)
        {
            int whereObjectBelongs;

            float yAtI;
            float yAtIMinusOne;

            for (int i = firstObject + 1; i < lastObjectExclusive; i++)
            {
                yAtI = this[i].TopParent.Position.Y;
                yAtIMinusOne = this[i - 1].TopParent.Position.Y;

                if (yAtI > yAtIMinusOne)
                {
                    if (i == firstObject + 1)
                    {
                        mInternalList.Insert(firstObject, this[i]);
                        mInternalList.RemoveAt(i + 1);
                        continue;
                    }

                    for (whereObjectBelongs = i - 2; whereObjectBelongs > firstObject - 1; whereObjectBelongs--)
                    {
                        if (yAtI <= (this[whereObjectBelongs]).TopParent.Position.Y)
                        {
                            mInternalList.Insert(whereObjectBelongs + 1, this[i]);
                            mInternalList.RemoveAt(i + 1);
                            break;
                        }
                        else if (whereObjectBelongs == firstObject && yAtI > (this[firstObject]).TopParent.Position.Y)
                        {
                            mInternalList.Insert(firstObject, this[i]);
                            mInternalList.RemoveAt(i + 1);
                            break;
                        }
                    }
                }
            }
        }

        List<int> sZBreaks = new List<int>(10);
        public void SortYInsertionAscendingOnZBreaks()
        {
            GetZBreaks(sZBreaks);

            sZBreaks.Insert(0, 0);
            sZBreaks.Add(Count);

            for (int i = 0; i < sZBreaks.Count - 1; i++)
            {
                SortYInsertionAscending(sZBreaks[i], sZBreaks[i + 1]);
            }
        }

        public void SortParentYInsertionDescendingOnZBreaks()
        {
            GetZBreaks(sZBreaks);

            sZBreaks.Insert(0, 0);
            sZBreaks.Add(Count);

            for (int i = 0; i < sZBreaks.Count - 1; i++)
            {
                SortParentYInsertionDescending(sZBreaks[i], sZBreaks[i + 1]);
            }
        }        

        public void SortYInsertionDescendingOnZBreaks()
        {
            GetZBreaks(sZBreaks);

            sZBreaks.Insert(0, 0);
            sZBreaks.Add(Count);

            for (int i = 0; i < sZBreaks.Count - 1; i++)
            {
                SortYInsertionDescending(sZBreaks[i], sZBreaks[i + 1]);
            }
        }

        public void SortZInsertionAscending()
        {
                if (Count == 1 || Count == 0)
                    return;

                int whereObjectBelongs;

                for (int i = 1; i < Count; i++)
                {
                    if ((this[i]).Position.Z < (this[i - 1]).Position.Z)
                    {
                        if (i == 1)
                        {
                            mInternalList.Insert(0, this[i]);
                        mInternalList.RemoveAt(i + 1);
                            continue;
                        }

                        for (whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                        {
                            if ((this[i]).Position.Z >= (this[whereObjectBelongs]).Position.Z)
                            {
                            mInternalList.Insert(whereObjectBelongs + 1, this[i]);
                            mInternalList.RemoveAt(i + 1);
                                break;
                            }
                            else if (whereObjectBelongs == 0 && (this[i]).Position.Z < (this[0]).Position.Z)
                            {
                            mInternalList.Insert(0, this[i]);
                            mInternalList.RemoveAt(i + 1);
                                break;
                            }
                        }
                    }
                }
            }

        public void SortZInsertionDescending()
        {
            if (Count == 1 || Count == 0)
                return;

            int whereObjectBelongs;

            for (int i = 1; i < Count; i++)
            {
                if ((this[i]).Position.Z > (this[i - 1]).Position.Z)
                {
                    if (i == 1)
                    {
                        mInternalList.Insert(0, this[i]);
                        mInternalList.RemoveAt(i + 1);
                        continue;
                    }

                    for (whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                    {
                        if ((this[i]).Position.Z <= (this[whereObjectBelongs]).Position.Z)
                        {
                            mInternalList.Insert(whereObjectBelongs + 1, this[i]);
                            mInternalList.RemoveAt(i + 1);
                            break;
                        }
                        else if (whereObjectBelongs == 0 && (this[i]).Position.Z > (this[0]).Position.Z)
                        {
                            mInternalList.Insert(0, this[i]);
                            mInternalList.RemoveAt(i + 1);
                            break;
                        }
                    }
                }
            }
        }

        #endregion

        #endregion

        #endregion

        #region IEquatable<PositionedObjectList<T>> Members

        bool IEquatable<PositionedObjectList<T>>.Equals(PositionedObjectList<T> other)
        {
            return this == other;
        }

        #endregion
    }

    public static class PositionedObjectListExtensionMethods
    {
        public static void DestroyAll<T>(this PositionedObjectList<T> list) where T : PositionedObject, IDestroyable
        {
            for(int i = list.Count-1; i > -1; i--)
            {
                list[i].Destroy();
            }
        }
    }
}
