// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

#region using...

using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Design;
using System.IO;
using System.Resources;
using System.Windows.Forms;
using Alsing.Drawing.GDI;
using Alsing.SourceCode;
using Alsing.Windows.Forms.CoreLib;
using Alsing.Windows.Forms.SyntaxBox;

#endregion

namespace Alsing.Windows.Forms
{
    /// <summary>
    /// Syntaxbox control that can be used as a pure text editor or as a code editor when a syntaxfile is used.
    /// </summary>
    [Designer(typeof (SyntaxBoxDesigner), typeof (IDesigner))]
    public class SyntaxBoxControl : SplitViewParentControl
    {
        protected internal bool DisableAutoList;
        protected internal bool DisableFindForm;
        protected internal bool DisableInfoTip;
        protected internal bool DisableIntelliMouse;

        #region General Declarations

        private bool _AllowBreakPoints = true;
        private Color _BackColor = Color.White;
        private Color _BracketBackColor = Color.LightSteelBlue;
        private Color _BracketBorderColor = Color.DarkBlue;
        private Color _BracketForeColor = Color.Black;
        private bool _BracketMatching = true;

        private Color _BreakPointBackColor = Color.DarkRed;
        private Color _BreakPointForeColor = Color.White;
        private SyntaxDocument _Document;
        private string _FontName = "Courier new";
        private float _FontSize = 10f;
        private Color _GutterMarginBorderColor = SystemColors.ControlDark;
        private Color _GutterMarginColor = SystemColors.Control;
        private int _GutterMarginWidth = 19;
        private bool _HighLightActiveLine;
        private Color _HighLightedLineColor = Color.LightYellow;
        private Color _InactiveSelectionBackColor = SystemColors.ControlDark;
        private Color _InactiveSelectionForeColor = SystemColors.ControlLight;
        private IndentStyle _Indent = IndentStyle.LastRow;
        private KeyboardActionList _KeyboardActions = new KeyboardActionList();
        private Color _LineNumberBackColor = SystemColors.Window;
        private Color _LineNumberBorderColor = Color.Teal;
        private Color _LineNumberForeColor = Color.Teal;
        private Color _OutlineColor = SystemColors.ControlDark;
        private bool _ParseOnPaste;
        private Color _ScopeBackColor = Color.Transparent;
        private Color _ScopeIndicatorColor = Color.Transparent;
        private Color _SelectionBackColor = SystemColors.Highlight;
        private Color _SelectionForeColor = SystemColors.HighlightText;
        private Color _SeparatorColor = SystemColors.Control;
        private bool _ShowGutterMargin = true;
        private bool _ShowLineNumbers = true;
        private bool _ShowTabGuides;
        private bool _ShowWhitespace;
        private int _SmoothScrollSpeed = 2;

        private Color _TabGuideColor = ControlPaint.Light(SystemColors.ControlLight)
                      ;

        private int _TabSize = 4;

        private int _TooltipDelay = 240;
        private bool _VirtualWhitespace;
        private Color _WhitespaceColor = SystemColors.ControlDark;
        private IContainer components;
        #endregion

        #region Internal Components/Controls

        private ImageList _AutoListIcons;
        private ImageList _GutterIcons;
        private Timer ParseTimer;

        #endregion

        #region Public Events

        /// <summary>
        /// An event that is fired when the cursor hovers a pattern;
        /// </summary>
        public event WordMouseHandler WordMouseHover = null;

        /// <summary>
        /// An event that is fired when the cursor hovers a pattern;
        /// </summary>
        public event WordMouseHandler WordMouseDown = null;

        /// <summary>
        /// An event that is fired when the control has updated the clipboard
        /// </summary>
        public event CopyHandler ClipboardUpdated = null;

        /// <summary>
        /// Event fired when the caret of the active view have moved.
        /// </summary>
        public event EventHandler CaretChange = null;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler SelectionChange = null;

        /// <summary>
        /// Event fired when the user presses the up or the down button on the infotip.
        /// </summary>
        public event EventHandler InfoTipSelectedIndexChanged = null;

        /// <summary>
        /// Event fired when a row is rendered.
        /// </summary>
        public event RowPaintHandler RenderRow = null;

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

        #endregion //END PUBLIC EGENTS

        #region Public Properties

        #region PUBLIC PROPERTY SHOWEOLMARKER

        private bool _ShowEOLMarker;

        [Category("Appearance"), Description(
            "Determines if a ¶ should be displayed at the end of a line")
        ]
        [DefaultValue(false)]
        public bool ShowEOLMarker
        {
            get { return _ShowEOLMarker; }
            set
            {
                _ShowEOLMarker = value;
                Redraw();
            }
        }

        #endregion

        #region PUBLIC PROPERTY EOLMARKERCOLOR

        private Color _EOLMarkerColor = Color.Red;

        [Category("Appearance"), Description("The color of the EOL marker")
        ]
        [DefaultValue(typeof (Color), "Red")]
        public Color EOLMarkerColor
        {
            get { return _EOLMarkerColor; }
            set
            {
                _EOLMarkerColor = value;
                Redraw();
            }
        }

        #endregion

        #region PUBLIC PROPERTY AUTOLISTAUTOSELECT

        private bool _AutoListAutoSelect = true;

        [DefaultValue(true)]
        public bool AutoListAutoSelect
        {
            get { return _AutoListAutoSelect; }
            set { _AutoListAutoSelect = value; }
        }

        #endregion

        #region PUBLIC PROPERTY COPYASRTF

        [Category("Behavior - Clipboard"), Description("determines if the copy actions should be stored as RTF")]
        [DefaultValue(typeof (Color), "false")]
        public bool CopyAsRTF { get; set; }

        #endregion

        private bool _CollapsedBlockTooltipsEnabled = true;

        [Category("Appearance - Scopes"), Description(
            "The color of the active scope")]
        [DefaultValue(typeof (Color),
            "Transparent")]
        public Color ScopeBackColor
        {
            get { return _ScopeBackColor; }
            set
            {
                _ScopeBackColor = value;
                Redraw();
            }
        }

        [Category("Appearance - Scopes"), Description(
            "The color of the scope indicator")]
        [DefaultValue(typeof (Color),
            "Transparent")]
        public Color ScopeIndicatorColor
        {
            get { return _ScopeIndicatorColor; }
            set
            {
                _ScopeIndicatorColor = value;
                Redraw();
            }
        }

        /// <summary>
        /// Positions the AutoList
        /// </summary>
        [Category("Behavior")]
        [Browsable(false)]
        public TextPoint AutoListPosition
        {
            get { return ((EditViewControl) _ActiveView).AutoListPosition; }
            set
            {
                if ((_ActiveView) == null)
                    return;

                ((EditViewControl) _ActiveView).AutoListPosition = value;
            }
        }

        /// <summary>
        /// Positions the InfoTip
        /// </summary>
        [Category("Behavior")]
        [Browsable(false)]
        public TextPoint InfoTipPosition
        {
            get { return ((EditViewControl) _ActiveView).InfoTipPosition; }
            set
            {
                if ((_ActiveView) == null)
                    return;

                ((EditViewControl) _ActiveView).InfoTipPosition = value;
            }
        }


        /// <summary>
        /// Prevents the control from changing the cursor.
        /// </summary>
        [Description("Prevents the control from changing the cursor.")]
        [Category("Appearance")]
        [Browsable(false)]
        public bool LockCursorUpdate { get; set; }

        /// <summary>
        /// The row padding in pixels.
        /// </summary>
        [Category("Appearance"), Description("The number of pixels to add between rows")]
        [DefaultValue(0)]
        public int RowPadding { get; set; }


