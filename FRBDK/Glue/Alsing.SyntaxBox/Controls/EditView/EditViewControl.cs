// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

#region using ...

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Resources;
using System.Text;
using System.Windows.Forms;
using Alsing.Globalization;
using Alsing.SourceCode;
using Alsing.Windows.Forms.CoreLib;
using Alsing.Windows.Forms.SyntaxBox.Painter;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#endregion

namespace Alsing.Windows.Forms.SyntaxBox
{
    [ToolboxItem(false)]
    public class EditViewControl : SplitViewChildControl
    {
        #region General Declarations

        private readonly Caret _Caret;
        private readonly Selection _Selection;

        private bool _AutoListVisible;
        private bool _InfoTipVisible;
        private double _IntelliScrollPos;
        private bool _KeyDownHandled;
        private bool _OverWrite;

        /// <summary>
        /// The Point in the text where the Autolist was activated.
        /// </summary>
        public TextPoint AutoListStartPos;

        /// <summary>
        /// The Point in the text where the InfoTip was activated.
        /// </summary>		
        public TextPoint InfoTipStartPos;

        private int MouseX;
        private int MouseY;
        public IPainter Painter;
        public ViewPoint View;

        List<Keys> mKeysThatInsertFromAutolist = new List<Keys>();

        #endregion

        #region Internal controls

        private WeakReference _Control;
        private Timer CaretTimer;
        private IContainer components;
        private PictureBox Filler;

        private IntelliMouseControl IntelliMouse;
        private ToolTip tooltip;

        #region PUBLIC PROPERTY AUTOLIST

        private AutoListForm _AutoList;

        public AutoListForm AutoList
        {
            get
            {
                CreateAutoList();

                return _AutoList;
            }
            set { _AutoList = value; }
        }

        #endregion

        #region PUBLIC PROPERTY INFOTIP

        private InfoTipForm _InfoTip;

        public InfoTipForm InfoTip
        {
            get
            {
                CreateInfoTip();

                return _InfoTip;
            }
            set { _InfoTip = value; }
        }

        #endregion

        #region PUBLIC PROPERTY IMEWINDOW

        public IMEWindow IMEWindow { get; set; }

        #endregion

        #region PUBLIC PROPERTY FINDREPLACEDIALOG 

        private FindReplaceForm _FindReplaceDialog;

        public FindReplaceForm FindReplaceDialog
        {
            get
            {
                CreateFindForm();


                return _FindReplaceDialog;
            }
            set { _FindReplaceDialog = value; }
        }

        #endregion

        public bool HasAutoList
        {
            get { return _AutoList != null; }
        }

        public bool HasInfoTip
        {
            get { return _InfoTip != null; }
        }


        public SyntaxBoxControl _SyntaxBox
        {
            get
            {
                try
                {
                    if (_Control != null && _Control.IsAlive)
                        return (SyntaxBoxControl) _Control.Target;
                    return null;
                }
                catch
                {
                    return null;
                }
            }
            set { _Control = new WeakReference(value); }
        }

        #endregion

        #region Public events

        /// <summary>
        /// An event that is fired when the caret has moved.
        /// </summary>
        public event EventHandler CaretChange = null;

        /// <summary>
        /// An event that is fired when the selection has changed.
        /// </summary>
        public event EventHandler SelectionChange = null;

        /// <summary>
        /// An event that is fired when mouse down occurs on a row
        /// </summary>
        public event RowMouseHandler RowMouseDown = null;

        /// <summary>
        /// An event that is fired when mouse move occurs on a row
        /// </summary>
        public event RowMouseHandler RowMouseMove = null;

        /// <summary>
        /// An event that is fired when mouse up occurs on a row
        /// </summary>
        public event RowMouseHandler RowMouseUp = null;

        /// <summary>
        /// An event that is fired when a click occurs on a row
        /// </summary>
        public event RowMouseHandler RowClick = null;

        /// <summary>
        /// An event that is fired when a double click occurs on a row
        /// </summary>
        public event RowMouseHandler RowDoubleClick = null;

        /// <summary>
        /// An event that is fired when the control has updated the clipboard
        /// </summary>
        public event CopyHandler ClipboardUpdated = null;

        #endregion

        private void CreateAutoList()
        {
            if (_SyntaxBox != null && !_SyntaxBox.DisableAutoList &&
                _AutoList == null)
            {
                Debug.WriteLine("Creating Autolist");

                AutoList = new AutoListForm();
                NativeMethods.SetWindowLong(AutoList.Handle,
                                            NativeMethods.GWL_STYLE,
                                            NativeMethods.WS_CHILD);

                AutoList.SendToBack();
                AutoList.Visible = false;
                //this.Controls.Add (this.AutoList);
                AutoList.DoubleClick += AutoListDoubleClick;

                AutoList.Images = _SyntaxBox.AutoListIcons;
                AutoList.Add("a123", "a123", "Some tooltip for this item 1", 1);
                AutoList.Add("b456", "b456", "Some tooltip for this item 2", 2);
                AutoList.Add("c789", "c789", "Some tooltip for this item 3", 2);
                AutoList.Add("d012", "d012", "Some tooltip for this item 4", 3);
                AutoList.Add("e345", "e345", "Some tooltip for this item 5", 4);
            }
        }

        private void CreateFindForm()
        {
            if (!_SyntaxBox.DisableFindForm && _FindReplaceDialog == null)
            {
                Debug.WriteLine("Creating Findform");
                FindReplaceDialog = new FindReplaceForm(this);
            }
        }

        private void CreateInfoTip()
        {
            if (_SyntaxBox != null && !_SyntaxBox.DisableInfoTip &&
                _InfoTip == null)
            {
                Debug.WriteLine("Creating Infotip");

                InfoTip = new InfoTipForm(this);
                NativeMethods.SetWindowLong(InfoTip.Handle,
                                            NativeMethods.GWL_STYLE,
                                            NativeMethods.WS_CHILD);

                InfoTip.SendToBack();
                InfoTip.Visible = false;
            }
        }

        private void IntelliMouse_BeginScroll(object sender, EventArgs e)
        {
            _IntelliScrollPos = 0;
            View.YOffset = 0;
        }

        private void IntelliMouse_EndScroll(object sender, EventArgs e)
        {
            View.YOffset = 0;
            Redraw();
        }

        private void IntelliMouse_Scroll(object sender,
                                         IntelliMouse.ScrollEventArgs e)
        {
            if (e.DeltaY < 0 && vScroll.Value == 0)
            {
                View.YOffset = 0;
                Redraw();
                return;
            }

            if (e.DeltaY > 0 && vScroll.Value >= vScroll.Maximum -
                                                 View.VisibleRowCount + 1)
            {
                View.YOffset = 0;
                Redraw();
                return;
            }

            _IntelliScrollPos += e.DeltaY/(double) 8;

            int scrollrows = (int) (_IntelliScrollPos)/View.RowHeight;
            if (scrollrows != 0)
            {
                _IntelliScrollPos -= scrollrows*View.RowHeight;
            }
            View.YOffset = - (int) _IntelliScrollPos;
            ScrollScreen(scrollrows);
        }


        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (int) WindowMessage.WM_DESTROY)
            {
                try
                {
                    if (FindReplaceDialog != null)
                        FindReplaceDialog.Close();

                    if (AutoList != null)
                        AutoList.Close();

                    if (InfoTip != null)
                        InfoTip.Close();
                }
                catch {}
            }

