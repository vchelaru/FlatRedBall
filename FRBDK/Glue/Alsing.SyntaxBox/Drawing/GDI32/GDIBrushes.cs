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
using Alsing.Windows;

namespace Alsing.Drawing.GDI
{
    //wrapper class for gdi brushes
    public class GDIBrush : GDIObject
    {
        public IntPtr hBrush;
        protected bool mSystemBrush;

        public GDIBrush(Color color)
        {
            hBrush = NativeMethods.CreateSolidBrush(NativeMethods.ColorToInt(color));
            Create();
        }


        public GDIBrush(Bitmap pattern)
        {
            hBrush = NativeMethods.CreatePatternBrush(pattern.GetHbitmap());
            Create();
        }

        public GDIBrush(IntPtr hBMP_Pattern)
        {
            hBrush = NativeMethods.CreatePatternBrush(hBMP_Pattern);
            //if (hBrush==(IntPtr)0)
            //Alsing.Debug.Debugger.WriteLine ("Failed to create brush with color : {0}",color.ToString());

            Create();
        }

        public GDIBrush(int Style, Color color)
        {
            hBrush = NativeMethods.CreateHatchBrush(Style, NativeMethods.ColorToInt(color));
            Create();
        }

        public GDIBrush(int BrushIndex)
        {
            hBrush = (IntPtr) BrushIndex;
            mSystemBrush = true;
            Create();
        }

        protected override void Destroy()
        {
            //only destroy if brush is created by us
            if (!mSystemBrush)
            {
                if (hBrush != (IntPtr) 0)
                    NativeMethods.DeleteObject(hBrush);
            }

            base.Destroy();
            hBrush = (IntPtr) 0;
        }
    }


    //needs to be recoded , cant create new instances for the same colors
    public class GDIBrushes
    {
        public static GDIBrush Black
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush White
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush Red
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush Cyan
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush Green
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush Blue
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush Yellow
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush Orange
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush Purple
        {
            get { return new GDIBrush(0); }
        }
    }

    public class GDISystemBrushes
    {
        public static GDIBrush ActiveBorder
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush ActiveCaption
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush ActiveCaptionText
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush AppWorkspace
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush Control
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush ControlDark
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush ControlDarkDark
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush ControlLight
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush ControlLightLight
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush ControlText
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush Desktop
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush Highlight
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush HighlightText
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush HotTrack
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush InactiveBorder
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush InactiveCaption
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush Info
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush Menu
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush ScrollBar
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush Window
        {
            get { return new GDIBrush(0); }
        }

        public static GDIBrush WindowText
        {
            get { return new GDIBrush(0); }
        }
    }
}