        /// <summary>
        /// The selected index in the infotip.
        /// </summary>
        [Category("Appearance - Infotip"), Description(
            "The currently active selection in the infotip")]
        [Browsable(false)
        ]
        public int InfoTipSelectedIndex
        {
            get { return ((EditViewControl) _ActiveView).InfoTip.SelectedIndex; }
            set
            {
                if ((_ActiveView) == null || ((EditViewControl)
                                              _ActiveView).InfoTip == null)
                    return;

                ((EditViewControl) _ActiveView).InfoTip.SelectedIndex = value;
            }
        }

        /// <summary>
        /// Gets or Sets the image used in the infotip.
        /// </summary>
        [Category("Appearance - InfoTip"), Description(
            "An image to show in the infotip")]
        [DefaultValue(null)]
        public
            Image InfoTipImage
        {
            get { return ((EditViewControl) _ActiveView).InfoTip.Image; }
            set
            {
                if ((_ActiveView) == null || ((EditViewControl)
                                              _ActiveView).InfoTip == null)
                    return;


                ((EditViewControl) _ActiveView).InfoTip.Image = value;
            }
        }

        /// <summary>
        /// Get or Sets the number of choices that could be made in the infotip.
        /// </summary>
        [Category("Appearance"), Description(
            "Get or Sets the number of choices that could be made in the infotip")]
        [Browsable(false)]
        public int InfoTipCount
        {
            get { return ((EditViewControl) _ActiveView).InfoTip.Count; }
            set
            {
                if ((_ActiveView) == null || ((EditViewControl)
                                              _ActiveView).InfoTip == null)
                    return;

                ((EditViewControl) _ActiveView).InfoTip.Count = value;
                ((EditViewControl) _ActiveView).InfoTip.Init();
            }
        }

        /// <summary>
        /// The text in the Infotip.
        /// </summary>
        /// <remarks><br/>
        /// The text uses a HTML like syntax.<br/>
        /// <br/>
        /// Supported tags are:<br/>
        /// <br/>
        /// &lt;Font Size="Size in Pixels" Face="Font Name" Color="Named color" &gt;&lt;/Font&gt; Set Font size,color and fontname.<br/>
        /// &lt;HR&gt; : Inserts a horizontal separator line.<br/>
        /// &lt;BR&gt; : Line break.<br/>
        /// &lt;B&gt;&lt;/B&gt; : Activate/Deactivate Bold style.<br/>
        /// &lt;I&gt;&lt;/I&gt; : Activate/Deactivate Italic style.<br/>
        /// &lt;U&gt;&lt;/U&gt; : Activate/Deactivate Underline style.	<br/>			
        /// </remarks>	
        /// <example >
        /// <code>
        /// MySyntaxBox.InfoTipText="public void MyMethod ( &lt;b&gt; string text &lt;/b&gt; );"; 		
        /// </code>
        /// </example>	
        [Category("Appearance - InfoTip"), Description("The infotip text")
        ]
        [DefaultValue("")]
        public string InfoTipText
        {
            get { return ((EditViewControl) _ActiveView).InfoTip.Data; }
            set
            {
                if ((_ActiveView) == null || ((EditViewControl)
                                              _ActiveView).InfoTip == null)
                    return;

                ((EditViewControl) _ActiveView).InfoTip.Data = value;
            }
        }

        /// <summary>
        /// Gets the Selection object from the active view.
        /// </summary>
        [Browsable(false)]
        public Selection Selection
        {
            get
            {
                if ((_ActiveView) != null)
                {
                    return ((EditViewControl) _ActiveView).Selection;
                }
                return null;
            }
        }

        /// <summary>
        /// Collection of KeyboardActions that is used by the control.
        /// Keyboard actions to add shortcut key combinations to certain tasks.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public KeyboardActionList KeyboardActions
        {
            get { return _KeyboardActions; }
            set { _KeyboardActions = value; }
        }

        /// <summary>
        /// Gets or Sets if the AutoList is visible in the active view.
        /// </summary>
        [Category("Appearance"), Description(
            "Gets or Sets if the AutoList is visible in the active view.")
        ]
        [Browsable(false)]
        public bool AutoListVisible
        {
            get
            {
                 return (_ActiveView) != null && ((EditViewControl) _ActiveView).AutoListVisible;
            }
            set
            {
                if ((_ActiveView) != null)
                    ((EditViewControl) _ActiveView).AutoListVisible = value;
            }
        }

        /// <summary>
        /// Gets or Sets if the InfoTip is visible in the active view.
        /// </summary>
        [Category("Appearance"), Description(
            "Gets or Sets if the InfoTip is visible in the active view.")
        ]
        [Browsable(false)]
        public bool InfoTipVisible
        {
            get
            {
                 return (_ActiveView) != null && ((EditViewControl) _ActiveView).InfoTipVisible;
            }
            set
            {
                if ((_ActiveView) != null)
                    ((EditViewControl) _ActiveView).InfoTipVisible = value;
            }
        }

        /// <summary>
        /// Gets if the control can perform a Copy action.
        /// </summary>
        [Browsable(false)]
        public bool CanCopy
        {
            get { return ((EditViewControl) _ActiveView).CanCopy; }
        }

        /// <summary>
        /// Gets if the control can perform a Paste action.
        /// (if the clipboard contains a valid text).
        /// </summary>
        [Browsable(false)]
        public bool CanPaste
        {
            get { return ((EditViewControl) _ActiveView).CanPaste; }
        }


        /// <summary>
        /// Gets if the control can perform a ReDo action.
        /// </summary>
        [Browsable(false)]
        public bool CanRedo
        {
            get { return ((EditViewControl) _ActiveView).CanRedo; }
        }

        /// <summary>
        /// Gets if the control can perform an Undo action.
        /// </summary>
        [Browsable(false)]
        public bool CanUndo
        {
            get { return ((EditViewControl) _ActiveView).CanUndo; }
        }

        /// <summary>
        /// Gets or Sets the imagelist to use in the gutter margin.
        /// </summary>
        /// <remarks>
        /// Image Index 0 is used to display the Breakpoint icon.
        /// Image Index 1 is used to display the Bookmark icon.
        /// </remarks>		
        [Category("Appearance - Gutter Margin"), Description(
            "Gets or Sets the imagelist to use in the gutter margin.")]
        public
            ImageList GutterIcons
        {
            get { return _GutterIcons; }
            set
            {
                _GutterIcons = value;
                Redraw();
            }
        }

        /// <summary>
        /// Gets or Sets the imagelist to use in the autolist.
        /// </summary>
        [Category("Appearance"), Description(
            "Gets or Sets the imagelist to use in the autolist.")
        ]
        [DefaultValue(null)]
        public ImageList AutoListIcons
        {
            get { return _AutoListIcons; }
            set
            {
                _AutoListIcons = value;


                foreach (EditViewControl ev in Views)
                {
                    if (ev != null && ev.AutoList != null)
                        ev.AutoList.Images = value;
                }
                Redraw();
            }
        }


        /// <summary>
        /// Gets or Sets the color to use when rendering Tab guides.
        /// </summary>
        [Category("Appearance - Tabs")]
        [Description(
            "Gets or Sets the color to use when rendering Tab guides.")
        ]
        [DefaultValue(typeof (Color), "Control")]
        public Color
            TabGuideColor
        {
            get { return _TabGuideColor; }
            set
            {
                _TabGuideColor = value;
                Redraw();
            }
        }

