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
using Alsing.SourceCode;

namespace Alsing.Windows.Forms.SyntaxBox
{
    /// <summary>
    /// Selection class used by the SyntaxBoxControl
    /// </summary>
    public class Selection
    {
        /// <summary>
        /// Event fired when the selection has changed.
        /// </summary>
        public event EventHandler Change = null;

        private void PositionChange(object s, EventArgs e)
        {
            OnChange();
        }

        private void OnChange()
        {
            if (Change != null)
                Change(this, null);
        }

        #region Instance constructors

        /// <summary>
        /// Selection Constructor.
        /// </summary>
        /// <param name="control">Control that will use this selection</param>
        public Selection(EditViewControl control)
        {
            Control = control;
            Bounds = new TextRange();
        }

        #endregion Instance constructors

        #region Public instance properties

        /// <summary>
        /// Gets the text of the active selection
        /// </summary>
        public String Text
        {
            get
            {
                if (!IsValid)
                {
                    return "";
                }
                else
                {
                    return Control.Document.GetRange(LogicalBounds);
                }
            }
            set
            {
                if (Text == value)
                    return;

                //selection text bug fix 
                //
                //selection gets too short if \n is used instead of newline
                string tmp = value.Replace(Environment.NewLine, "\n");
                tmp = tmp.Replace("\n", Environment.NewLine);
                value = tmp;
                //---


                TextPoint oCaretPos = Control.Caret.Position;
                int nCaretX = oCaretPos.X;
                int nCaretY = oCaretPos.Y;
                Control.Document.StartUndoCapture();
                DeleteSelection();
                Control.Document.InsertText(value, oCaretPos.X, oCaretPos.Y);
                SelLength = value.Length;
                if (nCaretX != oCaretPos.X || nCaretY != oCaretPos.Y)

                {
                    Control.Caret.Position = new TextPoint(Bounds.LastColumn,
                                                           Bounds.LastRow);
                }

                Control.Document.EndUndoCapture();
                Control.Document.InvokeChange();
            }
        }

        /// <summary>
        /// Returns the normalized positions of the selection.
        /// Swapping start and end values if the selection is reversed.
        /// </summary>
        public TextRange LogicalBounds
        {
            get
            {
                var r = new TextRange();
                if (Bounds.FirstRow < Bounds.LastRow)
                {
                    return Bounds;
                }
                else if (Bounds.FirstRow == Bounds.LastRow &&
                         Bounds.FirstColumn < Bounds.LastColumn)
                {
                    return Bounds;
                }
                else
                {
                    r.FirstColumn = Bounds.LastColumn;
                    r.FirstRow = Bounds.LastRow;
                    r.LastColumn = Bounds.FirstColumn;
                    r.LastRow = Bounds.FirstRow;
                    return r;
                }
            }
        }

        /// <summary>
        /// Returns true if the selection contains One or more chars
        /// </summary>
        public bool IsValid
        {
            get
            {
                return (LogicalBounds.FirstColumn != LogicalBounds.LastColumn
                        || LogicalBounds.FirstRow != LogicalBounds.LastRow);
            }
        }

        /// <summary>
        /// gets or sets the length of the selection in chars
        /// </summary>
        public int SelLength
        {
            get
            {
                var p1 = new TextPoint(Bounds.FirstColumn,
                                       Bounds.FirstRow);
                var p2 = new TextPoint(Bounds.LastColumn,
                                       Bounds.LastRow);
                int i1 = Control.Document.PointToIntPos(p1);
                int i2 = Control.Document.PointToIntPos(p2);
                return i2 - i1;
            }
            set { SelEnd = SelStart + value; }
        }

        /// <summary>
        /// Gets or Sets the Selection end as an index in the document text.
        /// </summary>
        public int SelEnd
        {
            get
            {
                var p = new TextPoint(Bounds.LastColumn, Bounds.LastRow)
                    ;
                return Control.Document.PointToIntPos(p);
            }
            set
            {
                TextPoint p = Control.Document.IntPosToPoint(value);
                Bounds.LastColumn = p.X;
                Bounds.LastRow = p.Y;
            }
        }


