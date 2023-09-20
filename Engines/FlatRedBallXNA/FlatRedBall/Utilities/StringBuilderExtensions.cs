using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Utilities
{
    public static class StringBuilderExtensions
    {
        public static bool Contains(this StringBuilder sb, string whatToSearchFor)
        {
            int index = sb.IndexOf(whatToSearchFor);

            return index != -1;
        }

        public static void InsertLine(this StringBuilder sb, ref int index, string value)
        {
            sb.Insert(index, value + "\r\n");
            index += value.Length + 2;
        }

        /// <summary>
        /// Get index of a char
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static int IndexOf(this StringBuilder sb, char value)
        {
            return IndexOf(sb, value, 0);
        }

        /// <summary>
        /// Get index of a char starting from a given index
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="c"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static int IndexOf(this StringBuilder sb, char value, int startIndex)
        {
            for (int i = startIndex; i < sb.Length; i++)
            {
                if (sb[i] == value)
                {
                    return i;
                }
            }
            return -1;
        }

        public static int IndexOfAny(this StringBuilder sb, char[] values, int startIndex)
        {
            for (int i = startIndex; i < sb.Length; i++)
            {
                for(int valueIndex = 0; valueIndex < values.Length; valueIndex++)
                {
                    if (sb[i] == values[valueIndex])
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Get index of a string
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static int IndexOf(this StringBuilder sb, string value)
        {
            return IndexOf(sb, value, 0, false);
        }

        /// <summary>
        /// Get index of a string from a given index
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="text"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static int IndexOf(this StringBuilder sb, string value, int startIndex)
        {
            return IndexOf(sb, value, startIndex, false);
        }

        /// <summary>
        /// Get index of a string with case option
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="text"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static int IndexOf(this StringBuilder sb, string value, bool ignoreCase)
        {
            return IndexOf(sb, value, 0, ignoreCase);
        }

        /// <summary>
        /// Get index of a string from a given index with case option
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="text"></param>
        /// <param name="startIndex"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static int IndexOf(this StringBuilder sb, string value, int startIndex, bool ignoreCase)
        {
            int num3;
            int length = value.Length;
            int num2 = (sb.Length - length) + 1;
            if (ignoreCase == false)
            {
                for (int i = startIndex; i < num2; i++)
                {
                    if (sb[i] == value[0])
                    {
                        num3 = 1;
                        while ((num3 < length) && (sb[i + num3] == value[num3]))
                        {
                            num3++;
                        }
                        if (num3 == length)
                        {
                            return i;
                        }
                    }
                }
            }
            else
            {
                for (int j = startIndex; j < num2; j++)
                {
                    if (char.ToLowerInvariant(sb[j]) == char.ToLowerInvariant(value[0]))
                    {
                        num3 = 1;
                        while ((num3 < length) && (char.ToLowerInvariant(sb[j + num3]) == char.ToLowerInvariant(value[num3])))
                        {
                            num3++;
                        }
                        if (num3 == length)
                        {
                            return j;
                        }
                    }
                }
            }
            return -1;
        }

        public static string Substring(this StringBuilder sb, int startIndex, int length)
        {
            return sb.ToString(startIndex, length);
        }

        public static int LastIndexOf(this StringBuilder sb, string value)
        {
            // Eventually let's make this faster:
            return sb.ToString().LastIndexOf(value, StringComparison.OrdinalIgnoreCase);

        }
 
    }
}
