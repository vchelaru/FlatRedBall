using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Extensions
{
    public static class IListExtensions
    {
        public static object FirstOrDefault(this IList list, Func<object, bool> func)
        {
            foreach (var item in list)
            {
                if (func(item))
                {
                    return item;
                }
            }

            return null;
        }
    }
}
