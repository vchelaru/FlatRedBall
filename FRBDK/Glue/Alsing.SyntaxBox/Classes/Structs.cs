// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using Alsing.Drawing.GDI;

namespace Alsing.Windows.Forms.SyntaxBox
{
    /// <summary>
    /// Indent styles used by the control
    /// </summary>
    public enum IndentStyle
    {
        /// <summary>
        /// Caret is always confined to the first column when a new line is inserted
        /// </summary>
        None = 0,
        /// <summary>
        /// New lines inherit the same indention as the previous row.
        /// </summary>
        LastRow = 1,
        /// <summary>
        /// New lines get their indention from the scoping level.
        /// </summary>
        Scope = 2,
        /// <summary>
        /// New lines get thir indention from the scoping level or from the previous row
        /// depending on which is most indented.
        /// </summary>
        Smart = 3,
    }
}

namespace Alsing.Windows.Forms.SyntaxBox
{
    /// <summary>
    /// Text actions that can be performed by the SyntaxBoxControl
    /// </summary>
    public enum EditAction
    {
        /// <summary>
        /// The control is not performing any action
        /// </summary>
        None = 0,
        /// <summary>
        /// The control is in Drag Drop mode
        /// </summary>
        DragText = 1,
        /// <summary>
        /// The control is selecting text
        /// </summary>
        SelectText = 2
    }
}

namespace Alsing.Windows.Forms.SyntaxBox.Painter
{
    /// <summary>
    /// View point struct used by the SyntaxBoxControl.
    /// The struct contains information about various rendering parameters that the IPainter needs.
    /// </summary>
    public struct ViewPoint
    {
        /// <summary>
        /// The action that the SyntaxBoxControl is currently performing
        /// </summary>
        public EditAction Action;

        /// <summary>
        /// Width of a char (space) in pixels
        /// </summary>
        public int CharWidth;


        /// <summary>
        /// Height of the client area in pixels
        /// </summary>
        public int ClientAreaStart;

        /// <summary>
        /// Width of the client area in pixels
        /// </summary>
        public int ClientAreaWidth;

        /// <summary>
        /// Index of the first visible column
        /// </summary>
        public int FirstVisibleColumn;

        /// <summary>
        /// Index of the first visible row
        /// </summary>
        public int FirstVisibleRow;

        /// <summary>
        /// Width of the gutter margin in pixels
        /// </summary>
        public int GutterMarginWidth;

        /// <summary>
        /// Width of the Linenumber margin in pixels
        /// </summary>
        public int LineNumberMarginWidth;

        /// <summary>
        /// Height of a row in pixels
        /// </summary>
        public int RowHeight;

        /// <summary>
        /// Width of the text margin (sum of gutter + linenumber + folding margins)
        /// </summary>
        public int TextMargin;

        /// <summary>
        /// 
        /// </summary>
        public int TotalMarginWidth;

        /// <summary>
        /// Number of rows that can be displayed in the current view
        /// </summary>
        public int VisibleRowCount;

        /// <summary>
        /// Used for offsetting the screen in y axis.
        /// </summary>
        public int YOffset;

        //document items
    }


    /// <summary>
    /// Struct used by the NativePainter class.
    /// </summary>
    public struct RenderItems
    {
        /// <summary>
        /// For internal use only
        /// </summary>
        public GDISurface BackBuffer; //backbuffer surface

        /// <summary>
        /// For internal use only
        /// </summary>
        public GDIBrush BackgroundBrush; //background brush

        /// <summary>
        /// For internal use only
        /// </summary>
        public GDIFont FontBold; //Font , bold

        /// <summary>
        /// For internal use only
        /// </summary>
        public GDIFont FontBoldItalic; //Font , bold & italic

        /// <summary>
        /// For internal use only
        /// </summary>
        public GDIFont FontBoldItalicUnderline; //Font , bold & italic

        /// <summary>
        /// For internal use only
        /// </summary>
        public GDIFont FontBoldUnderline; //Font , bold

        /// <summary>
        /// For internal use only
        /// </summary>
        public GDIFont FontItalic; //Font , italic

        /// <summary>
        /// For internal use only
        /// </summary>
        public GDIFont FontItalicUnderline; //Font , italic

        /// <summary>
        /// For internal use only
        /// </summary>
        public GDIFont FontNormal; //Font , no decoration		

        /// <summary>
        /// For internal use only
        /// </summary>
        public GDIFont FontUnderline; //Font , no decoration		

        /// <summary>
        /// For internal use only
        /// </summary>
        public GDIBrush GutterMarginBorderBrush; //Gutter magrin brush

        /// <summary>
        /// For internal use only
        /// </summary>
        public GDIBrush GutterMarginBrush; //Gutter magrin brush

        /// <summary>
        /// For internal use only
        /// </summary>
        public GDIBrush HighLightLineBrush; //background brush

        /// <summary>
        /// For internal use only
        /// </summary>
        public GDIBrush LineNumberMarginBorderBrush; //linenumber margin brush

        /// <summary>
        /// For internal use only
        /// </summary>
        public GDIBrush LineNumberMarginBrush; //linenumber margin brush

        /// <summary>
        /// For internal use only
        /// </summary>
        public GDIBrush OutlineBrush; //background brush

        /// <summary>
        /// For internal use only
        /// </summary>
        public GDISurface SelectionBuffer; //backbuffer surface

        /// <summary>
        /// For internal use only
        /// </summary>
        public GDISurface StringBuffer; //backbuffer surface
    }
}