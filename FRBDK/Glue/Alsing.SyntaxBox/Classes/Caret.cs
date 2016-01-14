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
using Alsing.SourceCode;

namespace Alsing.Windows.Forms.SyntaxBox
{
    /// <summary>
    /// Caret class used by the SyntaxBoxControl
    /// </summary>
    public sealed class Caret
    {
        /// <summary>
        /// Gets or Sets the position of the caret.
        /// </summary>
        public TextPoint Position
        {
            get { return _Position; }
            set
            {
                _Position = value;
                _Position.Change += PositionChange;
                OnChange();
            }
        }

        /// <summary>
        /// Event fired when the carets position has changed.
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

        #region General Declarations

        // X Position of the caret (in logical units (eg. 1 tab = 5 chars)

        private readonly EditViewControl Control;

        /// <summary>
        /// The Position of the caret in Chars (Column and Row index)
        /// </summary>
        private TextPoint _Position;

        /// <summary>
        /// Used by the painter to determine if the caret should be rendered or not
        /// </summary>
        public bool Blink;

        private int OldLogicalXPos;

        // to what control does the caret belong??

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Caret constructor
        /// </summary>
        /// <param name="control">The control that will use the caret</param>
        public Caret(EditViewControl control)
        {
            Position = new TextPoint(0, 0);
            Control = control;
        }

        #endregion

        #region Helpers

        private void RememberXPos()
        {
            OldLogicalXPos = LogicalPosition.X;
        }

        /// <summary>
        /// Confines the caret to a valid position within the active document
        /// </summary>
        public void CropPosition()
        {
            if (Position.X < 0)
                Position.X = 0;

            if (Position.Y >= Control.Document.Count)
                Position.Y = Control.Document.Count - 1;

            if (Position.Y < 0)
                Position.Y = 0;

            Row xtr = CurrentRow;

            if (Position.X > xtr.Text.Length && !Control.VirtualWhitespace)
                Position.X = xtr.Text.Length;
        }

        #endregion

        #region Movement Methods

        /// <summary>
        /// Moves the caret right one step.
        /// if the caret is placed at the last column of a row the caret will move down one row and be placed at the first column of that row.
        /// </summary>
        /// <param name="Select">True if a selection should be created from the current caret pos to the new pos</param>
        public void MoveRight(bool Select)
        {
            CropPosition();
            Position.X++;

            if (CurrentRow.IsCollapsed)
            {
                if (Position.X > CurrentRow.Expansion_EndChar)
                {
                    Position.Y = CurrentRow.Expansion_EndRow.Index;
                    Position.X = CurrentRow.Expansion_EndRow.Expansion_StartChar;
                    CropPosition();
                }
                RememberXPos();
                CaretMoved(Select);
            }
            else
            {
                Row xtr = CurrentRow;
                if (Position.X > xtr.Text.Length && !Control.VirtualWhitespace)
                {
                    if (Position.Y < Control.Document.Count - 1)
                    {
                        MoveDown(Select);
                        Position.X = 0;
                        //this.Position.Y ++;
                        CropPosition();
                    }
                    else
                        CropPosition();
                }
                RememberXPos();
                CaretMoved(Select);
            }
        }

        /// <summary>
        /// Moves the caret up one row.
        /// </summary>
        /// <param name="Select">True if a selection should be created from the current caret pos to the new pos</param>
        public void MoveUp(bool Select)
        {
            CropPosition();
            int x = OldLogicalXPos;
            //error here
            try
            {
                if (CurrentRow != null && CurrentRow.PrevVisibleRow != null)
                {
                    Position.Y = CurrentRow.PrevVisibleRow.Index;
                    if (CurrentRow.IsCollapsed)
                    {
                        x = 0;
                    }
                }
            }
            catch
            {
                
            }
            finally
            {
                CropPosition();
                LogicalPosition = new TextPoint(x, Position.Y);
                CropPosition();
                CaretMoved(Select);
            }
        }

        /// <summary>
        /// Moves the caret up x rows
        /// </summary>
        /// <param name="rows">Number of rows the caret should be moved up</param>
        /// <param name="Select">True if a selection should be created from the current caret pos to the new pos</param>
        public void MoveUp(int rows, bool Select)
        {
            CropPosition();
            int x = OldLogicalXPos;
            try
            {
                int pos = CurrentRow.VisibleIndex;
                pos -= rows;
                if (pos < 0)
                    pos = 0;
                Row r = Control.Document.VisibleRows[pos];
                pos = r.Index;


                Position.Y = pos;

                //				for (int i=0;i<rows;i++)
                //				{
                //					this.Position.Y =  this.CurrentRow.PrevVisibleRow.Index;
                //				}
                if (CurrentRow.IsCollapsed)
                {
                    x = 0;
                }
            }
            catch {}
            CropPosition();
            LogicalPosition = new TextPoint(x, Position.Y);
            CropPosition();
            CaretMoved(Select);
        }

