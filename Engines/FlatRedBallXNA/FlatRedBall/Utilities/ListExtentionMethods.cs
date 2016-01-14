using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Utilities
{
    public static class ListExtentionMethods
    {
        static List<int> ListToDelete = new List<int>();

        public static void RemoveAll<T>(this List<T> thisList, Predicate<T> p) 
        {
            ListToDelete.Clear();

            for(int i = 0; i < thisList.Count; i++)
            {
                T elementAtIndex = thisList[i];

                if (p(elementAtIndex))
                {
                    ListToDelete.Add(i);
                }
            }

            for (int i = thisList.Count - 1; i > -1; i--)
            {
                thisList.RemoveAt(ListToDelete[i]);
            }
        } 
    }
}
