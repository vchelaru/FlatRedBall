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
    public class TokenTreeNode
    {
        public bool CaseSensitive = true;
        public char Char = char.MinValue;
        public TokenTreeNode[] ChildNodes;
        public bool ContainsCaseInsensitiveData;
        public long Count;
        public PatternMatchReference FirstExpression;
        public bool IsEnd;
        public bool NeedSeparators;
        public TokenTreeNode NextSibling;
        public object[] Tags;

        public TokenTreeNode()
        {
            ChildNodes = new TokenTreeNode[256];
        }

        public override string ToString()
        {
            if (Tags != null)
                return Tags.ToString();

            return "TokenTreeNode " + Char; // do not localize
        }

        public void AddPattern(string prefix, bool caseSensitive, bool needSeparators, IPatternMatcher matcher,
                                  object[] tags)
        {
            if (string.IsNullOrEmpty(prefix))
                throw new ArgumentNullException("prefix");

            TokenTreeNode node = AddTokenInternal(prefix, caseSensitive);

            var patternMatcherReference = new PatternMatchReference(matcher)
            {
                NextSibling = FirstExpression,
                Tags = tags,
                NeedSeparators = needSeparators
            };

            node.FirstExpression = patternMatcherReference;           
        }

        public void AddPattern(bool caseSensitive, bool needSeparators, IPatternMatcher matcher, object[] tags)
        {
            var patternMatcherReference = new PatternMatchReference(matcher)
            {
                NextSibling = FirstExpression,
                Tags = tags,
                NeedSeparators = needSeparators
            };

            FirstExpression = patternMatcherReference;
        }

        public void AddToken(string token, bool caseSensitive, bool needSeparators, object[] tags)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException("token");

            TokenTreeNode node = AddTokenInternal(token, caseSensitive);
            node.IsEnd = true;
            node.Tags = tags;
            node.NeedSeparators = needSeparators;
            node.CaseSensitive = caseSensitive;
        }

        public TokenTreeNode AddTokenInternal(string token, bool caseSensitive)
        {
            Char = token[0];


            if (!caseSensitive)
                ContainsCaseInsensitiveData = true;

            if (token.Length == 1)
                return this;

            string leftovers = token.Substring(1);
            char childChar = leftovers[0];
            int childIndex = childChar & 0xff;
            //make a lookupindex (dont mind if unicode chars end up as siblings as ascii)

            TokenTreeNode node = ChildNodes[childIndex];
            TokenTreeNode res;
            if (node == null)
            {
                var child = new TokenTreeNode();
                ChildNodes[childIndex] = child;
                res = child.AddTokenInternal(leftovers, caseSensitive);

                MakeRepeatingWS(child);
            }
            else
            {
                node = GetMatchingNode(childChar, node);
                res = node.AddTokenInternal(leftovers, caseSensitive);
            }

            return res;
        }

        private static void MakeRepeatingWS(TokenTreeNode child) {
            if (child.Char == ' ')
            {
                // if the node contains " " (whitespace)
                // then add the node as a childnode of itself.
                // thus allowing it to parse things like
                // "end         sub" even if the pattern is "end sub" // do not localize
                child.ChildNodes[' '] = child;
            }
        }

        private static TokenTreeNode GetMatchingNode(char childChar, TokenTreeNode node)
        {
            //find a bucket with the same childChar as we need
            while (node.NextSibling != null && node.Char != childChar)
            {
                node = node.NextSibling;
            }
            
            if (node.Char != childChar)
            {
                var child = new TokenTreeNode();
                node.NextSibling = child;
                return child;
            }
            return node;
        }

        public TokenTreeNode GetNextNode(char c)
        {
            char tmp = c;
            //if case sensitive, to lower
            if (ContainsCaseInsensitiveData)
                tmp = CharUtils.ToLower(c);

            //hash the index
            int index = tmp & 0xff;

            //get node at index
            TokenTreeNode node = ChildNodes[index];

            while (node != null)
            {
                tmp = c;
                if (node.ContainsCaseInsensitiveData)
                    tmp = CharUtils.ToLower(c);

                if (node.Char == tmp)
                    break;

                node = node.NextSibling;
            }

            return node;
        }        
    }
}