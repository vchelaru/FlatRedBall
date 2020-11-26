using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace System.Linq
{
    public class LambdaComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _expression;

        public LambdaComparer(Func<T, T, bool> lambda)
        {
            _expression = lambda;
        }

        public bool Equals(T x, T y)
        {
            return _expression(x, y);
        }

        public int GetHashCode(T obj)
        {
            /*
             If you just return 0 for the hash the Equals comparer will kick in. 
             The underlying evaluation checks the hash and then short circuits the evaluation if it is false.
             Otherwise, it checks the Equals. If you force the hash to be true (by assuming 0 for both objects), 
             you will always fall through to the Equals check which is what we are always going for.
            */
            return 0;
        }
    }

    public static class LinqExtensions
    {
        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> list, Func<T, T, bool> lambda)
        {
            return list.Distinct(new LambdaComparer<T>(lambda));
        }

        public static bool Any(this UIElementCollection collection, Func<UIElement, bool> predicate)
        {
            foreach(UIElement item in collection)
            {
                if(predicate(item))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
