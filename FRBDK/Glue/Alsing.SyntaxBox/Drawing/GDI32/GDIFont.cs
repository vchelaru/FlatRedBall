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
using Alsing.Windows;

namespace Alsing.Drawing.GDI
{
    public class GDIFont : GDIObject
    {
        public bool Bold;
        public byte Charset;
        public string FontName;
        public IntPtr hFont;
        public bool Italic;
        public float Size;
        public bool Strikethrough;
        public bool Underline;


        public GDIFont()
        {
            Create();
        }

        public GDIFont(string fontname, float size)
        {
            Init(fontname, size, false, false, false, false);
            Create();
        }

        public GDIFont(string fontname, float size, bool bold, bool italic, bool underline, bool strikethrough)
        {
            Init(fontname, size, bold, italic, underline, strikethrough);
            Create();
        }

        protected void Init(string fontname, float size, bool bold, bool italic, bool underline, bool strikethrough)
        {
            FontName = fontname;
            Size = size;
            Bold = bold;
            Italic = italic;
            Underline = underline;
            Strikethrough = strikethrough;

            var tFont = new LogFont
                        {
                            lfItalic = ((byte) (Italic ? 1 : 0)),
                            lfStrikeOut = ((byte) (Strikethrough ? 1 : 0)),
                            lfUnderline = ((byte) (Underline ? 1 : 0)),
                            lfWeight = (Bold ? 700 : 400),
                            lfWidth = 0,
                            lfHeight = ((int) (-Size*1.3333333333333)),
                            lfCharSet = 1,
                            lfFaceName = FontName
                        };


            hFont = NativeMethods.CreateFontIndirect(tFont);
        }

        ~GDIFont()
        {
            Destroy();
        }

        protected override void Destroy()
        {
            if (hFont != (IntPtr) 0)
                NativeMethods.DeleteObject(hFont);
            base.Destroy();
            hFont = (IntPtr) 0;
        }
    }
}