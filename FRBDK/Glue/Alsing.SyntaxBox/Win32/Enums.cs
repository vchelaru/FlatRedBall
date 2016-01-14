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
using System.Drawing;
using System.Runtime.InteropServices;

namespace Alsing.Windows
{

    #region WINDOWPOS

    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPOS
    {
        public IntPtr hwnd;
        public IntPtr hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public uint flags;
    }

    #endregion

    #region _NCCALCSIZE_PARAMS

    [StructLayout(LayoutKind.Sequential)]
    public struct _NCCALCSIZE_PARAMS
    {
        public APIRect NewRect;
        public APIRect OldRect;
        public APIRect OldClientRect;

        public WINDOWPOS lppos;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MyStruct
    {
        public int SomeValue;
        public byte b1;
        public byte b2;
        public byte b3;
        public byte b4;
        public byte b5;
        public byte b6;
        public byte b7;
        public byte b8;
    }

    #endregion

    public enum WindowStylesEx
    {
        WS_EX_DLGMODALFRAME = 0x00000001,
        WS_EX_NOPARENTNOTIFY = 0x00000004,
        WS_EX_TOPMOST = 0x00000008,
        WS_EX_ACCEPTFILES = 0x00000010,
        WS_EX_TRANSPARENT = 0x00000020,

        WS_EX_MDICHILD = 0x00000040,
        WS_EX_TOOLWINDOW = 0x00000080,
        WS_EX_WINDOWEDGE = 0x00000100,
        WS_EX_CLIENTEDGE = 0x00000200,
        WS_EX_CONTEXTHELP = 0x00000400,

        WS_EX_RIGHT = 0x00001000,
        WS_EX_LEFT = 0x00000000,
        WS_EX_RTLREADING = 0x00002000,
        WS_EX_LTRREADING = 0x00000000,
        WS_EX_LEFTSCROLLBAR = 0x00004000,
        WS_EX_RIGHTSCROLLBAR = 0x00000000,

        WS_EX_CONTROLPARENT = 0x00010000,
        WS_EX_STATICEDGE = 0x00020000,
        WS_EX_APPWINDOW = 0x00040000,
    }

    public enum WindowStyles
    {
        WS_OVERLAPPED = 0x00000000,
        WS_POPUP = unchecked((int) 0x80000000),
        WS_CHILD = 0x40000000,
        WS_MINIMIZE = 0x20000000,
        WS_VISIBLE = 0x10000000,
        WS_DISABLED = 0x08000000,
        WS_CLIPSIBLINGS = 0x04000000,
        WS_CLIPCHILDREN = 0x02000000,
        WS_MAXIMIZE = 0x01000000,
        WS_CAPTION = 0x00C00000,
        WS_BORDER = 0x00800000,
        WS_DLGFRAME = 0x00400000,
        WS_VSCROLL = 0x00200000,
        WS_HSCROLL = 0x00100000,
        WS_SYSMENU = 0x00080000,
        WS_THICKFRAME = 0x00040000,
        WS_GROUP = 0x00020000,
        WS_TABSTOP = 0x00010000,
        WS_MINIMIZEBOX = 0x00020000,
        WS_MAXIMIZEBOX = 0x00010000,
        WS_TILED = WS_OVERLAPPED,
        WS_ICONIC = WS_MINIMIZE,
        WS_SIZEBOX = WS_THICKFRAME,
    }

