// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using Alsing.Drawing.GDI;
using Alsing.Globalization;
using Alsing.SourceCode;

namespace Alsing.Windows.Forms.SyntaxBox.Painter
{
    /// <summary>
    /// Painter class that uses GDI32 to render the content of a SyntaxBoxControl
    /// </summary>
    public class NativePainter : IPainter
    {
        private readonly EditViewControl Control;
        private Word BracketEnd;
        private Word BracketStart;
        private int FirstSpanRow;
        private RenderItems GFX;
        private int LastRow;
        private int LastSpanRow;
        private bool RenderCaretRowOnly;
        private int ResizeCount;
        private bool SpanFound;
        private int yOffset;

        /// <summary>
        /// NativePainter constructor.
        /// </summary>
        /// <param name="control">The control that will use the Painter</param>
        public NativePainter(EditViewControl control)
        {
            Control = control;
            InitGraphics();
        }

        #region IPainter Members

        /// <summary>
        /// Implementation of the IPainter Resize method
        /// </summary>
        public void Resize()
        {
            ResizeCount++;
            InitGraphics();
            //	Console.WriteLine ("painterresize {0} {1}",ResizeCount,Control.Name);
        }

        /// <summary>
        /// Implementation of the IPainter MeasureString method
        /// </summary>
        /// <param name="s">String to measure</param>
        /// <returns>Size of the string in pixels</returns>
        public Size MeasureString(string s)
        {
            try
            {
                GFX.StringBuffer.Font = GFX.FontNormal;
                return GFX.StringBuffer.MeasureTabbedString(s, Control.TabSize);
            }
            catch
            {
                return new Size(0, 0);
            }
        }

        /// <summary>
        /// Implementation of the IPainter InitGraphics method.
        /// Initializes GDI32 backbuffers and brushes.
        /// </summary>
        public void InitGraphics()
        {
            try
            {
                if (GFX.BackgroundBrush != null)
                    GFX.BackgroundBrush.Dispose();

                if (GFX.GutterMarginBrush != null)
                    GFX.GutterMarginBrush.Dispose();

                if (GFX.LineNumberMarginBrush != null)
                    GFX.LineNumberMarginBrush.Dispose();

                if (GFX.HighLightLineBrush != null)
                    GFX.HighLightLineBrush.Dispose();

                if (GFX.LineNumberMarginBorderBrush != null)
                    GFX.LineNumberMarginBorderBrush.Dispose();

                if (GFX.GutterMarginBorderBrush != null)
                    GFX.GutterMarginBorderBrush.Dispose();

                if (GFX.OutlineBrush != null)
                    GFX.OutlineBrush.Dispose();


                GFX.BackgroundBrush = new GDIBrush(Control.BackColor);
                GFX.GutterMarginBrush = new GDIBrush(Control.GutterMarginColor);
                GFX.LineNumberMarginBrush = new GDIBrush(Control.LineNumberBackColor);
                GFX.HighLightLineBrush = new GDIBrush(Control.HighLightedLineColor);
                GFX.LineNumberMarginBorderBrush = new GDIBrush(Control.LineNumberBorderColor);
                GFX.GutterMarginBorderBrush = new GDIBrush(Control.GutterMarginBorderColor);
                GFX.OutlineBrush = new GDIBrush(Control.OutlineColor);


                if (GFX.FontNormal != null)
                    GFX.FontNormal.Dispose();

                if (GFX.FontBold != null)
                    GFX.FontBold.Dispose();

                if (GFX.FontItalic != null)
                    GFX.FontItalic.Dispose();

                if (GFX.FontBoldItalic != null)
                    GFX.FontBoldItalic.Dispose();

                if (GFX.FontUnderline != null)
                    GFX.FontUnderline.Dispose();

                if (GFX.FontBoldUnderline != null)
                    GFX.FontBoldUnderline.Dispose();

                if (GFX.FontItalicUnderline != null)
                    GFX.FontItalicUnderline.Dispose();

                if (GFX.FontBoldItalicUnderline != null)
                    GFX.FontBoldItalicUnderline.Dispose();


                //	string font="courier new";
                string font = Control.FontName;
                float fontsize = Control.FontSize;
                GFX.FontNormal = new GDIFont(font, fontsize, false, false, false, false);
                GFX.FontBold = new GDIFont(font, fontsize, true, false, false, false);
                GFX.FontItalic = new GDIFont(font, fontsize, false, true, false, false);
                GFX.FontBoldItalic = new GDIFont(font, fontsize, true, true, false, false);
                GFX.FontUnderline = new GDIFont(font, fontsize, false, false, true, false);
                GFX.FontBoldUnderline = new GDIFont(font, fontsize, true, false, true, false);
                GFX.FontItalicUnderline = new GDIFont(font, fontsize, false, true, true, false);
                GFX.FontBoldItalicUnderline = new GDIFont(font, fontsize, true, true, true, false);

                InitIMEWindow();
            }
            catch (Exception) {}

            if (Control != null)
            {
                if (Control.IsHandleCreated)
                {
                    if (GFX.StringBuffer != null)
                        GFX.StringBuffer.Dispose();

                    if (GFX.SelectionBuffer != null)
                        GFX.SelectionBuffer.Dispose();

                    if (GFX.BackBuffer != null)
                        GFX.BackBuffer.Dispose();

                    GFX.StringBuffer = new GDISurface(1, 1, Control, true) {Font = GFX.FontNormal};
                    int h = GFX.StringBuffer.MeasureTabbedString("ABC", 0).Height + Control._SyntaxBox.RowPadding;
                    GFX.BackBuffer = new GDISurface(Control.ClientWidth, h, Control, true) {Font = GFX.FontNormal};

                    GFX.SelectionBuffer = new GDISurface(Control.ClientWidth, h, Control, true) {Font = GFX.FontNormal};

                    Control.View.RowHeight = GFX.BackBuffer.MeasureTabbedString("ABC", 0).Height + Control._SyntaxBox.RowPadding;
                    Control.View.CharWidth = GFX.BackBuffer.MeasureTabbedString(" ", 0).Width;
                }
            }
        }




        /// <summary>
        /// Implementation of the IPainter RenderAll method.
        /// </summary>
        public void RenderAll()
        {
            //
            Control.View.RowHeight = GFX.BackBuffer.MeasureString("ABC").Height;
            Control.View.CharWidth = GFX.BackBuffer.MeasureString(" ").Width;


            Control.InitVars();

            Graphics g = Control.CreateGraphics();

            RenderAll(g);

            g.Dispose();
        }

        public void RenderCaret(Graphics g)
        {
            RenderCaretRowOnly = true;
            RenderAll(g);
            RenderCaretRowOnly = false;
        }

