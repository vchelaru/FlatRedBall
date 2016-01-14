// *
// * Copyright (C) 2008 Roger Alsing : http://www.rogeralsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

namespace Alsing.Text.PatternMatchers
{
    /// <summary>
    /// Pattern matcher that matches a propertypath
    /// </summary>
    public class PropertyPathPatterhMatcher : PatternMatcherBase
    {
        //perform the match
        public override int Match(string textToMatch, int matchAtIndex)
        {
            bool start = true;

            int currentIndex = matchAtIndex;
            do
            {
                char currentChar = textToMatch[currentIndex];
                if (start && IsValidStartChar(currentChar))
                {
                    start = false;
                }
                else if (start && IsWildcard(currentChar))
                {
                    currentIndex++;
                    break;
                }
                else if (!start && IsSeparator(currentChar))
                {
                    start = true;
                }
                else if (!start && IsValidChar(currentChar)) {}
                else
                {
                    break;
                }
                currentIndex++;
            } while (currentIndex < textToMatch.Length);

            if (textToMatch.Substring(matchAtIndex, currentIndex - matchAtIndex) == "*")
                return 0;

            return currentIndex - matchAtIndex;
        }


        private static bool IsWildcard(char c)
        {
            return c == '*' || c == '¤';
        }

        private static bool IsSeparator(char c)
        {
            return c == '.';
        }

        private static bool IsValidStartChar(char c)
        {
            if (CharUtils.IsLetter(c))
                return true;

            if ("_@".IndexOf(c) >= 0)
                return true;

            return false;
        }

        private static bool IsValidChar(char c)
        {
            if (CharUtils.IsLetterOrDigit(c) || c == '_')
                return true;

            return false;
        }
    }
}