        /// <summary>
        /// Moves the caret down x rows.
        /// </summary>
        /// <param name="rows">The number of rows the caret should be moved down</param>
        /// <param name="Select">True if a selection should be created from the current caret pos to the new pos</param>
        public void MoveDown(int rows, bool Select)
        {
            int x = OldLogicalXPos;
            CropPosition();
            //this.Position.Y +=rows;
            try
            {
                int pos = CurrentRow.VisibleIndex;
                pos += rows;
                if (pos > Control.Document.VisibleRows.Count - 1)
                    pos = Control.Document.VisibleRows.Count - 1;

                Row r = Control.Document.VisibleRows[pos];
                pos = r.Index;
                Position.Y = pos;

                //				for (int i=0;i<rows;i++)
                //				{
                //					this.Position.Y =  this.CurrentRow.NextVisibleRow.Index;
                //					
                //				}
                if (CurrentRow.IsCollapsed)
                {
                    x = 0;
                }
            }
            catch {}
            finally
            {
                CropPosition();
                LogicalPosition = new TextPoint(x, Position.Y);
                CropPosition();
                CaretMoved(Select);
            }
        }


        /// <summary>
        /// Moves the caret down one row.
        /// </summary>
        /// <param name="Select">True if a selection should be created from the current caret pos to the new pos</param>
        public void MoveDown(bool Select)
        {
            CropPosition();
            int x = OldLogicalXPos;
            //error here
            try
            {
                Row r = CurrentRow;
                Row r2 = r.NextVisibleRow;
                if (r2 == null)
                    return;

                Position.Y = r2.Index;
                if (CurrentRow.IsCollapsed)
                {
                    x = 0;
                }
            }
            catch {}
            finally
            {
                CropPosition();
                LogicalPosition = new TextPoint(x, Position.Y);
                CropPosition();
                CaretMoved(Select);
            }
        }

        /// <summary>
        /// Moves the caret left one step.
        /// if the caret is placed at the first column the caret will be moved up one line and placed at the last column of the row.
        /// </summary>
        /// <param name="Select">True if a selection should be created from the current caret pos to the new pos</param>
        public void MoveLeft(bool Select)
        {
            CropPosition();
            Position.X--;

            if (CurrentRow.IsCollapsedEndPart)
            {
                if (Position.X < CurrentRow.Expansion_StartChar)
                {
                    if (CurrentRow.Expansion_StartRow.Index == - 1)
                        Debugger.Break();
                    Position.Y = CurrentRow.Expansion_StartRow.Index;
                    Position.X = CurrentRow.Expansion_StartRow.Expansion_EndChar;
                    CropPosition();
                }
                RememberXPos();
                CaretMoved(Select);
            }
            else
            {
                if (Position.X < 0)
                {
                    if (Position.Y > 0)
                    {
                        MoveUp(Select);
                        CropPosition();
                        Row xtr = CurrentRow;
                        Position.X = xtr.Text.Length;
                        if (CurrentRow.IsCollapsed)
                        {
                            Position.Y = CurrentRow.Expansion_EndRow.Index;
                            Position.X = CurrentRow.Text.Length;
                        }
                    }
                    else
                        CropPosition();
                }
                RememberXPos();
                CaretMoved(Select);
            }
        }


        /// <summary>
        /// Moves the caret to the first non whitespace column at the active row
        /// </summary>
        /// <param name="Select">True if a selection should be created from the current caret pos to the new pos</param>
        public void MoveHome(bool Select)
        {
            CropPosition();
            if (CurrentRow.IsCollapsedEndPart)
            {
                Position.Y = CurrentRow.Expansion_StartRow.Index;
                MoveHome(Select);
            }
            else
            {
                int i = CurrentRow.GetLeadingWhitespace().Length;
                Position.X = Position.X == i ? 0 : i;
                RememberXPos();
                CaretMoved(Select);
            }
        }

        /// <summary>
        /// Moves the caret to the end of a row ignoring any whitespace characters at the end of the row
        /// </summary>
        /// <param name="Select">True if a selection should be created from the current caret pos to the new pos</param>
        public void MoveEnd(bool Select)
        {
            if (CurrentRow.IsCollapsed)
            {
                Position.Y = CurrentRow.Expansion_EndRow.Index;
                MoveEnd(Select);
            }
            else
            {
                CropPosition();
                Row xtr = CurrentRow;
                Position.X = xtr.Text.Length;
                RememberXPos();
                CaretMoved(Select);
            }
        }