        /// <summary>
        /// Gets or Sets the color of the bracket match borders.
        /// </summary>
        /// <remarks>
        /// NOTE: use Color.Transparent to turn off the bracket match borders.
        /// </remarks>
        [Category("Appearance - Bracket Match")]
        [Description(
            "Gets or Sets the color of the bracket match borders.")
        ]
        [DefaultValue(typeof (Color), "DarkBlue")]
        public Color
            BracketBorderColor
        {
            get { return _BracketBorderColor; }
            set
            {
                _BracketBorderColor = value;
                Redraw();
            }
        }

        /// <summary>
        /// Gets or Sets if the control should render Tab guides.
        /// </summary>
        [Category("Appearance - Tabs")]
        [Description(
            "Gets or Sets if the control should render Tab guides.")
        ]
        [DefaultValue(false)]
        public bool ShowTabGuides
        {
            get { return _ShowTabGuides; }
            set
            {
                _ShowTabGuides = value;
                Redraw();
            }
        }

        /// <summary>
        /// Gets or Sets the color to use when rendering whitespace characters
        /// </summary>
        [Category("Appearance")]
        [Description(
            "Gets or Sets the color to use when rendering whitespace characters.")]
        [DefaultValue(typeof (Color), "Control")]
        public Color WhitespaceColor
        {
            get { return _WhitespaceColor; }
            set
            {
                _WhitespaceColor = value;
                Redraw();
            }
        }

        /// <summary>
        /// Gets or Sets the color of the code Outlining (both folding lines and collapsed blocks).
        /// </summary>
        [Category("Appearance")]
        [Description(
            "Gets or Sets the color of the code Outlining (both folding lines and collapsed blocks).")]
        [DefaultValue(typeof (Color), "ControlDark")]
        public Color OutlineColor
        {
            get { return _OutlineColor; }
            set
            {
                _OutlineColor = value;
                InitGraphics();
                Redraw();
            }
        }


        /// <summary>
        /// Determines if the control should use a smooth scroll when scrolling one row up or down.
        /// </summary>
        [Category("Behavior")]
        [Description("Determines if the control should use a smooth scroll when scrolling one row up or down.")]
        [DefaultValue(typeof (Color), "False")]
        public bool SmoothScroll { get; set; }

        /// <summary>
        /// Gets or Sets the speed of the vertical scroll when SmoothScroll is activated
        /// </summary>
        [Category("Behavior")]
        [Description(
            "Gets or Sets the speed of the vertical scroll when SmoothScroll is activated")]
        [DefaultValue(2)]
        public int SmoothScrollSpeed
        {
            get { return _SmoothScrollSpeed; }
            set
            {
                if (value <= 0)
                {
                    throw (new Exception("Scroll speed may not be less than 1"));
                }

                _SmoothScrollSpeed = value;
            }
        }

        /// <summary>
        /// Gets or Sets if the control can display breakpoints or not.
        /// </summary>
        [Category("Behavior")]
        [Description(
            "Gets or Sets if the control can display breakpoints or not.")
        ]
        [DefaultValue(true)]
        public bool AllowBreakPoints
        {
            get { return _AllowBreakPoints; }
            set { _AllowBreakPoints = value; }
        }

        /// <summary>
        /// Gets or Sets if the control should perform a full parse of the document when content is drag dropped or pasted into the control
        /// </summary>
        [Category("Behavior - Clipboard")]
        [Description(
            "Gets or Sets if the control should perform a full parse of the document when content is drag dropped or pasted into the control"
            )]
        [DefaultValue(false)]
        public bool ParseOnPaste
        {
            get { return _ParseOnPaste; }
            set
            {
                _ParseOnPaste = value;
                Redraw();
            }
        }

        /// <summary>
        /// Gets or Sets the Size of the font.
        /// <seealso cref="FontName"/>
        /// </summary>
        [Category("Appearance - Font")]
        [Description("The size of the font")
        ]
        [DefaultValue(10f)]
        public float FontSize
        {
            get { return _FontSize; }
            set
            {
                _FontSize = value;
                InitGraphics();
                Redraw();
            }
        }

        /// <summary>
        /// Determines what indentstyle to use on a new line.
        /// </summary>
        [Category("Behavior")]
        [Description(
            "Determines how the the control indents a new line")
        ]
        [DefaultValue(IndentStyle.LastRow)]
        public IndentStyle Indent
        {
            get { return _Indent; }
            set { _Indent = value; }
        }

        /// <summary>
        /// Gets or Sets the SyntaxDocument the control is currently attatched to.
        /// </summary>
        [Category("Content")]
        [Description(
            "The SyntaxDocument that is attatched to the contro")]
        public
            SyntaxDocument Document
        {
            get { return _Document; }
            set { AttachDocument(value); }
        }

        /// <summary>
        /// Get or Set the delay before the tooltip is displayed over a collapsed block
        /// </summary>
        [Category("Behavior")]
        [Description(
            "The delay before the tooltip is displayed over a collapsed block")]
        [DefaultValue(240)]
        public int TooltipDelay
        {
            get { return _TooltipDelay; }
            set { _TooltipDelay = value; }
        }

        // ROB: Added property to turn collapsed block tooltips on and off.

        /// <summary>
        /// Get or Set whether or not tooltips will be deplayed for collapsed blocks.
        /// </summary>
        [Category("Behavior")]
        [Description("The delay before the tooltip is displayed over a collapsed block")]
        [DefaultValue(true)]
        public bool CollapsedBlockTooltipsEnabled
        {
            get { return _CollapsedBlockTooltipsEnabled; }
            set { _CollapsedBlockTooltipsEnabled = value; }
        }

        // END-ROB ----------------------------------------------------------

        /// <summary>
        /// Get or Set the delay before the tooltip is displayed over a collapsed block
        /// </summary>
        [Category("Behavior")]
        [Description("Determines if the control is readonly or not")]
        [DefaultValue(false)]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or Sets the name of the font.
        /// <seealso cref="FontSize"/>
        /// </summary>
        [Category("Appearance - Font")]
        [Description(
            "The name of the font that is used to render the control")
        ]
        [Editor(typeof (FontList), typeof
            (UITypeEditor))]
        [DefaultValue("Courier New")
        ]
        public string FontName
        {
            get { return _FontName; }
            set
            {
                if (Views == null)
                    return;

                _FontName = value;
                InitGraphics();
                foreach (EditViewControl evc in Views)
                    evc.CalcMaxCharWidth();

                Redraw();
            }
        }

        /// <summary>
        /// Gets or Sets if bracketmatching is active
        /// <seealso cref="BracketForeColor"/>
        /// <seealso cref="BracketBackColor"/>
        /// </summary>
        [Category("Appearance - Bracket Match")]
        [Description(
            "Determines if the control should highlight scope patterns")
        ]
        [DefaultValue(true)]
        public bool BracketMatching
        {
            get { return _BracketMatching; }
            set
            {
                _BracketMatching = value;
                Redraw();
            }
        }

        /// <summary>
        /// Gets or Sets if Virtual Whitespace is active.
        /// <seealso cref="ShowWhitespace"/>
        /// </summary>
        [Category("Behavior")]
        [Description(
            "Determines if virtual Whitespace is active")]
        [DefaultValue(false)
        ]
        public bool VirtualWhitespace
        {
            get { return _VirtualWhitespace; }
            set
            {
                _VirtualWhitespace = value;
                Redraw();
            }
        }