            base.WndProc(ref m);
        }

        protected void CopyAsRTF()
        {
            TextStyle[] styles = Document.Parser.SyntaxDefinition.Styles;
            Document.ParseAll(true);
            int r1 = Selection.LogicalBounds.FirstRow;
            int r2 = Selection.LogicalBounds.LastRow;
            int c1 = Selection.LogicalBounds.FirstColumn;
            int c2 = Selection.LogicalBounds.LastColumn;

            var sb = new StringBuilder();
            sb.Append(@"{\rtf1\ansi\ansicpg1252\deff0\deflang1053{\fonttbl{\f0\fmodern\fprq1\fcharset0 " + FontName +
                      @";}}");
            sb.Append(@"{\colortbl ;");

            foreach (TextStyle ts in styles)
            {
                sb.AppendFormat("\\red{0}\\green{1}\\blue{2};", ts.ForeColor.R,
                                ts.ForeColor.G, ts.ForeColor.B);
                sb.AppendFormat("\\red{0}\\green{1}\\blue{2};", ts.BackColor.R,
                                ts.BackColor.G, ts.BackColor.B);
            }

            sb.Append(@";}");
            sb.Append(@"\viewkind4\uc1\pard\f0\fs20");


            bool Done = false;
            for (int i = r1; i <= r2; i++)
            {
                Row row = Document[i];


                foreach (Word w in row)
                {
                    if (i == r1 && w.Column + w.Text.Length < c1)
                        continue;

                    bool IsFirst = (i == r1 && w.Column <= c1 && w.Column + w.Text.Length
                                                                 > c1);
                    bool IsLast = (i == r2 && w.Column < c2 && w.Column + w.Text.Length >
                                                               c2);


                    if (w.Type == WordType.Word && w.Style != null)
                    {
                        int clrindex = Array.IndexOf(styles, w.Style);
                        clrindex *= 2;
                        clrindex++;

                        sb.Append("{\\cf" + clrindex.ToString
                                                (CultureInfo.InvariantCulture));
                        if (!w.Style.Transparent)
                        {
                            sb.Append("\\highlight" + (clrindex + 1).ToString
                                                          (CultureInfo.InvariantCulture));
                        }
                        sb.Append(" ");
                    }

                    if (w.Style != null)
                    {
                        if (w.Style.Bold)
                            sb.Append(@"\b ");
                        if (w.Style.Underline)
                            sb.Append(@"\ul ");
                        if (w.Style.Italic)
                            sb.Append(@"\i ");
                    }
                    string wordtext = w.Text;

                    if (IsLast)
                        wordtext = wordtext.Substring(0, c2 - w.Column);

                    if (IsFirst)
                        wordtext = wordtext.Substring(c1 - w.Column);


                    wordtext =
                        wordtext.Replace(@"\", @" \ \ ").Replace(@"
        }
        ", @" \
      }
      ").
                            Replace(@"
      {
        ", @" \
        {
          ");

                    sb.Append(wordtext);

                    if (w.Style != null)
                    {
                        if (w.Style.Bold) sb.Append(@"\b0 ");
                        if (w.Style.Underline)
                            sb.Append(@"\ul0 ");
                        if (w.Style.Italic) sb.Append(@"\i0 ");
                    }

                    if (w.Type == WordType.Word && w.Style != null)
                    {
                        sb.Append("}");
                    }

                    if (IsLast)
                    {
                        Done = true;
                        break;
                    }
                }
                if (Done) break;

                sb.Append(@"\par");
            }


            var da = new DataObject();
            da.SetData(DataFormats.Rtf, sb.ToString());
            string s = Selection.Text;
            da.SetData(DataFormats.Text, s);
            Clipboard.SetDataObject(da);

            var ea = new CopyEventArgs {Text = s};
            OnClipboardUpdated(ea);
        }

        #region Constructor

        /// <summary>
        /// Default constructor for the SyntaxBoxControl
        /// </summary>
        public EditViewControl(SyntaxBoxControl Parent)
        {
            _SyntaxBox = Parent;

            Painter = new NativePainter(this);
            _Selection = new Selection(this);
            _Caret = new Caret(this);

            _Caret.Change += CaretChanged;
            _Selection.Change += SelectionChanged;


            InitializeComponent();


            CreateAutoList();
            //CreateFindForm ();
            CreateInfoTip();

            SetStyle(ControlStyles.AllPaintingInWmPaint, false);
            SetStyle(ControlStyles.DoubleBuffer, false);
            SetStyle(ControlStyles.Selectable, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.UserPaint, true);
            UpdateStyles();

            mKeysThatInsertFromAutolist.Add(Keys.Space);
            mKeysThatInsertFromAutolist.Add(Keys.Enter);
            mKeysThatInsertFromAutolist.Add(Keys.Tab);
            mKeysThatInsertFromAutolist.Add(Keys.OemPeriod);
            mKeysThatInsertFromAutolist.Add(Keys.OemOpenBrackets);
            //mKeysThatInsertFromAutolist.Add(


            //			this.IMEWindow = new Alsing.Globalization.IMEWindow (this.Handle,_SyntaxBox.FontName,_SyntaxBox.FontSize);
        }

        #endregion

        #region DISPOSE()

        /// <summary>
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            RemoveFocus();
#if DEBUG
            try
            {
                Console.WriteLine("disposing editview");
            }
            catch {}
#endif

            if (disposing)
            {
                if (components != null)
                    components.Dispose();

                try
                {
                    if (Painter != null)
                        Painter.Dispose();
                }
                catch {}
            }
            base.Dispose(disposing);
        }

        #endregion

        #region Private/Protected/public Properties

        public int PixelTabSize
        {
            get { return _SyntaxBox.TabSize*View.CharWidth; }
        }

        #endregion

        #region Private/Protected/Internal Methods

        private int MaxCharWidth = 8;

        private void DoResize()
        {
            if (Visible && Width > 0 && Height > 0 &&
                IsHandleCreated)
            {
                try
                {
                    if (Filler == null)
                        return;

                    TopThumb.Width = SystemInformation.VerticalScrollBarWidth;
                    LeftThumb.Height = SystemInformation.HorizontalScrollBarHeight;
                    vScroll.Width = SystemInformation.VerticalScrollBarWidth;
                    hScroll.Height = SystemInformation.HorizontalScrollBarHeight;

                    if (TopThumbVisible)
                    {
                        vScroll.Top = TopThumb.Height;
                        if (hScroll.Visible)
                            vScroll.Height = ClientHeight - hScroll.Height -
                                             TopThumb.Height;
                        else
                            vScroll.Height = ClientHeight - TopThumb.Height;
                    }
                    else
                    {
                        if (hScroll.Visible)
                            vScroll.Height = ClientHeight - hScroll.Height;
                        else
                            vScroll.Height = ClientHeight;

                        vScroll.Top = 0;
                    }

                    if (LeftThumbVisible)
                    {
                        hScroll.Left = LeftThumb.Width;
                        if (vScroll.Visible)
                            hScroll.Width = ClientWidth - vScroll.Width -
                                            LeftThumb.Width;
                        else
                            hScroll.Width = ClientWidth - LeftThumb.Width;
                    }
                    else
                    {
                        if (vScroll.Visible)
                            hScroll.Width = ClientWidth - vScroll.Width;
                        else
                            hScroll.Width = ClientWidth;

                        hScroll.Left = 0;
                    }


                    if (Width != OldWidth && Width > 0)
                    {
                        OldWidth = Width;
                        if (Painter != null)
                            Painter.Resize();
                    }


                    vScroll.Left = ClientWidth - vScroll.Width;
                    hScroll.Top = ClientHeight - hScroll.Height;

                    LeftThumb.Left = 0;
                    LeftThumb.Top = hScroll.Top;

                    TopThumb.Left = vScroll.Left;
                    TopThumb.Top = 0;


                    Filler.Left = vScroll.Left;
                    Filler.Top = hScroll.Top;
                    Filler.Width = vScroll.Width;
                    Filler.Height = hScroll.Height;
                }
                catch {}
            }
        }

        private void InsertText(string text)
        {
            Caret.CropPosition();
            if (Selection.IsValid)
            {
                Selection.DeleteSelection();
                InsertText(text);
            }
            else
            {
                if (!_OverWrite || text.Length > 1)
                {
                    TextPoint p = Document.InsertText(text, Caret.Position.X,
                                                      Caret.Position.Y);
                    Caret.CurrentRow.Parse(true);
                    if (text.Length == 1)
                    {
                        Caret.SetPos(p);
                        Caret.CaretMoved(false);
                    }
                    else
                    {
                        //Document.i = true;

                        Document.ResetVisibleRows();
                        Caret.SetPos(p);
                        Caret.CaretMoved(false);
                    }
                }
                else
                {
                    var r = new TextRange
                            {
                                FirstColumn = Caret.Position.X,
                                FirstRow = Caret.Position.Y,
                                LastColumn = (Caret.Position.X + 1),
                                LastRow = Caret.Position.Y
                            };
                    var ag = new UndoBlockCollection();
                    var b = new UndoBlock
                                  {
                                      Action = UndoAction.DeleteRange,
                                      Text = Document.GetRange(r),
                                      Position = Caret.Position
                                  };
                    ag.Add(b);
                    Document.DeleteRange(r, false);
                    b = new UndoBlock
                        {
                            Action = UndoAction.InsertRange
                        };
                    string NewChar = text;
                    b.Text = NewChar;
                    b.Position = Caret.Position;
                    ag.Add(b);
                    Document.AddToUndoList(ag);
                    Document.InsertText(NewChar, Caret.Position.X, Caret.Position.Y,
                                        false);
                    Caret.CurrentRow.Parse(true);

                    Caret.MoveRight(false);
                }
            }
            //	this.ScrollIntoView ();
        }

        private void InsertEnter()
        {
            Caret.CropPosition();
            if (Selection.IsValid)
            {
                Selection.DeleteSelection();
                InsertEnter();
            }
            else
            {
                switch (Indent)
                {
                    case IndentStyle.None:
                        {
                            Document.InsertText("\n", Caret.Position.X, Caret.Position.Y);
                            //depends on what sort of indention we are using....
                            Caret.CurrentRow.Parse();
                            Caret.MoveDown(false);
                            Caret.CurrentRow.Parse(false);
                            Caret.CurrentRow.Parse(true);

                            Caret.Position.X = 0;
                            Caret.SetPos(Caret.Position);
                            break;
                        }
                    case IndentStyle.LastRow:
                        {
                            Row xtr = Caret.CurrentRow;
                            string indent = xtr.GetLeadingWhitespace();
                            int Max = Math.Min(indent.Length, Caret.Position.X);
                            string split = "\n" + indent.Substring(0, Max);
                            Document.InsertText(split, Caret.Position.X, Caret.Position.Y);
                            Document.ResetVisibleRows();
                            Caret.CurrentRow.Parse(false);
                            Caret.CurrentRow.Parse(true);
                            Caret.MoveDown(false);
                            Caret.CurrentRow.Parse(false);
                            Caret.CurrentRow.Parse(true);

                            Caret.Position.X = indent.Length;
                            Caret.SetPos(Caret.Position);
                            xtr.Parse(false);
                            xtr.Parse(true);
                            xtr.NextRow.Parse(false);
                            xtr.NextRow.Parse(true);

                            break;
                        }
                    case IndentStyle.Scope:
                        {
                            Row xtr = Caret.CurrentRow;
                            xtr.Parse(true);
                            if (xtr.ShouldOutdent)
                            {
                                OutdentEndRow();
                            }

                            Document.InsertText("\n", Caret.Position.X, Caret.Position.Y);
                            //depends on what sort of indention we are using....
                            Caret.CurrentRow.Parse();
                            Caret.MoveDown(false);
                            Caret.CurrentRow.Parse(false);

                            var indent = new String('\t', Caret.CurrentRow.Depth);
                            Document.InsertText(indent, 0, Caret.Position.Y);

                            Caret.CurrentRow.Parse(false);
                            Caret.CurrentRow.Parse(true);

                            Caret.Position.X = indent.Length;
                            Caret.SetPos(Caret.Position);
                            Caret.CropPosition();
                            Selection.ClearSelection();

                            xtr.Parse(false);
                            xtr.Parse(true);
                            xtr.NextRow.Parse(false);
                            xtr.NextRow.Parse(true);

                            break;
                        }
                    case IndentStyle.Smart:
                        {
                            Row xtr = Caret.CurrentRow;
                            if (xtr.ShouldOutdent)
                            {
                                OutdentEndRow();
                            }
                            Document.InsertText("\n", Caret.Position.X, Caret.Position.Y);
                            Caret.MoveDown(false);
                            Caret.CurrentRow.Parse(false);
                            Caret.CurrentRow.Parse(true);
                            Caret.CurrentRow.startSpan.StartRow.Parse(false);
                            Caret.CurrentRow.startSpan.StartRow.Parse(true);

                            string prev = "\t" +
                                          Caret.CurrentRow.startSpan.StartRow.GetVirtualLeadingWhitespace();

                            string indent = Caret.CurrentRow.PrevRow.GetLeadingWhitespace();
                            if (indent.Length < prev.Length)
                                indent = prev;

                            string ts = "\t" + new String(' ', TabSize);
                            while (indent.IndexOf(ts) >= 0)
                            {
                                indent = indent.Replace(ts, "\t\t");
                            }

                            Document.InsertText(indent, 0, Caret.Position.Y);

                            Caret.CurrentRow.Parse(false);
                            Caret.CurrentRow.Parse(true);

                            Caret.Position.X = indent.Length;
                            Caret.SetPos(Caret.Position);

                            Caret.CropPosition();
                            Selection.ClearSelection();
                            xtr.Parse(false);
                            xtr.Parse(true);
                            xtr.NextRow.Parse(false);
                            xtr.NextRow.Parse(true);
                            break;
                        }
                }
                ScrollIntoView();
            }
        }

        private void OutdentEndRow()
        {
            try
            {
                if (Indent == IndentStyle.Scope)
                {
                    Row xtr = Caret.CurrentRow;
                    var indent1 = new String('\t', Caret.CurrentRow.Depth);
                    var tr = new TextRange
                             {
                                 FirstColumn = 0,
                                 LastColumn = xtr.GetLeadingWhitespace().Length,
                                 FirstRow = xtr.Index,
                                 LastRow = xtr.Index
                             };

                    Document.DeleteRange(tr);
                    Document.InsertText(indent1, 0, xtr.Index, true);

                    int diff = indent1.Length - tr.LastColumn;
                    Caret.Position.X += diff;
                    Caret.SetPos(Caret.Position);
                    Caret.CropPosition();
                    Selection.ClearSelection();
                    Caret.CurrentRow.Parse(false);
                    Caret.CurrentRow.Parse(true);
                }
                else if (Indent == IndentStyle.Smart)
                {
                    Row xtr = Caret.CurrentRow;

                    if (xtr.FirstNonWsWord == xtr.expansion_EndSpan.EndWord)
                    {
                        //int j=xtr.Expansion_StartRow.StartWordIndex;
                        string indent1 =
                            xtr.startSpan.StartWord.Row.GetVirtualLeadingWhitespace();
                        var tr = new TextRange
                                 {
                                     FirstColumn = 0,
                                     LastColumn = xtr.GetLeadingWhitespace().Length,
                                     FirstRow = xtr.Index,
                                     LastRow = xtr.Index
                                 };
                        Document.DeleteRange(tr);
                        string ts = "\t" + new String(' ', TabSize);
                        while (indent1.IndexOf(ts) >= 0)
                        {
                            indent1 = indent1.Replace(ts, "\t\t");
                        }
                        Document.InsertText(indent1, 0, xtr.Index, true);

                        int diff = indent1.Length - tr.LastColumn;
                        Caret.Position.X += diff;
                        Caret.SetPos(Caret.Position);
                        Caret.CropPosition();
                        Selection.ClearSelection();
                        Caret.CurrentRow.Parse(false);
                        Caret.CurrentRow.Parse(true);
                    }
                }
            }
            catch {}
        }

        private void DeleteForward()
        {
            Caret.CropPosition();
            if (Selection.IsValid)
                Selection.DeleteSelection();
            else
            {
                Row xtr = Caret.CurrentRow;
                if (Caret.Position.X == xtr.Text.Length)
                {
                    if (Caret.Position.Y <= Document.Count - 2)
                    {
                        var r = new TextRange {FirstColumn = Caret.Position.X, FirstRow = Caret.Position.Y};
                        r.LastRow = r.FirstRow + 1;
                        r.LastColumn = 0;

                        Document.DeleteRange(r);
                        Document.ResetVisibleRows();
                    }
                }
                else
                {
                    var r = new TextRange {FirstColumn = Caret.Position.X, FirstRow = Caret.Position.Y};
                    r.LastRow = r.FirstRow;
                    r.LastColumn = r.FirstColumn + 1;
                    Document.DeleteRange(r);
                    Document.ResetVisibleRows();
                    Caret.CurrentRow.Parse(false);
                    Caret.CurrentRow.Parse(true);
                }
            }
        }

        private void DeleteBackwards()
        {
            Caret.CropPosition();
            if (Selection.IsValid)
                Selection.DeleteSelection();
            else
            {
                Row xtr = Caret.CurrentRow;
                if (Caret.Position.X == 0)
                {
                    if (Caret.Position.Y > 0)
                    {
                        Caret.Position.Y--;
                        Caret.MoveEnd(false);
                        DeleteForward();
                        //Caret.CurrentRow.Parse ();
                        Document.ResetVisibleRows();
                    }
                }
                else
                {
                    if (Caret.Position.X >= xtr.Text.Length)
                    {
                        var r = new TextRange {FirstColumn = (Caret.Position.X - 1), FirstRow = Caret.Position.Y};
                        r.LastRow = r.FirstRow;
                        r.LastColumn = r.FirstColumn + 1;
                        Document.DeleteRange(r);
                        Document.ResetVisibleRows();
                        Caret.MoveEnd(false);
                        Caret.CurrentRow.Parse();
                    }
                    else
                    {
                        var r = new TextRange {FirstColumn = (Caret.Position.X - 1), FirstRow = Caret.Position.Y};
                        r.LastRow = r.FirstRow;
                        r.LastColumn = r.FirstColumn + 1;
                        Document.DeleteRange(r);
                        Document.ResetVisibleRows();
                        Caret.MoveLeft(false);
                        Caret.CurrentRow.Parse();
                    }
                }
            }
        }

        private void ScrollScreen(int Amount)
        {
            try
            {
                tooltip.RemoveAll();

                int newval = vScroll.Value + Amount;

                newval = Math.Max(newval, vScroll.Minimum);
                newval = Math.Min(newval, vScroll.Maximum);

                if (newval >= vScroll.Maximum - View.VisibleRowCount + 1)
                    newval = vScroll.Maximum - View.VisibleRowCount + 1;

                vScroll.Value = newval;
                Redraw();
            }
            catch {}
        }

        private void PasteText()
        {
            try
            {
                IDataObject iData = Clipboard.GetDataObject();

                if (iData != null)
                    if (iData.GetDataPresent(DataFormats.UnicodeText))
                    {
                        // Yes it is, so display it in a text box.
                        var s = (string) iData.GetData(DataFormats.UnicodeText);

                        InsertText(s);
                        if (ParseOnPaste)
                            Document.ParseAll(true);
                    }
                    else if (iData.GetDataPresent(DataFormats.Text))
                    {
                        // Yes it is, so display it in a text box.
                        var s = (string) iData.GetData(DataFormats.Text);

                        InsertText(s);
                        if (ParseOnPaste)
                            Document.ParseAll(true);
                    }
            }
            catch
            {
                //ignore
            }
        }


        private void BeginDragDrop()
        {
            DoDragDrop(Selection.Text, DragDropEffects.All);
        }

        private void Redraw()
        {
            Invalidate();
        }


        private void RedrawCaret()
        {
            Graphics g = CreateGraphics();
            Painter.RenderCaret(g);
            g.Dispose();
        }

        private void SetMouseCursor(int x, int y)
        {
            if (_SyntaxBox.LockCursorUpdate)
            {
                Cursor = _SyntaxBox.Cursor;
                return;
            }

            if (View.Action == EditAction.DragText)
            {
                Cursor = Cursors.Hand;
                //Cursor.Current = Cursors.Hand;
            }
            else
            {
                if (x < View.TotalMarginWidth)
                {
                    if (x < View.GutterMarginWidth)
                    {
                        Cursor = Cursors.Arrow;
                    }
                    else
                    {
                        var ms = new MemoryStream(Properties.Resources.FlippedCursor);
                        Cursor = new Cursor(ms);
                    }
                }
                else
                {
                    if (x > View.TextMargin - 8)
                    {
                        if (IsOverSelection(x, y))
                            Cursor = Cursors.Arrow;
                        else
                        {
                            TextPoint tp = Painter.CharFromPixel(x, y);
                            Word w = Document.GetWordFromPos(tp);
                            if (w != null && w.Pattern != null && w.Pattern.Category != null)
                            {
                                var e = new WordMouseEventArgs {
                                            Pattern = w.Pattern,
                                            Button = MouseButtons.None,
                                            Cursor = Cursors.Hand,
                                            Word = w
                                        };

                                _SyntaxBox.OnWordMouseHover(ref e);

                                Cursor = e.Cursor;
                            }
                            else
                                Cursor = Cursors.IBeam;
                        }
                    }
                    else
                    {
                        Cursor = Cursors.Arrow;
                    }
                }
            }
        }

        private void CopyText()
        {
            //no freaking vs.net copy empty selection 
            if (!Selection.IsValid)
                return;

            if (_SyntaxBox.CopyAsRTF)
            {
                CopyAsRTF();
            }
            else
            {
                try
                {
                    string t = Selection.Text;
                    Clipboard.SetDataObject(t, true);
                    var ea = new CopyEventArgs {Text = t};
                    OnClipboardUpdated(ea);
                }
                catch
                {
                    try
                    {
                        string t = Selection.Text;
                        Clipboard.SetDataObject(t, true);
                        var ea = new CopyEventArgs {Text = t};
                        OnClipboardUpdated(ea);
                    }
                    catch {}
                }
            }
        }

        /// <summary>
        /// For public use only
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected override bool IsInputKey(Keys key)
        {
            switch (key)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Right:
                case Keys.Left:
                case Keys.Tab:
                case Keys.PageDown:
                case Keys.PageUp:
                case Keys.Enter:
                    return true;
            }
            return true; //base.IsInputKey(key);			
        }

        protected override bool IsInputChar(char c)
        {
            return true;
        }

        public void RemoveFocus()
        {
            if (InfoTip == null || AutoList == null)
                return;

            if (!ContainsFocus && !InfoTip.ContainsFocus &&
                !AutoList.ContainsFocus)
            {
                CaretTimer.Enabled = false;
                Caret.Blink = false;
                _AutoListVisible = false;
                _InfoTipVisible = false;
            }
            Redraw();
        }

        private void SelectCurrentWord()
        {
            Row xtr = Caret.CurrentRow;
            if (xtr.Text == "")
                return;

            if (Caret.Position.X >= xtr.Text.Length)
                return;

            string Char = xtr.Text.Substring(Caret.Position.X, 1);
            int Type = CharType(Char);

            int left = Caret.Position.X;
            int right = Caret.Position.X;

            while (left >= 0 && CharType(xtr.Text.Substring(left, 1)) == Type)
                left--;

            while (right <= xtr.Text.Length - 1 && CharType(xtr.Text.Substring(right,
                                                                               1)) == Type)
                right++;

            Selection.Bounds.FirstRow = Selection.Bounds.LastRow = xtr.Index;
            Selection.Bounds.FirstColumn = left + 1;
            Selection.Bounds.LastColumn = right;
            Caret.Position.X = right;
            Caret.SetPos(Caret.Position);
            Redraw();
        }

        private static int CharType(string s)
        {
            const string g1 = " \t";
            const string g2 = ".,-+'?´=)(/&%¤#!\"\\<>[]$£@*:;{}";

            if (g1.IndexOf(s) >= 0)
                return 1;

            if (g2.IndexOf(s) >= 0)
                return 2;

            return 3;
        }

        private void SelectPattern(int RowIndex, int Column, int Length)
        {
            Selection.Bounds.FirstColumn = Column;
            Selection.Bounds.FirstRow = RowIndex;
            Selection.Bounds.LastColumn = Column + Length;
            Selection.Bounds.LastRow = RowIndex;
            Caret.Position.X = Column + Length;
            Caret.Position.Y = RowIndex;
            Caret.CurrentRow.EnsureVisible();
            ScrollIntoView();
            Redraw();
        }

        public void InitVars()
        {
            //setup viewpoint data


            if (View.RowHeight == 0)
                View.RowHeight = 48;

            if (View.CharWidth == 0)
                View.CharWidth = 16;

            //View.RowHeight=16;
            //View.CharWidth=8;

            View.FirstVisibleColumn = hScroll.Value;
            View.FirstVisibleRow = vScroll.Value;
            //	View.yOffset =_yOffset;

            View.VisibleRowCount = 0;
            if (hScroll.Visible)
                View.VisibleRowCount = (Height - hScroll.Height)/View.RowHeight
                                       + 1;
            else
                View.VisibleRowCount = (Height - hScroll.Height)/View.RowHeight
                                       + 2;

            View.GutterMarginWidth = ShowGutterMargin ? GutterMarginWidth : 0;

            if (ShowLineNumbers)
            {
                int chars = (Document.Count).ToString
                    (CultureInfo.InvariantCulture).Length;
                var s = new String('9', chars);
                View.LineNumberMarginWidth = 10 + Painter.MeasureString(s).Width;
            }
            else
                View.LineNumberMarginWidth = 0;


            View.TotalMarginWidth = View.GutterMarginWidth +
                                    View.LineNumberMarginWidth;
            if (Document.Folding)
                View.TextMargin = View.TotalMarginWidth + 20;
            else
                View.TextMargin = View.TotalMarginWidth + 7;


            View.ClientAreaWidth = Width - vScroll.Width - View.TextMargin;
            View.ClientAreaStart = View.FirstVisibleColumn*View.CharWidth;
        }

        public void CalcMaxCharWidth()
        {
            MaxCharWidth = Painter.GetMaxCharWidth();
        }

        public void SetMaxHorizontalScroll()
        {
            CalcMaxCharWidth();
            int CharWidth = View.CharWidth;
            if (CharWidth == 0)
                CharWidth = 1;

            if (View.ClientAreaWidth/CharWidth < 0)
            {
                hScroll.Maximum = 1000;
                return;
            }

            hScroll.LargeChange = View.ClientAreaWidth/CharWidth;

            try
            {
                int max = 0;
                for (int i = View.FirstVisibleRow;
                     i <
                     Document.VisibleRows.Count;
                     i++)
                {
                    if (i >= View.VisibleRowCount + View.FirstVisibleRow)
                        break;

                    string l = Document.VisibleRows[i].IsCollapsed ? Document.VisibleRows[i].VirtualCollapsedRow.Text : Document.VisibleRows[i].Text;

                    l = l.Replace("\t", new string(' ', TabSize));
                    if (l.Length > max)
                        max = l.Length;
                }

                int pixels = max*MaxCharWidth;
                int chars = pixels/CharWidth;


                if (hScroll.Value <= chars)
                    hScroll.Maximum = chars;
            }
            catch
            {
                hScroll.Maximum = 1000;
            }
        }

        public void InitScrollbars()
        {
            if (Document.VisibleRows.Count > 0)
            {
                vScroll.Maximum = Document.VisibleRows.Count + 1;
                //+this.View.VisibleRowCount-2;// - View.VisibleRowCount  ;
                vScroll.LargeChange = View.VisibleRowCount;
                SetMaxHorizontalScroll();
            }
            else
                vScroll.Maximum = 1;
        }


        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new Container();
            var resources = new
                ResourceManager(typeof (EditViewControl));
            Filler = new PictureBox();
            CaretTimer = new Timer (components);
            tooltip = new ToolTip(components);

            SuspendLayout();

            if (!_SyntaxBox.DisableIntelliMouse)
            {
                IntelliMouse = new
                    IntelliMouseControl
                               {
                                   BackgroundImage = ((Bitmap)
                                                      (resources.GetObject("IntelliMouse.BackgroundImage"))),
                                   Image = ((Bitmap) (resources.GetObject(
                                                         "IntelliMouse.Image"))),
                                   Location = new Point(197, 157),
                                   Name = "IntelliMouse",
                                   Size = new Size(28, 28),
                                   TabIndex = 4,
                                   TransparencyKey = Color.FromArgb((
                                                                        (255)), ((0)), ((255))),
                                   Visible = false
                               };
                // 
                // IntelliMouse
                // 
                IntelliMouse.EndScroll += IntelliMouse_EndScroll;
                IntelliMouse.BeginScroll += IntelliMouse_BeginScroll;
                IntelliMouse.Scroll += IntelliMouse_Scroll;
            }


            // 
            // hScroll
            // 
            hScroll.Cursor = Cursors.Default;
            hScroll.Scroll += hScroll_Scroll;
            // 
            // vScroll
            // 
            vScroll.Cursor = Cursors.Default;
            vScroll.Scroll += vScroll_Scroll;

            // 
            // CaretTimer
            // 
            CaretTimer.Enabled = true;
            CaretTimer.Interval = 500;
            CaretTimer.Tick += CaretTimer_Tick;
            // 
            // tooltip
            // 
            tooltip.AutoPopDelay = 50000;
            tooltip.InitialDelay = 0;
            tooltip.ReshowDelay = 1000;
            tooltip.ShowAlways = true;
            // 
            // TopThumb
            // 
            TopThumb.BackColor = SystemColors.Control;
            TopThumb.Cursor = Cursors.HSplit;
            TopThumb.Location = new Point(101, 17);
            TopThumb.Name = "TopThumb";
            TopThumb.Size = new Size(16, 8);
            TopThumb.TabIndex = 3;
            TopThumb.Visible = false;
            // 
            // LeftThumb
            // 
            LeftThumb.BackColor = SystemColors.Control;
            LeftThumb.Cursor = Cursors.VSplit;
            LeftThumb.Location = new Point(423, 17);
            LeftThumb.Name = "LeftThumb";
            LeftThumb.Size = new Size(8, 16);
            LeftThumb.TabIndex = 3;
            LeftThumb.Visible = false;
            // 
            // EditViewControl
            // 
            try
            {
                AllowDrop = true;
            }
            catch
            {
                //	Console.WriteLine ("error in editview allowdrop {0}",x.Message);
            }

            Controls.AddRange(new Control[]
                              {
                                  IntelliMouse
                              }
                );
            Size = new Size(0, 0);
            LostFocus += EditViewControl_Leave;
            GotFocus += EditViewControl_Enter;
            ResumeLayout(false);
        }


        public void InsertAutolistText()
        {
            string text = AutoList.SelectedText;
            var tr = new TextRange
                     {
                         FirstRow = Caret.Position.Y,
                         LastRow = Caret.Position.Y,
                         FirstColumn = AutoListStartPos.X,
                         LastColumn = Caret.Position.X
                     };

            // Vic says - why do we delete range here?
            // It seems to cause bugs.
            Document.DeleteRange(tr, true);
            //Caret.Position.X = AutoListStartPos.X;
            InsertText(text);
            SetFocus();
        }

        private void MoveCaretToNextWord(bool Select)
        {
            int x = Caret.Position.X;
            int y = Caret.Position.Y;
            int StartType;
            bool found = false;
            if (x == Caret.CurrentRow.Text.Length)
            {
                StartType = 1;
            }
            else
            {
                string StartChar = Document[y].Text.Substring(Caret.Position.X, 1);
                StartType = CharType(StartChar);
            }


            while (y < Document.Count)
            {
                while (x < Document[y].Text.Length)
                {
                    string Char = Document[y].Text.Substring(x, 1);
                    int Type = CharType(Char);
                    if (Type != StartType)
                    {
                        if (Type == 1)
                        {
                            StartType = 1;
                        }
                        else
                        {
                            found = true;
                            break;
                        }
                    }
                    x++;
                }
                if (found)
                    break;
                x = 0;
                y++;
            }

            if (y >= Document.Count - 1)
            {
                y = Document.Count - 1;

                if (x >= Document[y].Text.Length)
                    x = Document[y].Text.Length - 1;

                if (x == - 1)
                    x = 0;
            }

            Caret.SetPos(new TextPoint(x, y));
            if (!Select)
                Selection.ClearSelection();
            if (Select)
            {
                Selection.MakeSelection();
            }


            ScrollIntoView();
        }

        public void InitGraphics()
        {
            Painter.InitGraphics();
        }


        private void MoveCaretToPrevWord(bool Select)
        {
            int x = Caret.Position.X;
            int y = Caret.Position.Y;
            int StartType;
            bool found = false;
            if (x == Caret.CurrentRow.Text.Length)
            {
                StartType = 1;
                x = Caret.CurrentRow.Text.Length - 1;
            }
            else
            {
                string StartChar = Document[y].Text.Substring(Caret.Position.X, 1);
                StartType = CharType(StartChar);
            }


            while (y >= 0)
            {
                while (x >= 0 && x < Document[y].Text.Length)
                {
                    string Char = Document[y].Text.Substring(x, 1);
                    int Type = CharType(Char);
                    if (Type != StartType)
                    {
                        found = true;

                        while (x > 0)
                        {
                            string Char2 = Document[y].Text.Substring(x, 1);
                            int Type2 = CharType(Char2);
                            if (Type2 != Type)
                            {
                                x++;
                                break;
                            }

                            x--;
                        }

                        break;
                    }
                    x--;
                }
                if (found)
                    break;

                if (y == 0)
                {
                    x = 0;
                    break;
                }
                y--;
                x = Document[y].Text.Length - 1;
            }


            Caret.SetPos(new TextPoint(x, y));
            if (!Select)
                Selection.ClearSelection();

            if (Select)
            {
                Selection.MakeSelection();
            }

            ScrollIntoView();
        }


        private void SetFocus()
        {
            Focus();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Displays the GotoLine dialog.
        /// </summary>
        public void ShowGotoLine()
        {
            var go = new GotoLineForm(this, Document.Count);
            //			if (this.TopLevelControl is Form)
            //				go.Owner=(Form)this.TopLevelControl;

            go.ShowDialog(TopLevelControl);
        }

        /// <summary>
        /// -
        /// </summary>
        public void ShowSettings()
        {
            //	SettingsForm se=new SettingsForm (this);
            //	se.ShowDialog();
        }

        /// <summary>
        /// Places the caret on a specified line and scrolls the caret into view.
        /// </summary>
        /// <param name="RowIndex">the zero based index of the line to jump to</param>
        public void GotoLine(int RowIndex)
        {
            if (RowIndex >= Document.Count)
                RowIndex = Document.Count - 1;

            if (RowIndex < 0)
                RowIndex = 0;

            Caret.Position.Y = RowIndex;
            Caret.Position.X = 0;
            Caret.CurrentRow.EnsureVisible();
            ClearSelection();
            ScrollIntoView();
            Redraw();
        }


        /// <summary>
        /// Clears the active selection.
        /// </summary>
        public void ClearSelection()
        {
            Selection.ClearSelection();
            Redraw();
        }

        /// <summary>
        /// Returns if a specified pixel position is over the current selection.
        /// </summary>
        /// <param name="x">X Position in pixels</param>
        /// <param name="y">Y Position in pixels</param>
        /// <returns>true if over selection othewise false</returns>
        public bool IsOverSelection(int x, int y)
        {
            TextPoint p = Painter.CharFromPixel(x, y);

            if (p.Y >= Selection.LogicalBounds.FirstRow && p.Y <=
                                                           Selection.LogicalBounds.LastRow && Selection.IsValid)
            {
                if (p.Y > Selection.LogicalBounds.FirstRow && p.Y <
                                                              Selection.LogicalBounds.LastRow && Selection.IsValid)
                    return true;
                if (p.Y == Selection.LogicalBounds.FirstRow &&
                    Selection.LogicalBounds.FirstRow ==
                    Selection.LogicalBounds.LastRow)
                {
                    if (p.X >= Selection.LogicalBounds.FirstColumn && p.X <=
                                                                      Selection.LogicalBounds.LastColumn)
                        return true;
                    return false;
                }
                if (p.X >= Selection.LogicalBounds.FirstColumn && p.Y ==
                                                                  Selection.LogicalBounds.FirstRow)
                    return true;
                if (p.X <= Selection.LogicalBounds.LastColumn && p.Y ==
                                                                 Selection.LogicalBounds.LastRow)
                    return true;
                return false;
            }

            return false;
            //no chance we are over Selection.LogicalBounds
        }

        /// <summary>
        /// Scrolls a given position in the text into view.
        /// </summary>
        /// <param name="Pos">Position in text</param>
        public void ScrollIntoView(TextPoint Pos)
        {
            TextPoint tmp = Caret.Position;
            Caret.Position = Pos;
            Caret.CurrentRow.EnsureVisible();
            ScrollIntoView();
            Caret.Position = tmp;
            Invalidate();
        }

        public void ScrollIntoView(int RowIndex)
        {
            Row r = Document[RowIndex];
            r.EnsureVisible();
            vScroll.Value = r.VisibleIndex;
            Invalidate();
        }

        /// <summary>
        /// Scrolls the caret into view.
        /// </summary>
        public void ScrollIntoView()
        {
            InitScrollbars();

            Caret.CropPosition();
            try
            {
                Row xtr2 = Caret.CurrentRow;
                if (xtr2.VisibleIndex >= View.FirstVisibleRow + View.VisibleRowCount -
                                         2)
                {
                    int Diff = Caret.CurrentRow.VisibleIndex - (View.FirstVisibleRow +
                                                                View.VisibleRowCount - 2) + View.FirstVisibleRow;
                    if (Diff > Document.VisibleRows.Count - 1)
                        Diff = Document.VisibleRows.Count - 1;

                    Row r = Document.VisibleRows[Diff];
                    int index = r.VisibleIndex;
                    if (index != - 1)
                        vScroll.Value = index;
                }
            }
            catch {}


            if (Caret.CurrentRow.VisibleIndex < View.FirstVisibleRow)
            {
                Row r = Caret.CurrentRow;
                int index = r.VisibleIndex;
                if (index != - 1)
                    vScroll.Value = index;
            }

            Row xtr = Caret.CurrentRow;


            int x;
            if (Caret.CurrentRow.IsCollapsedEndPart)
            {
                x = Painter.MeasureRow(xtr, Caret.Position.X).Width +
                    Caret.CurrentRow.Expansion_PixelStart;
                x -= Painter.MeasureRow(xtr, xtr.Expansion_StartChar).Width;

                if (x >= View.ClientAreaWidth + View.ClientAreaStart)
                    hScroll.Value = Math.Min(hScroll.Maximum, ((x - View.ClientAreaWidth)
                                                               /View.CharWidth) + 15);

                if (x < View.ClientAreaStart + 10)
                    hScroll.Value = Math.Max(hScroll.Minimum, ((x)/View.CharWidth) - 15)
                        ;
            }
            else
            {
                x = Painter.MeasureRow(xtr, Caret.Position.X).Width;

                if (x >= View.ClientAreaWidth + View.ClientAreaStart)
                    hScroll.Value = Math.Min(hScroll.Maximum, ((x - View.ClientAreaWidth)
                                                               /View.CharWidth) + 15);

                if (x < View.ClientAreaStart)
                    hScroll.Value = Math.Max(hScroll.Minimum, ((x)/View.CharWidth) - 15)
                        ;
            }
        }

        /// <summary>
        /// Moves the caret to the next line that has a bookmark.
        /// </summary>
        public void GotoNextBookmark()
        {
            int index = Document.GetNextBookmark(Caret.Position.Y);
            Caret.SetPos(new TextPoint(0, index));
            ScrollIntoView();
            Redraw();
        }


        /// <summary>
        /// Moves the caret to the previous line that has a bookmark.
        /// </summary>
        public void GotoPreviousBookmark()
        {
            int index = Document.GetPreviousBookmark(Caret.Position.Y);
            Caret.SetPos(new TextPoint(0, index));
            ScrollIntoView();
            Redraw();
        }

        /// <summary>
        /// Selects next occurance of the given pattern.
        /// </summary>
        /// <param name="Pattern">Pattern to find</param>
        /// <param name="MatchCase">Case sensitive</param>
        /// <param name="WholeWords">Match whole words only</param>
        /// <param name="UseRegEx"></param>
        public bool SelectNext(string Pattern, bool MatchCase, bool WholeWords,
                               bool UseRegEx)
        {
            string pattern = Pattern;
            for (int i = Caret.Position.Y; i < Document.Count; i++)
            {
                Row r = Document[i];

                string t = r.Text;
                if (WholeWords)
                {
                    string s = " " + r.Text + " ";
                    t = "";
                    pattern = " " + Pattern + " ";
                    foreach (char c in s)
                    {
                        if (".,+-*^\\/()[]{}@:;'?£$#%& \t=<>".IndexOf(c) >= 0)
                            t += " ";
                        else
                            t += c;
                    }
                }

                if (!MatchCase)
                {
                    t = t.ToLowerInvariant();
                    pattern = pattern.ToLowerInvariant();
                }

                int Col = t.IndexOf(pattern);

                int StartCol = Caret.Position.X;
                int StartRow = Caret.Position.Y;
                if ((Col >= StartCol) || (i > StartRow && Col >= 0))
                {
                    SelectPattern(i, Col, Pattern.Length);
                    return true;
                }
            }
            return false;
        }

        public bool ReplaceSelection(string text)
        {
            if (!Selection.IsValid)
                return false;

            int x = Selection.LogicalBounds.FirstColumn;
            int y = Selection.LogicalBounds.FirstRow;

            Selection.DeleteSelection();

            Caret.Position.X = x;
            Caret.Position.Y = y;

            InsertText(text);


            Selection.Bounds.FirstRow = y;
            Selection.Bounds.FirstColumn = x + text.Length;

            Selection.Bounds.LastRow = y;
            Selection.Bounds.LastColumn = x + text.Length;

            Caret.Position.X = x + text.Length;
            Caret.Position.Y = y;
            return true;
        }


        /// <summary>
        /// Toggles a bookmark on/off on the active row.
        /// </summary>
        public void ToggleBookmark()
        {
            Document[Caret.Position.Y].Bookmarked =
                !Document[Caret.Position.Y].Bookmarked;
            Redraw();
        }


        /// <summary>
        /// Deletes selected text if possible otherwise deletes forward. (delete key)
        /// </summary>
        public void Delete()
        {
            DeleteForward();
            Refresh();
        }

        /// <summary>
        /// Selects all text in the active document. (control + a)
        /// </summary>
        public void SelectAll()
        {
            Selection.SelectAll();
            Redraw();
        }


        /// <summary>
        /// Paste text from clipboard to current caret position. (control + v)
        /// </summary>
        public void Paste()
        {
            PasteText();
            Refresh();
        }

        /// <summary>
        /// Copies selected text to clipboard. (control + c)
        /// </summary>
        public void Copy()
        {
            CopyText();
        }

        /// <summary>
        /// Cuts selected text to clipboard. (control + x)
        /// </summary>
        public void Cut()
        {
            CopyText();
            Selection.DeleteSelection();
        }

        /// <summary>
        /// Removes the current row
        /// </summary>
        public void RemoveCurrentRow()
        {
            if (Caret.CurrentRow != null && Document.Count > 1)
            {
                Document.Remove(Caret.CurrentRow.Index, true);
                Document.ResetVisibleRows();
                Caret.CropPosition();
                Caret.CurrentRow.Text = Caret.CurrentRow.Text;
                Caret.CurrentRow.Parse(true);
                Document.ResetVisibleRows();
                ScrollIntoView();
                //this.Refresh ();
            }
        }

        public void CutClear()
        {
            if (Selection.IsValid)
                Cut();
            else
                RemoveCurrentRow();
        }

        /// <summary>
        /// Redo last undo action. (control + y)
        /// </summary>
        public void Redo()
        {
            TextPoint p = Document.Redo();
            if (p.X != - 1 && p.Y != - 1)
            {
                Caret.Position = p;
                Selection.ClearSelection();
                ScrollIntoView();
            }
        }

        /// <summary>
        /// Undo last edit action. (control + z)
        /// </summary>
        public void Undo()
        {
            TextPoint p = Document.Undo();
            if (p.X != - 1 && p.Y != - 1)
            {
                Caret.Position = p;
                Selection.ClearSelection();
                ScrollIntoView();
            }
        }

        /// <summary>
        /// Returns a point where x is the column and y is the row from a given pixel position.
        /// </summary>
        /// <param name="x">X Position in pixels</param>
        /// <param name="y">Y Position in pixels</param>
        /// <returns>Column and Rowindex</returns>
        public TextPoint CharFromPixel(int x, int y)
        {
            return Painter.CharFromPixel(x, y);
        }

        public void ShowFind()
        {
            if (FindReplaceDialog != null)
            {
                FindReplaceDialog.TopLevel = true;
                if (TopLevelControl is Form)
                {
                    FindReplaceDialog.Owner = (Form) TopLevelControl;
                }
                FindReplaceDialog.ShowFind();
            }
        }

        public void ShowReplace()
        {
            if (FindReplaceDialog != null)
            {
                FindReplaceDialog.TopLevel = true;
                if (TopLevelControl is Form)
                {
                    FindReplaceDialog.Owner = (Form) TopLevelControl;
                }
                FindReplaceDialog.ShowReplace();
            }
        }

        public void AutoListBeginLoad()
        {
            AutoList.BeginLoad();
        }

        public void AutoListEndLoad()
        {
            AutoList.EndLoad();
        }

        public void FindNext()
        {
            FindReplaceDialog.FindNext();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Returns true if the control is in overwrite mode.
        /// </summary>
        [Browsable(false)]
        public bool OverWrite
        {
            get { return _OverWrite; }
        }

        /// <summary>
        /// Returns True if the control contains a selected text.
        /// </summary>
        [Browsable(false)]
        public bool CanCopy
        {
            get { return Selection.IsValid; }
        }

        /// <summary>
        /// Returns true if there is any valid text data inside the Clipboard.
        /// </summary>
        [Browsable(false)]
        public bool CanPaste
        {
            get
            {
                string s = "";
                try
                {
                    IDataObject iData = Clipboard.GetDataObject();

                    if (iData != null)
                        if (iData.GetDataPresent(DataFormats.Text))
                        {
                            // Yes it is, so display it in a text box.
                            s = (String) iData.GetData(DataFormats.Text);
                        }

                    if (s != null)
                        return true;
                }
                catch {}
                return false;
            }
        }

        /// <summary>
        /// Returns true if the undobuffer contains one or more undo actions.
        /// </summary>
        [Browsable(false)]
        public bool CanUndo
        {
            get { return (Document.UndoStep > 0); }
        }

        /// <summary>
        /// Returns true if the control can redo the last undo action/s
        /// </summary>
        [Browsable(false)]
        public bool CanRedo
        {
            get { return (Document.UndoStep < Document.UndoBuffer.Count - 1); }
        }


        /// <summary>
        /// Gets the size (in pixels) of the font to use when rendering the the content.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public float FontSize
        {
            get { return _SyntaxBox.FontSize; }
        }


        /// <summary>
        /// Gets the indention style to use when inserting new lines.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public IndentStyle Indent
        {
            get { return _SyntaxBox.Indent; }
        }

        /// <summary>
        /// Gets the SyntaxDocument the control is currently attatched to.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        [Category("Content")]
        [Description(
            "The SyntaxDocument that is attatched to the contro")]
        public
            SyntaxDocument Document
        {
            get { return _SyntaxBox.Document; }
        }

        /// <summary>
        /// Gets the delay in MS before the tooltip is displayed when hovering a collapsed section.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public int TooltipDelay
        {
            get { return _SyntaxBox.TooltipDelay; }
        }

        // ROB: Required to support CollapsedBlockTooltipsEnabled
        public bool CollapsedBlockTooltipsEnabled
        {
            get { return _SyntaxBox.CollapsedBlockTooltipsEnabled; }
        }

        // END-ROB ----------------------------------------------------------


        /// <summary>
        /// Gets if the control is readonly.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public bool ReadOnly
        {
            get { return _SyntaxBox.ReadOnly; }
        }


        /// <summary>
        /// Gets the name of the font to use when rendering the control.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public string FontName
        {
            get { return _SyntaxBox.FontName; }
        }

        /// <summary>
        /// Gets if the control should render bracket matching.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public bool BracketMatching
        {
            get { return _SyntaxBox.BracketMatching; }
        }


        /// <summary>
        /// Gets if the control should render whitespace chars.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public bool VirtualWhitespace
        {
            get { return _SyntaxBox.VirtualWhitespace; }
        }


        /// <summary>
        /// Gets the Color of the horizontal separators (a'la visual basic 6).
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public Color SeparatorColor
        {
            get { return _SyntaxBox.SeparatorColor; }
        }


        /// <summary>
        /// Gets the text color to use when rendering bracket matching.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public Color BracketForeColor
        {
            get { return _SyntaxBox.BracketForeColor; }
        }


        /// <summary>
        /// Gets the back color to use when rendering bracket matching.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public Color BracketBackColor
        {
            get { return _SyntaxBox.BracketBackColor; }
        }


        /// <summary>
        /// Gets the back color to use when rendering the selected text.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public Color SelectionBackColor
        {
            get { return _SyntaxBox.SelectionBackColor; }
        }


        /// <summary>
        /// Gets the text color to use when rendering the selected text.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public Color SelectionForeColor
        {
            get { return _SyntaxBox.SelectionForeColor; }
        }

        /// <summary>
        /// Gets the back color to use when rendering the inactive selected text.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public Color InactiveSelectionBackColor
        {
            get { return _SyntaxBox.InactiveSelectionBackColor; }
        }


        /// <summary>
        /// Gets the text color to use when rendering the inactive selected text.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public Color InactiveSelectionForeColor
        {
            get { return _SyntaxBox.InactiveSelectionForeColor; }
        }


        /// <summary>
        /// Gets the color of the border between the gutter area and the line number area.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public Color GutterMarginBorderColor
        {
            get { return _SyntaxBox.GutterMarginBorderColor; }
        }


        /// <summary>
        /// Gets the color of the border between the line number area and the folding area.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public Color LineNumberBorderColor
        {
            get { return _SyntaxBox.LineNumberBorderColor; }
        }


        /// <summary>
        /// Gets the text color to use when rendering breakpoints.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public Color BreakPointForeColor
        {
            get { return _SyntaxBox.BreakPointForeColor; }
        }

        /// <summary>
        /// Gets the back color to use when rendering breakpoints.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public Color BreakPointBackColor
        {
            get { return _SyntaxBox.BreakPointBackColor; }
        }

        /// <summary>
        /// Gets the text color to use when rendering line numbers.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public Color LineNumberForeColor
        {
            get { return _SyntaxBox.LineNumberForeColor; }
        }

        /// <summary>
        /// Gets the back color to use when rendering line number area.
        /// </summary>
        public Color LineNumberBackColor
        {
            get { return _SyntaxBox.LineNumberBackColor; }
        }

        /// <summary>
        /// Gets the color of the gutter margin.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public Color GutterMarginColor
        {
            get { return _SyntaxBox.GutterMarginColor; }
        }

        /// <summary>
        /// Gets or Sets the background Color of the client area.
        /// </summary>
        [Category("Appearance")]
        [Description(
            "The background color of the client area")]
        public new Color
            BackColor
        {
            get { return _SyntaxBox.BackColor; }
            set { _SyntaxBox.BackColor = value; }
        }

        /// <summary>
        /// Gets the back color to use when rendering the active line.
        /// </summary>
        public Color HighLightedLineColor
        {
            get { return _SyntaxBox.HighLightedLineColor; }
        }

        /// <summary>
        /// Get if the control should highlight the active line.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public bool HighLightActiveLine
        {
            get { return _SyntaxBox.HighLightActiveLine; }
        }

        /// <summary>
        /// Get if the control should render whitespace chars.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public bool ShowWhitespace
        {
            get { return _SyntaxBox.ShowWhitespace; }
        }


        /// <summary>
        /// Get if the line number margin is visible or not.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public bool ShowLineNumbers
        {
            get { return _SyntaxBox.ShowLineNumbers; }
        }


        /// <summary>
        /// Get if the gutter margin is visible or not.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public bool ShowGutterMargin
        {
            get { return _SyntaxBox.ShowGutterMargin; }
        }

        /// <summary>
        /// Get the Width of the gutter margin (in pixels)
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public int GutterMarginWidth
        {
            get { return _SyntaxBox.GutterMarginWidth; }
        }


        /// <summary>
        /// Get the numbers of space chars in a tab.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public int TabSize
        {
            get { return _SyntaxBox.TabSize; }
        }

        /// <summary>
        /// Get whether or not TabsToSpaces is turned on.
        /// </summary>
        public bool TabsToSpaces
        {
            get { return _SyntaxBox.TabsToSpaces; }
        }


        /// <summary>
        /// Get if the control should render 'Tab guides'
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public bool ShowTabGuides
        {
            get { return _SyntaxBox.ShowTabGuides; }
        }

        /// <summary>
        /// Gets the color to use when rendering whitespace chars.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public Color WhitespaceColor
        {
            get { return _SyntaxBox.WhitespaceColor; }
        }

        /// <summary>
        /// Gets the color to use when rendering tab guides.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public Color TabGuideColor
        {
            get { return _SyntaxBox.TabGuideColor; }
        }

        /// <summary>
        /// Get the color to use when rendering bracket matching borders.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        /// <remarks>
        /// NOTE: Use the Color.Transparent turn off the bracket match borders.
        /// </remarks>
        public Color BracketBorderColor
        {
            get { return _SyntaxBox.BracketBorderColor; }
        }


        /// <summary>
        /// Get the color to use when rendering Outline symbols.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public Color OutlineColor
        {
            get { return _SyntaxBox.OutlineColor; }
        }


        /// <summary>
        /// Positions the AutoList
        /// </summary>
        [Category("Behavior")]
        public TextPoint AutoListPosition
        {
            get { return AutoListStartPos; }
            set { AutoListStartPos = value; }
        }

        /// <summary>
        /// Positions the InfoTip
        /// </summary>
        [Category("Behavior")]
        public TextPoint InfoTipPosition
        {
            get { return InfoTipStartPos; }
            set { InfoTipStartPos = value; }
        }


        /// <summary>
        /// Gets or Sets if the intellisense list is visible.
        /// </summary>
        [Category("Behavior")]
        public bool AutoListVisible
        {
            set
            {
                CreateAutoList();
                if (AutoList == null)
                    return;

                if (value)
                {
                    AutoList.TopLevel = true;
                    AutoList.BringToFront();

                    // ROB: Fuck knows what I did to cause having to do this..
                    // Show it off the screen, let the painter position it.
                    AutoList.Location = new Point(-16000, -16000);
                    AutoList.Show();
                    InfoTip.BringToFront();
                    if (TopLevelControl is Form)
                    {
                        AutoList.Owner = (Form) TopLevelControl;
                    }
                }
                else
                {
                    // ROB: Another hack.
                    AutoList.Hide();
                }

                _AutoListVisible = value;
                InfoTip.BringToFront();

                Redraw();
            }
            get { return _AutoListVisible; }
        }

        /// <summary>
        /// Gets or Sets if the infotip is visible
        /// </summary>
        [Category("Behavior")]
        public bool InfoTipVisible
        {
            set
            {
                CreateInfoTip();
                if (InfoTip == null)
                    return;

                if (value)
                {
                    InfoTip.TopLevel = true;
                    AutoList.BringToFront();
                    if (TopLevelControl is Form)
                    {
                        InfoTip.Owner = (Form) TopLevelControl;
                    }
                }

                InfoTip.BringToFront();

                _InfoTipVisible = value;

                if (InfoTip != null && value)
                {
                    InfoTip.Init();
                }

                // ROB: Cludge for infotip bug, whereby infotip does not close when made invisible..
                if (_InfoTip != null && !value)
                {
                    _InfoTip.Visible = false;
                }
            }
            get { return _InfoTipVisible; }
        }

        /// <summary>
        /// Get if the control should use smooth scrolling.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public bool SmoothScroll
        {
            get { return _SyntaxBox.SmoothScroll; }
        }

        /// <summary>
        /// Get the number of pixels the screen should be scrolled per frame when using smooth scrolling.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public int SmoothScrollSpeed
        {
            get { return _SyntaxBox.SmoothScrollSpeed; }
        }


        /// <summary>
        /// Get if the control should parse all text when text is pasted from the clipboard.
        /// The value is retrived from the owning Syntaxbox control.
        /// </summary>
        public bool ParseOnPaste
        {
            get { return _SyntaxBox.ParseOnPaste; }
        }


        /// <summary>
        /// Gets the Caret object.
        /// </summary>
        public Caret Caret
        {
            get { return _Caret; }
        }

        /// <summary>
        /// Gets the Selection object.
        /// </summary>
        public Selection Selection
        {
            get { return _Selection; }
        }

        #endregion

        #region eventhandlers

        private int OldWidth;

        private void OnClipboardUpdated(CopyEventArgs e)
        {
            if (ClipboardUpdated != null)
                ClipboardUpdated(this, e);
        }


        private void OnRowMouseDown(RowMouseEventArgs e)
        {
            if (RowMouseDown != null)
                RowMouseDown(this, e);
        }

        private void OnRowMouseMove(RowMouseEventArgs e)
        {
            if (RowMouseMove != null)
                RowMouseMove(this, e);
        }

        private void OnRowMouseUp(RowMouseEventArgs e)
        {
            if (RowMouseUp != null)
                RowMouseUp(this, e);
        }

        private void OnRowClick(RowMouseEventArgs e)
        {
            if (RowClick != null)
                RowClick(this, e);
        }

        private void OnRowDoubleClick(RowMouseEventArgs e)
        {
            if (RowDoubleClick != null)
                RowDoubleClick(this, e);
        }


        protected override void OnLoad(EventArgs e)
        {
            DoResize();
            Refresh();
        }


        public void OnParse()
        {
            Redraw();
        }

        public void OnChange()
        {
            if (Caret.Position.Y > Document.Count - 1)
            {
                Caret.Position.Y = Document.Count - 1;
                //this.Caret.MoveAbsoluteHome (false);
                ScrollIntoView();
            }

            try
            {
                if (VirtualWhitespace == false && Caret.CurrentRow != null &&
                    Caret.Position.X > Caret.CurrentRow.Text.Length)
                {
                    Caret.Position.X = Caret.CurrentRow.Text.Length;
                    Redraw();
                }
            }
            catch {}


            if (!ContainsFocus)
            {
                Selection.ClearSelection();
            }


            if (Selection.LogicalBounds.FirstRow > Document.Count)
            {
                Selection.Bounds.FirstColumn = Caret.Position.X;
                Selection.Bounds.LastColumn = Caret.Position.X;
                Selection.Bounds.FirstRow = Caret.Position.Y;
                Selection.Bounds.LastRow = Caret.Position.Y;
            }

            if (Selection.LogicalBounds.LastRow > Document.Count)
            {
                Selection.Bounds.FirstColumn = Caret.Position.X;
                Selection.Bounds.LastColumn = Caret.Position.X;
                Selection.Bounds.FirstRow = Caret.Position.Y;
                Selection.Bounds.LastRow = Caret.Position.Y;
            }

            Redraw();
        }





        /// <summary>
        /// Overrides the default OnKeyDown
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            _KeyDownHandled = e.Handled;

            if (e.KeyCode == Keys.Escape && (InfoTipVisible || AutoListVisible))
            {
                InfoTipVisible = false;
                AutoListVisible = false;
                e.Handled = true;
                Redraw();
                return;
            }

            if (!e.Handled && InfoTipVisible && InfoTip.Count > 1)
            {
                //move infotip selection
                if (e.KeyCode == Keys.Up)
                {
                    _SyntaxBox.InfoTipSelectedIndex++;
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.Down)
                {
                    _SyntaxBox.InfoTipSelectedIndex--;

                    e.Handled = true;
                    return;
                }
            }

            if (!e.Handled && AutoListVisible)
            {
                //move autolist selection
                if ((e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode ==
                                                                       Keys.PageUp || e.KeyCode == Keys.PageDown))
                {
                    AutoList.SendKey((int) e.KeyCode);
                    e.Handled = true;
                    return;
                }



                //inject inser text from autolist
                if (mKeysThatInsertFromAutolist.Contains(e.KeyCode) || 
                    /////////////HACK ALERT:  Won't work on international keyboards!!!!
                    (e.KeyCode == Keys.D9 && e.Modifiers == Keys.Shift))
                {
                    string s = AutoList.SelectedText;
                    if (s != "")
                        InsertAutolistText();
                    AutoListVisible = false;
                    e.Handled = true;
                    Redraw();
                    return;
                }
            }


            if (!e.Handled)
            {
                //do keyboard actions
                foreach (KeyboardAction ka in _SyntaxBox.KeyboardActions)
                {
                    if (!ReadOnly || ka.AllowReadOnly)
                    {
                        if ((ka.Key == (Keys) (int) e.KeyCode) && ka.Alt == e.Alt && ka.Shift
                                                                                     == e.Shift &&
                            ka.Control == e.Control)
                            ka.Action();
                        //if keys match , call action delegate
                    }
                }


                //------------------------------------------------------------------------------------------------------------


                switch ((Keys) (int) e.KeyCode)
                {
                    case Keys.ShiftKey:
                    case Keys.ControlKey:
                    case Keys.Alt:
                        return;

                    case Keys.Down:
                        if (e.Control)
                            ScrollScreen(1);
                        else
                        {
                            Caret.MoveDown(e.Shift);
                            Redraw();
                        }
                        break;
                    case Keys.Up:
                        if (e.Control)
                            ScrollScreen(- 1);
                        else
                        {
                            Caret.MoveUp(e.Shift);
                        }
                        Redraw();
                        break;
                    case Keys.Left:
                        {
                            if (e.Control)
                            {
                                MoveCaretToPrevWord(e.Shift);
                            }
                            else
                            {
                                Caret.MoveLeft(e.Shift);
                            }
                        }
                        Redraw();
                        break;
                    case Keys.Right:
                        {
                            if (e.Control)
                            {
                                MoveCaretToNextWord(e.Shift);
                            }
                            else
                            {
                                Caret.MoveRight(e.Shift);
                            }
                        }
                        Redraw();
                        break;
                    case Keys.End:
                        if (e.Control)
                            Caret.MoveAbsoluteEnd(e.Shift);
                        else
                            Caret.MoveEnd(e.Shift);
                        Redraw();
                        break;
                    case Keys.Home:
                        if (e.Control)
                            Caret.MoveAbsoluteHome(e.Shift);
                        else
                            Caret.MoveHome(e.Shift);
                        Redraw();
                        break;
                    case Keys.PageDown:
                        Caret.MoveDown(View.VisibleRowCount - 2, e.Shift);
                        Redraw();
                        break;
                    case Keys.PageUp:
                        Caret.MoveUp(View.VisibleRowCount - 2, e.Shift);
                        Redraw();
                        break;

                    default:
                        break;
                }


                //dont do if readonly
                if (!ReadOnly)
                {
                    switch ((Keys) (int) e.KeyCode)
                    {
                        case Keys.Enter:
                            {
                                if (e.Control)
                                {
                                    if (Caret.CurrentRow.CanFold)
                                    {
                                        Caret.MoveHome(false);
                                        Document.ToggleRow(Caret.CurrentRow);
                                        Redraw();
                                    }
                                }
                                else
                                    InsertEnter();
                                break;
                            }
                        case Keys.Back:
                            if (!e.Control)
                                DeleteBackwards();
                            else
                            {
                                if (Selection.IsValid)
                                    Selection.DeleteSelection();
                                else
                                {
                                    Selection.ClearSelection();
                                    MoveCaretToPrevWord(true);
                                    Selection.DeleteSelection();
                                }
                                Caret.CurrentRow.Parse(true);
                            }

                            break;
                        case Keys.Delete:
                            {
                                if (!e.Control && !e.Alt && !e.Shift)
                                {
                                    Delete();
                                }
                                else if (e.Control && !e.Alt && !e.Shift)
                                {
                                    if (Selection.IsValid)
                                    {
                                        Cut();
                                    }
                                    else
                                    {
                                        Selection.ClearSelection();
                                        MoveCaretToNextWord(true);
                                        Selection.DeleteSelection();
                                    }
                                    Caret.CurrentRow.Parse(true);
                                }
                                break;
                            }
                        case Keys.Insert:
                            {
                                if (!e.Control && !e.Alt && !e.Shift)
                                {
                                    _OverWrite = !_OverWrite;
                                }
                                break;
                            }
                        case Keys.Tab:
                            {
                                if (!Selection.IsValid)
                                {
                                    // ROB: Implementation of .TabsToSpaces
                                    if (!TabsToSpaces)
                                    {
                                        InsertText("\t");
                                    }
                                    else
                                    {
                                        InsertText(new string(' ', TabSize));
                                    }
                                    // ROB-END
                                }

                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }
                }
                Caret.Blink = true;
                //this.Redraw ();
            }
        }

        /// <summary>
        /// Overrides the default OnKeyPress
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);


            if (!e.Handled && !_KeyDownHandled && e.KeyChar != (char) 127)
            {
                if ((e.KeyChar) < 32)
                    return;

                if (!ReadOnly)
                {
                    switch ((Keys) (int) e.KeyChar)
                    {
                        default:
                            {
                                InsertText(e.KeyChar.ToString
                                               (CultureInfo.InvariantCulture)
                                    );
                                if (Indent == IndentStyle.Scope || Indent ==
                                                                   IndentStyle.Smart)
                                {
                                    if (Caret.CurrentRow.ShouldOutdent)
                                    {
                                        OutdentEndRow();
                                    }
                                }
                                break;
                            }
                    }
                }
            }

            if (AutoListVisible && !e.Handled && _SyntaxBox.AutoListAutoSelect)
            {
                string s = Caret.CurrentRow.Text;
                try
                {
                    if (Caret.Position.X - AutoListStartPos.X >= 0)
                    {
                        s = s.Substring(AutoListStartPos.X, Caret.Position.X -
                                                            AutoListStartPos.X);
                        AutoList.SelectItem(s);
                    }
                }
                catch {}
            }
        }

        /// <summary>
        /// Overrides the default OnMouseDown
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            MouseX = e.X;
            MouseY = e.Y;

            SetFocus();
            base.OnMouseDown(e);
            TextPoint pos = Painter.CharFromPixel(e.X, e.Y);
            Row row = null;
            if (pos.Y >= 0 && pos.Y < Document.Count)
                row = Document[pos.Y];

            #region RowEvent

            var rea = new RowMouseEventArgs {Row = row, Button = e.Button, MouseX = MouseX, MouseY = MouseY};
            if (e.X >= View.TextMargin - 7)
            {
                rea.Area = RowArea.TextArea;
            }
            else if (e.X < View.GutterMarginWidth)
            {
                rea.Area = RowArea.GutterArea;
            }
            else if (e.X < View.LineNumberMarginWidth +
                           View.GutterMarginWidth)
            {
                rea.Area = RowArea.LineNumberArea;
            }
            else if (e.X < View.TextMargin - 7)
            {
                rea.Area = RowArea.FoldingArea;
            }

            OnRowMouseDown(rea);

            #endregion

            try
            {
                Row r2 = Document[pos.Y];
                if (r2 != null)
                {
                    if (e.X >= r2.Expansion_PixelEnd && r2.IsCollapsed)
                    {
                        if (r2.expansion_StartSpan != null)
                        {
                            if (r2.expansion_StartSpan.StartRow != null &&
                                r2.expansion_StartSpan.EndRow != null &&
                                r2.expansion_StartSpan.Expanded == false)
                            {
                                if (!IsOverSelection(e.X, e.Y))
                                {
                                    Caret.Position.X = pos.X;
                                    Caret.Position.Y = pos.Y;
                                    Selection.ClearSelection();

                                    Row r3 = r2.Expansion_EndRow;
                                    int x3 = r3.Expansion_StartChar;

                                    Caret.Position.X = x3;
                                    Caret.Position.Y = r3.Index;
                                    Selection.MakeSelection();

                                    Redraw();
                                    View.Action = EditAction.SelectText;

                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                //this is untested code...
            }

            bool shift = NativeMethods.IsKeyPressed(Keys.ShiftKey);

            if (e.X > View.TotalMarginWidth)
            {
                if (e.X > View.TextMargin - 8)
                {
                    if (!IsOverSelection(e.X, e.Y))
                    {
                        //selecting
                        if (e.Button == MouseButtons.Left)
                        {
                            if (!shift)
                            {
                                TextPoint tp = pos;
                                Word w = Document.GetWordFromPos(tp);
                                if (w != null && w.Pattern != null && w.Pattern.Category !=
                                                                      null)
                                {
                                    var pe = new WordMouseEventArgs
                                    {
                                        Pattern = w.Pattern,
                                        Button = e.Button,
                                        Cursor = Cursors.Hand,
                                        Word = w
                                    };

                                    _SyntaxBox.OnWordMouseDown(ref pe);

                                    Cursor = pe.Cursor;
                                    return;
                                }

                                View.Action = EditAction.SelectText;
                                Caret.SetPos(pos);
                                Selection.ClearSelection();
                                Caret.Blink = true;
                                Redraw();
                            }
                            else
                            {
                                View.Action = EditAction.SelectText;
                                Caret.SetPos(pos);
                                Selection.MakeSelection();
                                Caret.Blink = true;
                                Redraw();
                            }
                        }
                    }
                }
                else
                {
                    if (row != null)
                        if (row.expansion_StartSpan != null)
                        {
                            Caret.SetPos(new TextPoint(0, pos.Y));
                            Selection.ClearSelection();
                            Document.ToggleRow(row);
                            Redraw();
                        }
                }
            }
            else
            {
                if (e.X < View.GutterMarginWidth)
                {
                    if (_SyntaxBox.AllowBreakPoints)
                    {
                        Row r = Document[Painter.CharFromPixel(e.X, e.Y).Y];
                        r.Breakpoint = !r.Breakpoint;
                        Redraw();
                    }
                    else
                    {
                        Row r = Document[Painter.CharFromPixel(e.X, e.Y).Y];
                        r.Breakpoint = false;
                        Redraw();
                    }
                }
                else
                {
                    View.Action = EditAction.SelectText;
                    Caret.SetPos(Painter.CharFromPixel(0, e.Y));
                    Selection.ClearSelection();
                    Caret.MoveDown(true);

                    Redraw();
                }
            }
            SetMouseCursor(e.X, e.Y);
        }

        /// <summary>
        /// Overrides the default OnMouseMove
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            MouseX = e.X;
            MouseY = e.Y;

            TextPoint pos = Painter.CharFromPixel(e.X, e.Y);
            Row row = null;
            if (pos.Y >= 0 && pos.Y < Document.Count)
                row = Document[pos.Y];

            #region RowEvent

            var rea = new RowMouseEventArgs {Row = row, Button = e.Button, MouseX = MouseX, MouseY = MouseY};
            if (e.X >= View.TextMargin - 7)
            {
                rea.Area = RowArea.TextArea;
            }
            else if (e.X < View.GutterMarginWidth)
            {
                rea.Area = RowArea.GutterArea;
            }
            else if (e.X < View.LineNumberMarginWidth +
                           View.GutterMarginWidth)
            {
                rea.Area = RowArea.LineNumberArea;
            }
            else if (e.X < View.TextMargin - 7)
            {
                rea.Area = RowArea.FoldingArea;
            }

            OnRowMouseMove(rea);

            #endregion

            try
            {
                if (Document != null)
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        if (View.Action == EditAction.SelectText)
                        {
                            //Selection ACTIONS!!!!!!!!!!!!!!
                            Caret.Blink = true;
                            Caret.SetPos(pos);
                            if (e.X <= View.TotalMarginWidth)
                                Caret.MoveDown(true);
                            Caret.CropPosition();
                            Selection.MakeSelection();
                            ScrollIntoView();
                            Redraw();
                        }
                        else if (View.Action == EditAction.None)
                        {
                            if (IsOverSelection(e.X, e.Y))
                            {
                                BeginDragDrop();
                            }
                        }
                        else if (View.Action == EditAction.DragText) {}
                    }
                    else
                    {
                        TextPoint p = pos;
                        Row r = Document[p.Y];
                        bool DidShow = false;

                        if (r != null)
                        {
                            if (e.X >= r.Expansion_PixelEnd && r.IsCollapsed)
                            {
                                // ROB: Added check for Collapsed tooltips.
                                if (CollapsedBlockTooltipsEnabled)
                                {
                                    if (r.expansion_StartSpan != null)
                                    {
                                        if (r.expansion_StartSpan.StartRow != null &&
                                            r.expansion_StartSpan.EndRow != null &&
                                            r.expansion_StartSpan.Expanded == false)
                                        {
                                            string t = "";
                                            int j = 0;
                                            for (int i = r.expansion_StartSpan.StartRow.Index;
                                                 i <=
                                                 r.expansion_StartSpan.EndRow.Index;
                                                 i++)
                                            {
                                                if (j > 0)
                                                    t += "\n";
                                                Row tmp = Document[i];
                                                string tmpstr = tmp.Text.Replace("\t", "    ");
                                                t += tmpstr;
                                                if (j > 20)
                                                {
                                                    t += "...";
                                                    break;
                                                }

                                                j++;
                                            }

                                            //tooltip.res
                                            tooltip.InitialDelay = TooltipDelay;
                                            if (tooltip.GetToolTip(this) != t)
                                                tooltip.SetToolTip(this, t);
                                            tooltip.Active = true;
                                            DidShow = true;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Word w = Document.GetFormatWordFromPos(p);
                                if (w != null)
                                {
                                    if (w.InfoTip != null)
                                    {
                                        tooltip.InitialDelay = TooltipDelay;
                                        if (tooltip.GetToolTip(this) != w.InfoTip)
                                            tooltip.SetToolTip(this, w.InfoTip);
                                        tooltip.Active = true;
                                        DidShow = true;
                                    }
                                }
                            }
                        }

                        if (tooltip != null)
                        {
                            if (!DidShow)
                                tooltip.SetToolTip(this, "");
                        }
                    }

                    SetMouseCursor(e.X, e.Y);
                    base.OnMouseMove(e);
                }
            }
            catch {}
        }

        /// <summary>
        /// Overrides the default OnMouseUp
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            MouseX = e.X;
            MouseY = e.Y;

            TextPoint pos = Painter.CharFromPixel(e.X, e.Y);
            Row row = null;
            if (pos.Y >= 0 && pos.Y < Document.Count)
                row = Document[pos.Y];

            #region RowEvent

            var rea = new RowMouseEventArgs {Row = row, Button = e.Button, MouseX = MouseX, MouseY = MouseY};
            if (e.X >= View.TextMargin - 7)
            {
                rea.Area = RowArea.TextArea;
            }
            else if (e.X < View.GutterMarginWidth)
            {
                rea.Area = RowArea.GutterArea;
            }
            else if (e.X < View.LineNumberMarginWidth +
                           View.GutterMarginWidth)
            {
                rea.Area = RowArea.LineNumberArea;
            }
            else if (e.X < View.TextMargin - 7)
            {
                rea.Area = RowArea.FoldingArea;
            }

            OnRowMouseUp(rea);

            #endregion

            if (View.Action == EditAction.None)
            {
                if (e.X > View.TotalMarginWidth)
                {
                    if (IsOverSelection(e.X, e.Y) && e.Button == MouseButtons.Left)
                    {
                        View.Action = EditAction.SelectText;
                        Caret.SetPos(Painter.CharFromPixel(e.X, e.Y));
                        Selection.ClearSelection();
                        Redraw();
                    }
                }
            }

            View.Action = EditAction.None;
            base.OnMouseUp(e);
        }

        /// <summary>
        /// Overrides the default OnMouseWheel
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int l = SystemInformation.MouseWheelScrollLines;
            ScrollScreen(- (e.Delta/120)*l);

            base.OnMouseWheel(e);
        }

        /// <summary>
        /// Overrides the default OnPaint
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Document != null && Width > 0 && Height > 0)
            {
                Painter.RenderAll(e.Graphics);
            }
        }

        /// <summary>
        /// Overrides the default OnResize
        /// </summary>
        /// <param name="e"></param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            DoResize();
        }

        /// <summary>
        /// Overrides the default OnDragOver
        /// </summary>
        /// <param name="drgevent"></param>
        protected override void OnDragOver(DragEventArgs
                                               drgevent)
        {
            if (!ReadOnly)
            {
                if (Document != null)
                {
                    View.Action = EditAction.DragText;

                    Point pt = PointToClient(new Point(drgevent.X, drgevent.Y));

                    int x = pt.X;
                    int y = pt.Y;

                    //	drgevent.Effect = DragDropEffects.All  ;
                    //Caret.Position = Painter.CharFromPixel(x,y);

                    drgevent.Effect = (drgevent.KeyState & 8) == 8 ? DragDropEffects.Copy : DragDropEffects.Move;
                    Caret.SetPos(Painter.CharFromPixel(x, y));
                    Redraw();
                }
            }
            else
            {
                drgevent.Effect = DragDropEffects.None;
            }
        }

        /// <summary>
        /// Overrides the default OnDragDrop
        /// </summary>
        /// <param name="drgevent"></param>
        protected override void OnDragDrop(DragEventArgs
                                               drgevent)
        {
            if (!ReadOnly)
            {
                if (Document != null)
                {
                    View.Action = EditAction.None;
                    int SelStart = Selection.LogicalSelStart;
                    int DropStart = Document.PointToIntPos(Caret.Position);


                    object dataAsObject = drgevent.Data.GetData(typeof (string));
                    string s = null;
                    int SelLen = 0;

                    if (dataAsObject != null)
                    {
                        s = dataAsObject.ToString();
                        SelLen = s.Length;
                    }
                    //int SelLen=s.Replace ("\r\n","\n").Length ;


                    if (DropStart >= SelStart && DropStart <= SelStart + Math.Abs
                                                                             (Selection.SelLength))
                        DropStart = SelStart;
                    else if (DropStart >= SelStart + SelLen)
                        DropStart -= SelLen;


                    Document.StartUndoCapture();
                    if ((drgevent.KeyState & 8) == 0)
                    {
                        _SyntaxBox.Selection.DeleteSelection();
                        Caret.Position = Document.IntPosToPoint(DropStart);
                    }

                    Document.InsertText(s, Caret.Position.X, Caret.Position.Y);
                    Document.EndUndoCapture();

                    Selection.SelStart = Document.PointToIntPos(Caret.Position);
                    Selection.SelLength = SelLen;
                    Document.ResetVisibleRows();
                    ScrollIntoView();
                    Redraw();
                    drgevent.Effect = DragDropEffects.All;

                    if (ParseOnPaste)
                        Document.ParseAll(true);

                    View.Action = EditAction.None;
                }
            }
        }


        /// <summary>
        ///  Overrides the default OnDragEnter
        /// </summary>
        /// <param name="drgevent"></param>
        protected override void OnDragEnter(DragEventArgs
                                                drgevent) {}

        /// <summary>
        ///  Overrides the default OnDragLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDragLeave(EventArgs e)
        {
            View.Action = EditAction.None;
        }

        /// <summary>
        ///  Overrides the default OnDoubleClick
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDoubleClick(EventArgs e)
        {
            TextPoint pos = Painter.CharFromPixel(MouseX, MouseY);
            Row row = null;
            if (pos.Y >= 0 && pos.Y < Document.Count)
                row = Document[pos.Y];

            #region RowEvent

            var rea = new RowMouseEventArgs {Row = row, Button = MouseButtons.None, MouseX = MouseX, MouseY = MouseY};
            if (MouseX >= View.TextMargin - 7)
            {
                rea.Area = RowArea.TextArea;
            }
            else if (MouseX < View.GutterMarginWidth)
            {
                rea.Area = RowArea.GutterArea;
            }
            else if (MouseX < View.LineNumberMarginWidth +
                              View.GutterMarginWidth)
            {
                rea.Area = RowArea.LineNumberArea;
            }
            else if (MouseX < View.TextMargin - 7)
            {
                rea.Area = RowArea.FoldingArea;
            }

            OnRowDoubleClick(rea);

            #endregion

            try
            {
                Row r2 = Document[pos.Y];
                if (r2 != null)
                {
                    if (MouseX >= r2.Expansion_PixelEnd && r2.IsCollapsed)
                    {
                        if (r2.expansion_StartSpan != null)
                        {
                            if (r2.expansion_StartSpan.StartRow != null &&
                                r2.expansion_StartSpan.EndRow != null &&
                                r2.expansion_StartSpan.Expanded == false)
                            {
                                r2.Expanded = true;
                                Document.ResetVisibleRows();
                                Redraw();
                                return;
                            }
                        }
                    }
                }
            }
            catch
            {
                //this is untested code...
            }

            if (MouseX > View.TotalMarginWidth)
                SelectCurrentWord();
        }

        protected override void OnClick(EventArgs e)
        {
            TextPoint pos = Painter.CharFromPixel(MouseX, MouseY);
            Row row = null;
            if (pos.Y >= 0 && pos.Y < Document.Count)
                row = Document[pos.Y];

            #region RowEvent

            var rea = new RowMouseEventArgs {Row = row, Button = MouseButtons.None, MouseX = MouseX, MouseY = MouseY};
            if (MouseX >= View.TextMargin - 7)
            {
                rea.Area = RowArea.TextArea;
            }
            else if (MouseX < View.GutterMarginWidth)
            {
                rea.Area = RowArea.GutterArea;
            }
            else if (MouseX < View.LineNumberMarginWidth +
                              View.GutterMarginWidth)
            {
                rea.Area = RowArea.LineNumberArea;
            }
            else if (MouseX < View.TextMargin - 7)
            {
                rea.Area = RowArea.FoldingArea;
            }

            OnRowClick(rea);

            #endregion
        }

        private void vScroll_Scroll(object sender,
                                    ScrollEventArgs e)
        {
            SetMaxHorizontalScroll();
            _InfoTipVisible = false;
            _AutoListVisible = false;

            SetFocus();


            int diff = e.NewValue - vScroll.Value;
            if ((diff == - 1 || diff == 1) && (e.Type ==
                                               ScrollEventType.SmallDecrement || e.Type ==
                                                                                 ScrollEventType.SmallIncrement))
            {
                ScrollScreen(diff);
            }
            else
            {
                Invalidate();
            }
        }

        private void hScroll_Scroll(object sender,
                                    ScrollEventArgs e)
        {
            _InfoTipVisible = false;
            _AutoListVisible = false;

            SetFocus();
            Invalidate();
        }

        private void CaretTimer_Tick(object sender, EventArgs e)
        {
            Caret.Blink = !Caret.Blink;
            RedrawCaret();
        }


        private void AutoListDoubleClick(object sender, EventArgs e)
        {
            string s = AutoList.SelectedText;
            if (s != "")
                InsertAutolistText();
            AutoListVisible = false;
            Redraw();
        }


        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (tooltip != null)
                tooltip.RemoveAll();
        }

        private void CaretChanged(object s, EventArgs e)
        {
            OnCaretChange();
        }

        private void EditViewControl_Leave(object sender, EventArgs e)
        {
            RemoveFocus();
        }

        private void EditViewControl_Enter(object sender, EventArgs e)
        {
            CaretTimer.Enabled = true;
        }

        private void SelectionChanged(object s, EventArgs e)
        {
            OnSelectionChange();
        }

        private void OnCaretChange()
        {
            if (CaretChange != null)
                CaretChange(this, null);
        }

        private void OnSelectionChange()
        {
            if (SelectionChange != null)
                SelectionChange(this, null);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible == false)
                RemoveFocus();

            base.OnVisibleChanged(e);
            DoResize();
        }

        #endregion
    }
}