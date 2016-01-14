// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using System.Windows.Forms;

namespace Alsing.Windows.Forms.CoreLib
{
    public partial class ThumbControl : Control
	{
		public ThumbControl()
		{
			InitializeComponent();
		}

        /// <summary>
        /// Draws a 2px Raised Border for the ThumbControl
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            ControlPaint.DrawBorder3D(e.Graphics, 0, 0, Width, Height, Border3DStyle.Raised);
        }
	}
}
