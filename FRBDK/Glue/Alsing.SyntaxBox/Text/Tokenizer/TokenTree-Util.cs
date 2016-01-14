// *
// * Copyright (C) 2008 Roger Alsing : http://www.rogeralsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using Alsing.Text.PatternMatchers;

namespace Alsing.Text
{
    public partial class TokenTree
    {
        private void AddCaseInsensitiveToken(string text, bool needSeparators, object[] tags)
        {
            //make a lowercase string and add it as a token
            text = text.ToLower();
            char startChar = text[0];
            int startIndex = startChar;

            if (nodes[startIndex] == null)
                nodes[startIndex] = new TokenTreeNode();

            nodes[startIndex].AddToken(text, false, needSeparators, tags);

            //make a lowercase string with a uppercase start char and add it as a token
            text = char.ToUpper(startChar) + text.Substring(1);
            startChar = text[0];
            startIndex = startChar;
            if (nodes[startIndex] == null)
                nodes[startIndex] = new TokenTreeNode();

            nodes[startIndex].AddToken(text, false, needSeparators, tags);
        }

        private void AddCaseSensitiveToken(string text, bool needSeparators, object[] tags)
        {
            char startChar = text[0];
            int startIndex = startChar;
            if (nodes[startIndex] == null)
                nodes[startIndex] = new TokenTreeNode();

            nodes[startIndex].AddToken(text, true, needSeparators, tags);
        }

        private void AddPatternWithCaseInsensitivePrefix(string prefix, IPatternMatcher matcher, bool needSeparators, object[] tags)
        {
            //make a lowercase string and add it as a token
            prefix = prefix.ToLower();
            char startChar = prefix[0];
            int startIndex = startChar;
            if (nodes[startIndex] == null)
                nodes[startIndex] = new TokenTreeNode();

            nodes[startIndex].AddPattern(prefix, false, needSeparators, matcher, tags);

            //make a lowercase string with a uppercase start char and add it as a token
            prefix = char.ToUpper(startChar) + prefix.Substring(1);
            startChar = prefix[0];
            startIndex = startChar;
            if (nodes[startIndex] == null)
                nodes[startIndex] = new TokenTreeNode();

            nodes[startIndex].AddPattern(prefix, false, needSeparators, matcher, tags);
        }

        private void AddPatternWithCaseSensitivePrefix(string prefix, IPatternMatcher matcher, bool needSeparators, object[] tags)
        {
            char startChar = prefix[0];
            int startIndex = startChar;
            if (nodes[startIndex] == null)
                nodes[startIndex] = new TokenTreeNode();

            nodes[startIndex].AddPattern(prefix, true, needSeparators, matcher, tags);
        }

        private void AddPatternWithoutPrefix(IPatternMatcher matcher, bool caseSensitive, bool needSeparators,
                                                object[] tags)
        {
            if (matcher.DefaultPrefixes != null)
            {
                foreach (string defaultPrefix in matcher.DefaultPrefixes)
                {
                    AddPattern(defaultPrefix, matcher, caseSensitive, needSeparators, tags);
                }
            }
            else
            {
                var patternMatcherReference = new PatternMatchReference(matcher)
                {
                    Tags = tags,
                    NextSibling = root.FirstExpression,
                    NeedSeparators = needSeparators
                };

                root.FirstExpression = patternMatcherReference;
            }
        }
    }
}
