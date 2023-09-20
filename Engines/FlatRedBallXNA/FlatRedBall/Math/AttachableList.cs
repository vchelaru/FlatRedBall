using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using FlatRedBall.Utilities;

namespace FlatRedBall.Math
{
 
    /// <summary>
    /// A list of IAttachables which is by default two-way.
    /// </summary>
    /// <typeparam name="T">Type of the list which is of IAttachable.</typeparam>
    public class AttachableList<T> : IAttachableRemovable, INotifyCollectionChanged, INameable, IList, IList<T> where T : IAttachable // Don't limit T to be a class because this creates bad IL.  This is a bug in .NET 2.0
    {
        #region Fields
        string mName;

        // made internal for performance
        // May 27, 2012
        // A user asked me
        // why the AttachableList
        // class doesn't inherit from
        // IList.  The reason is because
        // if we want the AttachableList to
        // have two-way functionality then we
        // need custom code in Add and Remove (and
        // a few other methods).  If AttachableList
        // inherited from List, then these methods that
        // need custom logic would have to be "new"-ed because
        // they are not marked as "virtual" in List.  This means
        // that if the AttachableList were casted to an IList, it
        // would lose its two-way functionality.  We don't want that,
        // so instead we inherit from IList so that the Add/Remove methods
        // work properly regardless of the cast of AttachableList.
        internal List<T> mInternalList;
        internal IList mInternalListAsIList;

#if DEBUG
        internal HashSet<T> InternalHashSet;
#endif

#endregion

        #region Properties

        #region XML Docs
        /// <summary>
        /// The number of elements contained in the list.
        /// </summary>
        #endregion
        public int Count
        {
            get { return mInternalList.Count; }
        }

        #region XML Docs
        /// <summary>
        /// Gets and sets the name of this instance.
        /// </summary>
        #endregion
        public string Name
        {
            get { return mName; }
            set { mName = value; }
        }
        #endregion

        #region Methods

        #region Constructor / Initialize

        /// <summary>
        /// Creates a new AttachableList.
        /// </summary>
        public AttachableList()
        { 
            mInternalList = new List<T>();
            mInternalListAsIList = mInternalList;
        }

        /// <summary>
        /// Creates a new AttachableList with the argument capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity of the new AttachableList.</param>
        public AttachableList(int capacity) 
        { 
            mInternalList = new List<T>(capacity);
            mInternalListAsIList = mInternalList;
        }

#if DEBUG
        public void AddInternalHashSet()
        {
            InternalHashSet = new HashSet<T>();

            ListChanged += HandleCollectionChangedForHashSet;
        }

        private void HandleCollectionChangedForHashSet(IAttachable item, NotifyCollectionChangedAction action)
        {
            switch(action)
            {
                case NotifyCollectionChangedAction.Add:
                    InternalHashSet.Add((T)item);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    InternalHashSet.Remove((T)item);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    InternalHashSet.Clear();
                    break;
                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException();
                    
                case NotifyCollectionChangedAction.Move:
                    break;
            }
        }

#endif

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Returns the top parents in the argument AttachableList
        /// </summary>
        /// <typeparam name="OutType">The type of object in the returned list.</typeparam>
        /// <typeparam name="InType">Tye type of object in the argument list</typeparam>
        /// <param name="poa">The list to search through.</param>
        /// <returns>List of T's that are the top parents of the objects in the argument AttachableList.</returns>
        public static AttachableList<OutType> GetTopParents<OutType,InType>(AttachableList<InType> poa) 
            where OutType : PositionedObject
            where InType : OutType, IAttachable
        {
            
            AttachableList<OutType> oldestParentsOneWay = new AttachableList<OutType>();
        
            foreach (InType po in poa)
            {
                oldestParentsOneWay.AddUniqueOneWay(po.TopParent as OutType);
            }

            return oldestParentsOneWay;
             
        }


        #endregion

        #region Public Methods


        #region Add methods

