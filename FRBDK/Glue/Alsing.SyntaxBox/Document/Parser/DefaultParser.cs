// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *
//no parsing , just splitting and making whitespace possible
//1 sec to finnish ca 10000 rows

namespace Alsing.SourceCode.SyntaxDocumentParsers
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class DefaultParser : IParser
    {
        private SyntaxDefinition mSyntaxDefinition;
        private long Version;

        /// <summary>
        /// 
        /// </summary>
        public DefaultParser()
        {
            mSyntaxDefinition = null;
        }

        #region IParser Members

        /// <summary>
        /// 
        /// </summary>
        public SyntaxDefinition SyntaxDefinition
        {
            get { return mSyntaxDefinition; }
            set
            {
                mSyntaxDefinition = value;

                if (mSyntaxDefinition == null)
                {
                    var l = new SyntaxDefinition();
                    l.mainSpanDefinition = new SpanDefinition(l) {MultiLine = true};
                    mSyntaxDefinition = l;
                }

                Version = long.MinValue;
                mSyntaxDefinition.Version = long.MinValue + 1;
                Document.ReParse();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="ParseKeywords"></param>
        public void ParseRow(int index, bool ParseKeywords)
        {
            InternalParseLine(index, ParseKeywords);
            if (SyntaxDefinition != null)
            {
                if (Version != SyntaxDefinition.Version)
                {
                    SyntaxDefinition.UpdateLists();
                    Version = SyntaxDefinition.Version;
                }
            }


            Document.InvokeRowParsed(Document[index]);
        }

        #endregion

        #region PUBLIC PROPERTY SEPARATORS

        public string Separators
        {
            get { return SyntaxDefinition.Separators; }
            set { SyntaxDefinition.Separators = value; }
        }

        #endregion

        #region Optimerat och klart

        // ska anropas om "is same but different" är true

        /// <summary>
        /// 
        /// </summary>
        public SyntaxDocument Document { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="SyntaxFile"></param>
        public void Init(string SyntaxFile)
        {
            try
            {
                if (!SyntaxFile.ToLowerInvariant().EndsWith(".syn")
                    )
                    SyntaxFile += ".syn";


                SyntaxDefinition = new SyntaxDefinitionLoader().Load(SyntaxFile);
            }
            catch {}
        }

        public void Init(string syntaxFile, string separators)
        {
            try
            {
                if (!syntaxFile.ToLowerInvariant().EndsWith(".syn")
                    )
                    syntaxFile += ".syn";


                SyntaxDefinition = new SyntaxDefinitionLoader().Load(syntaxFile, separators);
            }
            catch {}
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="syntaxDefinition"></param>
        public void Init(SyntaxDefinition syntaxDefinition)
        {
            SyntaxDefinition = syntaxDefinition;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="RowIndex"></param>
        public void ParsePreviewLine(int RowIndex)
        {
            Row Row = Document[RowIndex];
            Row.Clear();
            Row.Add(Row.Text);
            Row.RowState = RowState.NotParsed;
        }

        private void MakeSame(int RowIndex)
        {
            Row row = Document[RowIndex];

            //copy back the old segments to this line...
            Span seg = row.endSpan;
            Span seg2 = Document[RowIndex + 1].startSpan;
            while (seg != null)
            {
                foreach (Word w in row)
                {
                    if (w.Span == seg)
                    {
                        if (w.Span.StartWord == w)
                            seg2.StartWord = w;

                        if (w.Span.EndWord == w)
                            seg2.EndWord = w;

                        w.Span = seg2;
                    }
                }

                if (seg == row.startSpan)
                    row.startSpan = seg2;

                if (seg == row.endSpan)
                    row.endSpan = seg2;


                if (row.startSpans.IndexOf(seg) >= 0)
                    row.startSpans[row.startSpans.IndexOf(seg)] = seg2;

                if (row.endSpans.IndexOf(seg) >= 0)
                    row.endSpans[row.endSpans.IndexOf(seg)] = seg2;

                seg = seg.Parent;
                seg2 = seg2.Parent;
            }
            row.SetExpansionSegment();
        }

        //om denna är true
        // så ska INTE nästa rad parse'as , utan denna ska fixas så den blir som den förra... (kopiera span)
        private bool IsSameButDifferent(int RowIndex, Span oldStartSpan)
        {
            //is this the last row ? , if so , bailout
            if (RowIndex >= Document.Count - 1)
                return false;

            Row row = Document[RowIndex];
            Span seg = row.endSpan;
            Span oldEndSpan = Document[RowIndex + 1].startSpan;
            Span oseg = oldEndSpan;

            bool diff = false;

            while (seg != null)
            {
                if (oseg == null)
                {
                    diff = true;
                    break;
                }

                //Id1+=seg.spanDefinition.GetHashCode ().ToString (System.Globalization.CultureInfo.InvariantCulture);
                if (seg.spanDefinition != oseg.spanDefinition)
                {
                    diff = true;
                    break;
                }

                if (seg.Parent != oseg.Parent)
                {
                    diff = true;
                    break;
                }

                seg = seg.Parent;
                oseg = oseg.Parent;
            }


            if (diff || row.startSpan != oldStartSpan)
                return false;

            return true;
        }

        #endregion

        private ScanResultWord GetNextWord(string Text, Span currentSpan, int
                                                                                    StartPos, ref bool HasComplex)
        {
            SpanDefinition spanDefinition = currentSpan.spanDefinition;

            #region ComplexFind

            int BestComplexPos = - 1;
            Pattern BestComplexPattern = null;
            string BestComplexToken = "";
            var complexword = new ScanResultWord();
            if (HasComplex)
            {
                foreach (Pattern pattern in spanDefinition.ComplexPatterns)
                {
                    PatternScanResult scanres = pattern.IndexIn(Text, StartPos,
                                                                pattern.Parent.CaseSensitive, Separators);
                    if (scanres.Token != "")
                    {
                        if (scanres.Index < BestComplexPos || BestComplexPos == - 1)
                        {
                            BestComplexPos = scanres.Index;
                            BestComplexPattern = pattern;
                            BestComplexToken = scanres.Token;
                        }
                    }
                }


                if (BestComplexPattern != null)
                {
                    complexword.HasContent = true;
                    complexword.ParentList = BestComplexPattern.Parent;
                    complexword.Pattern = BestComplexPattern;
                    complexword.Position = BestComplexPos;
                    complexword.Token = BestComplexToken;
                }
                else
                {
                    HasComplex = false;
                }
            }

            #endregion

            #region SimpleFind 

            var simpleword = new ScanResultWord();
            for (int i = StartPos; i < Text.Length; i++)
            {
                //bailout if we found a complex pattern before this char pos
                if (i > complexword.Position && complexword.HasContent)
                    break;

                #region 3+ char pattern

                if (i <= Text.Length - 3)
                {
                    string key = Text.Substring(i, 3).ToLowerInvariant();
                    var patterns2 = (PatternCollection)
                                    spanDefinition.LookupTable[key];
                    //ok , there are patterns that start with this char
                    if (patterns2 != null)
                    {
                        foreach (Pattern pattern in patterns2)
                        {
                            int len = pattern.StringPattern.Length;
                            if (i + len > Text.Length)
                                continue;

                            char lastpatternchar = char.ToLower(pattern.StringPattern[len -
                                                                                      1]);
                            char lasttextchar = char.ToLower(Text[i + len - 1]);

                            #region Case Insensitive

                            if (lastpatternchar == lasttextchar)
                            {
                                if (!pattern.IsKeyword || (pattern.IsKeyword &&
                                                           pattern.HasSeparators(Text, i)))
                                {
                                    if (!pattern.Parent.CaseSensitive)
                                    {
                                        string s = Text.Substring(i, len).ToLowerInvariant();

                                        if (s == pattern.StringPattern.ToLowerInvariant())
                                        {
                                            simpleword.HasContent = true;
                                            simpleword.ParentList = pattern.Parent;
                                            simpleword.Pattern = pattern;
                                            simpleword.Position = i;
                                            simpleword.Token = pattern.Parent.NormalizeCase ? pattern.StringPattern : Text.Substring(i, len);
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        string s = Text.Substring(i, len);

                                        if (s == pattern.StringPattern)
                                        {
                                            simpleword.HasContent = true;
                                            simpleword.ParentList = pattern.Parent;
                                            simpleword.Pattern = pattern;
                                            simpleword.Position = i;
                                            simpleword.Token = pattern.StringPattern;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        #endregion
                    }
                }

                #endregion

                if (simpleword.HasContent)
                    break;

                #region single char pattern

                char c = Text[i];
                var patterns = (PatternCollection) spanDefinition.LookupTable[c];
                if (patterns != null)
                {
                    //ok , there are patterns that start with this char
                    foreach (Pattern pattern in patterns)
                    {
                        int len = pattern.StringPattern.Length;
                        if (i + len > Text.Length)
                            continue;

                        char lastpatternchar = pattern.StringPattern[len - 1];
                        char lasttextchar = Text[i + len - 1];

                        if (!pattern.Parent.CaseSensitive)
                        {
                            #region Case Insensitive

                            if (char.ToLower(lastpatternchar) == char.ToLower(lasttextchar))
                            {
                                if (!pattern.IsKeyword || (pattern.IsKeyword &&
                                                           pattern.HasSeparators(Text, i)))
                                {
                                    string s = Text.Substring(i, len).ToLowerInvariant();

                                    if (s == pattern.StringPattern.ToLowerInvariant())
                                    {
                                        simpleword.HasContent = true;
                                        simpleword.ParentList = pattern.Parent;
                                        simpleword.Pattern = pattern;
                                        simpleword.Position = i;
                                        simpleword.Token = pattern.Parent.NormalizeCase ? pattern.StringPattern : Text.Substring(i, len);
                                        break;
                                    }
                                }
                            }

                            #endregion
                        }
                        else
                        {
                            #region Case Sensitive

                            if (lastpatternchar == lasttextchar)
                            {
                                if (!pattern.IsKeyword || (pattern.IsKeyword &&
                                                           pattern.HasSeparators(Text, i)))
                                {
                                    string s = Text.Substring(i, len);

                                    if (s == pattern.StringPattern)
                                    {
                                        simpleword.HasContent = true;
                                        simpleword.ParentList = pattern.Parent;
                                        simpleword.Pattern = pattern;
                                        simpleword.Position = i;
                                        simpleword.Token = pattern.StringPattern;
                                        break;
                                    }
                                }
                            }

                            #endregion
                        }
                    }

                    if (simpleword.HasContent)
                        break;
                }

                #endregion
            }

            #endregion

            if (complexword.HasContent && simpleword.HasContent)
            {
                if (simpleword.Position == complexword.Position)
                {
                    if (simpleword.Token.Length >= complexword.Token.Length)
                        return simpleword;
                    return complexword;
                }

                if (simpleword.Position < complexword.Position)
                    return simpleword;

                if (simpleword.Position > complexword.Position)
                    return complexword;
            }

            if (simpleword.HasContent)
                return simpleword;

            if (complexword.HasContent)
                return complexword;


            return new ScanResultWord();
        }


        private void ParseText(Row Row, Span currentSpan, string Text)
        {
            int CurrentPosition = 0;
            bool HasComplex = true;
            while (true)
            {
                ScanResultWord Word = GetNextWord(Text, currentSpan, CurrentPosition,
                                                  ref HasComplex);

                if (!Word.HasContent)
                {
                    ParseTools.AddString(Text.Substring(CurrentPosition), Row,
                                         currentSpan.spanDefinition.Style, currentSpan);
                    break;
                }
                ParseTools.AddString(Text.Substring(CurrentPosition, Word.Position -
                                                                     CurrentPosition), Row,
                                     currentSpan.spanDefinition.Style, currentSpan);
                ParseTools.AddPatternString(Word.Token, Row, Word.Pattern,
                                            Word.ParentList.Style, currentSpan,
                                            false);
                CurrentPosition = Word.Position + Word.Token.Length;
            }
        }

        private void InternalParseLine(int index, bool ParseKeywords)
        {
            if (mSyntaxDefinition == null)
                return;

            //
            //			if (ParseKeywords)
            //				return;
            //			ParseKeywords=true;
            SyntaxDocument doc = Document;
            Row Row = doc[index];
            Span oldEndSpan = Row.endSpan;
            Span oldStartSpan = Row.startSpan;
            bool Fold = !Row.IsCollapsed;


            if (Row.IsCollapsedEndPart)
            {
                //Row.expansion_EndSpan.Expanded = true;
                //Row.expansion_EndSpan.EndRow = null;
                Row.expansion_EndSpan.EndWord = null;
            }


            //set startsegment for this row
            if (index > 0)
            {
                Row.startSpan = Document[index - 1].endSpan;
            }
            else
            {
                if (Row.startSpan == null)
                {
                    Row.startSpan = new Span(Row) {spanDefinition = mSyntaxDefinition.mainSpanDefinition};
                }
            }

            int CurrentPosition = 0;
            Span currentSpan = Row.startSpan;


            //kör tills vi kommit till slutet av raden..
            Row.endSpans.Clear();
            Row.startSpans.Clear();
            Row.Clear();
            //		bool HasEndSegment=false;

            while (true)
            {
                ScanResultSegment ChildSegment = GetNextChildSegment(Row,
                                                                     currentSpan, CurrentPosition);
                ScanResultSegment EndSegment = GetEndSegment(Row, currentSpan,
                                                             CurrentPosition);

                if ((EndSegment.HasContent && ChildSegment.HasContent &&
                     EndSegment.Position <= ChildSegment.Position) ||
                    (EndSegment.HasContent && ChildSegment.HasContent == false))
                {
                    //this is an end span

                    if (ParseKeywords)
                    {
                        string Text = Row.Text.Substring(CurrentPosition,
                                                         EndSegment.Position - CurrentPosition);
                        ParseText(Row, currentSpan, Text);
                    }

                    Span oldseg = currentSpan;
                    while (currentSpan != EndSegment.span)
                    {
                        Row.endSpans.Add(currentSpan);
                        currentSpan = currentSpan.Parent;
                    }
                    Row.endSpans.Add(currentSpan);

                    TextStyle st2 = currentSpan.Scope.Style;

                    ParseTools.AddPatternString(EndSegment.Token, Row, EndSegment.Pattern,
                                                st2, currentSpan, false);
                    while (oldseg != EndSegment.span)
                    {
                        oldseg.EndRow = Row;
                        oldseg.EndWord = Row[Row.Count - 1];
                        oldseg = oldseg.Parent;
                    }

                    currentSpan.EndRow = Row;
                    currentSpan.EndWord = Row[Row.Count - 1];


                    if (currentSpan.Parent != null)
                        currentSpan = currentSpan.Parent;

                    CurrentPosition = EndSegment.Position + EndSegment.Token.Length;
                }
                else if (ChildSegment.HasContent)
                {
                    //this is a child block

                    if (ParseKeywords)
                    {
                        string Text = Row.Text.Substring(CurrentPosition,
                                                         ChildSegment.Position - CurrentPosition);
                        //TextStyle st=currentSpan.spanDefinition.Style;
                        ParseText(Row, currentSpan, Text);
                        //ParseTools.AddString (Text,Row,st,currentSpan);
                    }


                    var NewSeg = new Span
                                 {
                                     Parent = currentSpan,
                                     spanDefinition = ChildSegment.spanDefinition,
                                     Scope = ChildSegment.Scope
                                 };

                    Row.startSpans.Add(NewSeg);

                    TextStyle st2 = NewSeg.Scope.Style;
                    ParseTools.AddPatternString(ChildSegment.Token, Row,
                                                ChildSegment.Pattern, st2, NewSeg, false);
                    NewSeg.StartRow = Row;
                    NewSeg.StartWord = Row[Row.Count - 1];


                    currentSpan = NewSeg;
                    CurrentPosition = ChildSegment.Position + ChildSegment.Token.Length;

                    if (ChildSegment.Scope.spawnSpanOnStart != null)
                    {
                        var SpawnSeg = new Span
                                       {
                                           Parent = NewSeg,
                                           spanDefinition = ChildSegment.Scope.spawnSpanOnStart,
                                           Scope = new Scope(),
                                           StartWord = NewSeg.StartWord
                                       };
                        Row.startSpans.Add(SpawnSeg);
                        currentSpan = SpawnSeg;
                    }
                }
                else
                {
                    if (CurrentPosition < Row.Text.Length)
                    {
                        if (ParseKeywords)
                        {
                            //we did not find a childblock nor an endblock , just output the last pice of text
                            string Text = Row.Text.Substring(CurrentPosition);
                            //TextStyle st=currentSpan.spanDefinition.Style;	
                            ParseText(Row, currentSpan, Text);
                            //ParseTools.AddString (Text,Row,st,currentSpan);
                        }
                    }
                    break;
                }
            }

            while (!currentSpan.spanDefinition.MultiLine)
            {
                Row.endSpans.Add(currentSpan);
                currentSpan = currentSpan.Parent;
            }

            Row.endSpan = currentSpan;
            Row.SetExpansionSegment();

            Row.RowState = ParseKeywords ? RowState.AllParsed : RowState.SpanParsed;

            if (IsSameButDifferent(index, oldStartSpan))
            {
                MakeSame(index);
                //if (!IsSameButDifferent(index))
                //	System.Diagnostics.Debugger.Break();
            }

            if (Row.CanFold)
                Row.expansion_StartSpan.Expanded = Fold;

            //dont flag next line as needs parsing if only parsing keywords
            if (!ParseKeywords)
            {
                if (oldEndSpan != null)
                {
                    if (Row.endSpan != oldEndSpan && index <= Document.Count - 2)
                    {
                        //if (Row.CanFold)
                        //	Row.expansion_StartSpan.Expanded = true;
                        Document[index + 1].AddToParseQueue();
                        Document.NeedResetRows = true;
                    }
                }
                else if (index <= Document.Count - 2)
                {
                    //if (Row.CanFold)
                    //	Row.expansion_StartSpan.Expanded = true;
                    Document[index + 1].AddToParseQueue();
                    Document.NeedResetRows = true;
                }
            }

            if (oldEndSpan != null)
            {
                //expand span if this line dont have an end word
                if (oldEndSpan.EndWord == null)
                    oldEndSpan.Expanded = true;
            }
        }


        private ScanResultSegment GetEndSegment(Row Row, Span currentSpan,
                                                int StartPos)
        {
            //this row has no text , just bail out...
            if (StartPos >= Row.Text.Length || currentSpan.Scope == null)
                return new ScanResultSegment();

            var Result = new ScanResultSegment {HasContent = false, IsEndSegment = false};


            //--------------------------------------------------------------------------------
            //scan for childblocks
            //scan each scope in each childblock

            Span seg = currentSpan;

            while (seg != null)
            {
                if (seg == currentSpan || seg.spanDefinition.TerminateChildren)
                {
                    foreach (Pattern end in seg.Scope.EndPatterns)
                    {
                        PatternScanResult psr = end.IndexIn(Row.Text, StartPos,
                                                            seg.Scope.CaseSensitive, Separators);
                        int CurrentPosition = psr.Index;
                        if (psr.Token != "")
                        {
                            if ((psr.Index < Result.Position && Result.HasContent) ||
                                !Result.HasContent)
                            {
                                //we found a better match
                                //store this new match
                                Result.Pattern = end;
                                Result.Position = CurrentPosition;
                                Result.Token = psr.Token;
                                Result.HasContent = true;
                                Result.span = seg;
                                Result.Scope = null;


                                if (!end.IsComplex)
                                {
                                    if (seg.Scope.NormalizeCase)
                                        if (!seg.Scope.Start.IsComplex)
                                            Result.Token = end.StringPattern;
                                }
                            }
                        }
                    }
                }
                seg = seg.Parent;
            }

            //no result , return new ScanResultSegment();
            if (!Result.HasContent)
                return new ScanResultSegment();

            return Result;
        }

        private ScanResultSegment GetNextChildSegment(Row Row, Span
                                                                   currentSpan, int StartPos)
        {
            //this row has no text , just bail out...
            if (StartPos >= Row.Text.Length)
                return new ScanResultSegment();


            var Result = new ScanResultSegment {HasContent = false, IsEndSegment = false};


            foreach (SpanDefinition ChildBlock in currentSpan.spanDefinition.childSpanDefinitions)
            {
                //scan each scope in each childblock
                foreach (Scope Scope in ChildBlock.ScopePatterns)
                {
                    PatternScanResult psr = Scope.Start.IndexIn(Row.Text, StartPos,
                                                                Scope.CaseSensitive, Separators);
                    int CurrentPosition = psr.Index;
                    if ((!Result.HasContent || CurrentPosition < Result.Position) &&
                        psr.Token != "")
                    {
                        //we found a better match
                        //store this new match
                        Result.Pattern = Scope.Start;
                        Result.Position = CurrentPosition;
                        Result.Token = psr.Token;
                        Result.HasContent = true;
                        Result.spanDefinition = ChildBlock;
                        Result.Scope = Scope;

                        if (Scope.NormalizeCase)
                            if (!Scope.Start.IsComplex)
                                Result.Token = Scope.Start.StringPattern;
                    }
                }
            }


            //no result ,  new ScanResultSegment();
            if (!Result.HasContent)
                return new ScanResultSegment();

            return Result;
        }
    }
}