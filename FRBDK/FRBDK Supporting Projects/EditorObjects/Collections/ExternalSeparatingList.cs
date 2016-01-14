using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EditorObjects.Collections
{
    public class ExternalSeparatingList<T> : List<T>
    {
        List<T> mExternalFiles = new List<T>();




        public void AddExternal(T item)
        {
            this.Add(item);
            mExternalFiles.Add(item);
        }

        public void AddExternalRange(IEnumerable<T> collection)
        {
            this.AddRange(collection);
            mExternalFiles.AddRange(collection);
        }

        public void MakeExternal(T item)
        {
            if (this.Contains(item))
            {
                this.Remove(item);
                this.mExternalFiles.Add(item);

            }
        }

        public void RemoveExternals()
        {
            foreach (var item in mExternalFiles)
            {
                this.Remove(item);
            }
        }

        public void ReAddExternals()
        {
            this.AddRange(mExternalFiles);
        }



    }
}