        /// <summary>
        /// Adds the argument to the AttachableList and creates a two-way relationship.
        /// </summary>
        /// <param name="attachable">The IAttachable to add.</param>
        public void Add(T attachable)
        {
#if DEBUG
            if(InternalHashSet != null)
            {
                if(InternalHashSet.Contains(attachable))
                {
                    throw new InvalidOperationException("Can't add the following object twice: " + attachable.Name);
                }
            }
            else if (mInternalList.Contains(attachable))
            {
                throw new InvalidOperationException("Can't add the following object twice: " + attachable.Name);
            }
#endif

            // January 4, 2012
            // Victor Chelaru
            // I think we can remove this for performance reasons...but I don't want
            // to until I have a big game I can test this on.
            // Update September 9, 2012
            // Removing now and will be testing on Baron etc
            //if (attachable.ListsBelongingTo.Contains(this) == false)
            attachable.ListsBelongingTo.Add(this);


            int countBefore = mInternalList.Count;
            mInternalList.Add(attachable);

            if (this.CollectionChanged != null)
            {
                // We put the index for Silverlight - but I don't want to do indexof for performance reasons so 0 it is
                this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, attachable, countBefore));
            }
            ListChanged?.Invoke(attachable, NotifyCollectionChangedAction.Add);
        }

        public void AddRange(IEnumerable<T> collection)
        {
            foreach (T t in collection)
                Add(t);
        }

        /// <summary>
        /// Adds all IAttachables contained in the argument AttachableList to this AttachableList and creates two
        /// way relationships.
        /// </summary>
        /// <param name="listToAdd"></param>
        public void AddRange(AttachableList<T> listToAdd)
        {
            for (int i = 0; i < listToAdd.Count; i++)
            {
                Add(listToAdd[i]);
            }
        }

        /// <summary>
        /// Adds the argument attachable to this without creating a two-way relationship.
        /// </summary>
        /// <param name="attachable">The IAttachable to add to this.</param>
        public void AddOneWay(T attachable)
        {
            if (attachable == null) return;



            mInternalList.Add(attachable);
            if (this.CollectionChanged != null)
            {
                // We put the index for Silverlight - but I don't want to do indexof for performance reasons so 0 it is
                this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, attachable, 0));
            }

            ListChanged?.Invoke(attachable, NotifyCollectionChangedAction.Add);
        }

        /// <summary>
        /// Adds all IAttachables contained in the argument AttachableList to this
        /// without creating two-way relationships.
        /// </summary>
        /// <param name="listToAdd">The list of IAttachables to add.</param>
        public void AddRangeOneWay(AttachableList<T> listToAdd)
        {
            for (int i = 0; i < listToAdd.Count; i++)
            {
                mInternalList.Add(listToAdd[i]);

                if (this.CollectionChanged != null)
                {
                    this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, listToAdd[i], i));
                }
                ListChanged?.Invoke(listToAdd[i], NotifyCollectionChangedAction.Add);
            }
        }

        /// <summary>
        /// Adds a new IAttachable if it is not already in the list.
        /// </summary>
        /// <param name="attachable">The IAttachable to add.</param>
        /// <returns>Index where the IAttachable was added.  -1 is returned if the list already contains the argument attachable</returns>
        public void AddUnique(T attachable)
        {
            if (this.Contains(attachable))
                return;
            else
                Add(attachable);
        }

        /// <summary>
        /// Adds the argument IAttachable to this and creates a two-way relationship if
        /// this does not already contain the IAttachable.
        /// </summary>
        /// <param name="attachable">The IAttachable to add.</param>
        public void AddUniqueOneWay(T attachable)
        {
            if (this.Contains(attachable))
                return;
            else
                AddOneWay(attachable);
        }

        #endregion

        /*
         I don't know if this method is needed or not - it may provide some speed improvements, but I'm
         * going to put it off for some more sure-fire speed improvemens.
        public void ChangeIndexOfObject(int oldIndex, int newIndex)
        {
            if (oldIndex > newIndex)
            {
                // Moving the object down in the list (so it comes earlier)
                T oldObject = this[oldIndex];

                for (int i = oldIndex; i > newIndex; i--)
                {
                    this[i] = this[i - 1];
                }
                this[newIndex] = oldObject;
            }
            else if (newIndex > oldIndex)
            {
                // Moving the object up in the list (so it comes later)
                T oldObject = this[oldIndex];

                for (int i = oldIndex; i < newIndex; i++)
                {

                }
            }
          
         

        }
        */

        /// <summary>
        /// Removes all IAttachables contained in this and eliminates all
        /// two-way relationships.
        /// </summary>
        public void Clear()
        {
            List<T> removed = null;
            if (this.CollectionChanged != null)
            {
                removed = new List<T>();
                removed.AddRange(mInternalList);
            }
            for (int i = 0; i < mInternalList.Count; i++)
            {
                if (mInternalList[i].ListsBelongingTo.Contains(this))
                    mInternalList[i].ListsBelongingTo.Remove(this);
            }

            mInternalList.Clear();

            if (this.CollectionChanged != null)
            {
                // We put the index for Silverlight - but I don't want to do indexof for performance reasons so 0 it is
                this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed, 0));
            }

            if(ListChanged != null)
            {
                ListChanged.Invoke(null, NotifyCollectionChangedAction.Reset);
            }
        }

        /// <summary>
        /// Returns whether this contains the argument IAttachable.
        /// </summary>
        /// <remarks>
        /// If the argument is part of this instance and the two share a 
        /// two-way relationship then this method is able to use this two-way
        /// relationship to speed up the method call.
        /// </remarks>
        /// <param name="attachable">The argument IAttachable to search for.</param>
        /// <returns>Whether the argument attachable is contained in this list.</returns>
        public bool Contains(T attachable)
        {
#if DEBUG
            if(InternalHashSet != null)
            {
                return InternalHashSet.Contains(attachable);
            }
#endif
            return (attachable != null &&
                (attachable.ListsBelongingTo.Contains(this) || mInternalList.Contains(attachable)));
        }

        /// <summary>
        /// Returns the IAttachable with name matching the argument, or null if not found.
        /// </summary>
        /// <remarks>This method performs a case-sensitive search.</remarks>
        /// <param name="nameToSearchFor">The name to match when searching.</param>
        /// <returns>The IAttachable with matching name or null if none are found.</returns>
        public T FindByName(string nameToSearchFor)
        {
            for (int i = 0; i < Count; i++)
                if ((this[i]).Name == nameToSearchFor)
                    return this[i];
            return default(T);
        }

        /// <summary>
        /// Returns the first IAttachable with a name containing the argument string.
        /// </summary>
        /// <remarks>This method returns any IAttachable that has a name that contains the argument.
        /// For example, an object with the name "MySprite" would return if the argument was "Sprite".</remarks>
        /// <param name="stringToSearchFor">The string to check IAttachables for.</param>
        /// <returns>The IAttachable with a name containing the argument string or null if none are found.</returns>
        public T FindWithNameContaining(string stringToSearchFor)
        {
            for (int i = 0; i < this.Count; i++)
            {
                T t = this[i];
                if (t.Name.Contains(stringToSearchFor))
                    return t;
            }
            
            return default(T);
        }

        /// <summary>
        /// Returns the first IAttachable with a name containing the argument string, case insensitive.
        /// </summary>
        /// <remarks>This method returns any IAttachable that has a name that contains the argument.
        /// For example, an object with the name "MySprite" would return if the argument was "Sprite".</remarks>
        /// <param name="stringToSearchFor">The string to check IAttachables for.</param>
        /// <returns>The IAttachable with a name containing the argument string or null if none are found.</returns>
        public T FindWithNameContainingCaseInsensitive(string stringToSearchFor)
        {
            for (int i = 0; i < this.Count; i++)
            {
                T t = this[i];
                if (t.Name.IndexOf(stringToSearchFor, StringComparison.OrdinalIgnoreCase) >= 0)
                    return t;
            }

            return default(T);
        }


        public AttachableList<T> FindAllWithNameContaining(string stringToSearchFor)
        {
            AttachableList<T> listToReturn = new AttachableList<T>();

            for (int i = 0; i < this.Count; i++)
            {
                T t = this[i];
                if (t.Name.Contains(stringToSearchFor))
                {
                    listToReturn.Add(t);
                }
            }

            return listToReturn;
        }



        /// <summary>
        /// Inserts the argument IAttachable at the argument index and creates a 
        /// two-way relationship.
        /// </summary>
        /// <param name="index">The index to insert at.</param>
        /// <param name="attachable">The IAttachable to insert.</param>
        public void Insert(int index, T attachable)
        {
#if DEBUG
            if (attachable == null)
                throw new ArgumentNullException("Cannot insert a null object");
#endif

            if (attachable.ListsBelongingTo.Contains(this) == false)
                attachable.ListsBelongingTo.Add(this);


            mInternalList.Insert(index, attachable);

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, attachable, 0));
            ListChanged?.Invoke(attachable, NotifyCollectionChangedAction.Add);
        }

        /// <summary>
        /// Inserts the argument IAttachable at the argument index but does not create
        /// a two-way relationship.
        /// </summary>
        /// <param name="index">The index to insert at.</param>
        /// <param name="attachable">The IAttachable to insert.</param>
        public void InsertOneWay(int index, T attachable)
        {
            mInternalList.Insert(index, attachable);

            if (this.CollectionChanged != null)
            {
                // We put the index for Silverlight - but I don't want to do indexof for performance reasons so 0 it is
                this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, attachable, 0));
            }
            ListChanged?.Invoke(attachable, NotifyCollectionChangedAction.Add);
        }

        /// <summary>
        /// Breaks all two-way relationships between this and all contained
        /// IAttachables.
        /// </summary>
        /// <remarks>
        /// This will still contain the same number of IAttachables before and
        /// after the call.
        /// </remarks>
        public void MakeOneWay()
        {
            for (int i = 0; i < this.Count; i++)
            {
                IAttachable ia = this[i];
                if (ia.ListsBelongingTo.Contains(this))
                    ia.ListsBelongingTo.Remove(this);
            }
        }

        /// <summary>
        /// Makes the relationship between all contained IAttachables and this a two way relationship.
        /// </summary>
        /// <remarks>
        /// If an IAttachable is added (through the Add method), the relationship is already a
        /// two-way relationship.  IAttachables which already have two-way relationships will not be affected
        /// by this call.  IAttachables that have been added through the AddOneWay call or added
        /// through a call that returns a one-way array will be modified so that they hold a reference to
        /// this instance in their ListsBelongingTo field.  One-way relationships are often created in
        /// FRB methods which return AttachableLists.
        /// </remarks>
        public void MakeTwoWay()
        {
            for (int i = 0; i < this.Count; i++)
            {
                IAttachable ia = this[i];
                if (ia.ListsBelongingTo.Contains(this) == false)
                    ia.ListsBelongingTo.Add(this);
            }
        }

        /// <summary>
        /// Moves the position of a block of IAttachables beginning at the argument
        /// sourceIndex of numberToMove count to the argument destinationIndex.
        /// </summary>
        /// <param name="sourceIndex">The index of the first IAttachable in the block.</param>
        /// <param name="numberToMove">The number of elements in the block.</param>
        /// <param name="destinationIndex">The index to insert the block at.</param>
        public void MoveBlock(int sourceIndex, int numberToMove, int destinationIndex)
        {
            if (destinationIndex < sourceIndex)
            {
                for (int i = 0; i < numberToMove; i++)
                {
                    mInternalList.Insert(destinationIndex + i, this[sourceIndex + i]);
                    mInternalList.RemoveAt(sourceIndex + i + 1);
                }
            }
            else
            {
                for (int i = 0; i < numberToMove; i++)
                {
                    mInternalList.Insert(destinationIndex, this[sourceIndex]);
                    mInternalList.RemoveAt(sourceIndex);
                }
            }
        }

        /// <summary>
        /// Removes the argument IAttachable from this and clears the two-way relationship.
        /// </summary>
        /// <param name="attachable">The IAttachable to remove from this.</param>
        public void Remove(T attachable)
        {
            if (attachable.ListsBelongingTo.Contains(this))
            {
                attachable.ListsBelongingTo.Remove(this);
            }

            // Vic says:  This makes things safer, but can also hurt performance.  Should we leave this in?
            // Update on March 7, 2011 - this kills performance for particles on the phone.  We gotta take it
            // out
            //if (mInternalList.Contains(attachable))
                mInternalList.Remove(attachable);


            if (this.CollectionChanged != null)
            {
                // We put the index for Silverlight - but I don't want to do indexof for performance reasons so 0 it is
                this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, attachable, 0));
            }
            ListChanged?.Invoke(attachable, NotifyCollectionChangedAction.Remove);
        }

        /// <summary>
        /// Removes all IAttachables contained in the argument attachableList from this and clears the two-way relationships between
        /// this and all IAttachables removed.
        /// </summary>
        /// <param name="attachableList">The list of IAttachables to remove.</param>
        public void Remove(AttachableList<T> attachableList)
        {
            for (int i = 0; i < attachableList.Count; i++)
            {
                Remove(attachableList[i]);
            }
        }

        /// <summary>
        /// Removes the IAttachable at the argument index and clears two-way relationships.
        /// </summary>
        /// <param name="index">The index of the object to remove.</param>
        public void RemoveAt(int index)
        {
            T removed = mInternalList[index];

            if (mInternalList[index].ListsBelongingTo.Contains(this))
                mInternalList[index].ListsBelongingTo.Remove(this);
            mInternalList.RemoveAt(index);

            if (this.CollectionChanged != null)
            {
                // We put the index for Silverlight - but I don't want to do indexof for performance reasons so 0 it is
                this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed, 0));
            }
            ListChanged?.Invoke(removed, NotifyCollectionChangedAction.Remove);
        }

        /// <summary>
        /// Removes the IAttachable at the argument index from the list, but the IAttachable will continue to reference
        /// this List in its ListsBelongingTo.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAtOneWay(int index)
        {
            T removed = mInternalList[index];

            mInternalList.RemoveAt(index);

            if (this.CollectionChanged != null)
            {
                // We put the index for Silverlight - but I don't want to do indexof for performance reasons so 0 it is
                this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed, 0));
            }
            ListChanged?.Invoke(removed, NotifyCollectionChangedAction.Remove);
        }

        public void Sort(Comparison<T> comparison)
        {
            mInternalList.Sort(comparison);
        }

        public void Sort(IComparer<T> comparer)
        {
            // Calling Sort on the List is an unstable sort. This
            // results in flickering. We want to implement our own sorting
            // algorithm. We'll do the naive sort used elsewhere:
            // mInternalList.Sort(comparer);

            if (Count == 1 || Count == 0)
                return;

            int whereObjectBelongs;

            for (int i = 1; i < Count; i++)
            {
                var itemAti = this[i];

                if (comparer.Compare(itemAti, this[i-1]) < 0)
                {
                    if (i == 1)
                    {
                        Insert(0, itemAti);
                        RemoveAtOneWay(i + 1);
                        continue;
                    }

                    for (whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                    {
                        if (comparer.Compare(itemAti, this[whereObjectBelongs]) >= 0)
                        {
                            Insert(whereObjectBelongs + 1, itemAti);
                            RemoveAtOneWay(i + 1);
                            break;
                        }
                        else if (whereObjectBelongs == 0 && comparer.Compare(itemAti, this[0]) < 0)
                        {
                            Insert(0, itemAti);
                            RemoveAtOneWay(i + 1);
                            break;
                        }
                    }
                }
            }

        }


        public void SortNameAscending()
        {
            if (Count == 1 || Count == 0)
                return;

            int whereObjectBelongs;

            for (int i = 1; i < Count; i++)
            {
                if ((this[i]).Name.CompareTo(this[i - 1].Name) < 0)
                {
                    if (i == 1)
                    {
                        Insert(0, this[i]);
                        RemoveAtOneWay(i + 1);
                        continue;
                    }

                    for (whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                    {
                        if ((this[i]).Name.CompareTo(this[whereObjectBelongs].Name) >= 0)
                        {
                            Insert(whereObjectBelongs + 1, this[i]);
                            RemoveAtOneWay(i + 1);
                            break;
                        }
                        else if (whereObjectBelongs == 0 && (this[i]).Name.CompareTo(this[0].Name) < 0)
                        {
                            Insert(0, this[i]);
                            RemoveAtOneWay(i + 1);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a string with the name and the number of elements that this contains.
        /// </summary>
        /// <returns>The string with this instance's name and element count.</returns>
        public override string ToString()
        {
            return mName + ": " + this.Count;
        }


        #endregion

        #region Protected Methods



        #endregion

        #endregion


        #region IList<T> Members

        public int IndexOf(T item)
        {
            return mInternalList.IndexOf(item);
        }

        public T this[int index]
        {
            get => mInternalList[index];
            set => mInternalList[index] = value;
        }

        #endregion

        #region ICollection<T> Members


        public void CopyTo(T[] array, int arrayIndex)
        {
            mInternalList.CopyTo(array, arrayIndex);
        }

        public bool IsReadOnly
        {
            get { return ((IList)mInternalList).IsReadOnly; }
        }

        bool ICollection<T>.Remove(T item)
        {
            // so inefficient
            bool returnValue = this.Contains(item);
            Remove((T)item);
            return returnValue;
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < mInternalList.Count; i++)
            {
                yield return mInternalList[i];
            }
            //return mInternalList.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < mInternalList.Count; i++)
            {
                yield return mInternalList[i];
            }
//            return mInternalList.GetEnumerator();
        }

        #endregion

        #region IList Members

        //public int Add(object value)
        //{
            
        //}

        int IList.Add(object value)
        {
            int returnValue = this.Count;

            this.Add((T)value);

            return returnValue;


        }

        bool IList.Contains(object value)
        {
            return ((IList)mInternalList).Contains(value);
        }

        int IList.IndexOf(object value)
        {
            return ((IList)mInternalList).IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            this.Insert(index, (T)value);
        }

        bool IList.IsFixedSize
        {
            get { return ((IList)mInternalList).IsFixedSize; }
        }

        void IList.Remove(object value)
        {
            Remove((T)value);
        }

        object IList.this[int index]
        {
            get
            {
                return mInternalList[index];
            }
            set
            {
                ((IList)mInternalList)[index] = value;
            }
        }

        #endregion

        #region ICollection Members

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)mInternalList).CopyTo(array, index);
        }

        bool ICollection.IsSynchronized
        {
            get { return ((ICollection)mInternalList).IsSynchronized; }
        }

        object ICollection.SyncRoot
        {
            get { return ((ICollection)mInternalList).SyncRoot; }
        }

        #endregion

        #region IAttachableRemovable Members

        void IAttachableRemovable.RemoveGuaranteedContain(IAttachable attachable)
        {
            attachable.ListsBelongingTo.Remove(this);

            mInternalListAsIList.Remove(attachable);

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, attachable, 0));
            ListChanged?.Invoke(attachable, NotifyCollectionChangedAction.Remove);
        }

        #endregion

        #region IList Members

        void IList.Clear() => Clear();

        bool IList.IsReadOnly => IsReadOnly; 

        void IList.RemoveAt(int index) => RemoveAt(index);

        #endregion

        #region ICollection Members


        int ICollection.Count => mInternalList.Count; 

        #endregion

        /// <summary>
        /// Event raised when this collection changes (eg item is added or removed). Note that this
        /// allocates internally resulting potentially expensive garbage collection. Consider using ListChanged.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event Action<IAttachable, NotifyCollectionChangedAction> ListChanged;
    }
}
