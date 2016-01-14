using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Threading
{
    public struct BufferedInsertion<T>
    {
        public T Item;
        public int Index;
    }

    public class ListBuffer<T>
    {
        #region Fields

        IList<T> mListToEmptyTo;
        List<T> mAddBuffer = new List<T>();
        List<T> mRemoveBuffer = new List<T>();
        List<BufferedInsertion<T>> mBufferedInsertions = new List<BufferedInsertion<T>>();

        #endregion

        #region Methods

        #region Constructor

        public ListBuffer(IList<T> listToEmptyTo)
        {
            mListToEmptyTo = listToEmptyTo;
        }

        #endregion

        #region Public Methods

        public void Add(T itemToAdd)
        {

            if (mRemoveBuffer.Contains(itemToAdd))
            {
                lock (mRemoveBuffer)
                {
                    mRemoveBuffer.Remove(itemToAdd);
                }
            }
            else
            {
                lock (mAddBuffer)
                {
                    mAddBuffer.Add(itemToAdd);
                }
            }
        }

        public void BufferedRemove(T item)
        {
            if (mAddBuffer.Contains(item))
            {
                lock (mAddBuffer)
                {
                    mAddBuffer.Remove(item);
                }
            }
            else
            {
                lock (mRemoveBuffer)
                {
                    mRemoveBuffer.Add(item);
                }
            }

        }

        public bool Contains(T item)
        {
            return mAddBuffer.Contains(item);
        }

        public void Flush()
        {
            #region Perform removal
            if (mRemoveBuffer.Count != 0)
            {
                lock (mRemoveBuffer)
                {
                    foreach (T t in mRemoveBuffer)
                    {
                        mListToEmptyTo.Remove(t);
                    }

                    mRemoveBuffer.Clear();

                }

            }
            #endregion

            #region Perform Addition

            if (mAddBuffer.Count != 0)
            {
                lock (mAddBuffer)
                {
                    foreach (T t in mAddBuffer)
                    {
                        mListToEmptyTo.Add(t);
                    }

                    mAddBuffer.Clear();
                }
            }
            #endregion

            #region Perform Insertion

            if (mBufferedInsertions.Count != 0)
            {
                lock (mBufferedInsertions)
                {
                    foreach (BufferedInsertion<T> insertion in mBufferedInsertions)
                    {
                        mListToEmptyTo.Insert(insertion.Index, insertion.Item);
                    }
                    mBufferedInsertions.Clear();
                }

            }

            #endregion
        }

        public void Insert(int index, T item)
        {
            BufferedInsertion<T> bi = new BufferedInsertion<T>();
            bi.Item = item;
            bi.Index = index;

            mBufferedInsertions.Add(bi);

            if (mRemoveBuffer.Contains(item))
            {
                lock (mRemoveBuffer)
                {
                    mRemoveBuffer.Remove(item);
                }
            }
        }

        #endregion

        #endregion
    }
}
