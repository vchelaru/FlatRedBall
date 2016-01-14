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
    public class RangePatternMatcher : PatternMatcherBase
    {
        public RangePatternMatcher(char quote)
        {
            StartChar = quote;
            EndChar = quote;
        }

        public RangePatternMatcher(char start, char end)
        {
            StartChar = start;
            EndChar = end;
        }

        public RangePatternMatcher(char start, char end, char escape)
        {
            StartChar = start;
            EndChar = end;
            EscapeChar = escape;
        }

        public override string[] DefaultPrefixes
        {
            get { return new[] {StartChar.ToString()}; }
        }

        public char StartChar { get; set; }
        public char EndChar { get; set; }
        public char EscapeChar { get; set; }

        public override int Match(string textToMatch, int matchAtIndex)
        {
            int length = 0;
            int textLength = textToMatch.Length;

            while (matchAtIndex + length != textLength)
            {
                if (textToMatch[matchAtIndex + length] == EndChar &&
                    (matchAtIndex + length < textLength - 1 && textToMatch[matchAtIndex + length + 1] == EndChar))
                {
                    length++;
                }
                else if (textToMatch[matchAtIndex + length] == EndChar &&
                         (matchAtIndex + length == textLength - 1 || textToMatch[matchAtIndex + length + 1] != EndChar))
                    return length + 1;

                length++;
            }


            return 0;
        }
    }
}