    public enum DrawTextFlags
    {
        DT_TOP = 0x00000000,
        DT_LEFT = 0x00000000,
        DT_CENTER = 0x00000001,
        DT_RIGHT = 0x00000002,
        DT_VCENTER = 0x00000004,
        DT_BOTTOM = 0x00000008,
        DT_WORDBREAK = 0x00000010,
        DT_SINGLELINE = 0x00000020,
        DT_EXPANDTABS = 0x00000040,
        DT_TABSTOP = 0x00000080,
        DT_NOCLIP = 0x00000100,
        DT_EXTERNALLEADING = 0x00000200,
        DT_CALCRECT = 0x00000400,
        DT_NOPREFIX = 0x00000800,
        DT_INTERNAL = 0x00001000,
        DT_EDITCONTROL = 0x00002000,
        DT_PATH_ELLIPSIS = 0x00004000,
        DT_END_ELLIPSIS = 0x00008000,
        DT_MODIFYSTRING = 0x00010000,
        DT_RTLREADING = 0x00020000,
        DT_WORD_ELLIPSIS = 0x00040000,
        DT_NOFULLWIDTHCHARBREAK = 0x00080000,
        DT_HIDEPREFIX = 0x00100000,
        DT_PREFIXONLY = 0x00200000,
    }

    public enum TextBoxNotifications
    {
        EN_SETFOCUS = 0x0100,
        EN_KILLFOCUS = 0x0200,
        EN_CHANGE = 0x0300,
        EN_UPDATE = 0x0400,
        EN_ERRSPACE = 0x0500,
        EN_MAXTEXT = 0x0501,
        EN_HSCROLL = 0x0601,
        EN_VSCROLL = 0x0602,
    }

    public enum TextBoxStyles
    {
        ES_LEFT = 0x0000,
        ES_CENTER = 0x0001,
        ES_RIGHT = 0x0002,
        ES_MULTILINE = 0x0004,
        ES_UPPERCASE = 0x0008,
        ES_LOWERCASE = 0x0010,
        ES_PASSWORD = 0x0020,
        ES_AUTOVSCROLL = 0x0040,
        ES_AUTOHSCROLL = 0x0080,
        ES_NOHIDESEL = 0x0100,
        ES_OEMCONVERT = 0x0400,
        ES_READONLY = 0x0800,
        ES_WANTRETURN = 0x1000,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct APIPoint
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct APIRect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public int Width
        {
            get { return right - left; }
        }

        public int Height
        {
            get { return bottom - top; }
        }

        public APIRect(Rectangle rect)
        {
            bottom = rect.Bottom;
            left = rect.Left;
            right = rect.Right;
            top = rect.Top;
        }

        public APIRect(int left, int top, int right, int bottom)
        {
            this.bottom = bottom;
            this.left = left;
            this.right = right;
            this.top = top;
        }
    }

    #region HitTest 

    public enum HitTest
    {
        HTERROR = (-2),
        HTTRANSPARENT = (-1),
        HTNOWHERE = 0,
        HTCLIENT = 1,
        HTCAPTION = 2,
        HTSYSMENU = 3,
        HTGROWBOX = 4,
        HTSIZE = HTGROWBOX,
        HTMENU = 5,
        HTHSCROLL = 6,
        HTVSCROLL = 7,
        HTMINBUTTON = 8,
        HTMAXBUTTON = 9,
        HTLEFT = 10,
        HTRIGHT = 11,
        HTTOP = 12,
        HTTOPLEFT = 13,
        HTTOPRIGHT = 14,
        HTBOTTOM = 15,
        HTBOTTOMLEFT = 16,
        HTBOTTOMRIGHT = 17,
        HTBORDER = 18,
        HTREDUCE = HTMINBUTTON,
        HTZOOM = HTMAXBUTTON,
        HTSIZEFIRST = HTLEFT,
        HTSIZELAST = HTBOTTOMRIGHT,
        HTOBJECT = 19,
        HTCLOSE = 20,
        HTHELP = 21
    }

    #endregion

    public enum TextBoxMessages
    {
        EM_GETSEL = 0x00B0,
        EM_LINEINDEX = 0x00BB,
        EM_LINEFROMCHAR = 0x00C9,
        EM_POSFROMCHAR = 0x00D6,
    }