        /// <summary>
        /// Gets or Sets the separator Color.
        /// <seealso cref="BracketMatching"/>
        /// <seealso cref="BracketBackColor"/>
        /// </summary>
        [Category("Appearance")]
        [Description("The separator color")]
        [DefaultValue
            (typeof (Color), "Control")]
        public Color SeparatorColor
        {
            get { return _SeparatorColor; }
            set
            {
                _SeparatorColor = value;
                Redraw();
            }
        }


        /// <summary>
        /// Gets or Sets the foreground Color to use when BracketMatching is activated.
        /// <seealso cref="BracketMatching"/>
        /// <seealso cref="BracketBackColor"/>
        /// </summary>
        [Category("Appearance - Bracket Match")]
        [Description(
            "The foreground color to use when BracketMatching is activated")
        ]
        [DefaultValue(typeof (Color), "Black")]
        public Color
            BracketForeColor
        {
            get { return _BracketForeColor; }
            set
            {
                _BracketForeColor = value;
                Redraw();
            }
        }

        /// <summary>
        /// Gets or Sets the background Color to use when BracketMatching is activated.
        /// <seealso cref="BracketMatching"/>
        /// <seealso cref="BracketForeColor"/>
        /// </summary>
        [Category("Appearance - Bracket Match")]
        [Description(
            "The background color to use when BracketMatching is activated")
        ]
        [DefaultValue(typeof (Color), "LightSteelBlue")]
        public Color
            BracketBackColor
        {
            get { return _BracketBackColor; }
            set
            {
                _BracketBackColor = value;
                Redraw();
            }
        }


        /// <summary>
        /// The inactive selection background color.
        /// </summary>
        [Category("Appearance - Selection")]
        [Description(
            "The inactive selection background color.")]
        [DefaultValue(typeof
            (Color), "ControlDark")]
        public Color InactiveSelectionBackColor
        {
            get { return _InactiveSelectionBackColor; }
            set
            {
                _InactiveSelectionBackColor = value;
                Redraw();
            }
        }

        /// <summary>
        /// The inactive selection foreground color.
        /// </summary>
        [Category("Appearance - Selection")]
        [Description(
            "The inactive selection foreground color.")]
        [DefaultValue(typeof
            (Color), "ControlLight")]
        public Color InactiveSelectionForeColor
        {
            get { return _InactiveSelectionForeColor; }
            set
            {
                _InactiveSelectionForeColor = value;
                Redraw();
            }
        }

        /// <summary>
        /// The selection background color.
        /// </summary>
        [Category("Appearance - Selection")]
        [Description(
            "The selection background color.")]
        [DefaultValue(typeof (Color),
            "Highlight")]
        public Color SelectionBackColor
        {
            get { return _SelectionBackColor; }
            set
            {
                _SelectionBackColor = value;
                Redraw();
            }
        }

        /// <summary>
        /// The selection foreground color.
        /// </summary>
        [Category("Appearance - Selection")]
        [Description(
            "The selection foreground color.")]
        [DefaultValue(typeof (Color),
            "HighlightText")]
        public Color SelectionForeColor
        {
            get { return _SelectionForeColor; }
            set
            {
                _SelectionForeColor = value;
                Redraw();
            }
        }

        /// <summary>
        /// Gets or Sets the border Color of the gutter margin.
        /// <seealso cref="GutterMarginColor"/>
        /// </summary>
        [Category("Appearance - Gutter Margin")]
        [Description(
            "The border color of the gutter margin")]
        [DefaultValue(typeof
            (Color), "ControlDark")]
        public Color GutterMarginBorderColor
        {
            get { return _GutterMarginBorderColor; }
            set
            {
                _GutterMarginBorderColor = value;
                InitGraphics();
                Redraw();
            }
        }

        /// <summary>
        /// Gets or Sets the border Color of the line number margin
        /// <seealso cref="LineNumberForeColor"/>
        /// <seealso cref="LineNumberBackColor"/>
        /// </summary>
        [Category("Appearance - Line Numbers")]
        [Description(
            "The border color of the line number margin")]
        [DefaultValue
            (typeof (Color), "Teal")]
        public Color LineNumberBorderColor
        {
            get { return _LineNumberBorderColor; }
            set
            {
                _LineNumberBorderColor = value;
                InitGraphics();
                Redraw();
            }
        }


        /// <summary>
        /// Gets or Sets the foreground Color of a Breakpoint.
        /// <seealso cref="BreakPointBackColor"/>
        /// </summary>
        [Category("Appearance - BreakPoints")]
        [Description(
            "The foreground color of a Breakpoint")]
        [DefaultValue(typeof
            (Color), "White")]
        public Color BreakPointForeColor
        {
            get { return _BreakPointForeColor; }
            set
            {
                _BreakPointForeColor = value;
                Redraw();
            }
        }

        /// <summary>
        /// Gets or Sets the background Color to use for breakpoint rows.
        /// <seealso cref="BreakPointForeColor"/>
        /// </summary>
        [Category("Appearance - BreakPoints")]
        [Description(
            "The background color to use when BracketMatching is activated")
        ]
        [DefaultValue(typeof (Color), "DarkRed")]
        public Color
            BreakPointBackColor
        {
            get { return _BreakPointBackColor; }
            set
            {
                _BreakPointBackColor = value;
                Redraw();
            }
        }

        /// <summary>
        /// Gets or Sets the foreground Color of line numbers.
        /// <seealso cref="LineNumberBorderColor"/>
        /// <seealso cref="LineNumberBackColor"/>
        /// </summary>
        [Category("Appearance - Line Numbers")]
        [Description(
            "The foreground color of line numbers")]
        [DefaultValue(typeof
            (Color), "Teal")]
        public Color LineNumberForeColor
        {
            get { return _LineNumberForeColor; }
            set
            {
                _LineNumberForeColor = value;
                InitGraphics();
                Redraw();
            }
        }

        /// <summary>
        /// Gets or Sets the background Color of line numbers.
        /// <seealso cref="LineNumberForeColor"/>
        /// <seealso cref="LineNumberBorderColor"/>
        /// </summary>
        [Category("Appearance - Line Numbers")]
        [Description(
            "The background color of line numbers")]
        [DefaultValue(typeof
            (Color), "Window")]
        public Color LineNumberBackColor
        {
            get { return _LineNumberBackColor; }
            set
            {
                _LineNumberBackColor = value;
                InitGraphics();
                Redraw();
            }
        }

        /// <summary>
        /// Gets or Sets the Color of the gutter margin
        /// <seealso cref="GutterMarginBorderColor"/>
        /// </summary>
        [Category("Appearance - Gutter Margin")]
        [Description(
            "The color of the gutter margin")]
        [DefaultValue(typeof (Color),
            "Control")]
        public Color GutterMarginColor
        {
            get { return _GutterMarginColor; }
            set
            {
                _GutterMarginColor = value;
                InitGraphics();
                Redraw();
            }
        }

        /// <summary>
        /// Gets or Sets the background Color of the client area.
        /// </summary>
        [Category("Appearance")]
        [Description(
            "The background color of the client area")]
        [DefaultValue(typeof
            (Color), "Window")]
        public new Color BackColor
        {
            get { return _BackColor; }
            set
            {
                _BackColor = value;
                InitGraphics();
                Redraw();
            }
        }

        /// <summary>
        /// Gets or Sets the background Color of the active line.
        /// <seealso cref="HighLightActiveLine"/>
        /// </summary>
        [Category("Appearance - Active Line")]
        [Description(
            "The background color of the active line")]
        [DefaultValue(typeof
            (Color), "LightYellow")]
        public Color HighLightedLineColor
        {
            get { return _HighLightedLineColor; }
            set
            {
                _HighLightedLineColor = value;
                InitGraphics();
                Redraw();
            }
        }

