using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography.X509Certificates;


#if !FRB_RAW
using Microsoft.Xna.Framework;
#endif

namespace FlatRedBall.Utilities
{
    /// <summary>
    /// A class containing common string maniuplation methods.
    /// </summary>
    public static class StringFunctions
    {
        #region Fields

        static HashSet<char> sValidNumericalChars = new HashSet<char> { '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', ',' };


        #endregion


        public static bool IsAscii(string stringToCheck)
        {
            bool isAscii = true;
            
            for (int i = 0; i < stringToCheck.Length; i++)
            {
                if (stringToCheck[i] > 255)
                {
                    isAscii = false;
                    break;
                }
            }

            return isAscii;
        }

        public static int CountOf(this string instanceToSearchIn, char characterToSearchFor)
        {
            return instanceToSearchIn.CountOf(characterToSearchFor, 0, instanceToSearchIn.Length);
        }

        public static int CountOf(this string instanceToSearchIn, char characterToSearchFor, int startIndex, int searchLength)
        {
            int count = 0;

            for (int i = startIndex; i < searchLength; i++)
            {
                char characterAtIndex = instanceToSearchIn[i];

                if (characterAtIndex == characterToSearchFor)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Returns the number of times that the argument whatToFind is found in the calling string.
        /// </summary>
        /// <param name="instanceToSearchIn">The string to search within.</param>
        /// <param name="whatToFind">The string to search for.</param>
        /// <returns>The number of instances found</returns>
        public static int CountOf(this string instanceToSearchIn, string whatToFind)
        {
            int count = 0;
            int index = 0;
            while (true)
            {
                int foundIndex = instanceToSearchIn.IndexOf(whatToFind, index);

                if (foundIndex != -1)
                {
                    count++;

                    index = foundIndex + 1;
                }
                else
                {
                    break;
                }
            }
            return count;

        }

        public static bool Contains(this string[] listToInvestigate, string whatToSearchFor)
        {
            for (int i = 0; i < listToInvestigate.Length; i++)
            {
                if (listToInvestigate[i] == whatToSearchFor)
                {
                    return true;
                }
            }

            return false;

        }

        #region XML Docs
        /// <summary>
        /// Returns the number of non-whitespace characters in the argument stringInQuestion.
        /// </summary>
        /// <remarks>
        /// This method is used internally by the TextManager to determine the number of vertices needed to
        /// draw a Text object.
        /// </remarks>
        /// <param name="stringInQuestion">The string to have its non-witespace counted.</param>
        /// <returns>The number of non-whitespace characters counted.</returns>
        #endregion
        public static int CharacterCountWithoutWhitespace(string stringInQuestion)
        {
            int characterCount = 0;
            for (int i = 0; i < stringInQuestion.Length; i++)
            {
                if (stringInQuestion[i] != ' ' && stringInQuestion[i] != '\t' && stringInQuestion[i] != '\n')
                    characterCount++;
            }
            return characterCount;
        }

        #region XML Docs
        /// <summary>
        /// Returns a string of the float with the argument decimalsAfterPoint digits of resolution after the point.
        /// </summary>
        /// <param name="floatToConvert">The float to convert.</param>
        /// <param name="decimalsAfterPoint">The number of decimals after the point.  For example, 3.14159 becomes "3.14" if the
        /// decimalsAfterPoint is 2.  This method will not append extra decimals to reach the argument decimalsAfterPoint.</param>
        /// <returns>The string representation of the argument float.</returns>
        #endregion
        public static string FloatToString(float floatToConvert, int decimalsAfterPoint)
        {
            if (decimalsAfterPoint == 0)
                return ((int)floatToConvert).ToString();
            else
            {
                int lengthAsInt = ((int)floatToConvert).ToString().Length;

                return floatToConvert.ToString().Substring(
                    0, System.Math.Min(floatToConvert.ToString().Length, lengthAsInt + 1 + decimalsAfterPoint));
            }
        }

        #region XML Docs
        /// <summary>
        /// Returns the character that can be found after a particular sequence of characters.
        /// </summary>
        /// <remarks>
        /// This will return the first character following a particular sequence of characters.  For example, 
        /// GetCharAfter("bcd", "abcdef") would return 'e'.
        /// </remarks>
        /// <param name="stringToSearchFor">The string to search for.</param>
        /// <param name="whereToSearch">The string to search in.</param>
        /// <returns>Returns the character found or the null character '\0' if the string is not found.</returns>
        #endregion
        public static char GetCharAfter(string stringToSearchFor, string whereToSearch)
        {
            int indexOf = whereToSearch.IndexOf(stringToSearchFor);
            if (indexOf != -1)
            {
                return whereToSearch[indexOf + stringToSearchFor.Length];
            }

            return '\0';
        }

        public static int GetClosingCharacter(string fullString, int startIndex, char openingCharacter, char closingCharacter)
        {
            int numberofParens = 1;
            for (int i = startIndex; i < fullString.Length; i++)
            {
                if (fullString[i] == openingCharacter)
                {
                    numberofParens++;
                }
                if (fullString[i] == closingCharacter)
                {
                    numberofParens--;
                }
                if (numberofParens == 0)
                {
                    return i;
                }
            }

            return -1;
        }


        public static int GetDigitCount(double x)
        {
            // Written by Kao Martin 12/6/2008

            int count = 1;
            double diff = x - System.Math.Floor(x);

            while ((x /= 10.0d) > 1.0d)
            {
                count++;
            }
            if (diff > 0)
            {
                count++;
                while ((diff *= 10.0d) > 1.0d)
                {
                    count++;
                    diff -= System.Math.Floor(diff);
                }
            }

            return count;
        }

        #region XML Docs
        /// <summary>
        /// Returns the float that can be found after a particular sequence of characters.
        /// </summary>
        /// <remarks>
        /// This will return the float following a particular sequence of characters.  For example, 
        /// GetCharAfter("height = 6; width = 3; depth = 7;", "width = ") would return 3.0f. 
        /// </remarks>
        /// <param name="stringToSearchFor">The string to search for.</param>
        /// <param name="whereToSearch">The string to search in.</param>
        /// <returns>Returns the float value found or float.NaN if the string is not found.</returns>
        #endregion
        public static float GetFloatAfter(string stringToSearchFor, string whereToSearch)
        {
            return GetFloatAfter(stringToSearchFor, whereToSearch, 0);
        }


        public static float GetFloatAfter(string stringToSearchFor, string whereToSearch, int startIndex)
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

                    for (int i = startOfNumber; i < endOfNumber; i++)
                    {
                        bool found = sValidNumericalChars.Contains(whereToSearch[i]);

                        if (!found)
                        {
                            // if we got here, then the character is not valid, so end the string
                            endOfNumber = i;
                            break;
                        }

                    }

                    if (endOfNumber == -1 || (enterAt != -1 && enterAt < endOfNumber))
                        endOfNumber = enterAt;
                    if (endOfNumber == -1)
                        endOfNumber = whereToSearch.Length;

                    substring = whereToSearch.Substring(startOfNumber,
                        endOfNumber - startOfNumber);

                    // this method is called when reading from a file.  
                    // usually, files use the . rather than other numerical formats, so if this fails, just use the regular . format
                    float toReturn = float.Parse(substring);

                    // Let's see if this is using exponential notation, like 5.21029e-007
                    if (endOfNumber < whereToSearch.Length - 1)
                    {
                        // Is there an "e" there?
                        if (whereToSearch[endOfNumber] == 'e')
                        {
                            int exponent = GetIntAfter("e", whereToSearch, endOfNumber);

                            float multiplyValue = (float)System.Math.Pow(10, exponent);

                            toReturn *= multiplyValue;

                        }
                    } 
                    
                    return toReturn;

                }
            }
            catch (System.FormatException)
            {
                return float.Parse(substring, System.Globalization.NumberFormatInfo.InvariantInfo);

            }

