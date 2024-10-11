using System;
using System.Collections.Generic;
using System.Text;

namespace Beefball.Performance
{
    public class PoolList<T> where T : FlatRedBall.Performance.IPoolable
    {
        #region Fields
        List<T> mPoolables = new List<T>();
        int mNextAvailable = -1;
        public int Count => mPoolables.Count;
        #endregion

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
            // This should be 0.
            //mNextAvailable = -1;
            // The reason is that if
            // we set it to -1, then make
            // an Entity unused, then the MakeUnused
            // method may set the value to a value that
            // is not valid.  We don't need to use -1 as
            // a value to indicate this is uninitialized, the
            // factories themselves will do this
            mNextAvailable = 0;
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

        public void MakeUnused(T poolableToMakeUnused)
        {
            if (mNextAvailable == -1 || poolableToMakeUnused.Index < mNextAvailable)
            {
                mNextAvailable = poolableToMakeUnused.Index;
            }

            poolableToMakeUnused.Used = false;
        }

        #endregion

    }
}
