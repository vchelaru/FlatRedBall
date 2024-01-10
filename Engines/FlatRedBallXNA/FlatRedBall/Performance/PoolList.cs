using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Performance
{
    public class PoolList<T> : IEnumerable<T> where T : IPoolable
    {
        #region Fields
        List<T> mPoolables = new List<T>();
        int mNextAvailable = -1;
        public int Count => mPoolables.Count;
        #endregion

        public bool HasAnyUnusedItems
        {
            get { return mNextAvailable != -1; }
        }

        #region Methods

        public void AddToPool(T poolableToAdd)
        {

            int index = mPoolables.Count;

            if (mNextAvailable == -1)
            {
                mNextAvailable = index;
            }

            mPoolables.Add(poolableToAdd);
            poolableToAdd.Index = index;
            poolableToAdd.Used = false;
        }

        public void Clear()
        {
            mPoolables.Clear();
            mNextAvailable = -1;
        }

        public T GetNextAvailable()
        {
            if (mNextAvailable == -1 || mPoolables.Count == 0)
            {
                return default(T);
            }

            T returnReference = mPoolables[mNextAvailable];
            returnReference.Used = true;

            // find next available
            int count = mPoolables.Count;

            mNextAvailable = -1;

            for (int i = returnReference.Index + 1; i < count; i++)
            {
                T poolable = mPoolables[i];

                if (poolable.Used == false)
                {
                    mNextAvailable = i;
                    break;
                }
            }

            return returnReference;
        }

        public void MakeAllUnused()
        {
            int count = mPoolables.Count;

            for (int i = 0; i < count; i++)
            {
                MakeUnused(mPoolables[i]);
            }
        }
        
        public void MakeUnused(T poolableToMakeUnused)
        {
            if (mNextAvailable == -1 || poolableToMakeUnused.Index < mNextAvailable)
            {
                mNextAvailable = poolableToMakeUnused.Index;
            }

            poolableToMakeUnused.Used = false;
        }

        #endregion


        public IEnumerator<T> GetEnumerator()
        {
            return mPoolables.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return mPoolables.GetEnumerator();
        }
    }
}
