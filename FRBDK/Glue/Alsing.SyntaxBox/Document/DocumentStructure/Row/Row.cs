// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *
// * contributions by Sebastian Faltoni

using System.Collections;
using System.Drawing;

namespace Alsing.SourceCode
{
    /// <summary>
    /// Parser state of a row
    /// </summary>
    public enum RowState
    {
        /// <summary>
        /// the row is not parsed
        /// </summary>
        NotParsed = 0,
        /// <summary>
        /// the row is span parsed
        /// </summary>
        SpanParsed = 1,
        /// <summary>
        /// the row is both span and keyword parsed
        /// </summary>
        AllParsed = 2
    }

    /// <summary>
    /// The row class represents a row in a SyntaxDocument
    /// </summary>
    public sealed class Row : IEnumerable
    {
        #region General Declarations

        private RowState _RowState = RowState.NotParsed;

        /// <summary>
        /// The owner document
        /// </summary>
        public SyntaxDocument Document;

        /// <summary>
        /// The first span that terminates on this row.
        /// </summary>
        public Span endSpan;

        /// <summary>
        /// Segments that ends in this row
        /// </summary>
        public SpanList endSpans = new SpanList();

        /// <summary>
        /// For public use only
        /// </summary>
        public int Expansion_EndChar;

        /// <summary>
        /// 
        /// </summary>
        public Span expansion_EndSpan;

        /// <summary>
        /// For public use only
        /// </summary>
        public int Expansion_PixelEnd;

        /// <summary>
        /// For public use only
        /// </summary>
        public int Expansion_PixelStart;

        /// <summary>
        /// For public use only
        /// </summary>
        public int Expansion_StartChar;

        /// <summary>
        /// 
        /// </summary>
        public Span expansion_StartSpan;

        public WordList FormattedWords = new WordList();

        /// <summary>
        /// Collection of Image indices assigned to a row.
        /// </summary>
        /// <example>
        /// <b>Add an image to the current row.</b>
        /// <code>
        /// MySyntaxBox.Caret.CurrentRow.Images.Add(3);
        /// </code>
        /// </example>
        public ImageIndexList Images = new ImageIndexList();

        /// <summary>
        /// For public use only
        /// </summary>
        public int Indent; //value indicating how much this line should be indented (c style)


        /// <summary>
        /// Returns true if the row is in the owner documents keyword parse queue
        /// </summary>
        public bool InKeywordQueue; //is this line in the parseQueue?

        /// <summary>
        /// Returns true if the row is in the owner documents parse queue
        /// </summary>
        public bool InQueue; //is this line in the parseQueue?

        private bool mBookmarked; //is this line bookmarked?
        private bool mBreakpoint; //Does this line have a breakpoint?
        private string mText = "";
        internal WordList words = new WordList();

        /// <summary>
        /// The first collapsable span on this row.
        /// </summary>
        public Span startSpan;

        /// <summary>
        /// Segments that start on this row
        /// </summary>
        public SpanList startSpans = new SpanList();

        /// <summary>
        /// Object tag for storage of custom user data..
        /// </summary>
        /// <example>
        /// <b>Assign custom data to a row</b>
        /// <code>
        /// //custom data class
        /// class CustomData{
        ///		public int		abc=123;
        ///		publci string	def="abc";
        /// }
        /// 
        /// ...
        /// 
        /// //assign custom data to a row
        /// Row MyRow=MySyntaxBox.Caret.CurrentRow;
        /// CustomData MyData=new CustomData();
        /// MyData.abc=1337;
        /// MyRow.Tag=MyData;
        /// 
        /// ...
        /// 
        /// //read custom data from a row
        /// Row MyRow=MySyntaxBox.Caret.CurrentRow;
        /// if (MyRow.Tag != null){
        ///		CustomData MyData=(CustomData)MyRow.Tag;
        ///		if (MyData.abc==1337){
        ///			//Do something...
        ///		}
        /// }
        /// 
        /// 
        /// </code>
        /// </example>
        public object Tag;

        #region PUBLIC PROPERTY BACKCOLOR