        /// <summary>
        /// Determines if the active line should be highlighted.
        /// </summary>
        [Category("Appearance - Active Line")]
        [Description(
            "Determines if the active line should be highlighted")
        ]
        [DefaultValue(false)]
        public bool HighLightActiveLine
        {
            get { return _HighLightActiveLine; }
            set
            {
                _HighLightActiveLine = value;
                Redraw();
            }
        }

        /// <summary>
        /// Determines if Whitespace should be rendered as symbols.
        /// </summary>
        [Category("Appearance")]
        [Description(
            "Determines if Whitespace should be rendered as symbols")
        ]
        [DefaultValue(false)]
        public bool ShowWhitespace
        {
            get { return _ShowWhitespace; }
            set
            {
                _ShowWhitespace = value;
                Redraw();
            }
        }

        /// <summary>
        /// Determines if the line number margin should be visible.
        /// </summary>
        [Category("Appearance - Line Numbers")]
        [Description(
            "Determines if the line number margin should be visible")
        ]
        [DefaultValue(true)]
        public bool ShowLineNumbers
        {
            get { return _ShowLineNumbers; }
            set
            {
                _ShowLineNumbers = value;
                Redraw();
            }
        }

        /// <summary>
        /// Determines if the gutter margin should be visible.
        /// </summary>
        [Category("Appearance - Gutter Margin")]
        [Description(
            "Determines if the gutter margin should be visible")
        ]
        [DefaultValue(true)]
        public bool ShowGutterMargin
        {
            get { return _ShowGutterMargin; }
            set
            {
                _ShowGutterMargin = value;
                Redraw();
            }
        }

        /// <summary>
        /// Gets or Sets the witdth of the gutter margin in pixels.
        /// </summary>
        [Category("Appearance - Gutter Margin")]
        [Description(
            "Determines the width of the gutter margin in pixels")
        ]
        [DefaultValue(19)]
        public int GutterMarginWidth
        {
            get { return _GutterMarginWidth; }
            set
            {
                _GutterMarginWidth = value;
                Redraw();
            }
        }

        // ROB: Added .TabsToSpaces property.
        /// <summary>
        /// Gets or Sets the 'Tabs To Spaces' feature of the editor.
        /// </summary>
        [Category("Appearance - Tabs")]
        [Description("Determines whether or not the SyntaxBox converts tabs to spaces as you type.")]
        [DefaultValue(false)]
        public bool TabsToSpaces { get; set; }

        /// <summary>
        /// Get or Sets the size of a TAB char in number of SPACES.
        /// </summary>
        [Category("Appearance - Tabs")]
        [Description(
            "Determines the size of a TAB in number of SPACE chars")
        ]
        [DefaultValue(4)]
        public int TabSize
        {
            get { return _TabSize; }
            set
            {
                _TabSize = value;
                Redraw();
            }
        }

        #region PUBLIC PROPERTY SHOWSCOPEINDICATOR

        private bool _ShowScopeIndicator;

        [Category("Appearance - Scopes"), Description(
            "Determines if the scope indicator should be shown")
        ]
        [DefaultValue(true)]
        public bool ShowScopeIndicator
        {
            get { return _ShowScopeIndicator; }
            set
            {
                _ShowScopeIndicator = value;
                Redraw();
            }
        }

        #endregion

        // END-ROB

        // ROB: Added method: ConvertTabsToSpaces()
        /// <summary>
        /// Converts all tabs to spaces the size of .TabSize in the Document.
        /// </summary>
        public void ConvertTabsToSpaces()
        {
            if (_Document != null)
            {
                _Document.StartUndoCapture();
                var spaces = new string(' ', _TabSize);
                // Iterate all rows and convert tabs to spaces.
                for (int count = 0; count < _Document.Count; count++)
                {
                    Row row = _Document[count];

                    string rowText = row.Text;
                    string newText = rowText.Replace("\t", spaces);
                    // If this has made a change to the row, update it.
                    if (newText != rowText)
                    {
                        _Document.DeleteRange(new TextRange(0, count, rowText.Length, count));
                        _Document.InsertText(newText, 0, count, true);
                    }
                }
                _Document.EndUndoCapture();
            }
        }

        // END-ROB

        // ROB: Added method: ConvertSpacesToTabs()
        /// <summary>
        /// Converts all spaces the size of .TabSize in the Document to tabs.
        /// </summary>
        public void ConvertSpacesToTabs()
        {
            if (_Document != null)
            {
                _Document.StartUndoCapture();
                var spaces = new string(' ', _TabSize);
                // Iterate all rows and convert tabs to spaces.
                for (int count = 0; count < _Document.Count; count++)
                {
                    Row row = _Document[count];

                    string rowText = row.Text;
                    string newText = rowText.Replace(spaces, "\t");
                    // If this has made a change to the row, update it.
                    if (newText != rowText)
                    {
                        _Document.DeleteRange(new TextRange(0, count, rowText.Length - 1, count));
                        _Document.InsertText(newText, 0, count, true);
                    }
                }
                _Document.EndUndoCapture();
            }
        }

        // END-ROB

        #endregion // PUBLIC PROPERTIES

        #region Public Methods

        /// <summary>
        /// Gets the Caret object from the active view.
        /// </summary>
        [Browsable(false)]
        public Caret Caret
        {
            get
            {
                if ((_ActiveView) != null)
                {
                    return ((EditViewControl) _ActiveView).Caret;
                }
                return null;
            }
        }

        public void ScrollIntoView(int RowIndex)
        {
            ((EditViewControl) _ActiveView).ScrollIntoView(RowIndex);
        }

        /// <summary>
        /// Disables painting while loading data into the Autolist
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <example>
        /// <code>
        /// MySyntaxBox.AutoListClear();
        /// MySyntaxBox.AutoListBeginLoad();
        /// MySyntaxBox.AutoListAdd ("test",1);
        /// MySyntaxBox.AutoListAdd ("test",2);
        /// MySyntaxBox.AutoListAdd ("test",3);
        /// MySyntaxBox.AutoListAdd ("test",4);
        /// MySyntaxBox.AutoListEndLoad();
        /// </code>
        /// </example>
        public void AutoListBeginLoad()
        {
            ((EditViewControl) _ActiveView).AutoListBeginLoad();
        }

        /// <summary>
        /// Resumes painting and autosizes the Autolist.			
        /// </summary>		
        public void AutoListEndLoad()
        {
            ((EditViewControl) _ActiveView).AutoListEndLoad();
        }

        /// <summary>
        /// Clears the content in the autolist.
        /// </summary>
        public void AutoListClear()
        {
            ((EditViewControl) _ActiveView).AutoList.Clear();
        }

        /// <summary>
        /// Adds an item to the autolist control.
        /// </summary>
        /// <example>
        /// <code>
        /// MySyntaxBox.AutoListClear();
        /// MySyntaxBox.AutoListBeginLoad();
        /// MySyntaxBox.AutoListAdd ("test",1);
        /// MySyntaxBox.AutoListAdd ("test",2);
        /// MySyntaxBox.AutoListAdd ("test",3);
        /// MySyntaxBox.AutoListAdd ("test",4);
        /// MySyntaxBox.AutoListEndLoad();
        /// </code>
        /// </example>
        /// <param name="text">The text to display in the autolist</param>
        /// <param name="ImageIndex">The image index in the AutoListIcons</param>
        public void AutoListAdd(string text, int ImageIndex)
        {
            ((EditViewControl) _ActiveView).AutoList.Add(text, ImageIndex);
        }

