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
    /// Pattern matcher that matches binary tokens
    /// </summary>
    public class BinPatternMatcher : PatternMatcherBase
    {
        public static readonly BinPatternMatcher Default = new BinPatternMatcher();
        //perform the match

        //patterns that trigger this matcher
        public override string[] DefaultPrefixes
        {
            get { return new[] {"0", "1"}; }
        }

        public override int Match(string textToMatch, int matchAtIndex)
        {
            int currentIndex = matchAtIndex;
            do
            {
                char currentChar = textToMatch[currentIndex];
                if (currentChar == '0' || currentChar == '1')
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
    }
}