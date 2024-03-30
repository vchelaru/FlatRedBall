using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace FlatRedBall.ExtensionMethods
{
    public static class EnumerableExtensionMethods
    {
        public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> enumerable)
        {
            foreach(var item in enumerable)
            {
                hashSet.Add(item);
            }
        }
    }
}
