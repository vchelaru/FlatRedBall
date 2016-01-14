using System;
using System.Collections.Generic;
using System.Threading;
using Alsing.Text.PatternMatchers;

namespace Alsing.Text
{
    public class Tokenizer
    {
        private readonly TokenTree tree;
        public bool IsImmutable { get;private set; }

        public Tokenizer()
        {
            tree = new TokenTree();
        }

        public string Text { get; set; }

        public Tokenizer AddPattern(IPatternMatcher matcher,bool caseSensitive,bool needsSeparators,params object[] tags)
        {
            ThrowIfImmutable();

            tree.AddPattern(matcher, caseSensitive, needsSeparators, tags);
            return this;
        }

        public Tokenizer AddToken(string token, bool caseSensitive, bool needsSeparators, params object[] tags)
        {
            ThrowIfImmutable();

            tree.AddToken(token,caseSensitive, needsSeparators, tags);
            return this;
        }

        public Token[] Tokenize()
        {
            if (Text == null)
                throw new ArgumentNullException("Text");

            MakeImmutable();

            var tokens = new List<Token>();

            int index = 0;
            while (index < Text.Length)
            {
                MatchResult match = tree.Match(Text, index);

                if (match.Found)
                {
                    string dummyText = Text.Substring(index, match.Index - index);
                    var dummyToken = new Token(dummyText, null);
                    tokens.Add(dummyToken);

                    var realToken = new Token(match.GetText(), match.Tags);
                    index = match.Index + match.Length;
                    tokens.Add(realToken);                    
                }
                else
                {
                    string dummyText = Text.Substring(index);
                    var dummyToken = new Token(dummyText, null);
                    tokens.Add(dummyToken);

                    index = Text.Length;
                }
            }

            return tokens.ToArray();
        }

        private void ThrowIfImmutable() {
            if (IsImmutable)
                throw new Exception("Tokens can not be added to an immutable tokenizer");
        }

        public void MakeImmutable()
        {
            IsImmutable = true;
        }
    }
}