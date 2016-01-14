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
    /// Pattern matcher that matches culture invariant integer values
    /// </summary>
    public class IntPatternMatcher : PatternMatcherBase
    {
        public static readonly IntPatternMatcher Default = new IntPatternMatcher();

        //perform the match
        public override int Match(string textToMatch, int matchAtIndex)
        {
            int currentIndex = matchAtIndex;
            do
            {
                char currentChar = textToMatch[currentIndex];
                if (currentChar >= '0' && currentChar <= '9')
                {
                    //current char is hexchar
                }
                else
                {
                    break;
                }
                currentIndex++;
            } while (currentIndex < textToMatch.Length);

            return currentIndex - matchAtIndex;
        }

        //public override string[] DefaultPrefixes
        //{
        //    get
        //    {
        //        return new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        //    }
        //}
    }
}