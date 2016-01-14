// *
// * Copyright (C) 2008 Roger Alsing : http://www.rogeralsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using System;
using Alsing.Text.PatternMatchers;

namespace Alsing.Text
{
    public partial class TokenTree
    {
        private readonly TokenTreeNode[] nodes;
        private readonly TokenTreeNode root;
        private readonly char[] textLookup;
        private bool[] separatorCharLookup;

        public TokenTree()
        {
            nodes = new TokenTreeNode[65536];
            Separators = ".,;:<>[](){}!\"#¤%&/=?*+-/\\ \t\n\r";
            textLookup = new char[65536];
            for (int i = 0; i < 65536; i++)
            {
                textLookup[i] = (char) i;
            }
            textLookup['\t'] = ' ';

            root = new TokenTreeNode();
        }

        #region PUBLIC PROPERTY SEPARATORS

        private string separators;

        public string Separators
        {
            get { return separators; }
            set
            {
                separators = value;
                separatorCharLookup = new bool[65536]; //initialize all to false
                foreach (char separatorChar in value)
                {
                    separatorCharLookup[separatorChar] = true;
                }
            }
        }

        #endregion //END PUBLIC PROPERTY SEPARATORS

        

        

        //this is wicked fast
        //do not refactor extract methods from this if you want to keep the speed
        public MatchResult Match(string text, int startIndex)
        {
            if(string.IsNullOrEmpty(text))
                throw new ArgumentNullException(text);

            var lastMatch = new MatchResult {Text = text};
            int textLength = text.Length;

            for (int currentIndex = startIndex; currentIndex < textLength; currentIndex ++)
            {
                //call any prefixless patternmatchers

                #region HasExpressions

                if (root.FirstExpression != null)
                {
                    //begin with the first expression of the _root node_
                    PatternMatchReference patternMatcherReference = root.FirstExpression;
                    while (patternMatcherReference != null)
                    {
                        int patternMatchIndex = patternMatcherReference.Matcher.Match(text, currentIndex);
                        if (patternMatchIndex > 0 && patternMatchIndex > lastMatch.Length)
                        {
                            bool leftIsSeparator = currentIndex == 0 ? true : separatorCharLookup[text[currentIndex - 1]];
                            bool rightIsSeparator = (currentIndex+patternMatchIndex) == textLength ? true : separatorCharLookup[text[currentIndex + patternMatchIndex]];

                            if (!patternMatcherReference.NeedSeparators || (leftIsSeparator && rightIsSeparator))
                            {
                                lastMatch.Index = currentIndex;
                                lastMatch.Length = patternMatchIndex;
                                lastMatch.Found = true;
                                lastMatch.Tags = patternMatcherReference.Tags;
                            }
                        }

                        patternMatcherReference = patternMatcherReference.NextSibling;
                    }
                }

                #endregion

                //lookup the first token tree node
                TokenTreeNode node = nodes[text[currentIndex]];
                if (node == null)
                {
                    if (lastMatch.Found)
                        break;

                    continue;
                }


                for (int matchIndex = currentIndex + 1; matchIndex <= textLength; matchIndex++)
                {
                    //call patternmatchers for the current prefix

                    #region HasExpressions

                    if (node.FirstExpression != null)
                    {
                        //begin with the first expression of the _current node_
                        PatternMatchReference patternMatcherReference = node.FirstExpression;
                        while (patternMatcherReference != null)
                        {
                            int patternMatchIndex = patternMatcherReference.Matcher.Match(text, matchIndex);
                            if (patternMatchIndex > 0 && patternMatchIndex > lastMatch.Length)
                            {
                                bool leftIsSeparator = currentIndex == 0 ? true : separatorCharLookup[text[currentIndex - 1]];
                                bool rightIsSeparator = (currentIndex+patternMatchIndex+matchIndex) == textLength ? true : separatorCharLookup[text[currentIndex + patternMatchIndex+matchIndex]];

                                if (!patternMatcherReference.NeedSeparators || (leftIsSeparator && rightIsSeparator))
                                {
                                    lastMatch.Index = currentIndex;
                                    lastMatch.Length = patternMatchIndex + matchIndex - currentIndex;
                                    lastMatch.Found = true;
                                    lastMatch.Tags = patternMatcherReference.Tags;
                                }
                            }

                            patternMatcherReference = patternMatcherReference.NextSibling;
                        }
                    }

                    #endregion

                    #region IsEndNode

                    if (node.IsEnd && matchIndex - currentIndex >= lastMatch.Length)
                    {
                        bool leftIsSeparator = currentIndex == 0 ? true : separatorCharLookup[text[currentIndex - 1]];
                        bool rightIsSeparator = matchIndex == textLength ? true : separatorCharLookup[text[matchIndex]];

                        if (!node.NeedSeparators || (leftIsSeparator && rightIsSeparator))
                        {
                            lastMatch.Index = currentIndex;
                            lastMatch.Tags = node.Tags;
                            lastMatch.Found = true;
                            lastMatch.Length = matchIndex - currentIndex;
                            //TODO:perform case test here , case sensitive words might be matched even if they have incorrect case
                            if (currentIndex + lastMatch.Length == textLength)
                                break;
                        }
                    }

                    #endregion

                    //try fetch a node at this index
                    node = node.GetNextNode(textLookup[text[matchIndex]]);

                    //we found no node on the lookupindex or none of the siblingnodes at that index matched the current char
                    if (node == null)
                        break; // continue with the next character
                }

                //return last match
                if (lastMatch.Found)
                    return lastMatch;
            }

            if (lastMatch.Found)
                return lastMatch;
 
            //no match was found
            return MatchResult.NoMatch;
        }
    }
}