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

namespace Alsing.SourceCode
{
    /// <summary>
    /// A range of text
    /// </summary>
    public class TextRange
    {
        public TextRange() {}

        public TextRange(int firstColumn, int firstRow, int lastColumn, int lastRow)
        {
            this.firstColumn = firstColumn;
            this.firstRow = firstRow;
            this.lastColumn = lastColumn;
            this.lastRow = lastRow;
        }

        public event EventHandler Change = null;

        protected virtual void OnChange()
        {
            if (Change != null)
                Change(this, EventArgs.Empty);
        }

        /// <summary>
        /// The start row of the range
        /// </summary>

        /// <summary>
        /// The start column of the range
        /// </summary>

        /// <summary>
        /// The end row of the range
        /// </summary>

        /// <summary>
        /// The end column of the range
        /// </summary>

        public void SetBounds(int firstColumn, int firstRow, int lastColumn, int lastRow)
        {
            this.firstColumn = firstColumn;
            this.firstRow = firstRow;
            this.lastColumn = lastColumn;
            this.lastRow = lastRow;
            OnChange();
        }

        #region PUBLIC PROPERTY FIRSTROW

        private int firstRow;

        public int FirstRow
        {
            get { return firstRow; }
            set
            {
                firstRow = value;
                OnChange();
            }
        }

        #endregion

        #region PUBLIC PROPERTY FIRSTCOLUMN

        private int firstColumn;

        public int FirstColumn
        {
            get { return firstColumn; }
            set
            {
                firstColumn = value;
                OnChange();
            }
        }

        #endregion

        #region PUBLIC PROPERTY LASTROW

        private int lastRow;

        public int LastRow
        {
            get { return lastRow; }
            set
            {
                lastRow = value;
                OnChange();
            }
        }

        #endregion

        #region PUBLIC PROPERTY LASTCOLUMN

        private int lastColumn;

        public int LastColumn
        {
            get { return lastColumn; }
            set
            {
                lastColumn = value;
                OnChange();
            }
        }

        #endregion
    }
}