    [Flags]
    public enum WMPrintFlags
    {
        PRF_CHECKVISIBLE = 0x00000001,
        PRF_NONCLIENT = 0x00000002,
        PRF_CLIENT = 0x00000004,
        PRF_ERASEBKGND = 0x00000008,
        PRF_CHILDREN = 0x00000010,
        PRF_OWNED = 0x0000020,
    }

    public enum WindowMessage
    {
        WM_NULL = 0x0000,
        WM_CREATE = 0x0001,
        WM_DESTROY = 0x0002,
        WM_MOVE = 0x0003,
        WM_SIZE = 0x0005,
        WM_ACTIVATE = 0x0006,
        WM_SETFOCUS = 0x0007,
        WM_KILLFOCUS = 0x0008,
        WM_ENABLE = 0x000A,
        WM_SETREDRAW = 0x000B,
        WM_SETTEXT = 0x000C,
        WM_GETTEXT = 0x000D,
        WM_GETTEXTLENGTH = 0x000E,
        WM_PAINT = 0x000F,
        WM_CLOSE = 0x0010,
        WM_QUERYENDSESSION = 0x0011,
        WM_QUIT = 0x0012,
        WM_QUERYOPEN = 0x0013,
        WM_ERASEBKGND = 0x0014,
        WM_SYSCOLORCHANGE = 0x0015,
        WM_ENDSESSION = 0x0016,
        WM_SHOWWINDOW = 0x0018,
        WM_CTLCOLOR = 0x0019,
        WM_WININICHANGE = 0x001A,
        WM_SETTINGCHANGE = 0x001A,
        WM_DEVMODECHANGE = 0x001B,
        WM_ACTIVATEAPP = 0x001C,
        WM_FONTCHANGE = 0x001D,
        WM_TIMECHANGE = 0x001E,
        WM_CANCELMODE = 0x001F,
        WM_SETCURSOR = 0x0020,
        WM_MOUSEACTIVATE = 0x0021,
        WM_CHILDACTIVATE = 0x0022,
        WM_QUEUESYNC = 0x0023,
        WM_GETMINMAXINFO = 0x0024,
        WM_PAINTICON = 0x0026,
        WM_ICONERASEBKGND = 0x0027,
        WM_NEXTDLGCTL = 0x0028,
        WM_SPOOLERSTATUS = 0x002A,
        WM_DRAWITEM = 0x002B,
        WM_MEASUREITEM = 0x002C,
        WM_DELETEITEM = 0x002D,
        WM_VKEYTOITEM = 0x002E,
        WM_CHARTOITEM = 0x002F,
        WM_SETFONT = 0x0030,
        WM_GETFONT = 0x0031,
        WM_SETHOTKEY = 0x0032,
        WM_GETHOTKEY = 0x0033,
        WM_QUERYDRAGICON = 0x0037,
        WM_COMPAREITEM = 0x0039,
        WM_GETOBJECT = 0x003D,
        WM_COMPACTING = 0x0041,
        WM_COMMNOTIFY = 0x0044,
        WM_WINDOWPOSCHANGING = 0x0046,
        WM_WINDOWPOSCHANGED = 0x0047,
        WM_POWER = 0x0048,
        WM_COPYDATA = 0x004A,
        WM_CANCELJOURNAL = 0x004B,
        WM_NOTIFY = 0x004E,
        WM_INPUTLANGCHANGEREQUEST = 0x0050,
        WM_INPUTLANGCHANGE = 0x0051,
        WM_TCARD = 0x0052,
        WM_HELP = 0x0053,
        WM_USERCHANGED = 0x0054,
        WM_NOTIFYFORMAT = 0x0055,
        WM_CONTEXTMENU = 0x007B,
        WM_STYLECHANGING = 0x007C,
        WM_STYLECHANGED = 0x007D,
        WM_DISPLAYCHANGE = 0x007E,
        WM_GETICON = 0x007F,
        WM_SETICON = 0x0080,
        WM_NCCREATE = 0x0081,
        WM_NCDESTROY = 0x0082,
        WM_NCCALCSIZE = 0x0083,
        WM_NCHITTEST = 0x0084,
        WM_NCPAINT = 0x0085,
        WM_NCACTIVATE = 0x0086,
        WM_GETDLGCODE = 0x0087,
        WM_SYNCPAINT = 0x0088,
        WM_NCMOUSEMOVE = 0x00A0,
        WM_NCLBUTTONDOWN = 0x00A1,
        WM_NCLBUTTONUP = 0x00A2,
        WM_NCLBUTTONDBLCLK = 0x00A3,
        WM_NCRBUTTONDOWN = 0x00A4,
        WM_NCRBUTTONUP = 0x00A5,
        WM_NCRBUTTONDBLCLK = 0x00A6,
        WM_NCMBUTTONDOWN = 0x00A7,
        WM_NCMBUTTONUP = 0x00A8,
        WM_NCMBUTTONDBLCLK = 0x00A9,
        WM_KEYDOWN = 0x0100,
        WM_KEYUP = 0x0101,
        WM_CHAR = 0x0102,
        WM_DEADCHAR = 0x0103,
        WM_SYSKEYDOWN = 0x0104,
        WM_SYSKEYUP = 0x0105,
        WM_SYSCHAR = 0x0106,
        WM_SYSDEADCHAR = 0x0107,
        WM_KEYLAST = 0x0108,
        WM_IME_STARTCOMPOSITION = 0x010D,
        WM_IME_ENDCOMPOSITION = 0x010E,
        WM_IME_COMPOSITION = 0x010F,
        WM_IME_KEYLAST = 0x010F,
        WM_INITDIALOG = 0x0110,
        WM_Element = 0x0111,
        WM_COMMAND = 0x0111,
        WM_SYSElement = 0x0112,
        WM_TIMER = 0x0113,
        WM_HSCROLL = 0x0114,
        WM_VSCROLL = 0x0115,
        WM_INITMENU = 0x0116,
        WM_INITMENUPOPUP = 0x0117,
        WM_MENUSELECT = 0x011F,
        WM_MENUCHAR = 0x0120,
        WM_ENTERIDLE = 0x0121,
        WM_MENURBUTTONUP = 0x0122,
        WM_MENUDRAG = 0x0123,
        WM_MENUGETOBJECT = 0x0124,
        WM_UNINITMENUPOPUP = 0x0125,
        WM_MENUElement = 0x0126,
        WM_CTLCOLORMSGBOX = 0x0132,
        WM_CTLCOLOREDIT = 0x0133,
        WM_CTLCOLORLISTBOX = 0x0134,
        WM_CTLCOLORBTN = 0x0135,
        WM_CTLCOLORDLG = 0x0136,
        WM_CTLCOLORSCROLLBAR = 0x0137,
        WM_CTLCOLORSTATIC = 0x0138,
        WM_MOUSEMOVE = 0x0200,
        WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202,
        WM_LBUTTONDBLCLK = 0x0203,
        WM_RBUTTONDOWN = 0x0204,
        WM_RBUTTONUP = 0x0205,
        WM_RBUTTONDBLCLK = 0x0206,
        WM_MBUTTONDOWN = 0x0207,
        WM_MBUTTONUP = 0x0208,
        WM_MBUTTONDBLCLK = 0x0209,
        WM_MOUSEWHEEL = 0x020A,
        WM_PARENTNOTIFY = 0x0210,
        WM_ENTERMENULOOP = 0x0211,
        WM_EXITMENULOOP = 0x0212,
        WM_NEXTMENU = 0x0213,
        WM_SIZING = 0x0214,
        WM_CAPTURECHANGED = 0x0215,
        WM_MOVING = 0x0216,
        WM_DEVICECHANGE = 0x0219,
        WM_MDICREATE = 0x0220,
        WM_MDIDESTROY = 0x0221,
        WM_MDIACTIVATE = 0x0222,
        WM_MDIRESTORE = 0x0223,
        WM_MDINEXT = 0x0224,
        WM_MDIMAXIMIZE = 0x0225,
        WM_MDITILE = 0x0226,
        WM_MDICASCADE = 0x0227,
        WM_MDIICONARRANGE = 0x0228,
        WM_MDIGETACTIVE = 0x0229,
        WM_MDISETMENU = 0x0230,
        WM_ENTERSIZEMOVE = 0x0231,
        WM_EXITSIZEMOVE = 0x0232,
        WM_DROPFILES = 0x0233,
        WM_MDIREFRESHMENU = 0x0234,
        WM_IME_SETCONTEXT = 0x0281,
        WM_IME_NOTIFY = 0x0282,
        WM_IME_CONTROL = 0x0283,
        WM_IME_COMPOSITIONFULL = 0x0284,
        WM_IME_SELECT = 0x0285,
        WM_IME_CHAR = 0x0286,
        WM_IME_REQUEST = 0x0288,
        WM_IME_KEYDOWN = 0x0290,
        WM_IME_KEYUP = 0x0291,
        WM_MOUSEHOVER = 0x02A1,
        WM_MOUSELEAVE = 0x02A3,
        WM_CUT = 0x0300,
        WM_COPY = 0x0301,
        WM_PASTE = 0x0302,
        WM_CLEAR = 0x0303,
        WM_UNDO = 0x0304,
        WM_RENDERFORMAT = 0x0305,
        WM_RENDERALLFORMATS = 0x0306,
        WM_DESTROYCLIPBOARD = 0x0307,
        WM_DRAWCLIPBOARD = 0x0308,
        WM_PAINTCLIPBOARD = 0x0309,
        WM_VSCROLLCLIPBOARD = 0x030A,
        WM_SIZECLIPBOARD = 0x030B,
        WM_ASKCBFORMATNAME = 0x030C,
        WM_CHANGECBCHAIN = 0x030D,
        WM_HSCROLLCLIPBOARD = 0x030E,
        WM_QUERYNEWPALETTE = 0x030F,
        WM_PALETTEISCHANGING = 0x0310,

