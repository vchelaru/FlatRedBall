// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using System.Collections.Generic;
using System.Text;

namespace Alsing.SourceCode.SyntaxDocumentParsers
{
    public sealed class ParseTools
    {
        public static void AddPatternString(string Text, Row Row, Pattern Pattern,
                                            TextStyle Style, Span span, bool HasError)
        {
            var x = new Word
            {
                Style = Style,
                Pattern = Pattern,
                HasError = HasError,
                Span = span,
                Text = Text
            };
            Row.Add(x);
        }

        public static unsafe void AddString(string Text, Row Row, TextStyle Style,
                                            Span span)
        {
            if (Text == "")
                return;

            var CurrentWord = new StringBuilder();
            char[] Buff = Text.ToCharArray();
            fixed (char* c = &Buff[0])
            {
                for (int i = 0; i < Text.Length; i++)
                {
                    if (c[i] == ' ' || c[i] == '\t')
                    {
                        if (CurrentWord.Length != 0)
                        {
                            Word word = Row.Add(CurrentWord.ToString());
                            word.Style = Style;
                            word.Span = span;
                            CurrentWord = new StringBuilder();
                        }

                        Word ws = Row.Add(c[i].ToString());
                        if (c[i] == ' ')
                            ws.Type = WordType.Space;
                        else
                            ws.Type = WordType.Tab;
                        ws.Style = Style;
                        ws.Span = span;
                    }
                    else
                        CurrentWord.Append(c[i].ToString());
                }
                if (CurrentWord.Length != 0)
                {
                    Word word = Row.Add(CurrentWord.ToString());
                    word.Style = Style;
                    word.Span = span;
                }
            }
        }


        public static List<string> GetWords(string text)
        {
            var words = new List<string>();
            var CurrentWord = new StringBuilder();
            foreach (char c in text)
            {
                if (c == ' ' || c == '\t')
                {
                    if (CurrentWord.ToString() != "")
                    {
                        words.Add(CurrentWord.ToString());
                        CurrentWord = new StringBuilder();
                    }

                    words.Add(c.ToString());
                }
                else
                    CurrentWord.Append(c.ToString()
                        );
            }
            if (CurrentWord.ToString() != "")
                words.Add(CurrentWord.ToString());
            return words;
        }

        public static PatternScanResult GetFirstWord(char[] TextBuffer,
                                                     PatternCollection Patterns, int StartPosition)
        {
            PatternScanResult Result;
            Result.Index = 0;
            Result.Token = "";

            //			for (int i=StartPosition;i<TextBuffer.Length;i++)
            //			{
            //
            //				//-----------------------------------------------
            //				if (c[i]==PatternBuffer[0])
            //				{
            //					bool found=true;
            //					for (int j=0;j<Pattern.Length;j++)
            //					{
            //						if (c[i+j]!=p[j])
            //						{
            //							found=false;
            //							break;
            //						}
            //					}
            //					if (found)
            //					{
            //						Result.Index =i+StartPosition;
            //						Result.Token = Text.Substring(i+StartPosition,this.Pattern.Length);
            //						return Result;
            //					}							
            //				}
            //				//-----------------------------------------------
            //			}


            return Result;
        }
    }
}