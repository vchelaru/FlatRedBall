using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    /// <summary>
    /// HashSet for Xbox360.
    /// 
    /// NOTE:
    /// This HastSet was designed to minimize block allocations.  I could have 
    /// implemented it more like a true Hash; allocating an array of linked lists
    /// but this felt cleaner.  I don't know the internals of Dictionary<> but I'm 
    /// guessing it's similar to a heap or std::map<> in c++, which is generally 
    /// implemented as a binary search tree.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class HashSet<T> : IEnumerable<T> where T : class
    {
        Dictionary<T, bool> data = new Dictionary<T, bool>();

        public HashSet()
        {
        }

        public void Add(T t)
        {
            if (Contains(t) == false)
            {
                data.Add(t, true);
            }
        }

        public void Clear()
        {
            data.Clear();
        }

        public int Count { get { return data.Count; } }

        public bool Contains(T t)
        {
            return data.ContainsKey(t);
        }

        struct ValueEnumerator : IEnumerator<T>
        {
            internal ValueEnumerator(Dictionary<T, bool> hashset)
            {
                data = hashset;
                values = data.GetEnumerator();
            }

            Dictionary<T, bool> data;
            Dictionary<T, bool>.Enumerator values;

            public T Current
            {
                get { return values.Current.Key; }
            }

            public void Dispose()
            {
                values.Dispose();
            }

            object System.Collections.IEnumerator.Current
            {
                get { return values.Current; }
            }

            public void Reset()
            {
                values = data.GetEnumerator();
            }

            public bool MoveNext()
            {
                return values.MoveNext();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ValueEnumerator(data);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new ValueEnumerator(data);
        }
    }
}
