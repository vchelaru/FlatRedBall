// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using System.Collections;
using System.Collections.Generic;

namespace Alsing.Windows.Forms.FormatLabel
{
    public class Row
    {
        public int BottomPadd;
        public int Height;
        public bool RenderSeparator;
        public int Top;
        public bool Visible;
        public int Width;
        public List<Word> Words = new List<Word>();
    }
}