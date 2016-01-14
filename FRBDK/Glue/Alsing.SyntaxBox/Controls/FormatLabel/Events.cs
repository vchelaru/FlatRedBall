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

namespace Alsing.Windows.Forms.FormatLabel
{
    /// <summary>
    /// 
    /// </summary>
    public class ClickLinkEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public string Link = "";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Link"></param>
        public ClickLinkEventArgs(string Link)
        {
            this.Link = Link;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public delegate void ClickLinkEventHandler(object sender, ClickLinkEventArgs e);
}