        /// <summary>
        /// Gets or Sets the Selection start as an index in the document text.
        /// </summary>
        public int SelStart
        {
            get
            {
                var p = new TextPoint(Bounds.FirstColumn,
                                      Bounds.FirstRow);
                return Control.Document.PointToIntPos(p);
            }
            set
            {
                TextPoint p = Control.Document.IntPosToPoint(value);
                Bounds.FirstColumn = p.X;
                Bounds.FirstRow = p.Y;
            }
        }

        /// <summary>
        /// Gets or Sets the logical Selection start as an index in the document text.
        /// </summary>
        public int LogicalSelStart
        {
            get
            {
                var p = new TextPoint(LogicalBounds.FirstColumn,
                                      LogicalBounds.FirstRow);
                return Control.Document.PointToIntPos(p);
            }
            set
            {
                TextPoint p = Control.Document.IntPosToPoint(value);
                Bounds.FirstColumn = p.X;
                Bounds.FirstRow = p.Y;
            }
        }

        #endregion Public instance properties

        #region Public instance methods

        /// <summary>
        /// Indent the active selection one step.
        /// </summary>
        public void Indent()
        {
            if (!IsValid)
                return;

            Row xtr = null;
            var ActionGroup = new UndoBlockCollection();
            for (int i = LogicalBounds.FirstRow;
                 i <=
                 LogicalBounds.LastRow;
                 i++)
            {
                xtr = Control.Document[i];
                xtr.Text = "\t" + xtr.Text;
                var b = new UndoBlock();
                b.Action = UndoAction.InsertRange;
                b.Text = "\t";
                b.Position.X = 0;
                b.Position.Y = i;
                ActionGroup.Add(b);
            }
            if (ActionGroup.Count > 0)
                Control.Document.AddToUndoList(ActionGroup);
            Bounds = LogicalBounds;
            Bounds.FirstColumn = 0;
            Bounds.LastColumn = xtr.Text.Length;
            Control.Caret.Position.X = LogicalBounds.LastColumn;
            Control.Caret.Position.Y = LogicalBounds.LastRow;
        }

        /// <summary>
        /// Outdent the active selection one step
        /// </summary>
        public void Outdent()
        {
            if (!IsValid)
                return;

            Row xtr = null;
            var ActionGroup = new UndoBlockCollection();
            for (int i = LogicalBounds.FirstRow;
                 i <=
                 LogicalBounds.LastRow;
                 i++)
            {
                xtr = Control.Document[i];
                var b = new UndoBlock();
                b.Action = UndoAction.DeleteRange;
                b.Position.X = 0;
                b.Position.Y = i;
                ActionGroup.Add(b);
                string s = xtr.Text;
                if (s.StartsWith("\t"))
                {
                    b.Text = s.Substring(0, 1);
                    s = s.Substring(1);
                }
                if (s.StartsWith("    "))
                {
                    b.Text = s.Substring(0, 4);
                    s = s.Substring(4);
                }
                xtr.Text = s;
            }
            if (ActionGroup.Count > 0)
                Control.Document.AddToUndoList(ActionGroup);
            Bounds = LogicalBounds;
            Bounds.FirstColumn = 0;
            Bounds.LastColumn = xtr.Text.Length;
            Control.Caret.Position.X = LogicalBounds.LastColumn;
            Control.Caret.Position.Y = LogicalBounds.LastRow;
        }


        public void Indent(string Pattern)
        {
            if (!IsValid)
                return;

            Row xtr = null;
            var ActionGroup = new UndoBlockCollection();
            for (int i = LogicalBounds.FirstRow;
                 i <=
                 LogicalBounds.LastRow;
                 i++)
            {
                xtr = Control.Document[i];
                xtr.Text = Pattern + xtr.Text;
                var b = new UndoBlock();
                b.Action = UndoAction.InsertRange;
                b.Text = Pattern;
                b.Position.X = 0;
                b.Position.Y = i;
                ActionGroup.Add(b);
            }
            if (ActionGroup.Count > 0)
                Control.Document.AddToUndoList(ActionGroup);
            Bounds = LogicalBounds;
            Bounds.FirstColumn = 0;
            Bounds.LastColumn = xtr.Text.Length;
            Control.Caret.Position.X = LogicalBounds.LastColumn;
            Control.Caret.Position.Y = LogicalBounds.LastRow;
        }