        /// <summary>
        /// Implementation of the IPainter RenderAll method
        /// </summary>
        /// <param name="g">Target Graphics object</param>
        public void RenderAll(Graphics g)
        {
            try
            {
                Control.InitVars();
                Control.InitScrollbars();
                SetBrackets();
                SetSpanIndicators();
                int j = Control.View.FirstVisibleRow;

                int diff = j - LastRow;
                LastRow = j;
                if (Control.SmoothScroll)
                {
                    if (diff == 1)
                    {
                        for (int i = Control.View.RowHeight; i > 0; i -= Control.SmoothScrollSpeed)
                        {
                            yOffset = i + Control.View.YOffset;
                            RenderAll2();
                            g.Flush();
                            Thread.Sleep(0);
                        }
                    }
                    else if (diff == -1)
                    {
                        for (int i = -Control.View.RowHeight; i < 0; i += Control.SmoothScrollSpeed)
                        {
                            yOffset = i + Control.View.YOffset;
                            RenderAll2();
                            g.Flush();
                            Thread.Sleep(0);
                        }
                    }
                }

                yOffset = Control.View.YOffset;
                RenderAll2();
                //g.Flush ();
                //System.Threading.Thread.Sleep (0);
            }
            catch {}
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="RowIndex"></param>
        public void RenderRow(int RowIndex)
        {
            RenderRow(RowIndex, 10);
        }

        /// <summary>
        /// Implementation of the iPainter CharFromPixel method
        /// </summary>
        /// <param name="X">Screen x position in pixels</param>
        /// <param name="Y">Screen y position in pixels</param>
        /// <returns>a Point where x is the column and y is the rowindex</returns>
        public TextPoint CharFromPixel(int X, int Y)
        {
            try
            {
                int RowIndex = Y/Control.View.RowHeight + Control.View.FirstVisibleRow;
                RowIndex = Math.Min(RowIndex, Control.Document.VisibleRows.Count);
                if (RowIndex == Control.Document.VisibleRows.Count)
                {
                    RowIndex--;
                    Row r = Control.Document.VisibleRows[RowIndex];
                    if (r.IsCollapsed)
                        r = r.Expansion_EndRow;

                    return new TextPoint(r.Text.Length, r.Index);
                }

                RowIndex = Math.Max(RowIndex, 0);
                Row row;
                if (Control.Document.VisibleRows.Count != 0)
                {
                    row = Control.Document.VisibleRows[RowIndex];
                    RowIndex = Control.Document.IndexOf(row);
                }
                else
                {
                    return new TextPoint(0, 0);
                }
                if (RowIndex == -1)
                    return new TextPoint(-1, -1);


                //normal line
                if (!row.IsCollapsed)
                    return ColumnFromPixel(RowIndex, X);

                //this.RenderRow (xtr.Index,-200);

                if (X < row.Expansion_PixelEnd - Control.View.FirstVisibleColumn*Control.View.CharWidth)
                {
                    //start of collapsed line
                    return ColumnFromPixel(RowIndex, X);
                }

                if (X >= row.Expansion_EndRow.Expansion_PixelStart - Control.View.FirstVisibleColumn*Control.View.CharWidth + Control.View.TextMargin)
                {
                    //end of collapsed line
                    return ColumnFromPixel(row.Expansion_EndRow.Index, X - row.Expansion_EndRow.Expansion_PixelStart + MeasureRow(row.Expansion_EndRow, row.Expansion_EndRow.Expansion_StartChar).Width);
                }

                //the collapsed text
                return new TextPoint(row.Expansion_EndChar, row.Index);
            }
            catch
            {
                Control._SyntaxBox.FontName = "Courier New";
                Control._SyntaxBox.FontSize = 10;
                return new TextPoint(0, 0);
            }
        }

        public int GetMaxCharWidth()
        {
            const string s = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            int max = 0;
            foreach (char c in s)
            {
                int tmp = MeasureString(c + "").Width;
                if (tmp > max)
                    max = tmp;
            }
            return max;
        }

        public void Dispose()
        {
            if (GFX.FontNormal != null)
                GFX.FontNormal.Dispose();

            if (GFX.FontBold != null)
                GFX.FontBold.Dispose();

            if (GFX.FontItalic != null)
                GFX.FontItalic.Dispose();

            if (GFX.FontBoldItalic != null)
                GFX.FontBoldItalic.Dispose();

            if (GFX.FontUnderline != null)
                GFX.FontUnderline.Dispose();

            if (GFX.FontBoldUnderline != null)
                GFX.FontBoldUnderline.Dispose();

            if (GFX.FontItalicUnderline != null)
                GFX.FontItalicUnderline.Dispose();

            if (GFX.FontBoldItalicUnderline != null)
                GFX.FontBoldItalicUnderline.Dispose();

            if (GFX.BackgroundBrush != null)
                GFX.BackgroundBrush.Dispose();

            if (GFX.GutterMarginBrush != null)
                GFX.GutterMarginBrush.Dispose();

            if (GFX.LineNumberMarginBrush != null)
                GFX.LineNumberMarginBrush.Dispose();

            if (GFX.HighLightLineBrush != null)
                GFX.HighLightLineBrush.Dispose();

            if (GFX.LineNumberMarginBorderBrush != null)
                GFX.LineNumberMarginBorderBrush.Dispose();

            if (GFX.GutterMarginBorderBrush != null)
                GFX.GutterMarginBorderBrush.Dispose();

            if (GFX.OutlineBrush != null)
                GFX.OutlineBrush.Dispose();

            if (GFX.StringBuffer != null)
                GFX.StringBuffer.Dispose();

            if (GFX.SelectionBuffer != null)
                GFX.SelectionBuffer.Dispose();

            if (GFX.BackBuffer != null)
                GFX.BackBuffer.Dispose();
        }

        #endregion

        private void InitIMEWindow()
        {
            if (Control.IMEWindow != null)
                Control.IMEWindow.SetFont(Control.FontName, Control.FontSize);
        }

        /// <summary>
        /// Implementation of the IPainter MeasureRow method.
        /// </summary>
        /// <param name="xtr">Row to measure</param>
        /// <param name="Count">Last char index</param>
        /// <returns>The size of the row in pixels</returns>
        public Size MeasureRow(Row xtr, int Count)
        {
            int width = 0;
            int taborig = -Control.View.FirstVisibleColumn*Control.View.CharWidth + Control.View.TextMargin;
            int xpos = Control.View.TextMargin - Control.View.ClientAreaStart;
            if (xtr.InQueue)
            {
                SetStringFont(false, false, false);
                int Padd = Math.Max(Count - xtr.Text.Length, 0);
                var PaddStr = new String(' ', Padd);
                string TotStr = xtr.Text + PaddStr;
                width = GFX.StringBuffer.MeasureTabbedString(TotStr.Substring(0, Count), Control.PixelTabSize).Width;
            }
            else
            {
                int CharNo = 0;
                int TotWidth = 0;
                foreach (Word w in xtr.FormattedWords)
                {
                    if (w.Type == WordType.Word && w.Style != null)
                        SetStringFont(w.Style.Bold, w.Style.Italic, w.Style.Underline);
                    else
                        SetStringFont(false, false, false);

                    if (w.Text.Length + CharNo >= Count || w == xtr.FormattedWords[xtr.FormattedWords.Count - 1])
                    {
                        int CharPos = Count - CharNo;
                        int MaxChars = Math.Min(CharPos, w.Text.Length);
                        TotWidth += GFX.StringBuffer.DrawTabbedString(w.Text.Substring(0, MaxChars), xpos + TotWidth, 0, taborig, Control.PixelTabSize).Width;
                        width = TotWidth;
                        break;
                    }

                    TotWidth += GFX.StringBuffer.DrawTabbedString(w.Text, xpos + TotWidth, 0, taborig, Control.PixelTabSize).Width;
                    CharNo += w.Text.Length;
                }

                SetStringFont(false, false, false);
                int Padd = Math.Max(Count - xtr.Text.Length, 0);
                var PaddStr = new String(' ', Padd);
                width += GFX.StringBuffer.DrawTabbedString(PaddStr, xpos + TotWidth, 0, taborig, Control.PixelTabSize).Width;
            }


            return new Size(width, 0);

            //	return GFX.BackBuffer.MeasureTabbedString (xtr.Text.Substring (0,Count),Control.PixelTabSize);
        }

        private void SetBrackets()
        {
            Span currentSpan;
            BracketEnd = null;
            BracketStart = null;

            Word CurrWord = Control.Caret.CurrentWord;
            if (CurrWord != null)
            {
                currentSpan = CurrWord.Span;
                if (currentSpan != null)
                {
                    if (CurrWord == currentSpan.StartWord || CurrWord == currentSpan.EndWord)
                    {
                        if (currentSpan.EndWord != null)
                        {
                            BracketEnd = currentSpan.EndWord;
                            BracketStart = currentSpan.StartWord;
                        }
                    }

                    try
                    {
                        if (CurrWord.Pattern == null)
                            return;

                        if (CurrWord.Pattern.BracketType == BracketType.EndBracket)
                        {
                            Word w = Control.Document.GetStartBracketWord(CurrWord, CurrWord.Pattern.MatchingBracket, CurrWord.Span);
                            BracketEnd = CurrWord;
                            BracketStart = w;
                        }
                        if (CurrWord.Pattern.BracketType == BracketType.StartBracket)
                        {
                            Word w = Control.Document.GetEndBracketWord(CurrWord, CurrWord.Pattern.MatchingBracket, CurrWord.Span);

                            //	if(w!=null)
                            //	{
                            BracketEnd = w;
                            BracketStart = CurrWord;
                            //	}
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void SetSpanIndicators()
        {
            SpanFound = false;
            try
            {
                Span s = Control.Caret.CurrentSegment();

                if (s == null || s.StartWord == null || s.StartWord.Row == null || s.EndWord == null || s.EndWord.Row == null)
                    return;

                FirstSpanRow = s.StartWord.Row.Index;
                LastSpanRow = s.EndWord.Row.Index;
                SpanFound = true;
            }
            catch {}
        }

        private void RenderAll2()
        {
            try
            {
                int j = Control.View.FirstVisibleRow;


                if (Control.AutoListStartPos != null)
                {
                    try
                    {
                        if (Control.AutoListVisible)
                        {
                            Point alP = GetTextPointPixelPos(Control.AutoListStartPos);
                            if (alP == new Point(-1, -1))
                            {
                                Control.AutoList.Visible = false;
                            }
                            else
                            {
                                alP.Y += Control.View.RowHeight + 2;
                                alP.X += -20;
                                alP = Control.PointToScreen(alP);

                                Screen screen = Screen.FromPoint(new Point(Control.Right, alP.Y));

                                if (alP.Y + Control.AutoList.Height > screen.WorkingArea.Height)
                                {
                                    alP.Y -= Control.View.RowHeight + 2 + Control.AutoList.Height;
                                }

                                if (alP.X + Control.AutoList.Width > screen.WorkingArea.Width)
                                {
                                    alP.X -= alP.X + Control.AutoList.Width - screen.WorkingArea.Width;
                                }


                                Control.AutoList.Location = alP;
                                //Control.Controls[0].Focus();
                                Control.Focus();
                            }
                        }
                        
                    }
                    catch {}
                }

                if (Control.InfoTipStartPos != null)
                {
                    try
                    {
                        if (Control.InfoTipVisible)
                        {
                            Point itP = GetTextPointPixelPos(Control.InfoTipStartPos);
                            if (itP == new Point(-1, -1))
                            {
                                Control.InfoTip.Visible = false;
                            }
                            else
                            {
                                itP.Y += Control.View.RowHeight + 2;
                                itP.X += -20;

                                itP = Control.PointToScreen(itP);

                                Screen screen = Screen.FromPoint(new Point(Control.Right, itP.Y));

                                if (itP.Y + Control.InfoTip.Height > screen.WorkingArea.Height)
                                {
                                    itP.Y -= Control.View.RowHeight + 2 + Control.InfoTip.Height;
                                }

                                if (itP.X + Control.InfoTip.Width > screen.WorkingArea.Width)
                                {
                                    itP.X -= itP.X + Control.InfoTip.Width - screen.WorkingArea.Width;
                                }


                                Control.InfoTip.Location = itP;
                                Control.InfoTip.Visible = true;
                                Debug.WriteLine("Infotip Made Visible");
                            }
                        }
                        else
                        {
                            Control.InfoTip.Visible = false;
                            Debug.WriteLine("Infotip Made Invisible");
                        }
                    }
                    catch {}
                }

                for (int i = 0; i < Control.View.VisibleRowCount; i++)
                {
                    if (j >= 0 && j < Control.Document.VisibleRows.Count)
                    {
                        Row r = Control.Document.VisibleRows[j];
                        if (RenderCaretRowOnly)
                        {
                            if (r == Control.Caret.CurrentRow)
                            {
                                RenderRow(Control.Document.IndexOf(r), i);
                            }
                            //Control.Caret.CurrentRow.expansion_EndSpan.StartRow.Index
                            if (Control.Caret.CurrentRow.expansion_EndSpan != null && Control.Caret.CurrentRow.expansion_EndSpan.StartRow != null && Control.Caret.CurrentRow.expansion_EndSpan.StartRow == r)
                            {
                                RenderRow(Control.Document.IndexOf(r), i);
                            }
                        }
                        else
                        {
                            RenderRow(Control.Document.IndexOf(r), i);
                        }
                    }
                    else
                    {
                        if (RenderCaretRowOnly) {}
                        else
                        {
                            RenderRow(Control.Document.Count, i);
                        }
                    }
                    j++;
                }
            }
            catch
            {
            }
        }

        private void RenderRow(int RowIndex, int RowPos)
        {
            if (RowIndex >= 0 && RowIndex < Control.Document.Count)
            {
                //do keyword parse before we render the line...
                if (Control.Document[RowIndex].RowState == RowState.SpanParsed)
                {
                    Control.Document.Parser.ParseRow(RowIndex, true);
                    Control.Document[RowIndex].RowState = RowState.AllParsed;
                }
            }


            try
            {
                GDISurface bbuff = GFX.BackBuffer;
                bool found = false;


                GDIBrush bg = GFX.BackgroundBrush;

                try
                {
                    if (RowIndex < Control.Document.Count && RowIndex >= 0)
                    {
                        Row r = Control.Document[RowIndex];
                        if (SpanFound && RowIndex >= FirstSpanRow && RowIndex <= LastSpanRow && Control._SyntaxBox.ScopeBackColor != Color.Transparent)
                        {
                            bg = new GDIBrush(Control._SyntaxBox.ScopeBackColor);
                            found = true;
                        }
                        else if (r.BackColor != Color.Transparent)
                        {
                            bg = new GDIBrush(r.BackColor);
                            found = true;
                        }
                        else
                        {
                            if (r.endSpan != null)
                            {
                                Span tmp = r.expansion_EndSpan;
                                while (tmp != null)
                                {
                                    if (tmp.spanDefinition.Transparent == false)
                                    {
                                        bg = new GDIBrush(tmp.spanDefinition.BackColor);
                                        found = true;
                                        break;
                                    }
                                    tmp = tmp.Parent;
                                }


                                if (!found)
                                {
                                    tmp = r.endSpan;
                                    while (tmp != null)
                                    {
                                        if (tmp.spanDefinition.Transparent == false)
                                        {
                                            bg = new GDIBrush(tmp.spanDefinition.BackColor);
                                            found = true;
                                            break;
                                        }
                                        tmp = tmp.Parent;
                                    }
                                }
                                if (!found)
                                {
                                    tmp = r.expansion_EndSpan;
                                    while (tmp != null)
                                    {
                                        if (tmp.spanDefinition.Transparent == false)
                                        {
                                            bg = new GDIBrush(tmp.spanDefinition.BackColor);
                                            found = true;
                                            break;
                                        }
                                        tmp = tmp.Parent;
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }

                if (RowIndex == Control.Caret.Position.Y && Control.HighLightActiveLine)
                    bbuff.Clear(GFX.HighLightLineBrush);

                else if (RowIndex >= 0 && RowIndex < Control.Document.Count)
                {
                    if (Control.Document[RowIndex].IsCollapsed)
                    {
                        if (Control.Document[RowIndex].Expansion_EndRow.Index == Control.Caret.Position.Y && Control.HighLightActiveLine)
                            bbuff.Clear(GFX.HighLightLineBrush);
                        else
                            bbuff.Clear(bg);
                    }
                    else
                        bbuff.Clear(bg);
                }
                else
                    bbuff.Clear(bg);


                //only render normal text if any part of the row is visible
                if (RowIndex <= Control.Selection.LogicalBounds.FirstRow || RowIndex >= Control.Selection.LogicalBounds.LastRow)
                {
                    RenderText(RowIndex);
                }

                //only render selection text if the line is selected
                if (Control.Selection.IsValid)
                {
                    if (RowIndex >= Control.Selection.LogicalBounds.FirstRow && RowIndex <= Control.Selection.LogicalBounds.LastRow)
                    {
                        if (Control.ContainsFocus)
                            GFX.SelectionBuffer.Clear(Control.SelectionBackColor);
                        else
                            GFX.SelectionBuffer.Clear(Control.InactiveSelectionBackColor);

                        RenderSelectedText(RowIndex);
                    }
                }


                if (Control.ContainsFocus || Control.View.Action == EditAction.DragText)
                {
                    RenderCaret(RowIndex, RowPos * Control.View.RowHeight + yOffset);
                }

                RenderSelection(RowIndex);
                RenderMargin(RowIndex);
                if (Control.Document.Folding)
                    RenderExpansion(RowIndex);

                var e = new RowPaintEventArgs();

                var rec = new Rectangle(0, 0, Control.Width, Control.View.RowHeight);
                e.Graphics = Graphics.FromHdc(bbuff.hDC);
                e.Bounds = rec;
                e.Row = null;
                if (RowIndex >= 0 && RowIndex < Control.Document.Count)
                    e.Row = Control.Document[RowIndex];

                Control._SyntaxBox.OnRenderRow(e);


                bbuff.Flush();
                bbuff.RenderToControl(0, RowPos * Control.View.RowHeight + yOffset);

                //GFX.SelectionBuffer.RenderToControl (0,RowPos*Control.View.RowHeight+this.yOffset);


                if (found)
                    bg.Dispose();
            }
            catch
            {
            }
        }

        private void SetFont(bool bold, bool italic, bool underline, GDISurface surface)
        {
            if (bold && italic && underline)
                surface.Font = GFX.FontBoldItalicUnderline;

            else if (bold && italic)
                surface.Font = GFX.FontBoldItalic;

            else if (bold && underline)
                surface.Font = GFX.FontBoldUnderline;

            else if (bold)
                surface.Font = GFX.FontBold;

            else if (italic && underline)
                surface.Font = GFX.FontItalicUnderline;

            else if (!italic && underline)
                surface.Font = GFX.FontUnderline;

            else if (italic)
                surface.Font = GFX.FontItalic;

            else if (true)
                surface.Font = GFX.FontNormal;
        }

        private void SetStringFont(bool bold, bool italic, bool underline)
        {
            SetFont(bold, italic, underline, GFX.StringBuffer);            
        }

        private void RenderCollapsedSelectedText(int RowIndex, int xPos)
        {
            GDISurface bbuff = GFX.SelectionBuffer;
            bbuff.Font = GFX.FontBold;
            bbuff.FontTransparent = true;

            bbuff.TextForeColor = Control.ContainsFocus ? Control.SelectionForeColor : Control.InactiveSelectionForeColor;

            //bbuff.TextForeColor =Color.DarkBlue;
            Row r = Control.Document[RowIndex];
            string str = r.CollapsedText;

            xPos++;
            int taborig = -Control.View.FirstVisibleColumn*Control.View.CharWidth + Control.View.TextMargin;
            GFX.StringBuffer.Font = GFX.FontBold;
            int wdh = GFX.StringBuffer.DrawTabbedString(str, xPos + 1, 0, taborig, Control.PixelTabSize).Width;

            if (Control.ContainsFocus)
            {
                bbuff.FillRect(Control.SelectionForeColor, xPos + 0, 0, wdh + 2, Control.View.RowHeight);
                bbuff.FillRect(Control.SelectionBackColor, xPos + 1, 1, wdh, Control.View.RowHeight - 2);
            }
            else
            {
                bbuff.FillRect(Control.InactiveSelectionForeColor, xPos + 0, 0, wdh + 2, Control.View.RowHeight);
                bbuff.FillRect(Control.InactiveSelectionBackColor, xPos + 1, 1, wdh, Control.View.RowHeight - 2);
            }


            wdh = bbuff.DrawTabbedString(str, xPos + 1, 0, taborig, Control.PixelTabSize).Width;


            //this can crash if document not fully parsed , on error resume next
            try
            {
                if (r.expansion_StartSpan.EndRow != null)
                {
                    if (r.expansion_StartSpan.EndRow.RowState == RowState.SpanParsed)
                        Control.Document.Parser.ParseRow(r.expansion_StartSpan.EndRow.Index, true);

                    Word last = r.expansion_StartSpan.EndWord;
                    xPos += Control.View.FirstVisibleColumn*Control.View.CharWidth;
                    r.expansion_StartSpan.EndRow.Expansion_PixelStart = xPos + wdh - Control.View.TextMargin + 2;
                    r.Expansion_PixelEnd = xPos - 1;
                    RenderSelectedText(Control.Document.IndexOf(r.expansion_StartSpan.EndRow), r.expansion_StartSpan.EndRow.Expansion_PixelStart, last);
                }
            }
            catch {}
        }

        private void RenderCollapsedText(int RowIndex, int xPos)
        {
            GDISurface bbuff = GFX.BackBuffer;
            bbuff.Font = GFX.FontBold;
            bbuff.FontTransparent = true;

            bbuff.TextForeColor = Control.OutlineColor;

            Row r = Control.Document[RowIndex];
            string str = r.CollapsedText;

            xPos++;
            int taborig = -Control.View.FirstVisibleColumn*Control.View.CharWidth + Control.View.TextMargin;
            GFX.StringBuffer.Font = GFX.FontBold;
            int wdh = GFX.StringBuffer.DrawTabbedString(str, xPos + 1, 0, taborig, Control.PixelTabSize).Width;
            bbuff.FillRect(GFX.OutlineBrush, xPos + 0, 0, wdh + 2, Control.View.RowHeight);
            bbuff.FillRect(GFX.BackgroundBrush, xPos + 1, 1, wdh, Control.View.RowHeight - 2);
            wdh = bbuff.DrawTabbedString(str, xPos + 1, 0, taborig, Control.PixelTabSize).Width;


            //this can crash if document not fully parsed , on error resume next
            try
            {
                if (r.expansion_StartSpan.EndRow != null)
                {
                    if (r.expansion_StartSpan.EndRow.RowState == RowState.SpanParsed)
                        Control.Document.Parser.ParseRow(r.expansion_StartSpan.EndRow.Index, true);

                    Word last = r.expansion_StartSpan.EndWord;
                    xPos += Control.View.FirstVisibleColumn*Control.View.CharWidth;
                    r.expansion_StartSpan.EndRow.Expansion_PixelStart = xPos + wdh - Control.View.TextMargin + 2;
                    r.Expansion_PixelEnd = xPos - 1;
                    RenderText(Control.Document.IndexOf(r.expansion_StartSpan.EndRow), r.expansion_StartSpan.EndRow.Expansion_PixelStart, last);
                }
            }
            catch {}
        }

        private void RenderText(int RowIndex)
        {
            RenderText(RowIndex, 0, null);
        }

        private void RenderText(int RowIndex, int XOffset, Word StartWord)
        {
            GDISurface bbuff = GFX.BackBuffer;
            bbuff.Font = GFX.FontNormal;
            bbuff.FontTransparent = true;
            bool DrawBreakpoint = false;
            if (RowIndex <= Control.Document.Count - 1)
            {
                bbuff.TextForeColor = Color.Black;
                Row xtr = Control.Document[RowIndex];

                //if (xtr.startSpan != null)
                //	bbuff.DrawTabbedString (xtr.startSpan.GetHashCode ().ToString (System.Globalization.CultureInfo.InvariantCulture),100,0,0,0);

                //bbuff.TextForeColor = Color.Black;
                //bbuff.DrawTabbedString (xtr.Text,(int)(Control.View.TextMargin -Control.View.ClientAreaStart),1,-Control.View.FirstVisibleColumn*Control.View.CharWidth+Control.View.TextMargin,Control.PixelTabSize);					

                int xpos = Control.View.TextMargin - Control.View.ClientAreaStart + XOffset;
                int wdh = 0;
                int taborig = -Control.View.FirstVisibleColumn*Control.View.CharWidth + Control.View.TextMargin;


                bool ws = Control.ShowWhitespace;
                bool StartDraw = false;
                if (StartWord == null)
                    StartDraw = true;
                xtr.Expansion_StartChar = 0;
                xtr.Expansion_EndChar = 0;
                bool HasExpansion = false;
                foreach (Word w in xtr.FormattedWords)
                {
                    if (StartDraw)
                    {
                        if (w.Span == xtr.expansion_StartSpan && xtr.expansion_StartSpan != null)
                            if (xtr.expansion_StartSpan.Expanded == false)
                            {
                                RenderCollapsedText(RowIndex, xpos);
                                HasExpansion = true;
                                break;
                            }

                        if ((w.Type == WordType.Space || w.Type == WordType.Tab) && !DrawBreakpoint && Control.ShowTabGuides)
                        {
                            int xtab = xpos - (Control.View.TextMargin - Control.View.ClientAreaStart + XOffset);
                            if ((xtab/(double) Control.PixelTabSize) == (xtab/Control.PixelTabSize))
                                bbuff.FillRect(Control.TabGuideColor, xpos, 0, 1, Control.View.RowHeight);
                        }

                        if (w.Type == WordType.Word || ws == false)
                        {
                            if (w.Style != null)
                            {
                                SetFont(w.Style.Bold, w.Style.Italic, w.Style.Underline, bbuff);
                                bbuff.TextBackColor = w.Style.BackColor;
                                bbuff.TextForeColor = w.Style.ForeColor;
                                bbuff.FontTransparent = w.Style.Transparent;
                            }
                            else
                            {
                                bbuff.Font = GFX.FontNormal;
                                bbuff.TextForeColor = Color.Black;
                                bbuff.FontTransparent = true;
                            }


                            if (w.Type == WordType.Word)
                                DrawBreakpoint = true;

                            if (xtr.Breakpoint && DrawBreakpoint)
                            {
                                bbuff.TextForeColor = Control.BreakPointForeColor;
                                bbuff.TextBackColor = Control.BreakPointBackColor;
                                bbuff.FontTransparent = false;
                            }


                            if (Control.BracketMatching && (w == BracketEnd || w == BracketStart))
                            {
                                bbuff.TextForeColor = Control.BracketForeColor;

                                if (Control.BracketBackColor != Color.Transparent)
                                {
                                    bbuff.TextBackColor = Control.BracketBackColor;
                                    bbuff.FontTransparent = false;
                                }

                                wdh = bbuff.DrawTabbedString(w.Text, xpos, 0, taborig, Control.PixelTabSize).Width;
                                if (Control.BracketBorderColor != Color.Transparent)
                                {
                                    bbuff.DrawRect(Control.BracketBorderColor, xpos - 1, 0, wdh, Control.View.RowHeight - 1);
                                }
                            }
                            else
                            {
                                wdh = bbuff.DrawTabbedString(w.Text, xpos, 0, taborig, Control.PixelTabSize).Width;
                            }


                            //render errors
                            if (w.HasError)
                            {
                                //bbuff.FillRect (Color.Red,xpos,Control.View.RowHeight-2,wdh,2);
                                int ey = Control.View.RowHeight - 1;
                                Color c = w.ErrorColor;
                                for (int x = 0; x < wdh + 3; x += 4)
                                {
                                    bbuff.DrawLine(c, new Point(xpos + x, ey), new Point(xpos + x + 2, ey - 2));
                                    bbuff.DrawLine(c, new Point(xpos + x + 2, ey - 2), new Point(xpos + x + 4, ey));
                                }
                            }
                        }
                        else if (w.Type == WordType.Space)
                        {
                            bbuff.Font = GFX.FontNormal;
                            bbuff.TextForeColor = Control.WhitespaceColor;
                            bbuff.FontTransparent = true;

                            if (xtr.Breakpoint && DrawBreakpoint)
                            {
                                bbuff.TextForeColor = Control.BreakPointForeColor;
                                bbuff.TextBackColor = Control.BreakPointBackColor;
                                bbuff.FontTransparent = false;
                            }

                            bbuff.DrawTabbedString("·", xpos, 0, taborig, Control.PixelTabSize);
                            wdh = bbuff.DrawTabbedString(w.Text, xpos, 0, taborig, Control.PixelTabSize).Width;
                        }
                        else if (w.Type == WordType.Tab)
                        {
                            bbuff.Font = GFX.FontNormal;
                            bbuff.TextForeColor = Control.WhitespaceColor;
                            bbuff.FontTransparent = true;

                            if (xtr.Breakpoint && DrawBreakpoint)
                            {
                                bbuff.TextForeColor = Control.BreakPointForeColor;
                                bbuff.TextBackColor = Control.BreakPointBackColor;
                                bbuff.FontTransparent = false;
                            }

                            bbuff.DrawTabbedString("»", xpos, 0, taborig, Control.PixelTabSize);
                            wdh = bbuff.DrawTabbedString(w.Text, xpos, 0, taborig, Control.PixelTabSize).Width;
                        }
                        if (w.Pattern != null)
                            if (w.Pattern.IsSeparator)
                            {
                                bbuff.FillRect(Control.SeparatorColor, Control.View.TextMargin - 4, Control.View.RowHeight - 1, Control.View.ClientAreaWidth, 1);
                            }

                        xpos += wdh;
                    }


                    if (!StartDraw)
                        xtr.Expansion_StartChar += w.Text.Length;

                    if (w == StartWord)
                        StartDraw = true;

                    xtr.Expansion_EndChar += w.Text.Length;
                }


                if (xtr.IsCollapsed) {}
                else if (xtr.endSpan != null && xtr.endSpan.spanDefinition != null && xtr.endSpan.spanDefinition.Style != null)
                {
                    bbuff.FillRect(xtr.endSpan.spanDefinition.Style.BackColor, xpos, 0, Control.Width - xpos, Control.View.RowHeight);
                }

                if (Control._SyntaxBox.ShowEOLMarker && !HasExpansion)
                {
                    bbuff.Font = GFX.FontNormal;
                    bbuff.TextForeColor = Control._SyntaxBox.EOLMarkerColor;
                    bbuff.FontTransparent = true;
                    bbuff.DrawTabbedString("¶", xpos, 0, taborig, Control.PixelTabSize);
                }
            }
        }


        private void RenderSelectedText(int RowIndex)
        {
            RenderSelectedText(RowIndex, 0, null);
        }

        private void RenderSelectedText(int RowIndex, int XOffset, Word StartWord)
        {
            GDISurface bbuff = GFX.SelectionBuffer;
            bbuff.Font = GFX.FontNormal;
            bbuff.FontTransparent = true;
            if (RowIndex <= Control.Document.Count - 1)
            {
                bbuff.TextForeColor = Control.ContainsFocus ? Control.SelectionForeColor : Control.InactiveSelectionForeColor;

                Row xtr = Control.Document[RowIndex];

                //if (xtr.startSpan != null)
                //	bbuff.DrawTabbedString (xtr.startSpan.GetHashCode ().ToString (System.Globalization.CultureInfo.InvariantCulture),100,0,0,0);

                //bbuff.TextForeColor = Color.Black;
                //bbuff.DrawTabbedString (xtr.Text,(int)(Control.View.TextMargin -Control.View.ClientAreaStart),1,-Control.View.FirstVisibleColumn*Control.View.CharWidth+Control.View.TextMargin,Control.PixelTabSize);					

                int xpos = Control.View.TextMargin - Control.View.ClientAreaStart + XOffset;
                int wdh = 0;
                int taborig = -Control.View.FirstVisibleColumn*Control.View.CharWidth + Control.View.TextMargin;


                bool ws = Control.ShowWhitespace;
                bool StartDraw = false;
                if (StartWord == null)
                    StartDraw = true;
                xtr.Expansion_StartChar = 0;
                xtr.Expansion_EndChar = 0;
                bool HasExpansion = false;
                foreach (Word w in xtr.FormattedWords)
                {
                    if (StartDraw)
                    {
                        if (w.Span == xtr.expansion_StartSpan && xtr.expansion_StartSpan != null)
                            if (xtr.expansion_StartSpan.Expanded == false)
                            {
                                RenderCollapsedSelectedText(RowIndex, xpos);
                                HasExpansion = true;
                                break;
                            }


                        if (w.Type == WordType.Word || ws == false)
                        {
                            if (w.Style != null)
                            {
                                SetFont(w.Style.Bold, w.Style.Italic, w.Style.Underline, bbuff);
                            }
                            else
                            {
                                bbuff.Font = GFX.FontNormal;
                            }

                            wdh = bbuff.DrawTabbedString(w.Text, xpos, 0, taborig, Control.PixelTabSize).Width;

                            //render errors
                            if (w.HasError)
                            {
                                //bbuff.FillRect (Color.Red,xpos,Control.View.RowHeight-2,wdh,2);
                                int ey = Control.View.RowHeight - 1;
                                Color c = w.ErrorColor;
                                for (int x = 0; x < wdh + 3; x += 4)
                                {
                                    bbuff.DrawLine(c, new Point(xpos + x, ey), new Point(xpos + x + 2, ey - 2));
                                    bbuff.DrawLine(c, new Point(xpos + x + 2, ey - 2), new Point(xpos + x + 4, ey));
                                }
                            }
                        }
                        else if (w.Type == WordType.Space)
                        {
                            bbuff.Font = GFX.FontNormal;
                            bbuff.DrawTabbedString("·", xpos, 0, taborig, Control.PixelTabSize);
                            wdh = bbuff.DrawTabbedString(w.Text, xpos, 0, taborig, Control.PixelTabSize).Width;
                        }
                        else if (w.Type == WordType.Tab)
                        {
                            bbuff.Font = GFX.FontNormal;
                            bbuff.DrawTabbedString("»", xpos, 0, taborig, Control.PixelTabSize);
                            wdh = bbuff.DrawTabbedString(w.Text, xpos, 0, taborig, Control.PixelTabSize).Width;
                        }
                        if (w.Pattern != null)
                            if (w.Pattern.IsSeparator)
                            {
                                bbuff.FillRect(Control.SeparatorColor, Control.View.TextMargin - 4, Control.View.RowHeight - 1, Control.View.ClientAreaWidth, 1);
                            }

                        xpos += wdh;
                    }


                    if (!StartDraw)
                        xtr.Expansion_StartChar += w.Text.Length;

                    if (w == StartWord)
                        StartDraw = true;

                    xtr.Expansion_EndChar += w.Text.Length;
                }

                if (xtr.IsCollapsed) {}
                else if (xtr.endSpan != null && xtr.endSpan.spanDefinition != null && xtr.endSpan.spanDefinition.Style != null)
                {
                    GFX.BackBuffer.FillRect(xtr.endSpan.spanDefinition.Style.BackColor, xpos, 0, Control.Width - xpos, Control.View.RowHeight);
                }

                if (Control._SyntaxBox.ShowEOLMarker && !HasExpansion)
                {
                    bbuff.Font = GFX.FontNormal;
                    bbuff.TextForeColor = Control.SelectionForeColor;
                    bbuff.FontTransparent = true;
                    bbuff.DrawTabbedString("¶", xpos, 0, taborig, Control.PixelTabSize);
                }
            }
        }

        private void RenderCaret(int RowIndex, int ypos)
        {
            int StartRow = -1;
            int cr = Control.Caret.Position.Y;

            if (cr >= 0 && cr <= Control.Document.Count - 1)
            {
                Row r = Control.Document[cr];
                if (r.expansion_EndSpan != null)
                {
                    if (r.expansion_EndSpan.Expanded == false)
                    {
                        r = r.expansion_EndSpan.StartRow;
                        StartRow = r.Index;
                    }
                }
            }

            bool Collapsed = (RowIndex == StartRow);


            if (RowIndex != cr && RowIndex != StartRow)
                return;

            if (Control.View.Action == EditAction.DragText)
            {
                //drop Control.Caret
                Row xtr = Control.Document[cr];

                int pos = MeasureRow(xtr, Control.Caret.Position.X).Width + 1;

                if (Collapsed)
                {
                    pos += xtr.Expansion_PixelStart;
                    pos -= MeasureRow(xtr, xtr.Expansion_StartChar).Width;
                }

                GFX.BackBuffer.InvertRect(pos + Control.View.TextMargin - Control.View.ClientAreaStart - 1, 0, 3, Control.View.RowHeight);
                GFX.BackBuffer.InvertRect(pos + Control.View.TextMargin - Control.View.ClientAreaStart, 1, 1, Control.View.RowHeight - 2);
            }
            else
            {
                //normal Control.Caret

                Row xtr = Control.Document[cr];
                if (!Control.OverWrite)
                {
                    int pos = Control.View.TextMargin - Control.View.ClientAreaStart;
                    pos += MeasureRow(xtr, Control.Caret.Position.X).Width + 1;
                    if (Collapsed)
                    {
                        pos += xtr.Expansion_PixelStart;
                        pos -= MeasureRow(xtr, xtr.Expansion_StartChar).Width;
                    }

                    int wdh = Control.View.CharWidth/12 + 1;
                    if (wdh < 2)
                        wdh = 2;

                    if (Control.Caret.Blink)
                    {
                        GFX.BackBuffer.InvertRect(pos, 0, wdh, Control.View.RowHeight);
                    }

                    if (Control.IMEWindow == null)
                    {
                        Control.IMEWindow = new IMEWindow(Control.Handle, Control.FontName, Control.FontSize);
                        InitIMEWindow();
                    }
                    Control.IMEWindow.Loation = new Point(pos, ypos);
                }
                else
                {
                    int pos1 = MeasureRow(xtr, Control.Caret.Position.X).Width;
                    int pos2 = MeasureRow(xtr, Control.Caret.Position.X + 1).Width;
                    int wdh = pos2 - pos1;
                    if (Collapsed)
                    {
                        pos1 += xtr.Expansion_PixelStart;
                        pos1 -= MeasureRow(xtr, xtr.Expansion_StartChar).Width;
                    }

                    int pos = pos1 + Control.View.TextMargin - Control.View.ClientAreaStart;
                    if (Control.Caret.Blink)
                    {
                        GFX.BackBuffer.InvertRect(pos, 0, wdh, Control.View.RowHeight);
                    }
                    Control.IMEWindow.Loation = new Point(pos, ypos);
                }
            }
        }

        private void RenderMargin(int RowIndex)
        {
            GDISurface bbuff = GFX.BackBuffer;

            if (Control.ShowGutterMargin)
            {
                bbuff.FillRect(GFX.GutterMarginBrush, 0, 0, Control.View.GutterMarginWidth, Control.View.RowHeight);
                bbuff.FillRect(GFX.GutterMarginBorderBrush, Control.View.GutterMarginWidth - 1, 0, 1, Control.View.RowHeight);
                if (RowIndex <= Control.Document.Count - 1)
                {
                    Row r = Control.Document[RowIndex];

                    if (Control.View.RowHeight >= Control._SyntaxBox.GutterIcons.ImageSize.Height)
                    {
                        if (r.Bookmarked)
                            Control._SyntaxBox.GutterIcons.Draw(Graphics.FromHdc(bbuff.hDC), 0, 0, 1);
                        if (r.Breakpoint)
                            Control._SyntaxBox.GutterIcons.Draw(Graphics.FromHdc(bbuff.hDC), 0, 0, 0);
                    }
                    else
                    {
                        int w = Control.View.RowHeight;
                        if (r.Bookmarked)
                            Control._SyntaxBox.GutterIcons.Draw(Graphics.FromHdc(bbuff.hDC), 0, 0, w, w, 1);
                        if (r.Breakpoint)
                            Control._SyntaxBox.GutterIcons.Draw(Graphics.FromHdc(bbuff.hDC), 0, 0, w, w, 0);
                    }

                    if (r.Images != null)
                    {
                        foreach (int i in r.Images)
                        {
                            if (Control.View.RowHeight >= Control._SyntaxBox.GutterIcons.ImageSize.Height)
                            {
                                Control._SyntaxBox.GutterIcons.Draw(Graphics.FromHdc(bbuff.hDC), 0, 0, i);
                            }
                            else
                            {
                                int w = Control.View.RowHeight;
                                Control._SyntaxBox.GutterIcons.Draw(Graphics.FromHdc(bbuff.hDC), 0, 0, w, w, i);
                            }
                        }
                    }
                }
            }


            if (Control.ShowLineNumbers)
            {
                bbuff.FillRect(GFX.LineNumberMarginBrush, Control.View.GutterMarginWidth, 0, Control.View.LineNumberMarginWidth + 1, Control.View.RowHeight);

                //bbuff.FillRect (GFX.LineNumberMarginBrush  ,Control.View.GutterMarginWidth+Control.View.LineNumberMarginWidth,0,1,Control.View.RowHeight);

                for (int j = 0; j < Control.View.RowHeight; j += 2)
                {
                    bbuff.FillRect(GFX.LineNumberMarginBorderBrush, Control.View.GutterMarginWidth + Control.View.LineNumberMarginWidth, j, 1, 1);
                }
            }


            if (!Control.ShowLineNumbers || !Control.ShowGutterMargin)
                bbuff.FillRect(GFX.BackgroundBrush, Control.View.TotalMarginWidth, 0, Control.View.TextMargin - Control.View.TotalMarginWidth - 3, Control.View.RowHeight);
            else
                bbuff.FillRect(GFX.BackgroundBrush, Control.View.TotalMarginWidth + 1, 0, Control.View.TextMargin - Control.View.TotalMarginWidth - 4, Control.View.RowHeight);

            if (Control.ShowLineNumbers)
            {
                bbuff.Font = GFX.FontNormal;
                bbuff.FontTransparent = true;

                bbuff.TextForeColor = Control.LineNumberForeColor;
                if (RowIndex <= Control.Document.Count - 1)
                {
                    int nw = MeasureString((RowIndex + 1).ToString(CultureInfo.InvariantCulture)).Width;

                    bbuff.DrawTabbedString((RowIndex + 1).ToString(CultureInfo.InvariantCulture), Control.View.GutterMarginWidth + Control.View.LineNumberMarginWidth - nw - 1, 1, 0, Control.PixelTabSize);
                }
            }
        }

        private void RenderExpansion(int RowIndex)
        {
            if (Control == null)
                throw new NullReferenceException("Control may not be null");

            if (RowIndex <= Control.Document.Count - 1)
            {
                const int yo = 4;
                Row xtr = Control.Document[RowIndex];
                GDISurface bbuff = GFX.BackBuffer;
                if (xtr.endSpan != null)
                {
                    if (xtr.expansion_StartSpan != null && xtr.startSpan.Parent == null)
                    {
                        if (!xtr.IsCollapsed)
                        {
                            bbuff.FillRect(GFX.OutlineBrush, Control.View.TotalMarginWidth + 6, yo, 1, Control.View.RowHeight - yo);
                        }
                    }
                    else if ((xtr.endSpan.Parent != null || xtr.expansion_EndSpan != null))
                    {
                        bbuff.FillRect(GFX.OutlineBrush, Control.View.TotalMarginWidth + 6, 0, 1, Control.View.RowHeight);
                    }

                    if (xtr.expansion_StartSpan != null)
                    {
                        bbuff.FillRect(GFX.OutlineBrush, Control.View.TotalMarginWidth + 2, yo, 9, 9);
                        bbuff.FillRect(GFX.BackgroundBrush, Control.View.TotalMarginWidth + 3, yo + 1, 7, 7);
                        //render plus / minus
                        bbuff.FillRect(GFX.OutlineBrush, Control.View.TotalMarginWidth + 4, yo + 4, 5, 1);
                        if (!xtr.expansion_StartSpan.Expanded)
                            bbuff.FillRect(GFX.OutlineBrush, Control.View.TotalMarginWidth + 6, yo + 2, 1, 5);
                    }
                    if (xtr.expansion_EndSpan != null)
                    {
                        bbuff.FillRect(GFX.OutlineBrush, Control.View.TotalMarginWidth + 7, Control.View.RowHeight - 1, 5, 1);
                    }
                }

                //				//RENDER SPAN LINES
                //				if (SpanFound)
                //				{
                //					if (RowIndex==FirstSpanRow)
                //						bbuff.FillRect (GFX.OutlineBrush,this.Control.View.TotalMarginWidth +14,0,Control.ClientWidth ,1);
                //
                //					if (RowIndex==LastSpanRow)
                //						bbuff.FillRect (GFX.OutlineBrush,this.Control.View.TotalMarginWidth +14,Control.View.RowHeight-1,Control.ClientWidth,1);				
                //				}

                //RENDER SPAN MARGIN
                if (SpanFound && Control._SyntaxBox.ScopeIndicatorColor != Color.Transparent && Control._SyntaxBox.ShowScopeIndicator)
                {
                    if (RowIndex >= FirstSpanRow && RowIndex <= LastSpanRow)
                        bbuff.FillRect(Control._SyntaxBox.ScopeIndicatorColor, Control.View.TotalMarginWidth + 14, 0, 2, Control.View.RowHeight);

                    if (RowIndex == FirstSpanRow)
                        bbuff.FillRect(Control._SyntaxBox.ScopeIndicatorColor, Control.View.TotalMarginWidth + 14, 0, 4, 2);

                    if (RowIndex == LastSpanRow)
                        bbuff.FillRect(Control._SyntaxBox.ScopeIndicatorColor, Control.View.TotalMarginWidth + 14, Control.View.RowHeight - 2, 4, 2);
                }
            }
        }


        //draws aControl.Selection.LogicalBounds row in the backbuffer
        private void RenderSelection(int RowIndex)
        {
            if (RowIndex <= Control.Document.Count - 1 && Control.Selection.IsValid)
            {
                Row xtr = Control.Document[RowIndex];
                if (!xtr.IsCollapsed)
                {
                    if ((RowIndex > Control.Selection.LogicalBounds.FirstRow) && (RowIndex < Control.Selection.LogicalBounds.LastRow))
                    {
                        int width = MeasureRow(xtr, xtr.Text.Length).Width + MeasureString("¶").Width + 3;
                        RenderBox(Control.View.TextMargin, 0, Math.Max(width - Control.View.ClientAreaStart, 0), Control.View.RowHeight);
                    }
                    else if ((RowIndex == Control.Selection.LogicalBounds.FirstRow) && (RowIndex == Control.Selection.LogicalBounds.LastRow))
                    {
                        int start = MeasureRow(xtr, Math.Min(xtr.Text.Length, Control.Selection.LogicalBounds.FirstColumn)).Width;
                        int width = MeasureRow(xtr, Math.Min(xtr.Text.Length, Control.Selection.LogicalBounds.LastColumn)).Width - start;
                        RenderBox(Control.View.TextMargin + start - Control.View.ClientAreaStart, 0, width, Control.View.RowHeight);
                    }
                    else if (RowIndex == Control.Selection.LogicalBounds.LastRow)
                    {
                        int width = MeasureRow(xtr, Math.Min(xtr.Text.Length, Control.Selection.LogicalBounds.LastColumn)).Width;
                        RenderBox(Control.View.TextMargin, 0, Math.Max(width - Control.View.ClientAreaStart, 0), Control.View.RowHeight);
                    }
                    else if (RowIndex == Control.Selection.LogicalBounds.FirstRow)
                    {
                        int start = MeasureRow(xtr, Math.Min(xtr.Text.Length, Control.Selection.LogicalBounds.FirstColumn)).Width;
                        int width = MeasureRow(xtr, xtr.Text.Length).Width + MeasureString("¶").Width + 3 - start;
                        RenderBox(Control.View.TextMargin + start - Control.View.ClientAreaStart, 0, width, Control.View.RowHeight);
                    }
                }
                else
                {
                    RenderCollapsedSelection(RowIndex);
                }
            }
        }

        private void RenderCollapsedSelection(int RowIndex)
        {
            Row xtr = Control.Document[RowIndex];
            if ((RowIndex > Control.Selection.LogicalBounds.FirstRow) && (RowIndex < Control.Selection.LogicalBounds.LastRow))
            {
                int width = MeasureRow(xtr, xtr.Expansion_EndChar).Width;
                RenderBox(Control.View.TextMargin, 0, Math.Max(width - Control.View.ClientAreaStart, 0), Control.View.RowHeight);
            }
            else if ((RowIndex == Control.Selection.LogicalBounds.FirstRow) && (RowIndex == Control.Selection.LogicalBounds.LastRow))
            {
                int start = MeasureRow(xtr, Math.Min(xtr.Text.Length, Control.Selection.LogicalBounds.FirstColumn)).Width;
                int min = Math.Min(xtr.Text.Length, Control.Selection.LogicalBounds.LastColumn);
                min = Math.Min(min, xtr.Expansion_EndChar);
                int width = MeasureRow(xtr, min).Width - start;
                RenderBox(Control.View.TextMargin + start - Control.View.ClientAreaStart, 0, width, Control.View.RowHeight);
            }
            else if (RowIndex == Control.Selection.LogicalBounds.LastRow)
            {
                int width = MeasureRow(xtr, Math.Min(xtr.Text.Length, Control.Selection.LogicalBounds.LastColumn)).Width;
                RenderBox(Control.View.TextMargin, 0, Math.Max(width - Control.View.ClientAreaStart, 0), Control.View.RowHeight);
            }
            else if (RowIndex == Control.Selection.LogicalBounds.FirstRow)
            {
                int start = MeasureRow(xtr, Math.Min(xtr.Text.Length, Control.Selection.LogicalBounds.FirstColumn)).Width;
                int width = MeasureRow(xtr, Math.Min(xtr.Text.Length, xtr.Expansion_EndChar)).Width - start;
                RenderBox(Control.View.TextMargin + start - Control.View.ClientAreaStart, 0, width, Control.View.RowHeight);
            }

            if (Control.Selection.LogicalBounds.LastRow > RowIndex && Control.Selection.LogicalBounds.FirstRow <= RowIndex)
            {
                int start = xtr.Expansion_PixelEnd;
                int end = xtr.Expansion_EndRow.Expansion_PixelStart - start + Control.View.TextMargin;
                //start+=100;
                //end=200;
                RenderBox(start - Control.View.ClientAreaStart, 0, end, Control.View.RowHeight);
            }

            RowIndex = xtr.Expansion_EndRow.Index;
            xtr = xtr.Expansion_EndRow;

            if (Control.Selection.LogicalBounds.FirstRow <= RowIndex && Control.Selection.LogicalBounds.LastRow >= RowIndex)
            {
                int endchar = Control.Selection.LogicalBounds.LastRow != RowIndex ? xtr.Text.Length : Math.Min(Control.Selection.LogicalBounds.LastColumn, xtr.Text.Length);

                int end = MeasureRow(xtr, endchar).Width;
                end += xtr.Expansion_PixelStart;
                end -= MeasureRow(xtr, xtr.Expansion_StartChar).Width;

                int start;

                if (Control.Selection.LogicalBounds.FirstRow == RowIndex)
                {
                    int startchar = Math.Max(Control.Selection.LogicalBounds.FirstColumn, xtr.Expansion_StartChar);
                    start = MeasureRow(xtr, startchar).Width;
                    start += xtr.Expansion_PixelStart;
                    start -= MeasureRow(xtr, xtr.Expansion_StartChar).Width;
                }
                else
                {
                    start = MeasureRow(xtr, xtr.Expansion_StartChar).Width;
                    start += xtr.Expansion_PixelStart;
                    start -= MeasureRow(xtr, xtr.Expansion_StartChar).Width;
                }

                end -= start;

                if (Control.Selection.LogicalBounds.LastRow != RowIndex)
                    end += 6;


                RenderBox(Control.View.TextMargin + start - Control.View.ClientAreaStart, 0, end, Control.View.RowHeight);
            }
        }

        private void RenderBox(int x, int y, int width, int height)
        {
            GFX.SelectionBuffer.RenderTo(GFX.BackBuffer, x, y, width, height, x, y);
        }

        private TextPoint ColumnFromPixel(int RowIndex, int X)
        {
            Row xtr = Control.Document[RowIndex];
            X -= Control.View.TextMargin - 2 - Control.View.FirstVisibleColumn*Control.View.CharWidth;

            if (xtr.Count == 0)
            {
                if (Control.VirtualWhitespace && Control.View.CharWidth > 0)
                {
                    return new TextPoint(X/Control.View.CharWidth, RowIndex);
                }

                return new TextPoint(0, RowIndex);
            }


            int taborig = -Control.View.FirstVisibleColumn*Control.View.CharWidth + Control.View.TextMargin;
            int xpos = Control.View.TextMargin - Control.View.ClientAreaStart;
            int CharNo = 0;
            int TotWidth = 0;
            Word Word = null;
            int WordStart = 0;
            foreach (Word w in xtr.FormattedWords)
            {
                Word = w;
                WordStart = TotWidth;

                if (w.Type == WordType.Word && w.Style != null)
                    SetStringFont(w.Style.Bold, w.Style.Italic, w.Style.Underline);
                else
                    SetStringFont(false, false, false);

                int tmpWidth = GFX.StringBuffer.DrawTabbedString(w.Text, xpos + TotWidth, 0, taborig, Control.PixelTabSize).Width;

                if (TotWidth + tmpWidth >= X)
                {
                    break;
                }

                //dont do this for the last word
                if (w != xtr.FormattedWords[xtr.FormattedWords.Count - 1])
                {
                    TotWidth += tmpWidth;
                    CharNo += w.Text.Length;
                }
            }

            //CharNo is the index in the text where 'word' starts
            //'Word' is the word object that contains the 'X'
            //'WordStart' contains the pixel start position for 'Word'

            if (Word != null)
                if (Word.Type == WordType.Word && Word.Style != null)
                    SetStringFont(Word.Style.Bold, Word.Style.Italic, Word.Style.Underline);
                else
                    SetStringFont(false, false, false);

            //now , lets measure each char and get a correct pos

            bool found = false;
            if (Word != null)
                foreach (char c in Word.Text)
                {
                    int tmpWidth = GFX.StringBuffer.DrawTabbedString(c + "", xpos + WordStart, 0, taborig, Control.PixelTabSize).Width;
                    if (WordStart + tmpWidth >= X)
                    {
                        found = true;
                        break;
                    }
                    CharNo++;
                    WordStart += tmpWidth;
                }

            if (!found && Control.View.CharWidth > 0 && Control.VirtualWhitespace)
            {
                int xx = X - WordStart;
                int cn = xx/Control.View.CharWidth;
                CharNo += cn;
            }

            if (CharNo < 0)
                CharNo = 0;

            return new TextPoint(CharNo, RowIndex);
        }

        private Point GetTextPointPixelPos(TextPoint tp)
        {
            Row xtr = Control.Document[tp.Y];
            if (xtr.RowState == RowState.SpanParsed)
                Control.Document.Parser.ParseRow(xtr.Index, true);

            Row r = xtr.IsCollapsedEndPart ? xtr.Expansion_StartRow : xtr;

            int index = r.VisibleIndex;
            int yPos = (index - Control.View.FirstVisibleRow);
            if (yPos < 0 || yPos > Control.View.VisibleRowCount)
                return new Point(-1, -1);

            yPos *= Control.View.RowHeight;

            bool Collapsed = (xtr.IsCollapsedEndPart);
            int pos = MeasureRow(xtr, tp.X).Width + 1;


            if (Collapsed)
            {
                pos += xtr.Expansion_PixelStart;
                pos -= MeasureRow(xtr, xtr.Expansion_StartChar).Width;
            }

            int xPos = pos + Control.View.TextMargin - Control.View.ClientAreaStart;

            if (xPos < Control.View.TextMargin || xPos > Control.View.ClientAreaWidth + Control.View.TextMargin)
                return new Point(-1, -1);


            return new Point(xPos, yPos);
        }


    }
}