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
using System.Windows.Forms;

namespace Alsing.Windows
{
    public static class NativeMethods
    {
        public const int GWL_STYLE = -16;
        public const int WS_CHILD = 0x40000000;

        #region uxTheme.dll

        [DllImport("uxtheme.dll", SetLastError = true, CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr OpenThemeData(IntPtr hwnd, [MarshalAs(UnmanagedType.LPWStr)] string pszClassList);

        [DllImport("uxtheme.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr CloseThemeData(IntPtr hTheme);

        [DllImport("uxtheme.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool IsThemeActive();

        [DllImport("uxtheme.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int DrawThemeBackground(IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId,
                                                     ref APIRect rect, ref APIRect clipRect);

        [DllImport("uxtheme.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int DrawThemeText(IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, string pszText,
                                               int iCharCount, uint dwTextFlags, uint dwTextFlags2,
                                               [MarshalAs(UnmanagedType.Struct)] ref APIRect rect);

        [DllImport("uxtheme.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetThemeColor(IntPtr hTheme, int iPartId, int iStateId, int iPropId, out ulong color);

        /*
        [DllImportAttribute( "uxtheme.dll")]
        public static extern void GetThemeBackgroundContentRect( int hTheme, IntPtr hdc, int iPartId, int iStateId, ref RECT pBoundingRect, ref RECT pContentRect );

        [DllImportAttribute( "uxtheme.dll" )]
        public static extern void GetThemeBackgroundExtent( int hTheme, IntPtr hdc, int iPartId, int iStateId, ref RECT pContentRect, ref RECT pExtentRect );

        [DllImportAttribute( "uxtheme.dll")]
        public static extern uint GetThemePartSize( IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, IntPtr prc, int sizeType, out SIZE psz );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern uint GetThemeTextExtent( IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, string pszText, int iCharCount, uint dwTextFlags, [MarshalAs( UnmanagedType.Struct )] ref RECT pBoundingRect, out RECT pExtentRect );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemeTextMetrics( IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, out TEXTMETRIC ptm );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemeBackgroundRegion( IntPtr hTheme, int iPartId, int iStateId, RECT pRect, out IntPtr pRegion );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong HitTestThemeBackground( IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, ulong dwOptions, RECT pRect, IntPtr hrgn, POINT ptTest, out uint wHitTestCode );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong DrawThemeLine( IntPtr hTheme, IntPtr hdc, int iStateId, RECT pRect, ulong dwDtlFlags );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong DrawThemeEdge( IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, RECT pDestRect, uint uEdge, uint uFlags, out RECT contentRect );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong DrawThemeBorder( IntPtr hTheme, IntPtr hdc, int iStateId, RECT pRect );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong DrawThemeIcon( IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, RECT pRect, IntPtr himl, int iImageIndex );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern bool IsThemePartDefined( IntPtr hTheme, int iPartId, int iStateId );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern bool IsThemeBackgroundPartiallyTransparent( IntPtr hTheme, int iPartId, int iStateId );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern int GetThemeColor( IntPtr hTheme, int iPartId, int iStateId, int iPropId, out ulong color );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemeMetric( IntPtr hTheme, int iPartId, int iStateId, int iPropId, out int iVal );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemeString( IntPtr hTheme, int iPartId, int iStateId, int iPropId, out string pszBuff, int cchMaxBuffChars );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemeBool( IntPtr hTheme, int iPartId, int iStateId, int iPropId, out bool fVal );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemeInt( IntPtr hTheme, int iPartId, int iStateId, int iPropId, out int iVal );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemeEnumValue( IntPtr hTheme, int iPartId, int iStateId, int iPropId, out int iVal );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemePosition( IntPtr hTheme, int iPartId, int iStateId, int iPropId, out POINT point );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemeFont( IntPtr hTheme, int iPartId, int iStateId, int iPropId, out LOGFONT font );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemeRect( IntPtr hTheme, int iPartId, int iStateId, int iPropId, out RECT pRect );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemeMargins( IntPtr hTheme, int iPartId, int iStateId, int iPropId, out MARGINS margins );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemeIntList( IntPtr hTheme, int iPartId, int iStateId, int iPropId, out INTLIST intList );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemePropertyOrigin( IntPtr hTheme, int iPartId, int iStateId, int iPropId, out int origin );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong SetWindowTheme( IntPtr hwnd, string pszSubAppName, string pszSubIdList );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemeFilename( IntPtr hTheme, int iPartId, int iStateId, int iPropId, out string pszThemeFileName, int cchMaxBuffChars );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemeSysColor( IntPtr hTheme, int iColorId );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern IntPtr GetThemeSysColorBrush( IntPtr hTheme, int iColorId );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern int GetThemeSysSize( IntPtr hTheme, int iSizeId );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern bool GetThemeSysBool( IntPtr hTheme, int iBoolId );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemeSysFont( IntPtr hTheme, int iFontId, out LOGFONT lf );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemeSysString( IntPtr hTheme, int iStringId, out string pszStringBuff, int cchMaxStringChars );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemeSysInt( IntPtr hTheme, int iIntId, out int iValue );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern bool IsAppThemed();

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern IntPtr GetWindowTheme( IntPtr hwnd );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong EnableThemeDialogTexture( IntPtr hwnd, bool fEnable );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern bool IsThemeDialogTextureEnabled( IntPtr hwnd );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemeAppProperties();

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern void SetThemeAppProperties( ulong dwFlags );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetCurrentThemeName( out string pszThemeFileName, int cchMaxNameChars, out string pszColorBuff, int cchMaxColorChars, out string pszSizeBuff, int cchMaxSizeChars );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemeDocumentationProperty( string pszThemeName, string pszPropertyName, out string pszValueBuff, int cchMaxValChars );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemeLastErrorContext( out THEME_ERROR_CONTEXT context );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong FormatThemeMessage( ulong dwLanguageId, THEME_ERROR_CONTEXT context, out string pszMessageBuff, int cchMaxMessageChars );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern ulong GetThemeImageFromParent( IntPtr hwnd, IntPtr hdc, RECT rc );

        [DllImportAttribute( "uxtheme.dll", CharSet=CharSet.Auto )]
        public static extern IntPtr DrawThemeParentBackground( IntPtr hwnd, IntPtr hdc, ref RECT prc );
*/

        #endregion

        [DllImport("imm32.dll")]
        public static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, COMPOSITIONFORM lParam);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, LogFont lParam);

        [DllImport("user32.DLL", SetLastError = false, CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int DrawText(IntPtr hDC, string lpString, int nCount, ref APIRect Rect, int wFormat);


        [DllImport("gdi32", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int EnumFontFamiliesEx(IntPtr hDC, [MarshalAs(UnmanagedType.LPStruct)] LogFont lf,
                                                    FONTENUMPROC proc, Int64 LParam, Int64 DW);

        [DllImport("shlwapi.dll", SetLastError = true)]
        public static extern int SHAutoComplete(IntPtr hWnd, UInt32 flags);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.DLL")]
        public static extern IntPtr GetWindowRect(IntPtr hWND, ref APIRect Rect);

        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        public static extern int SendMessage(IntPtr hWND, int message, int WParam, int LParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("gdi32.DLL", SetLastError = false, CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int SetBkColor(IntPtr hDC, int crColor);

        [DllImport("gdi32.DLL", SetLastError = false, CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int SetBkMode(IntPtr hDC, int Mode);

        [DllImport("user32.DLL", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr ReleaseDC(IntPtr hWND, IntPtr hDC);

        [DllImport("gdi32.DLL", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr DeleteDC(IntPtr hDC);


        [DllImport("gdi32.DLL", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr GdiFlush();

        [DllImport("user32.DLL", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr GetWindowDC(IntPtr hWND);

        [DllImport("user32.DLL", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr GetDC(IntPtr hWND);

        [DllImport("gdi32.DLL", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

        [DllImport("gdi32.DLL", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.DLL", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.DLL", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr DeleteObject(IntPtr hObject);

        [DllImport("gdi32.DLL", SetLastError = false, CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int GetTextColor(IntPtr hDC);

        [DllImport("gdi32.DLL", SetLastError = false, CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int SetTextColor(IntPtr hDC, int crColor);

        [DllImport("gdi32.DLL", SetLastError = false, CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int GetBkColor(IntPtr hDC);


        [DllImport("gdi32.DLL", SetLastError = false, CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int GetBkMode(IntPtr hDC);

        [DllImport("user32", SetLastError = false, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall
            )]
        public static extern int DrawFocusRect(IntPtr hDC, ref APIRect rect);

        [DllImport("gdi32.DLL", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr CreateSolidBrush(int crColor);

        [DllImport("gdi32.DLL", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int Rectangle(IntPtr hDC, int left, int top, int right, int bottom);

        [DllImport("gdi32.DLL", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr CreateHatchBrush(int Style, int crColor);

        [DllImport("user32.DLL", SetLastError = false, CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int TabbedTextOut(IntPtr hDC, int x, int y, string lpString, int nCount, int nTabPositions,
                                               ref int lpnTabStopPositions, int nTabOrigin);

        [DllImport("gdi32.dll", SetLastError = false, CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr BitBlt(IntPtr hDestDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC,
                                           int xSrc, int ySrc, int dwRop);

        [DllImport("user32.dll", SetLastError = false, CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int FillRect(IntPtr hDC, ref APIRect rect, IntPtr hBrush);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetTextFace(IntPtr hDC, int nCount, string lpFacename);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetTextMetrics(IntPtr hDC, ref GDITextMetric TextMetric);

        [DllImport("gdi32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr CreateFontIndirect([MarshalAs(UnmanagedType.LPStruct)] LogFont LogFont);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern int GetTabbedTextExtent(IntPtr hDC, string lpString, int nCount, int nTabPositions,
                                                     ref int lpnTabStopPositions);

        [DllImport("user32.dll", SetLastError = false, CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int InvertRect(IntPtr hDC, ref APIRect rect);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr CreatePen(int nPenStyle, int nWidth, int crColor);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetBrushOrgEx(IntPtr hDC, int x, int y, ref APIPoint p);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr CreatePatternBrush(IntPtr hBMP);

        [DllImport("User32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int ShowWindow(IntPtr hWnd, short cmdShow);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr MoveToEx(IntPtr hDC, int x, int y, ref APIPoint lpPoint);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr LineTo(IntPtr hDC, int x, int y);

        [DllImport("user32", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt16 GetAsyncKeyState(int vKey);

        public static bool IsKeyPressed(Keys k)
        {
            int s = GetAsyncKeyState((int) k);
            s = (s & 0x8000) >> 15;
            return (s == 1);
        }


        //---------------------------------------
        //helper , return DC of a control
        public static IntPtr ControlDC(Control control)
        {
            return GetDC(control.Handle);
        }

        //---------------------------------------

        //---------------------------------------
        //helper , convert from and to colors from int values
        public static int ColorToInt(Color color)
        {
            return (color.B << 16 | color.G << 8 | color.R);
        }

        public static Color IntToColor(int color)
        {
            int b = (color >> 16) & 0xFF;
            int g = (color >> 8) & 0xFF;
            int r = (color) & 0xFF;
            return Color.FromArgb(r, g, b);
        }
    }
}