        /// <summary>
        /// Adds an item to the autolist control.
        /// </summary>
        /// <param name="text">The text to display in the autolist</param>
        /// <param name="InsertText">The text to insert in the code</param>
        /// <param name="ImageIndex">The image index in the AutoListIcons</param>
        public void AutoListAdd(string text, string InsertText, int ImageIndex)
        {
            ((EditViewControl) _ActiveView).AutoList.Add(text, InsertText, ImageIndex);
        }

        /// <summary>
        /// Adds an item to the autolist control.
        /// </summary>
        /// <param name="text">The text to display in the autolist</param>
        /// <param name="InsertText">The text to insert in the code</param>
        /// <param name="ToolTip"></param>
        /// <param name="ImageIndex">The image index in the AutoListIcons</param>
        public void AutoListAdd(string text, string InsertText, string ToolTip, int
                                                                                    ImageIndex)
        {
            ((EditViewControl) _ActiveView).AutoList.Add(text, InsertText, ToolTip,
                                                         ImageIndex);
        }

        /// <summary>
        /// Converts a Client pixel coordinate into a TextPoint (Column/Row)
        /// </summary>
        /// <param name="x">Pixel x position</param>
        /// <param name="y">Pixel y position</param>
        /// <returns>The row and column at the given pixel coordinate.</returns>
        public TextPoint CharFromPixel(int x, int y)
        {
            return ((EditViewControl) _ActiveView).CharFromPixel(x, y);
        }

        /// <summary>
        /// Clears the selection in the active view.
        /// </summary>
        public void ClearSelection()
        {
            ((EditViewControl) _ActiveView).ClearSelection();
        }

        /// <summary>
        /// Executes a Copy action on the selection in the active view.
        /// </summary>
        public void Copy()
        {
            ((EditViewControl) _ActiveView).Copy();
        }

        /// <summary>
        /// Executes a Cut action on the selection in the active view.
        /// </summary>
        public void Cut()
        {
            ((EditViewControl) _ActiveView).Cut();
        }

        /// <summary>
        /// Executes a Delete action on the selection in the active view.
        /// </summary>
        public void Delete()
        {
            ((EditViewControl) _ActiveView).Delete();
        }

        /// <summary>
        /// Moves the caret of the active view to a specific row.
        /// </summary>
        /// <param name="RowIndex">the row to jump to</param>
        public void GotoLine(int RowIndex)
        {
            ((EditViewControl) _ActiveView).GotoLine(RowIndex);
        }

        /// <summary>
        /// Moves the caret of the active view to the next bookmark.
        /// </summary>
        public void GotoNextBookmark()
        {
            ((EditViewControl) _ActiveView).GotoNextBookmark();
        }

        /// <summary>
        /// Moves the caret of the active view to the previous bookmark.
        /// </summary>
        public void GotoPreviousBookmark()
        {
            ((EditViewControl) _ActiveView).GotoPreviousBookmark();
        }


        /// <summary>
        /// Takes a pixel position and returns true if that position is inside the selected text.
        /// 
        /// </summary>
        /// <param name="x">Pixel x position.</param>
        /// <param name="y">Pixel y position</param>
        /// <returns>true if the position is inside the selection.</returns>
        public bool IsOverSelection(int x, int y)
        {
            return ((EditViewControl) _ActiveView).IsOverSelection(x, y);
        }

        /// <summary>
        /// Execute a Paste action if possible.
        /// </summary>
        public void Paste()
        {
            ((EditViewControl) _ActiveView).Paste();
        }

        /// <summary>
        /// Execute a ReDo action if possible.
        /// </summary>
        public void Redo()
        {
            ((EditViewControl) _ActiveView).Redo();
        }

        /// <summary>
        /// Makes the caret in the active view visible on screen.
        /// </summary>
        public void ScrollIntoView()
        {
            ((EditViewControl) _ActiveView).ScrollIntoView();
        }

        /// <summary>
        /// Scrolls the active view to a specific position.
        /// </summary>
        /// <param name="Pos"></param>
        public void ScrollIntoView(TextPoint Pos)
        {
            ((EditViewControl) _ActiveView).ScrollIntoView(Pos);
        }

        /// <summary>
        /// Select all the text in the active view.
        /// </summary>
        public void SelectAll()
        {
            ((EditViewControl) _ActiveView).SelectAll();
        }

        /// <summary>
        /// Selects the next word (from the current caret position) that matches the parameter criterias.
        /// </summary>
        /// <param name="Pattern">The pattern to find</param>
        /// <param name="MatchCase">Match case , true/false</param>
        /// <param name="WholeWords">Match whole words only , true/false</param>
        /// <param name="UseRegEx">To be implemented</param>
        public void FindNext(string Pattern, bool MatchCase, bool WholeWords, bool
                                                                                  UseRegEx)
        {
            ((EditViewControl) _ActiveView).SelectNext(Pattern, MatchCase, WholeWords,
                                                       UseRegEx);
        }

        /// <summary>
        /// Finds the next occurance of the pattern in the find/replace dialog
        /// </summary>
        public void FindNext()
        {
            ((EditViewControl) _ActiveView).FindNext();
        }

        /// <summary>
        /// Shows the default GotoLine dialog.
        /// </summary>
        /// <example>
        /// <code>
        /// //Display the Goto Line dialog
        /// MySyntaxBox.ShowGotoLine();
        /// </code>
        /// </example>
        public void ShowGotoLine()
        {
            ((EditViewControl) _ActiveView).ShowGotoLine();
        }

        /// <summary>
        /// Not yet implemented
        /// </summary>
        public void ShowSettings()
        {
            ((EditViewControl) _ActiveView).ShowSettings();
        }

        /// <summary>
        /// Toggles a bookmark on the active row of the active view.
        /// </summary>
        public void ToggleBookmark()
        {
            ((EditViewControl) _ActiveView).ToggleBookmark();
        }

        /// <summary>
        /// Executes an undo action if possible.
        /// </summary>
        public void Undo()
        {
            ((EditViewControl) _ActiveView).Undo();
        }


        /// <summary>
        /// Shows the Find dialog
        /// </summary>
        /// <example>
        /// <code>
        /// //Show FindReplace dialog
        /// MySyntaxBox.ShowFind();
        /// </code>
        /// </example>
        public void ShowFind()
        {
            ((EditViewControl) _ActiveView).ShowFind();
        }

        /// <summary>
        /// Shows the Replace dialog
        /// </summary>
        /// <example>
        /// <code>
        /// //Show FindReplace dialog
        /// MySyntaxBox.ShowReplace();
        /// </code>
        /// </example>
        public void ShowReplace()
        {
            ((EditViewControl) _ActiveView).ShowReplace();
        }

        #endregion //END Public Methods

        [Browsable(false)]
        [Obsolete("Use .FontName and .FontSize", true)]
        public
            override Font Font
        {
            get { return base.Font; }
            set { base.Font = value; }
        }

        //		[Browsable(true)]
        //		[DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden)]
        //		[RefreshProperties (RefreshProperties.All)]
        //		public override string Text
        //		{
        //			get
        //			{
        //				return this.Document.Text;
        //			}
        //			set
        //			{
        //				this.Document.Text=value;
        //			}
        //		}

        [Browsable(false)]
        [Obsolete("Apply a syntax instead", true)]
        public override
            Color ForeColor
        {
            get { return base.ForeColor; }
            set { base.ForeColor = value; }
        }