        WM_PALETTECHANGED = 0x0311,
        WM_HOTKEY = 0x0312,
        WM_PRINT = 0x0317,
        WM_PRINTCLIENT = 0x0318,
        WM_HANDHELDFIRST = 0x0358,
        WM_HANDHELDLAST = 0x035F,
        WM_AFXFIRST = 0x0360,
        WM_AFXLAST = 0x037F,
        WM_PENWINFIRST = 0x0380,
        WM_PENWINLAST = 0x038F,
        WM_APP = 0x8000,
        WM_USER = 0x0400,
        WM_REFLECT = WM_USER + 0x1c00,
        WM_THEMECHANGED = 794,
    }


    public enum GDIRop
    {
        SrcCopy = 13369376,
        Blackness = 0, //to be implemented
        Whiteness = 0
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct GDITextMetric
    {
        public int tmMemoryHeight;
        public int tmAscent;
        public int tmDescent;
        public int tmInternalLeading;
        public int tmExternalLeading;
        public int tmAveCharWidth;
        public int tmMaxCharWidth;
        public int tmWeight;
        public int tmOverhang;
        public int tmDigitizedAspectX;
        public int tmDigitizedAspectY;
        public byte tmFirstChar;
        public byte tmLastChar;
        public byte tmDefaultChar;
        public byte tmBreakChar;
        public byte tmItalic;
        public byte tmUnderlined;
        public byte tmStruckOut;
        public byte tmPitchAndFamily;
        public byte tmCharSet;
    }

