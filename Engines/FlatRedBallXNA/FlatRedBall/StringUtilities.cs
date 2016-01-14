using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Utilities
{
    static class StringUtilities
    {
        public static int GetIntAfter(string stringToSearchFor, string whereToSearch)
        {
            return GetIntAfter(stringToSearchFor, whereToSearch, 0);
        }

        public static int GetIntAfter(string stringToSearchFor, string whereToSearch, int startIndex)
        {
            int startOfNumber = -1;
            int endOfNumber = -1;
            int enterAt = -1;
            string substring = "uninitialized";

            try
            {
                int indexOf = whereToSearch.IndexOf(stringToSearchFor, startIndex);
                if (indexOf != -1)
                {
                    startOfNumber = indexOf + stringToSearchFor.Length;
                    endOfNumber = whereToSearch.IndexOf(' ', startOfNumber);
                    enterAt = whereToSearch.IndexOf('\n', startOfNumber);
                    if (whereToSearch.IndexOf('\r', startOfNumber) < enterAt)
                        enterAt = whereToSearch.IndexOf('\r', startOfNumber);

                    if (endOfNumber == -1 || enterAt < endOfNumber)
                        endOfNumber = enterAt;
                    if (endOfNumber == -1)
                        endOfNumber = whereToSearch.Length;

                    substring = whereToSearch.Substring(startOfNumber,
                        endOfNumber - startOfNumber);

                    // this method is called when reading from a file.  
                    // usually, files use the . rather than other numerical formats, so if this fails, just use the regular . format
                    int toReturn = int.Parse(substring);
                    return toReturn;

                }
            }
            catch (System.FormatException)
            {
                return int.Parse(substring, System.Globalization.NumberFormatInfo.InvariantInfo);

            }

            return 0;

        }
    }
}