        /// <summary>
        /// The currently highlighted text in the autolist.
        /// </summary>
        [Browsable(false)]
        public string AutoListSelectedText
        {
            get { return ((EditViewControl) _ActiveView).AutoList.SelectedText; }
            set
            {
                if ((_ActiveView) == null || ((EditViewControl)
                                              _ActiveView).AutoList == null)
                    return;

                ((EditViewControl) _ActiveView).AutoList.SelectItem(value);
            }
        }

        public void Save(string filename)
        {
            string text = Document.Text;

            var swr = new StreamWriter(filename);

            swr.Write(text);

            swr.Flush();

            swr.Close();
        }

        public void Open(string filename)
        {
            if (Document == null)
                throw new NullReferenceException("CodeEditorControl.Document");

            var swr = new StreamReader(filename);

            Document.Text = swr.ReadToEnd();

            swr.Close();
        }

        public void AttachDocument(SyntaxDocument document)
        {
            //_Document=document;

            if (_Document != null)
            {
                _Document.ParsingCompleted -= OnParsingCompleted;
                _Document.Parsing -= OnParse;
                _Document.Change -= OnChange;
            }

            if (document == null)
                document = new SyntaxDocument();

            _Document = document;

            if (_Document != null)
            {
                _Document.ParsingCompleted += OnParsingCompleted;
                _Document.Parsing += OnParse;
                _Document.Change += OnChange;
            }

            Redraw();
        }

        protected virtual void OnParse(object Sender, EventArgs e)
        {
            foreach (EditViewControl ev in Views)
            {
                ev.OnParse();
            }
        }

        protected virtual void OnParsingCompleted(object Sender, EventArgs e)
        {
            foreach (EditViewControl ev in Views)
            {
                ev.Invalidate();
            }
        }

        protected virtual void OnChange(object Sender, EventArgs e)
        {
            if (Views == null)
                return;


            foreach (EditViewControl ev in Views)
            {
                ev.OnChange();
            }
            OnTextChanged(EventArgs.Empty);
        }

        public void RemoveCurrentRow()
        {
            ((EditViewControl) _ActiveView).RemoveCurrentRow();
        }

        public void CutClear()
        {
            ((EditViewControl) _ActiveView).CutClear();
        }


        public void AutoListInsertSelectedText()
        {
            ((EditViewControl) _ActiveView).InsertAutolistText();
        }


        protected override SplitViewChildControl GetNewView()
        {
            return new EditViewControl(this);
        }

        protected override void OnImeModeChanged(EventArgs e)
        {
            base.OnImeModeChanged(e);
            foreach (EditViewControl ev in Views)
            {
                ev.ImeMode = ImeMode;
            }
        }

        #region Constructor

        /// <summary>
        /// Default constructor for the SyntaxBoxControl
        /// </summary>
        public SyntaxBoxControl()
        {
            try
            {
                Document = new SyntaxDocument();


                CreateViews();


                InitializeComponent();
                SetStyle(ControlStyles.Selectable, true);

                //assign keys
                KeyboardActions.Add(new KeyboardAction(Keys.Z, false, true, false,
                                                       false, Undo));
                KeyboardActions.Add(new KeyboardAction(Keys.Y, false, true, false,
                                                       false, Redo));

                KeyboardActions.Add(new KeyboardAction(Keys.F3, false, false, false,
                                                       true, FindNext));

                KeyboardActions.Add(new KeyboardAction(Keys.C, false, true, false, true,
                                                       Copy));
                KeyboardActions.Add(new KeyboardAction(Keys.X, false, true, false,
                                                       false, CutClear));
                KeyboardActions.Add(new KeyboardAction(Keys.V, false, true, false,
                                                       false, Paste));

                KeyboardActions.Add(new KeyboardAction(Keys.Insert, false, true, false,
                                                       true, Copy));
                KeyboardActions.Add(new KeyboardAction(Keys.Delete, true, false, false,
                                                       false, Cut));
                KeyboardActions.Add(new KeyboardAction(Keys.Insert, true, false, false,
                                                       false, Paste));

                KeyboardActions.Add(new KeyboardAction(Keys.A, false, true, false, true,
                                                       SelectAll));

                KeyboardActions.Add(new KeyboardAction(Keys.F, false, true, false,
                                                       false, ShowFind));
                KeyboardActions.Add(new KeyboardAction(Keys.H, false, true, false,
                                                       false, ShowReplace));
                KeyboardActions.Add(new KeyboardAction(Keys.G, false, true, false, true,
                                                       ShowGotoLine));
                KeyboardActions.Add(new KeyboardAction(Keys.T, false, true, false,
                                                       false, ShowSettings));

                KeyboardActions.Add(new KeyboardAction(Keys.F2, false, true, false,
                                                       true, ToggleBookmark));
                KeyboardActions.Add(new KeyboardAction(Keys.F2, false, false, false,
                                                       true, GotoNextBookmark));
                KeyboardActions.Add(new KeyboardAction(Keys.F2, true, false, false,
                                                       true, GotoPreviousBookmark)
                    );

                KeyboardActions.Add(new KeyboardAction(Keys.Escape, false, false, false,
                                                       true, ClearSelection));

                KeyboardActions.Add(new KeyboardAction(Keys.Tab, false, false, false,
                                                       false, Selection.Indent));
                KeyboardActions.Add(new KeyboardAction(Keys.Tab, true, false, false,
                                                       false, Selection.Outdent));

                AutoListIcons = _AutoListIcons;
            }
            catch
            {
                //	Console.WriteLine (x.StackTrace);
            }
        }

        #endregion //END Constructor		

        #region EventHandlers

        protected virtual void OnClipboardUpdated(CopyEventArgs e)
        {
            if (ClipboardUpdated != null)
                ClipboardUpdated(this, e);
        }

        protected virtual void OnRowMouseDown(RowMouseEventArgs e)
        {
            if (RowMouseDown != null)
                RowMouseDown(this, e);
        }

        protected virtual void OnRowMouseMove(RowMouseEventArgs e)
        {
            if (RowMouseMove != null)
                RowMouseMove(this, e);
        }

        protected virtual void OnRowMouseUp(RowMouseEventArgs e)
        {
            if (RowMouseUp != null)
                RowMouseUp(this, e);
        }

        protected virtual void OnRowClick(RowMouseEventArgs e)
        {
            if (RowClick != null)
                RowClick(this, e);
        }

        protected virtual void OnRowDoubleClick(RowMouseEventArgs e)
        {
            if (RowDoubleClick != null)
                RowDoubleClick(this, e);
        }


        private void ParseTimer_Tick(object sender, EventArgs e)
        {
            Document.ParseSome();
        }