    //	public IntPtr Fontname;

    [StructLayout(LayoutKind.Sequential)]
    public class ENUMLOGFONTEX
    {
        public LogFont elfLogFont;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string elfFullName = "";
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] elfStyle;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] elfScript;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class COMPOSITIONFORM
    {
        public int dwStyle;
        public APIPoint ptCurrentPos;
        public APIRect rcArea;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class LogFont
    {
        public int lfHeight;
        public int lfWidth;
        public int lfEscapement;
        public int lfOrientation;
        public int lfWeight;
        public byte lfItalic;
        public byte lfUnderline;
        public byte lfStrikeOut;
        public byte lfCharSet;
        public byte lfOutPrecision;
        public byte lfClipPrecision;
        public byte lfQuality;
        public byte lfPitchAndFamily;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string lfFaceName = "";
    }

//	public enum VirtualKeys
//	{
//		VK_LBUTTON  = 0x01,
//	VK_RBUTTON =0x02,
//	VK_CANCEL =0x03,
//	VK_MBUTTON =0x04 ,
//	VK_XBUTTON1 =0x05,
//	VK_XBUTTON2 =0x06,		
//	VK_BACK =0x08 ,
//	VK_TAB =0x09,
//	—  0A–0B Reserved  
//	VK_CLEAR 0C CLEAR key  
//	VK_RETURN 0D ENTER key  
//	—  0E–0F Undefined  
//	VK_SHIFT 10 SHIFT key  
//	VK_CONTROL 11 CTRL key  
//	VK_MENU 12 ALT key  
//	VK_PAUSE 13 PAUSE key  
//	VK_CAPITAL 14 CAPS LOCK key  
//	VK_KANA 15 IME Kana mode 
//	VK_HANGUEL 15 IME Hanguel mode (maintained for compatibility; use VK_HANGUL) 
//	VK_HANGUL 15 IME Hangul mode 
//	—  16 Undefined  
//	VK_JUNJA 17 IME Junja mode 
//	VK_FINAL 18 IME final mode 
//	VK_HANJA 19 IME Hanja mode 
//	VK_KANJI 19 IME Kanji mode 
//	—  1A Undefined  
//	VK_ESCAPE 1B ESC key  
//	VK_CONVERT 1C IME convert 
//	VK_NONCONVERT 1D IME nonconvert 
//	VK_ACCEPT 1E IME accept 
//	VK_MODECHANGE 1F IME mode change request 
//	VK_SPACE 20 SPACEBAR  
//	VK_PRIOR 21 PAGE UP key  
//	VK_NEXT 22 PAGE DOWN key  
//	VK_END 23 END key  
//	VK_HOME 24 HOME key  
//	VK_LEFT 25 LEFT ARROW key  
//	VK_UP 26 UP ARROW key  
//	VK_RIGHT 27 RIGHT ARROW key  
//	VK_DOWN 28 DOWN ARROW key  
//	VK_SELECT 29 SELECT key  
//	VK_PRINT 2A PRINT key 
//	VK_EXECUTE 2B EXECUTE key  
//	VK_SNAPSHOT 2C PRINT SCREEN key  
//	VK_INSERT 2D INS key  
//	VK_DELETE 2E DEL key  
//	VK_HELP 2F HELP key  
//	30 0 key  
//	31 1 key  
//	32 2 key  
//	33 3 key  
//	34 4 key  
//	35 5 key  
//	36 6 key  
//	37 7 key  
//	38 8 key  
//	39 9 key  
//	—  3A–40 Undefined  
//	41 A key  
//	42 B key  
//	43 C key  
//	44 D key  
//	45 E key  
//	46 F key  
//	47 G key  
//	48 H key  
//	49 I key  
//	4A J key  
//	4B K key  
//	4C L key  
//	4D M key  
//	4E N key  
//	4F O key  
//	50 P key  
//	51 Q key  
//	52 R key  
//	53 S key  
//	54 T key  
//	55 U key  
//	56 V key  
//	57 W key  
//	58 X key  
//	59 Y key  
//	5A Z key  
//	VK_LWIN 5B Left Windows key (Microsoft® Natural® keyboard)  
//	VK_RWIN 5C Right Windows key (Natural keyboard)  
//	VK_APPS 5D Applications key (Natural keyboard)  
//	—  5E Reserved  
//	VK_SLEEP 5F Computer Sleep key 
//	VK_NUMPAD0 60 Numeric keypad 0 key  
//	VK_NUMPAD1 61 Numeric keypad 1 key  
//	VK_NUMPAD2 62 Numeric keypad 2 key  
//	VK_NUMPAD3 63 Numeric keypad 3 key  
//	VK_NUMPAD4 64 Numeric keypad 4 key  
//	VK_NUMPAD5 65 Numeric keypad 5 key  
//	VK_NUMPAD6 66 Numeric keypad 6 key  
//	VK_NUMPAD7 67 Numeric keypad 7 key  
//	VK_NUMPAD8 68 Numeric keypad 8 key  
//	VK_NUMPAD9 69 Numeric keypad 9 key  
//	VK_MULTIPLY 6A Multiply key  
//	VK_ADD 6B Add key  
//	VK_SEPARATOR 6C Separator key  
//	VK_SUBTRACT 6D Subtract key  
//	VK_DECIMAL 6E Decimal key  
//	VK_DIVIDE 6F Divide key  
//	VK_F1 70 F1 key  
//	VK_F2 71 F2 key  
//	VK_F3 72 F3 key  
//	VK_F4 73 F4 key  
//	VK_F5 74 F5 key  
//	VK_F6 75 F6 key  
//	VK_F7 76 F7 key  
//	VK_F8 77 F8 key  
//	VK_F9 78 F9 key  
//	VK_F10 79 F10 key  
//	VK_F11 7A F11 key  
//	VK_F12 7B F12 key  
//	VK_F13 7C F13 key  
//	VK_F14 7D F14 key  
//	VK_F15 7E F15 key  
//	VK_F16 7F F16 key  
//	VK_F17 80H F17 key  
//	VK_F18 81H F18 key  
//	VK_F19 82H F19 key  
//	VK_F20 83H F20 key  
//	VK_F21 84H F21 key  
//	VK_F22 85H F22 key  
//	VK_F23 86H F23 key  
//	VK_F24 87H F24 key  
//	—  88–8F Unassigned  
//	VK_NUMLOCK 90 NUM LOCK key  
//	VK_SCROLL 91 SCROLL LOCK key  
//	92–96 OEM specific 
//	—  97–9F Unassigned  
//	VK_LSHIFT A0 Left SHIFT key 
//	VK_RSHIFT A1 Right SHIFT key 
//	VK_LCONTROL A2 Left CONTROL key 
//	VK_RCONTROL A3 Right CONTROL key 
//	VK_LMENU A4 Left MENU key 
//	VK_RMENU A5 Right MENU key 
//	VK_BROWSER_BACK A6 Windows 2000/XP: Browser Back key 
//	VK_BROWSER_FORWARD A7 Windows 2000/XP: Browser Forward key 
//	VK_BROWSER_REFRESH A8 Windows 2000/XP: Browser Refresh key 
//	VK_BROWSER_STOP A9 Windows 2000/XP: Browser Stop key 
//	VK_BROWSER_SEARCH AA Windows 2000/XP: Browser Search key 
//	VK_BROWSER_FAVORITES AB Windows 2000/XP: Browser Favorites key 
//	VK_BROWSER_HOME AC Windows 2000/XP: Browser Start and Home key 
//	VK_VOLUME_MUTE AD Windows 2000/XP: Volume Mute key 
//	VK_VOLUME_DOWN AE Windows 2000/XP: Volume Down key 
//	VK_VOLUME_UP AF Windows 2000/XP: Volume Up key 
//	VK_MEDIA_NEXT_TRACK B0 Windows 2000/XP: Next Track key 
//	VK_MEDIA_PREV_TRACK B1 Windows 2000/XP: Previous Track key 
//	VK_MEDIA_STOP B2 Windows 2000/XP: Stop Media key 
//	VK_MEDIA_PLAY_PAUSE B3 Windows 2000/XP: Play/Pause Media key 
//	VK_LAUNCH_MAIL B4 Windows 2000/XP: Start Mail key 
//	VK_LAUNCH_MEDIA_SELECT B5 Windows 2000/XP: Select Media key 
//	VK_LAUNCH_APP1 B6 Windows 2000/XP: Start Application 1 key 
//	VK_LAUNCH_APP2 B7 Windows 2000/XP: Start Application 2 key 
//	—  B8-B9 Reserved 
//	VK_OEM_1 BA Used for miscellaneous characters; it can vary by keyboard. 
//	Windows 2000/XP: For the US standard keyboard, the ';:' key
// 
//	VK_OEM_PLUS BB Windows 2000/XP: For any country/region, the '+' key 
//	VK_OEM_COMMA BC Windows 2000/XP: For any country/region, the ',' key 
//	VK_OEM_MINUS BD Windows 2000/XP: For any country/region, the '-' key 
//	VK_OEM_PERIOD BE Windows 2000/XP: For any country/region, the '.' key 
//	VK_OEM_2 BF Used for miscellaneous characters; it can vary by keyboard. 
//	Windows 2000/XP: For the US standard keyboard, the '/?' key
// 
//	VK_OEM_3 C0 Used for miscellaneous characters; it can vary by keyboard. 
//	Windows 2000/XP: For the US standard keyboard, the '`~' key
// 
//	—  C1–D7 Reserved  
//	—  D8–DA Unassigned 
//	VK_OEM_4 DB Used for miscellaneous characters; it can vary by keyboard. 
//	Windows 2000/XP: For the US standard keyboard, the '[{' key
// 
//	VK_OEM_5 DC Used for miscellaneous characters; it can vary by keyboard. 
//	Windows 2000/XP: For the US standard keyboard, the '\|' key
// 
//	VK_OEM_6 DD Used for miscellaneous characters; it can vary by keyboard. 
//	Windows 2000/XP: For the US standard keyboard, the ']}' key
// 
//	VK_OEM_7 DE Used for miscellaneous characters; it can vary by keyboard. 
//	Windows 2000/XP: For the US standard keyboard, the 'single-quote/double-quote' key
// 
//	VK_OEM_8 DF Used for miscellaneous characters; it can vary by keyboard. 
//	—  E0 Reserved 
//	E1 OEM specific 
//	VK_OEM_102 E2 Windows 2000/XP: Either the angle bracket key or the backslash key on the RT 102-key keyboard 
//	E3–E4 OEM specific 
//	VK_PROCESSKEY E5 Windows 95/98/Me, Windows NT 4.0, Windows 2000/XP: IME PROCESS key 
//	E6 OEM specific 
//	VK_PACKET E7 Windows 2000/XP: Used to pass Unicode characters as if they were keystrokes. The VK_PACKET key is the low word of a 32-bit Virtual Key value used for non-keyboard input methods. For more information, see Remark in KEYBDINPUT, SendInput, WM_KEYDOWN, and WM_KEYUP 
//	—  E8 Unassigned  
//	E9–F5 OEM specific 
//	VK_ATTN F6 Attn key 
//	VK_CRSEL F7 CrSel key 
//	VK_EXSEL F8 ExSel key 
//	VK_EREOF F9 Erase EOF key 
//	VK_PLAY FA Play key 
//	VK_ZOOM FB Zoom key 
//	VK_NONAME FC Reserved for future use  
//	VK_PA1 FD PA1 key 
//	VK_OEM_CLEAR FE Clear key 
//
//	}


    public delegate int FONTENUMPROC(ENUMLOGFONTEX f, int lpntme, int FontType, int lParam);
}