        private Color _BackColor = Color.Transparent;

        public Color BackColor
        {
            get { return _BackColor; }
            set { _BackColor = value; }
        }

        #endregion

        public int Depth
        {
            get
            {
                int i = 0;
                Span s = startSpan;
                while (s != null)
                {
                    if (s.Scope != null && s.Scope.CauseIndent)
                        i++;

                    s = s.Parent;
                }
                //				if (i>0)
                //					i--;

                if (ShouldOutdent)
                    i--;

                return i;
            }
        }

        public bool ShouldOutdent
        {
            get
            {
                if (startSpan.EndRow == this)
                {
                    if (startSpan.Scope.CauseIndent)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// The parse state of this row
        /// </summary>
        /// <example>
        /// <b>Test if the current row is fully parsed.</b>
        /// <code>
        /// if (MySyntaxBox.Caret.CurrentRow.RowState==RowState.AllParsed)
        /// {
        ///		//do something
        /// }
        /// </code>
        /// </example>
        public RowState RowState
        {
            get { return _RowState; }
            set
            {
                if (value == _RowState)
                    return;

                if (value == RowState.SpanParsed && !InKeywordQueue)
                {
                    Document.KeywordQueue.Add(this);
                    InKeywordQueue = true;
                }

                if ((value == RowState.AllParsed || value == RowState.NotParsed) && InKeywordQueue)
                {
                    Document.KeywordQueue.Remove(this);
                    InKeywordQueue = false;
                }

                _RowState = value;
            }
        }

        #endregion

        /// <summary>
        /// Gets or Sets if this row has a bookmark or not.
        /// </summary>
        public bool Bookmarked
        {
            get { return mBookmarked; }
            set
            {
                mBookmarked = value;

                if (value)
                    Document.InvokeBookmarkAdded(this);
                else
                    Document.InvokeBookmarkRemoved(this);

                Document.InvokeChange();
            }
        }

        /// <summary>
        /// Gets or Sets if this row has a breakpoint or not.
        /// </summary>
        public bool Breakpoint
        {
            get { return mBreakpoint; }
            set
            {
                mBreakpoint = value;
                if (value)
                    Document.InvokeBreakPointAdded(this);
                else
                    Document.InvokeBreakPointRemoved(this);

                Document.InvokeChange();
            }
        }

        /// <summary>
        /// Returns the number of words in the row.
        /// (this only applied if the row is fully parsed)
        /// </summary>
        public int Count
        {
            get { return words.Count; }
        }

        /// <summary>
        /// Gets or Sets the text of the row.
        /// </summary>
        public string Text
        {
            get { return mText; }

            set
            {
                bool ParsePreview = false;
                if (mText != value)
                {
                    ParsePreview = true;
                    Document.Modified = true;
                }

                mText = value;
                if (Document != null)
                {
                    if (ParsePreview)
                    {
                        Document.Parser.ParsePreviewLine(Document.IndexOf(this));
                        Document.OnApplyFormatRanges(this);
                    }

                    AddToParseQueue();
                }
            }
        }

        /// <summary>
        /// Return the Word object at the specified index.
        /// </summary>
        public Word this[int index]
        {
            get
            {
                if (index >= 0)
                    return words[index];
                return new Word();
            }
        }

        public int StartWordIndex
        {
            get
            {
                if (expansion_StartSpan == null)
                    return 0;

                //				if (this.expansion_StartSpan.StartRow != this)
                //					return 0;

                Word w = expansion_StartSpan.StartWord;

                int i = 0;
                foreach (Word wo in this)
                {
                    if (wo == w)
                        break;
                    i += wo.Text.Length;
                }
                return i;
            }
        }

        public Word FirstNonWsWord
        {
            get
            {
                foreach (Word w in this)
                {
                    if (w.Type == WordType.Word)
                        return w;
                }
                return null;
            }
        }

        /// <summary>
        /// Returns the index of this row in the owner SyntaxDocument.
        /// </summary>
        public int Index
        {
            get { return Document.IndexOf(this); }
        }

        /// <summary>
        /// Returns the visible index of this row in the owner SyntaxDocument
        /// </summary>
        public int VisibleIndex
        {
            get
            {
                int i = Document.VisibleRows.IndexOf(this);
                if (i == -1)
                {
                    if (startSpan != null && 
                        startSpan.StartRow != null && 
                        startSpan.StartRow != this)

                        return startSpan.StartRow.VisibleIndex;

                    return Index;
                }
                return Document.VisibleRows.IndexOf(this);
            }
        }

        /// <summary>
        /// Returns the next visible row.
        /// </summary>
        public Row NextVisibleRow
        {
            get
            {
                int i = VisibleIndex;
                if (i > Document.VisibleRows.Count)
                    return null;

                if (i + 1 < Document.VisibleRows.Count)
                {
                    return Document.VisibleRows[i + 1];
                }
                return null;
            }
        }

        /// <summary>
        /// Returns the next row
        /// </summary>
        public Row NextRow
        {
            get
            {
                int i = Index;
                if (i + 1 <= Document.Lines.Length - 1)
                    return Document[i + 1];
                return null;
            }
        }

        /// <summary>
        /// Returns the first visible row before this row.
        /// </summary>
        public Row PrevVisibleRow
        {
            get
            {
                int i = VisibleIndex;
                if (i < 0)
                    return null;

                if (i - 1 >= 0)
                    return Document.VisibleRows[i - 1];
                return null;
            }
        }

        /// <summary>
        /// Returns true if the row is collapsed
        /// </summary>
        public bool IsCollapsed
        {
            get
            {
                if (expansion_StartSpan != null)
                    if (expansion_StartSpan.Expanded == false)
                        return true;
                return false;
            }
        }

        /// <summary>
        /// Returns true if this row is the last part of a collepsed span
        /// </summary>
        public bool IsCollapsedEndPart
        {
            get
            {
                if (expansion_EndSpan != null)
                    if (expansion_EndSpan.Expanded == false)
                        return true;
                return false;
            }
        }


        /// <summary>
        /// Returns true if this row can fold
        /// </summary>
        public bool CanFold
        {
            get
            {
                return (expansion_StartSpan != null && expansion_StartSpan.EndRow != null &&
                        Document.IndexOf(expansion_StartSpan.EndRow) != 0);
            }
        }

        /// <summary>
        /// Gets or Sets if this row is expanded.
        /// </summary>
        public bool Expanded
        {
            get
            {
                if (CanFold)
                {
                    return (expansion_StartSpan.Expanded);
                }
                return false;
            }
            set
            {
                if (CanFold)
                {
                    expansion_StartSpan.Expanded = value;
                }
            }
        }

        public string ExpansionText
        {
            get { return expansion_StartSpan.Scope.ExpansionText; }
            set
            {
                Scope oScope = expansion_StartSpan.Scope;
                var oNewScope = new Scope
                                {
                                    CaseSensitive = oScope.CaseSensitive,
                                    CauseIndent = oScope.CauseIndent,
                                    DefaultExpanded = oScope.DefaultExpanded,
                                    EndPatterns = oScope.EndPatterns,
                                    NormalizeCase = oScope.NormalizeCase,
                                    Parent = oScope.Parent,
                                    spawnSpanOnEnd = oScope.spawnSpanOnEnd,
                                    spawnSpanOnStart = oScope.spawnSpanOnStart,
                                    Start = oScope.Start,
                                    Style = oScope.Style,
                                    ExpansionText = value
                                };
                expansion_StartSpan.Scope = oNewScope;
                Document.InvokeChange();
            }
        }

        /// <summary>
        /// Returns true if this row is the end part of a collapsable span
        /// </summary>
        public bool CanFoldEndPart
        {
            get { return (expansion_EndSpan != null); }
        }

        /// <summary>
        /// For public use only
        /// </summary>
        public bool HasExpansionLine
        {
            get { return (endSpan.Parent != null); }
        }

        /// <summary>
        /// Returns the last row of a collapsable span
        /// (this only applies if this row is the start row of the span)
        /// </summary>
        public Row Expansion_EndRow
        {
            get
            {
                if (CanFold)
                    return expansion_StartSpan.EndRow;
                return this;
            }
        }

        /// <summary>
        /// Returns the first row of a collapsable span
        /// (this only applies if this row is the last row of the span)
        /// </summary>
        public Row Expansion_StartRow
        {
            get
            {
                if (CanFoldEndPart)
                    return expansion_EndSpan.StartRow;
                return this;
            }
        }

        /// <summary>
        /// For public use only
        /// </summary>
        public Row VirtualCollapsedRow
        {
            get
            {
                var r = new Row();

                foreach (Word w in this)
                {
                    if (expansion_StartSpan == w.Span)
                        break;
                    r.Add(w);
                }

                Word wo = r.Add(CollapsedText);
                wo.Style = new TextStyle {BackColor = Color.Silver, ForeColor = Color.DarkBlue, Bold = true};

                bool found = false;
                if (Expansion_EndRow != null)
                {
                    foreach (Word w in Expansion_EndRow)
                    {
                        if (found)
                            r.Add(w);
                        if (w == Expansion_EndRow.expansion_EndSpan.EndWord)
                            found = true;
                    }
                }
                return r;
            }
        }

        /// <summary>
        /// Returns the text that should be displayed if the row is collapsed.
        /// </summary>
        public string CollapsedText
        {
            get
            {
                string str = "";
                int pos = 0;
                foreach (Word w in this)
                {
                    pos += w.Text.Length;
                    if (w.Span == expansion_StartSpan)
                    {
                        str = Text.Substring(pos).Trim();
                        break;
                    }
                }
                if (expansion_StartSpan.Scope.ExpansionText != "")
                    str = expansion_StartSpan.Scope.ExpansionText.Replace("***", str);
                return str;
            }
        }

        /// <summary>
        /// Returns the row before this row.
        /// </summary>
        public Row PrevRow
        {
            get
            {
                int i = Index;

                if (i - 1 >= 0)
                    return Document[i - 1];
                return null;
            }
        }

        #region IEnumerable Members

        /// <summary>
        /// Get the Word enumerator for this row
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            return words.GetEnumerator();
        }

        #endregion

        public void Clear()
        {
            words.Clear();
        }

        /// <summary>
        /// If the row is hidden inside a collapsed span , call this method to make the collapsed segments expanded.
        /// </summary>
        public void EnsureVisible()
        {
            if (RowState == RowState.NotParsed)
                return;

            Span seg = startSpan;
            while (seg != null)
            {
                seg.Expanded = true;
                seg = seg.Parent;
            }
            Document.ResetVisibleRows();
        }

        public Word Add(string text)
        {
            var xw = new Word {Row = this, Text = text};
            words.Add(xw);
            return xw;
        }

        /// <summary>
        /// Adds this row to the parse queue
        /// </summary>
        public void AddToParseQueue()
        {
            if (!InQueue)
                Document.ParseQueue.Add(this);
            InQueue = true;
            RowState = RowState.NotParsed;
        }

        /// <summary>
        /// Assigns a new text to the row.
        /// </summary>
        /// <param name="text"></param>
        public void SetText(string text)
        {
            Document.StartUndoCapture();
            var tp = new TextPoint(0, Index);
            var tr = new TextRange {FirstColumn = 0, FirstRow = tp.Y, LastColumn = Text.Length, LastRow = tp.Y};

            Document.StartUndoCapture();
            //delete the current line
            Document.PushUndoBlock(UndoAction.DeleteRange, Document.GetRange(tr), tr.FirstColumn, tr.FirstRow);
            //alter the text
            Document.PushUndoBlock(UndoAction.InsertRange, text, tp.X, tp.Y);
            Text = text;
            Document.EndUndoCapture();
            Document.InvokeChange();
        }

        /// <summary>
        /// Call this method to make all words match the case of their patterns.
        /// (this only applies if the row is fully parsed)
        /// </summary>
        public void MatchCase()
        {
            string s = "";
            foreach (Word w in words)
            {
                s = s + w.Text;
            }
            mText = s;
        }

        /// <summary>
        /// Force a span parse on the row.
        /// </summary>
        public void Parse()
        {
            Document.ParseRow(this);
        }

        /// <summary>
        /// Forces the parser to parse this row directly
        /// </summary>
        /// <param name="ParseKeywords">true if keywords and operators should be parsed</param>
        public void Parse(bool ParseKeywords)
        {
            Document.ParseRow(this, ParseKeywords);
        }

        public void SetExpansionSegment()
        {
            expansion_StartSpan = null;
            expansion_EndSpan = null;
            foreach (Span s in startSpans)
            {
                if (!endSpans.Contains(s))
                {
                    expansion_StartSpan = s;
                    break;
                }
            }

            foreach (Span s in endSpans)
            {
                if (!startSpans.Contains(s))
                {
                    expansion_EndSpan = s;
                    break;
                }
            }

            if (expansion_EndSpan != null)
                expansion_StartSpan = null;
        }

        /// <summary>
        /// Returns the whitespace string at the begining of this row.
        /// </summary>
        /// <returns>a string containing the whitespace at the begining of this row</returns>
        public string GetLeadingWhitespace()
        {
            string s = mText;
            int i;
            s = s.Replace("	", " ");
            for (i = 0; i < s.Length; i++)
            {
                if (s.Substring(i, 1) == " ") {}
                else
                {
                    break;
                }
            }
            return mText.Substring(0, i);
        }

        public string GetVirtualLeadingWhitespace()
        {
            int i = StartWordIndex;
            string ws = "";
            foreach (char c in Text)
            {
                if (c == '\t')
                    ws += c;
                else
                    ws += ' ';

                i--;
                if (i <= 0)
                    break;
            }
            return ws;
        }

        /// <summary>
        /// Adds a word object to this row
        /// </summary>
        /// <param name="word">Word object</param>
        public void Add(Word word)
        {
            word.Row = this;
            words.Add(word);
        }

        /// <summary>
        /// Returns the index of a specific Word object
        /// </summary>
        /// <param name="word">Word object to find</param>
        /// <returns>index of the word in the row</returns>
        public int IndexOf(Word word)
        {
            return words.IndexOf(word);
        }

        /// <summary>
        /// For public use only
        /// </summary>
        /// <param name="PatternList"></param>
        /// <param name="StartWord"></param>
        /// <param name="IgnoreStartWord"></param>
        /// <returns></returns>
        public Word FindRightWordByPatternList(PatternList PatternList, Word StartWord, bool IgnoreStartWord)
        {
            int i = StartWord.Index;
            if (IgnoreStartWord)
                i++;
            while (i < words.Count)
            {
                Word w = this[i];
                if (w.Pattern != null)
                {
                    if (w.Pattern.Parent != null)
                    {
                        if (w.Pattern.Parent == PatternList && w.Type != WordType.Space && w.Type != WordType.Tab)
                        {
                            return w;
                        }
                    }
                }
                i++;
            }
            return null;
        }

        /// <summary>
        /// For public use only
        /// </summary>
        /// <param name="PatternListName"></param>
        /// <param name="StartWord"></param>
        /// <param name="IgnoreStartWord"></param>
        /// <returns></returns>
        public Word FindRightWordByPatternListName(string PatternListName, Word StartWord, bool IgnoreStartWord)
        {
            int i = StartWord.Index;
            if (IgnoreStartWord)
                i++;

            while (i < words.Count)
            {
                Word w = this[i];
                if (w.Pattern != null)
                {
                    if (w.Pattern.Parent != null)
                    {
                        if (w.Pattern.Parent.Name == PatternListName && w.Type != WordType.Space &&
                            w.Type != WordType.Tab)
                        {
                            return w;
                        }
                    }
                }
                i++;
            }
            return null;
        }

        /// <summary>
        /// For public use only
        /// </summary>
        /// <param name="PatternList"></param>
        /// <param name="StartWord"></param>
        /// <param name="IgnoreStartWord"></param>
        /// <returns></returns>
        public Word FindLeftWordByPatternList(PatternList PatternList, Word StartWord, bool IgnoreStartWord)
        {
            int i = StartWord.Index;
            if (IgnoreStartWord)
                i--;
            while (i >= 0)
            {
                Word w = this[i];
                if (w.Pattern != null)
                {
                    if (w.Pattern.Parent != null)
                    {
                        if (w.Pattern.Parent == PatternList && w.Type != WordType.Space && w.Type != WordType.Tab)
                        {
                            return w;
                        }
                    }
                }
                i--;
            }
            return null;
        }

        /// <summary>
        /// For public use only
        /// </summary>
        /// <param name="PatternListName"></param>
        /// <param name="StartWord"></param>
        /// <param name="IgnoreStartWord"></param>
        /// <returns></returns>
        public Word FindLeftWordByPatternListName(string PatternListName, Word StartWord, bool IgnoreStartWord)
        {
            int i = StartWord.Index;
            if (IgnoreStartWord)
                i--;

            while (i >= 0)
            {
                Word w = this[i];
                if (w.Pattern != null)
                {
                    if (w.Pattern.Parent != null)
                    {
                        if (w.Pattern.Parent.Name == PatternListName && w.Type != WordType.Space &&
                            w.Type != WordType.Tab)
                        {
                            return w;
                        }
                    }
                }
                i--;
            }
            return null;
        }

        /// <summary>
        /// For public use only
        /// </summary>
        /// <param name="spanDefinition"></param>
        /// <param name="StartWord"></param>
        /// <param name="IgnoreStartWord"></param>
        /// <returns></returns>
        public Word FindLeftWordByBlockType(SpanDefinition spanDefinition, Word StartWord, bool IgnoreStartWord)
        {
            int i = StartWord.Index;
            if (IgnoreStartWord)
                i--;
            while (i >= 0)
            {
                Word w = this[i];
                if (w.Span.spanDefinition == spanDefinition && w.Type != WordType.Space && w.Type != WordType.Tab)
                {
                    return w;
                }
                i--;
            }
            return null;
        }

        /// <summary>
        /// For public use only
        /// </summary>
        /// <param name="spanDefinition"></param>
        /// <param name="StartWord"></param>
        /// <param name="IgnoreStartWord"></param>
        /// <returns></returns>
        public Word FindRightWordByBlockType(SpanDefinition spanDefinition, Word StartWord, bool IgnoreStartWord)
        {
            int i = StartWord.Index;
            if (IgnoreStartWord)
                i++;
            while (i < words.Count)
            {
                Word w = this[i];
                if (w.Span.spanDefinition == spanDefinition && w.Type != WordType.Space && w.Type != WordType.Tab)
                {
                    return w;
                }
                i++;
            }
            return null;
        }

        /// <summary>
        /// For public use only
        /// </summary>
        /// <param name="BlockTypeName"></param>
        /// <param name="StartWord"></param>
        /// <param name="IgnoreStartWord"></param>
        /// <returns></returns>
        public Word FindLeftWordByBlockTypeName(string BlockTypeName, Word StartWord, bool IgnoreStartWord)
        {
            int i = StartWord.Index;
            if (IgnoreStartWord)
                i--;
            while (i >= 0)
            {
                Word w = this[i];
                if (w.Span.spanDefinition.Name == BlockTypeName && w.Type != WordType.Space && w.Type != WordType.Tab)
                {
                    return w;
                }
                i--;
            }
            return null;
        }

        /// <summary>
        /// For public use only
        /// </summary>
        /// <param name="BlockTypeName"></param>
        /// <param name="StartWord"></param>
        /// <param name="IgnoreStartWord"></param>
        /// <returns></returns>
        public Word FindRightWordByBlockTypeName(string BlockTypeName, Word StartWord, bool IgnoreStartWord)
        {
            int i = StartWord.Index;
            if (IgnoreStartWord)
                i++;
            while (i < words.Count)
            {
                Word w = this[i];
                if (w.Span.spanDefinition.Name == BlockTypeName && w.Type != WordType.Space && w.Type != WordType.Tab)
                {
                    return w;
                }
                i++;
            }
            return null;
        }
    }
}