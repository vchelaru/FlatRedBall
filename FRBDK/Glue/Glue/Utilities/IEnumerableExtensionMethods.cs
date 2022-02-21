using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Utilities
{
    public static class IEnumerableExtensionMethods
    {
        public static IEnumerable Where(this IEnumerable enumerable, Func<object, bool> predicate)
        {
            foreach (var item in enumerable)
            {
                if (predicate(item))
                {
                    yield return item;
                }
            }
        }

        public static bool ContainsAny<T>(this IEnumerable<T> enumerable, IEnumerable<T> other)
        {
            foreach(var item in other)
            {
                if(enumerable.Contains(item))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