        /// <summary>
        /// Outdent the active selection one step
        /// </summary>
        public void Outdent(string Pattern)
        {
            if (!IsValid)
                return;

            Row xtr = null;
            var ActionGroup = new UndoBlockCollection();
            for (int i = LogicalBounds.FirstRow;
                 i <=
                 LogicalBounds.LastRow;
                 i++)
            {
                xtr = Control.Document[i];
                var b = new UndoBlock();
                b.Action = UndoAction.DeleteRange;
                b.Position.X = 0;
                b.Position.Y = i;
                ActionGroup.Add(b);
                string s = xtr.Text;
                if (s.StartsWith(Pattern))
                {
                    b.Text = s.Substring(0, Pattern.Length);
                    s = s.Substring(Pattern.Length);
                }
                xtr.Text = s;
            }
            if (ActionGroup.Count > 0)
                Control.Document.AddToUndoList(ActionGroup);
            Bounds = LogicalBounds;
            Bounds.FirstColumn = 0;
            Bounds.LastColumn = xtr.Text.Length;
            Control.Caret.Position.X = LogicalBounds.LastColumn;
            Control.Caret.Position.Y = LogicalBounds.LastRow;
        }

        /// <summary>
        /// Delete the active selection.
        /// <seealso cref="ClearSelection"/>
        /// </summary>
        public void DeleteSelection()
        {
            TextRange r = LogicalBounds;

            int x = r.FirstColumn;
            int y = r.FirstRow;
            Control.Document.DeleteRange(r);
            Control.Caret.Position.X = x;
            Control.Caret.Position.Y = y;
            ClearSelection();
            Control.ScrollIntoView();
        }

        /// <summary>
        /// Clear the active selection
        /// <seealso cref="DeleteSelection"/>
        /// </summary>
        public void ClearSelection()
        {
            Bounds.FirstColumn = Control.Caret.Position.X;
            Bounds.FirstRow = Control.Caret.Position.Y;
            Bounds.LastColumn = Control.Caret.Position.X;
            Bounds.LastRow = Control.Caret.Position.Y;
        }

        /// <summary>
        /// Make a selection from the current selection start to the position of the caret
        /// </summary>
        public void MakeSelection()
        {
            Bounds.LastColumn = Control.Caret.Position.X;
            Bounds.LastRow = Control.Caret.Position.Y;
        }

        /// <summary>
        /// Select all text.
        /// </summary>
        public void SelectAll()
        {
            Bounds.FirstColumn = 0;
            Bounds.FirstRow = 0;
            Bounds.LastColumn = Control.Document[Control.Document.Count -
                                                 1].Text.Length;
            Bounds.LastRow = Control.Document.Count - 1;
            Control.Caret.Position.X = Bounds.LastColumn;
            Control.Caret.Position.Y = Bounds.LastRow;
            Control.ScrollIntoView();
        }

        #endregion Public instance methods

        #region Public instance fields

        /// <summary>
        /// The bounds of the selection
        /// </summary>
        /// 
        private TextRange _Bounds;

        public TextRange Bounds
        {
            get { return _Bounds; }
            set
            {
                if (_Bounds != null)
                {
                    _Bounds.Change -= Bounds_Change;
                }

                _Bounds = value;
                _Bounds.Change += Bounds_Change;
                OnChange();
            }
        }

        private void Bounds_Change(object s, EventArgs e)
        {
            OnChange();
        }

        #endregion Public instance fields

        #region Protected instance fields

        private readonly EditViewControl Control;

        #endregion Protected instance fields
    }
}