        public void CaretMoved(bool Select)
        {
            Control.ScrollIntoView();
            if (!Select)
                Control.Selection.ClearSelection();
            else
                Control.Selection.MakeSelection();
        }

        /// <summary>
        /// Moves the caret to the first column of the active row
        /// </summary>
        /// <param name="Select">True if a selection should be created from the current caret pos to the new pos</param>
        public void MoveAbsoluteHome(bool Select)
        {
            Position.X = 0;
            Position.Y = 0;
            RememberXPos();
            CaretMoved(Select);
        }

        /// <summary>
        /// Moves the caret to the absolute end of the active row
        /// </summary>
        /// <param name="Select">True if a selection should be created from the current caret pos to the new pos</param>
        public void MoveAbsoluteEnd(bool Select)
        {
            Position.X = Control.Document[Control.Document.Count - 1].Text.Length;
            Position.Y = Control.Document.Count - 1;
            RememberXPos();
            CaretMoved(Select);
        }

        #endregion

        #region Get Related info from Caret Position

        /// <summary>
        /// Gets the word that the caret is placed on.
        /// This only applies if the active row is fully parsed.
        /// </summary>
        /// <returns>a Word object from the active row</returns>
        public Word CurrentWord
        {
            get { return Control.Document.GetWordFromPos(Position); }
        }

        public Word PreviousWord
        {
            get
            {
                if (Position.X > 1)
                {
                    TextPoint point = new TextPoint(Position.X - 2, Position.Y);
                    
                    return Control.Document.GetWordFromPos(point);
                }
                else
                {
                    return null;
                }
            }
        }

        public Word GetWord(int countBefore)
        {
            Word word = CurrentWord;

            Row row = Control.Caret.CurrentRow;

            int index = row.words.IndexOf(word);

            if (index >= countBefore)
            {
                return row.words[index - countBefore];
            }
            else
            {
                return null;
            }

            return CurrentWord;
        }

        public string GetWordText(int countBefore)
        {
            Word foundWord = GetWord(countBefore);

            if (foundWord == null)
            {
                return null;
            }
            else
            {
                return foundWord.Text;
            }
        }

        /// <summary>
        /// Returns the row that the caret is placed on
        /// </summary>
        /// <returns>a Row object from the active document</returns>
        public Row CurrentRow
        {
            get { return Control.Document[Position.Y]; }
        }

        /// <summary>
        /// Gets the word that the caret is placed on.
        /// This only applies if the active row is fully parsed.
        /// </summary>
        /// <returns>a Word object from the active row</returns>
        public Span CurrentSegment()
        {
            return Control.Document.GetSegmentFromPos(Position);
        }

        #endregion

        #region Set Position Methods/Props

        /// <summary>
        /// Gets or Sets the Logical position of the caret.
        /// </summary>
        public TextPoint LogicalPosition
        {
            get
            {
                if (Position.X < 0)
                    return new TextPoint(0, Position.Y);

                Row xtr = CurrentRow;
                int x = 0;
                if (xtr == null)
                    return new TextPoint(0, 0);

                int Padd = Math.Max(Position.X - xtr.Text.Length, 0);
                var PaddStr = new String(' ', Padd);
                string TotStr = xtr.Text + PaddStr;

                char[] buffer = TotStr.ToCharArray(0, Position.X);
                foreach (char c in buffer)
                {
                    if (c == '\t')
                    {
                        x += Control.TabSize - (x%Control.TabSize);
                    }
                    else
                    {
                        x++;
                    }
                }
                return new TextPoint(x, Position.Y);
            }
            set
            {
                Row xtr = CurrentRow;
                int x = 0;
                int xx = 0;
                if (value.X > 0)
                {
                    char[] chars = xtr.Text.ToCharArray();

                    int i = 0;

                    while (x < value.X)
                    {
                        char c = i < chars.Length ? chars[i] : ' ';
                        xx++;
                        if (c == '\t')
                        {
                            x += Control.TabSize - (x%Control.TabSize);
                        }
                        else
                        {
                            x++;
                        }
                        i++;
                    }
                }


                Position.Y = value.Y;
                Position.X = xx;
            }
        }

        /// <summary>
        /// Sets the position of the caret
        /// </summary>
        /// <param name="pos">Point containing the new x and y positions</param>
        public void SetPos(TextPoint pos)
        {
            Position = pos;
            RememberXPos();
        }

        #endregion
    }
}