using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Utilities
{
    #region Methods
    /// <summary>
    /// Class providing methods which can help during debugging.
    /// </summary>
    #endregion
    public static class DebuggingFunctions
    {
        static StringBuilder innerStringBuilder = new StringBuilder();
        static StringBuilder listStringBuilder = new StringBuilder();

        public static string ToBits(byte byteToConvert)
        {
            innerStringBuilder.Remove(0, innerStringBuilder.Length);

            for (int i = 7; i > -1; i--)
            {
                if ((byteToConvert & (1 << i)) == (1 << i))
                {
                    innerStringBuilder.Append("1");
                }
                else
                {
                    innerStringBuilder.Append("0");
                }

            }
            return innerStringBuilder.ToString();
        }

        public static string ToBits(byte[] bytesToConvert)
        {
            listStringBuilder.Remove(0, listStringBuilder.Length);

            for (int i = 0; i < bytesToConvert.Length; i++)
            {
                listStringBuilder.Append(ToBits(bytesToConvert[i]));
                listStringBuilder.Append("  ");
            }

            return listStringBuilder.ToString();
        }
    }
}