        protected virtual void OnInfoTipSelectedIndexChanged()
        {
            if (InfoTipSelectedIndexChanged != null)
                InfoTipSelectedIndexChanged(null, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            if ((_ActiveView) != null)
            {
                (_ActiveView).Focus();
            }
        }


        private void View_RowClick(object sender,
                                   RowMouseEventArgs e)
        {
            OnRowClick(e);
        }

        private void View_RowDoubleClick(object sender,
                                         RowMouseEventArgs e)
        {
            OnRowDoubleClick(e);
        }

        private void View_RowMouseDown(object sender,
                                       RowMouseEventArgs e)
        {
            OnRowMouseDown(e);
        }

        private void View_RowMouseMove(object sender,
                                       RowMouseEventArgs e)
        {
            OnRowMouseMove(e);
        }

        private void View_RowMouseUp(object sender,
                                     RowMouseEventArgs e)
        {
            OnRowMouseUp(e);
        }

        private void View_ClipboardUpdated(object sender, CopyEventArgs e)
        {
            OnClipboardUpdated(e);
        }


        public void OnRenderRow(RowPaintEventArgs e)
        {
            if (RenderRow != null)
                RenderRow(this, e);
        }

        public void OnWordMouseHover(ref WordMouseEventArgs e)
        {
            if (WordMouseHover != null)
                WordMouseHover(this, ref e);
        }

        public void OnWordMouseDown(ref WordMouseEventArgs e)
        {
            if (WordMouseDown != null)
                WordMouseDown(this, ref e);
        }

        protected virtual void OnCaretChange(object sender)
        {
            if (CaretChange != null)
                CaretChange(this, null);
        }

        protected virtual void OnSelectionChange(object sender)
        {
            if (SelectionChange != null)
                SelectionChange(this, null);
        }

        private void View_CaretChanged(object s, EventArgs e)
        {
            OnCaretChange(s);
        }

        private void View_SelectionChanged(object s, EventArgs e)
        {
            OnSelectionChange(s);
        }

        private void View_DoubleClick(object sender, EventArgs e)
        {
            OnDoubleClick(e);
        }

        private void View_MouseUp(object sender,
                                  MouseEventArgs e)
        {
            var ev = (EditViewControl) sender;
            var ea = new MouseEventArgs(e.Button, e.Clicks, e.X +
                                                            ev.Location.X + ev.BorderWidth,
                                        e.Y + ev.Location.Y + ev.BorderWidth,
                                        e.Delta);
            OnMouseUp(ea);
        }

        private void View_MouseMove(object sender,
                                    MouseEventArgs e)
        {
            var ev = (EditViewControl) sender;
            var ea = new MouseEventArgs(e.Button, e.Clicks, e.X +
                                                            ev.Location.X + ev.BorderWidth,
                                        e.Y + ev.Location.Y + ev.BorderWidth,
                                        e.Delta);
            OnMouseMove(ea);
        }

        private void View_MouseLeave(object sender, EventArgs e)
        {
            OnMouseLeave(e);
        }

        private void View_MouseHover(object sender, EventArgs e)
        {
            OnMouseHover(e);
        }

        private void View_MouseEnter(object sender, EventArgs e)
        {
            OnMouseEnter(e);
        }

        private void View_MouseDown(object sender,
                                    MouseEventArgs e)
        {
            var ev = (EditViewControl) sender;
            var ea = new MouseEventArgs(e.Button, e.Clicks, e.X +
                                                            ev.Location.X + ev.BorderWidth,
                                        e.Y + ev.Location.Y + ev.BorderWidth,
                                        e.Delta);
            OnMouseDown(ea);
        }

        private void View_KeyUp(object sender, KeyEventArgs e)
        {
            OnKeyUp(e);
        }

        private void View_KeyPress(object sender,
                                   KeyPressEventArgs e)
        {
            OnKeyPress(e);
        }

        private void View_KeyDown(object sender, KeyEventArgs
                                                     e)
        {
            OnKeyDown(e);
        }

        private void View_Click(object sender, EventArgs e)
        {
            OnClick(e);
        }

        private void View_DragOver(object sender,
                                   DragEventArgs e)
        {
            OnDragOver(e);
        }

        private void View_DragLeave(object sender, EventArgs e)
        {
            OnDragLeave(e);
        }

        private void View_DragEnter(object sender,
                                    DragEventArgs e)
        {
            OnDragEnter(e);
        }

        private void View_DragDrop(object sender,
                                   DragEventArgs e)
        {
            OnDragDrop(e);
        }

        private void View_InfoTipSelectedIndexChanged(object sender,
                                                      EventArgs e)
        {
            OnInfoTipSelectedIndexChanged();
        }

        #endregion

        #region DISPOSE()

        /// <summary>
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                    components.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion //END DISPOSE

        #region Private/Protected/Internal methods

        private void InitializeComponent()
        {
            components = new Container();
            var resources = new
                ResourceManager(typeof (SyntaxBoxControl));
            _GutterIcons = new ImageList(components);
            _AutoListIcons = new ImageList(components);
            ParseTimer = new Timer (components);
            // 
            // _GutterIcons
            // 
            _GutterIcons.ColorDepth = ColorDepth.Depth32Bit;
            _GutterIcons.ImageSize = new Size(17, 17);
            _GutterIcons.ImageStream = ((ImageListStreamer)
                                        (resources.GetObject(
                                            "_GutterIcons.ImageStream")));
            _GutterIcons.TransparentColor = Color.Transparent;
            // 
            // _AutoListIcons
            // 
            _AutoListIcons.ColorDepth =
                ColorDepth.Depth8Bit;
            _AutoListIcons.ImageSize = new Size(16, 16);
            _AutoListIcons.ImageStream = (
                                             (ImageListStreamer) (resources.GetObject(
                                                                     "_AutoListIcons.ImageStream")));
            _AutoListIcons.TransparentColor = Color.Transparent;
            // 
            // ParseTimer
            //
            ParseTimer.Enabled = !DesignMode;
            ParseTimer.Interval = 1;
            ParseTimer.Tick += ParseTimer_Tick;
        }


        protected override void OnLoad(EventArgs e)
        {
            Refresh();
        }

        private void Redraw()
        {
            if (Views == null)
                return;

            foreach (EditViewControl ev in Views)
            {
                if (ev != null)
                {
                    ev.Refresh();
                }
            }
        }

        private void InitGraphics()
        {
            if (Views == null || Parent == null)
                return;

            foreach (EditViewControl ev in Views)
            {
                ev.InitGraphics();
            }
        }


        protected override void CreateViews()
        {
            base.CreateViews();

            foreach (EditViewControl ev in Views)
            {
                if (DoOnce && ev == LowerRight)
                    continue;

                //attatch events to views
                ev.Enter += View_Enter;
                ev.Leave += View_Leave;
                ev.GotFocus += View_Enter;
                ev.LostFocus += View_Leave;
                ev.CaretChange += View_CaretChanged;
                ev.SelectionChange += View_SelectionChanged;
                ev.Click += View_Click;
                ev.DoubleClick += View_DoubleClick;
                ev.MouseDown += View_MouseDown;
                ev.MouseEnter += View_MouseEnter;
                ev.MouseHover += View_MouseHover;
                ev.MouseLeave += View_MouseLeave;
                ev.MouseMove += View_MouseMove;
                ev.MouseUp += View_MouseUp;
                ev.KeyDown += View_KeyDown;
                ev.KeyPress += View_KeyPress;
                ev.KeyUp += View_KeyUp;
                ev.DragDrop += View_DragDrop;
                ev.DragOver += View_DragOver;
                ev.DragLeave += View_DragLeave;
                ev.DragEnter += View_DragEnter;

                if (ev.InfoTip != null)
                {
                    ev.InfoTip.Data = "";
                    ev.InfoTip.SelectedIndexChanged += View_InfoTipSelectedIndexChanged;
                }

                ev.RowClick += View_RowClick;
                ev.RowDoubleClick += View_RowDoubleClick;

                ev.RowMouseDown += View_RowMouseDown;
                ev.RowMouseMove += View_RowMouseMove;
                ev.RowMouseUp += View_RowMouseUp;
                ev.ClipboardUpdated += View_ClipboardUpdated;
            }

            DoOnce = true;

            AutoListIcons = AutoListIcons;
            InfoTipImage = InfoTipImage;
            ChildBorderStyle = ChildBorderStyle;
            ChildBorderColor = ChildBorderColor;
            BackColor = BackColor;
            Document = Document;
            ImeMode = ImeMode;
            Redraw();
        }

        #endregion //END Private/Protected/Internal methods
    }
}