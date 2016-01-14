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
    /// Class representing a point in a text.
    /// where x is the column and y is the row.
    /// </summary>
    public class TextPoint
    {
        private int x;
        private int y;

        /// <summary>
        /// 
        /// </summary>
        public TextPoint() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        public TextPoint(int X, int Y)
        {
            this.X = X;
            this.Y = Y;
        }

        /// <summary>
        /// 
        /// </summary>
        public int X
        {
            get { return x; }
            set
            {
                x = value;
                OnChange();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int Y
        {
            get { return y; }
            set
            {
                y = value;
                OnChange();
            }
        }

        /// <summary>
        /// Event fired when the X or Y property has changed.
        /// </summary>
        public event EventHandler Change = null;

        private void OnChange()
        {
            if (Change != null)
                Change(this, new EventArgs());
        }
    }
}