            return float.NaN;
        }

        #region XML Docs
        /// <summary>
        /// Returns the first integer found after the argument stringToSearchFor in whereToSearch.
        /// </summary>
        /// <remarks>
        /// This method is used to help simplify parsing of text files and data strings.
        /// If stringToSearchFor is "Y:" and whereToSearch is "X: 30, Y:32", then the value
        /// of 32 will be returned.
        /// </remarks>
        /// <param name="stringToSearchFor">The string pattern to search for.</param>
        /// <param name="whereToSearch">The string that will be searched.</param>
        /// <returns>The integer value found after the argument stringToSearchFor.</returns>
        #endregion
        public static int GetIntAfter(string stringToSearchFor, string whereToSearch)
        {
            return GetIntAfter(stringToSearchFor, whereToSearch, 0);
        }

        #region XML Docs
        /// <summary>
        /// Returns the first integer found after the argument stringToSearchFor.  The search begins
        /// at the argument startIndex.
        /// </summary>
        /// <param name="stringToSearchFor">The string pattern to search for.</param>
        /// <param name="whereToSearch">The string that will be searched.</param>
        /// <param name="startIndex">The index to begin searching at.  This method
        /// will ignore any instances of stringToSearchFor which begin at an index smaller
        /// than the argument startIndex.</param>
        /// <returns></returns>
        #endregion
        public static int GetIntAfter(string stringToSearchFor, string whereToSearch, int startIndex)
        {
            int startOfNumber = -1;
            int endOfNumber = -1;
            string substring = string.Empty;

            try
            {
                int indexOf = whereToSearch.IndexOf(stringToSearchFor, startIndex);

                if (indexOf != -1)
                {
                    startOfNumber = indexOf + stringToSearchFor.Length;
                    endOfNumber = startIndex;

                    for(int i = startOfNumber; i < whereToSearch.Length; i++)
                    {
                        if (!sValidNumericalChars.Contains(whereToSearch[i]))
                        {
                            break;
                        }
                        else
                        {
                            endOfNumber = i+1;
                        }
                    }

                    if(endOfNumber != startOfNumber)
                    {
                        substring = whereToSearch.Substring(startOfNumber,
                            endOfNumber - startOfNumber);

                        // this method is called when reading from a file.  
                        // usually, files use the . rather than other numerical formats, so if this fails, just use the regular . format
                        int toReturn = int.Parse(substring);
                        return toReturn;
                    }
                }
            }
            catch (System.FormatException)
            {
                return int.Parse(substring, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            return 0;
        }

        public static int GetIntAfter(string stringToSearchFor, string whereToSearch, ref int startIndex)
        {
            int startOfNumber = -1;
            int endOfNumber = -1;

#if NET6_0_OR_GREATER
            ReadOnlySpan<char> span = null;
#else
            string substring = string.Empty;
#endif

            try
            {
                int indexOf = whereToSearch.IndexOf(stringToSearchFor, startIndex);

                if (indexOf != -1)
                {
                    startOfNumber = indexOf + stringToSearchFor.Length;
                    endOfNumber = startIndex;

                    startIndex = startOfNumber;
                    for (int i = startOfNumber; i < whereToSearch.Length; i++)
                    {
                        startIndex = i + 1;
                        if (!sValidNumericalChars.Contains(whereToSearch[i]))
                        {
                            break;
                        }
                        else
                        {
                            endOfNumber = i + 1;
                        }
                    }

                    if (endOfNumber > startOfNumber)
                    {
#if NET6_0_OR_GREATER
                        span = whereToSearch.AsSpan(startOfNumber, endOfNumber - startOfNumber);
                        int toReturn = int.Parse(span);
#else
                        substring = whereToSearch.Substring(startOfNumber, endOfNumber - startOfNumber);
                        // this method is called when reading from a file.  
                        // usually, files use the . rather than other numerical formats, so if this fails, just use the regular . format
                        int toReturn = int.Parse(substring);
#endif

                        return toReturn;
                    }
                }
            }
            catch (System.FormatException)
            {
#if NET6_0_OR_GREATER
                return int.Parse(span, System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo);
#else
                return int.Parse(substring, System.Globalization.NumberFormatInfo.InvariantInfo);
#endif
            }
            return 0;
        }

        static char[] WhitespaceChars = new char[] { ' ', '\n', '\t', '\r' };


        public static string GetWordAfter(string stringToStartAfter, string entireString)
        {
            return GetWordAfter(stringToStartAfter, entireString, 0);
        }

        public static string GetWordAfter(string stringToStartAfter, string entireString, int indexToStartAt)
        {
            return GetWordAfter(stringToStartAfter, entireString, indexToStartAt, StringComparison.Ordinal);
        }

        public static string GetWordAfter(string stringToStartAfter, string entireString, int indexToStartAt, StringComparison comparison)
        {
            int indexOf = entireString.IndexOf(stringToStartAfter, indexToStartAt, StringComparison.OrdinalIgnoreCase);
            if (indexOf != -1)
            {
                int startOfWord = indexOf + stringToStartAfter.Length;

                // Let's not count the start of the word if it's a newline
                while ( entireString[startOfWord] == WhitespaceChars[0] ||
                    entireString[startOfWord] == WhitespaceChars[1] ||
                    entireString[startOfWord] == WhitespaceChars[2] ||
                    entireString[startOfWord] == WhitespaceChars[3])
                {
                    startOfWord++;
                }

                int endOfWord = entireString.IndexOfAny(WhitespaceChars, startOfWord);
                if (endOfWord == -1)
                {
                    endOfWord = entireString.Length;
                }

                return entireString.Substring(startOfWord, endOfWord - startOfWord);
            }
            else
            {
                return null;
            }
        }


        public static string GetWordAfter(string stringToStartAfter, StringBuilder entireString)
        {
            return GetWordAfter(stringToStartAfter, entireString, 0);
        }

        public static string GetWordAfter(string stringToStartAfter, StringBuilder entireString, int indexToStartAt)
        {
            return GetWordAfter(stringToStartAfter, entireString, indexToStartAt, StringComparison.Ordinal);
        }

        public static string GetWordAfter(string stringToStartAfter, StringBuilder entireString, int indexToStartAt, StringComparison comparison)
        {
            int indexOf = entireString.IndexOf(stringToStartAfter, indexToStartAt, true);
            if (indexOf != -1)
            {
                int startOfWord = indexOf + stringToStartAfter.Length;

                // Let's not count the start of the word if it's a newline
                while (entireString[startOfWord] == WhitespaceChars[0] ||
                    entireString[startOfWord] == WhitespaceChars[1] ||
                    entireString[startOfWord] == WhitespaceChars[2] ||
                    entireString[startOfWord] == WhitespaceChars[3])
                {
                    startOfWord++;
                }

                int endOfWord = entireString.IndexOfAny(WhitespaceChars, startOfWord);
                if (endOfWord == -1)
                {
                    endOfWord = entireString.Length;
                }

                return entireString.Substring(startOfWord, endOfWord - startOfWord);
            }
            else
            {
                return null;
            }
        }



        #region XML Docs
        /// <summary>
        /// Returns the number of lines in a given string.  Newlines '\n' increase the 
        /// line count.
        /// </summary>
        /// <param name="stringInQuestion">The string that will have its lines counted.</param>
        /// <returns>The number of lines in the argument.  "Hello" will return a value of 1, "Hello\nthere" will return a value of 2.</returns>
        #endregion
        public static int GetLineCount(string stringInQuestion)
        {
            if (string.IsNullOrEmpty(stringInQuestion))
            {
                return 0;
            }

            int lines = 1;
            foreach (char character in stringInQuestion)
            {
                if (character == '\n')
                {
                    lines++;
                }
            }

            return lines;
        }

        #region XML Docs
        /// <summary>
        /// Returns the number found at the end of the argument stringToGetNumberFrom or throws an
        /// ArgumentException if no number is found.
        /// </summary>
        /// <remarks>
        /// A stringToGetNumberFrom of "sprite41" will result in the value of 41 returned.  A 
        /// stringToGetNumberFrom of "sprite" will result in an ArgumentException being thrown.
        /// </remarks>
        /// <exception cref="System.ArgumentException">Throws ArgumentException if no number is found at the end of the argument string.</exception>
        /// <param name="stringToGetNumberFrom">The number found at the end.</param>
        /// <returns>The integer value found at the end of the stringToGetNumberFrom.</returns>
        #endregion
        public static int GetNumberAtEnd(string stringToGetNumberFrom)
        {
            int letterChecking = stringToGetNumberFrom.Length;
            do
            {
                letterChecking--;
            } while (letterChecking > -1 && Char.IsDigit(stringToGetNumberFrom[letterChecking]));


            if (letterChecking == stringToGetNumberFrom.Length - 1 && !Char.IsDigit(stringToGetNumberFrom[letterChecking]))
            {
                throw new ArgumentException("The argument string has no number at the end.");
            }

            return System.Convert.ToInt32(
                stringToGetNumberFrom.Substring(letterChecking + 1, stringToGetNumberFrom.Length - letterChecking - 1));
        }

        public static void GetStartAndEndOfLineContaining(string contents, string whatToSearchFor, out int start, out int end)
        {
            int indexOfWhatToSearchFor = contents.IndexOf(whatToSearchFor);

            start = -1;
            end = -1;

            if (indexOfWhatToSearchFor != -1)
            {
                start = contents.LastIndexOfAny(new char[] { '\n', '\r' }, indexOfWhatToSearchFor) + 1;
                end = contents.IndexOfAny(new char[] { '\n', '\r' }, indexOfWhatToSearchFor);
            }
        }


        public static bool HasNumberAtEnd(string stringToGetNumberFrom)
        {
            int letterChecking = stringToGetNumberFrom.Length;
            do
            {
                letterChecking--;
            } while (letterChecking > -1 && Char.IsDigit(stringToGetNumberFrom[letterChecking]));


            if (letterChecking == stringToGetNumberFrom.Length - 1 && !Char.IsDigit(stringToGetNumberFrom[letterChecking]))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Increments the number at the end of a string or adds a number if none exists.
        /// </summary>
        /// <remarks>
        /// This method begins looking at the end of a string for numbers and moves towards the beginning of the string
        /// until it encounters a character which is not a numerical digit or the beginning of the string.  "Sprite123" would return
        /// "Sprite124", and "MyString" would return "MyString1".
        /// </remarks>
        /// <param name="originalString">The string to "increment".</param>
        /// <returns>Returns a string with the number at the end incremented, or with a number added on the end if none existed before.</returns>
        public static string IncrementNumberAtEnd(string originalString)
        {
            if(string.IsNullOrEmpty(originalString))
            {
                return "1";
            }

            // first we get the number at the end of the string

            // start at the end of the string and move backwards until reacing a non-Digit.
            int letterChecking = originalString.Length;
            do
            {
                letterChecking--;
            } while (letterChecking > -1 && Char.IsDigit(originalString[letterChecking]));


            if (letterChecking == originalString.Length - 1 && !Char.IsDigit(originalString[letterChecking]))
            {
                // we don't have a number there, so let's add one
                originalString = originalString + ((int)1).ToString();
                return originalString;
            }
            string numAtEnd = originalString.Substring(letterChecking + 1, originalString.Length - letterChecking - 1);
            string baseString = originalString.Remove(letterChecking + 1, originalString.Length - letterChecking - 1);
            int numAtEndAsInt = System.Convert.ToInt32(numAtEnd);
            numAtEndAsInt++;
            return baseString + numAtEndAsInt.ToString();

        }
        

        #region XML Docs
        /// <summary>
        /// Inserts spaces before every capital letter in a camel-case
        /// string.  Ignores the first letter.
        /// </summary>
        /// <remarks>
        /// For example "HelloThereIAmCamelCase" becomes
        /// "Hello There I Am Camel Case".
        /// </remarks>
        /// <param name="originalString">The string in which to insert spaces.</param>
        /// <returns>The string with spaces inserted.</returns>
        #endregion
        public static string InsertSpacesInCamelCaseString(string originalString)
        {
            // Normally in reverse loops you go til i > -1, but 
            // we don't want the character at index 0 to be tested.
            for (int i = originalString.Length - 1; i > 0; i--)
            {
                if (char.IsUpper(originalString[i]) && i != 0)
                {
                    originalString = originalString.Insert(i, " ");
                }
            }

            return originalString;
        }


        public static bool IsNameUnique<T>(INameable nameable, IList<T> listToCheckAgainst) where T : INameable
        {
            for (int i = 0; i < listToCheckAgainst.Count; i++)
            {
                INameable nameableToCheckAgainst = listToCheckAgainst[i];

                if (nameable != nameableToCheckAgainst && nameable.Name == nameableToCheckAgainst.Name)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsNumber(string stringToCheck)
        {
            double throwaway;
            return double.TryParse(stringToCheck, out throwaway);


        }
        


        public static string MakeCamelCase(string originalString)
        {
            if (string.IsNullOrEmpty(originalString))
            {
                return originalString;
            }

            char[] characterArray = originalString.ToCharArray();

            for (int i = 0; i < originalString.Length; i++)
            {
                if (i == 0 &&
                    characterArray[i] >= 'a' && characterArray[i] <= 'z')
                {
                    characterArray[i] -= (char)32;
                }

                if (characterArray[i] == ' ' && 
                    i < originalString.Length - 1 &&
                    characterArray[i + 1] >= 'a' && characterArray[i + 1] <= 'z')
                {
                    characterArray[i + 1] -= (char)32;
                }
            }

            return RemoveWhitespace(new string(characterArray));
        }




        /// <summary>
        /// Renames the argument INameable to prevent duplicate names.  This method is extremely inefficent for large lists.
        /// </summary>
        /// <typeparam name="T">The type of INameable contained int he list.</typeparam>
        /// <param name="nameable">The INameable to rename if necessary.</param>
        /// <param name="list">The list containing the INameables to compare against.</param>
        public static void MakeNameUnique<T>(T nameable, IList<T> list)
            where T : INameable
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (((object)nameable) != ((object)list[i]) &&
                    (nameable.Name == list[i].Name ||
                     (string.IsNullOrEmpty(nameable.Name) && string.IsNullOrEmpty(list[i].Name)))
                    )
                {
                    // the name matches an item in the list that isn't the same reference, so increment the number.
                    nameable.Name = IncrementNumberAtEnd(nameable.Name);

                    // restart the loop:
                    i = -1;
                }
            }
        }

        /// <summary>
        /// Adds or increments the number at the end of the nameable's Name until it is unique in the list.
        /// </summary>
        /// <typeparam name="T">Nameable type</typeparam>
        /// <param name="nameable">The nameable instance</param>
        /// <param name="list">The list of other nameables.</param>
        public static void MakeNameUnique<T>(T nameable, IEnumerable<T> list)
            where T : INameable
        {
            var count = list.Count();

            for (int i = 0; i < count; i++)
            {
                var atI = list.ElementAt(i);

                if (((object)nameable) != ((object)atI) &&
                    (nameable.Name == atI.Name ||
                     (string.IsNullOrEmpty(nameable.Name) && string.IsNullOrEmpty(atI.Name)))
                    )
                {
                    // the name matches an item in the list that isn't the same reference, so increment the number.
                    nameable.Name = IncrementNumberAtEnd(nameable.Name);

                    // restart the loop:
                    i = -1;
                }
            }
        }


        /// <summary>
        /// Makes an INameable's name unique given a list of existing INameables.
        /// </summary>
        /// <typeparam name="T">The type of nameable.</typeparam>
        /// <typeparam name="U">The type of IList, where the type is an INameable</typeparam>
        /// <param name="nameable">The instance to modify if necessary.</param>
        /// <param name="list">The list of INameables</param>
        public static void MakeNameUnique<T, U>(T nameable, IList<U> list) where T : INameable where U : INameable
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (((object)nameable) != ((object)list[i]) && 
                    (nameable.Name == list[i].Name || 
                     (string.IsNullOrEmpty(nameable.Name) && string.IsNullOrEmpty(list[i].Name)))
                    )
                {
                    // the name matches an item in the list that isn't the same reference, so increment the number.
                    nameable.Name = IncrementNumberAtEnd(nameable.Name);

                    // restart the loop:
                    i = -1;
                }
            }
        }

        public static string MakeStringUnique(string stringToMakeUnique, List<string> stringList)
        {
            return MakeStringUnique(stringToMakeUnique, stringList, 1);
        }

        public static string MakeStringUnique(string stringToMakeUnique, List<string> stringList, int numberToStartAt)
        {        
            for (int i = 0; i < stringList.Count; i++)
            {
                if (stringToMakeUnique == stringList[i])
                {
                    // the name matches an item in the list that isn't the same reference, so increment the number.
                    stringToMakeUnique = IncrementNumberAtEnd(stringToMakeUnique);

                    // Inefficient?  Maybe if we have large numbers, but we're just using it to start at #2
                    // I may revisit this if this causes problems
                    while (GetNumberAtEnd(stringToMakeUnique) < numberToStartAt)
                    {
                        stringToMakeUnique = IncrementNumberAtEnd(stringToMakeUnique);
                    }

                    // restart the loop:
                    i = -1;
                }
            }

            return stringToMakeUnique;
        }

        public static string MakeStringUnique<T>(string stringToMakeUnique, IEnumerable<T> nameableList) where T : INameable
        {
            int numberToStartAt = 1;

            bool repeat = true;
            while (repeat)
            {
                bool restart = false;
                foreach(T nameable in nameableList)
                {
                    if (stringToMakeUnique == nameable.Name)
                    {
                        // the name matches an item in the list that isn't the same reference, so increment the number.
                        stringToMakeUnique = IncrementNumberAtEnd(stringToMakeUnique);

                        // Inefficient?  Maybe if we have large numbers, but we're just using it to start at #2
                        // I may revisit this if this causes problems
                        while (GetNumberAtEnd(stringToMakeUnique) < numberToStartAt)
                        {
                            stringToMakeUnique = IncrementNumberAtEnd(stringToMakeUnique);
                        }

                        // restart the loop:
                        restart = true ;
                        break;
                    }
                }
                repeat = restart;
            }

            return stringToMakeUnique;
        }

        public static void RemoveDuplicates(List<string> strings)
        {
            bool ignoreCase = false;

            RemoveDuplicates(strings, ignoreCase);
        }

        public static void RemoveDuplicates(List<string> strings, bool ignoreCase)
        {
            Dictionary<string, int> uniqueStore;
            if(ignoreCase)
            {
                uniqueStore = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
            }
            else
            {
                uniqueStore = new Dictionary<string, int>();
            }
            List<string> finalList = new List<string>();

            foreach (string currValueUncasted in strings)
            {
                string currValue=currValueUncasted;

                if (!uniqueStore.ContainsKey(currValue))
                {
                    uniqueStore.Add(currValue, 0);
                    finalList.Add(currValueUncasted);
                }
            }

            strings.Clear();
            strings.AddRange(finalList);
        }

        #region XML Docs
        /// <summary>
        /// Removes the number found at the end of the argument originalString and returns the resulting
        /// string, or returns the original string if no number is found.
        /// </summary>
        /// <param name="originalString">The string that will have the number at its end removed.</param>
        /// <returns>The string after the number has been removed.</returns>
        #endregion
        public static string RemoveNumberAtEnd(string originalString)
        {
            // start at the end of the string and move backwards until reacing a non-Digit.
            int letterChecking = originalString.Length;
            do
            {
                letterChecking--;
            } while (letterChecking > -1 && Char.IsDigit(originalString[letterChecking]));


            if (letterChecking == originalString.Length - 1 && !Char.IsDigit(originalString[letterChecking]))
            {
                // we don't have a number there, so return the original
                return originalString;
            }
            return originalString.Remove(letterChecking + 1, originalString.Length - letterChecking - 1);
        }

        #region XML Docs
        /// <summary>
        /// Removes all whitespace found in the argument stringToRemoveWhitespaceFrom.
        /// </summary>
        /// <param name="stringToRemoveWhitespaceFrom">The string that will have its whitespace removed.</param>
        /// <returns>The string resulting from removing whitespace from the argument string.</returns>
        #endregion
        public static string RemoveWhitespace(string stringToRemoveWhitespaceFrom)
        {
            return stringToRemoveWhitespaceFrom.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");
        }

        public static void ReplaceLine(ref string contents, string contentsOfLineToReplace, string whatToReplaceWith)
        {
            int startOfLine;
            int endOfLine;
            GetStartAndEndOfLineContaining(contents, contentsOfLineToReplace, out startOfLine, out endOfLine);

            if (startOfLine != -1)
            {
                contents = contents.Remove(startOfLine, endOfLine - startOfLine);
                contents = contents.Insert(startOfLine, whatToReplaceWith);
            }
        }
    }
}
