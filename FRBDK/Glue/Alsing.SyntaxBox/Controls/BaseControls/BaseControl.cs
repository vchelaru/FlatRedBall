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
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Alsing.Drawing;

namespace Alsing.Windows.Forms
{
    [ToolboxItem(true)]
    public class BaseControl : Control
    {
        private const int WS_BORDER = unchecked(0x00800000);
        private const int WS_EX_CLIENTEDGE = unchecked(0x00000200);
        private Color borderColor = Color.Black;

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private BorderStyle borderStyle;

        private Container components;
        private bool RunOnce = true;


        public BaseControl()
        {
            SetStyle(ControlStyles.EnableNotifyMessage, true);
            BorderStyle = BorderStyle.FixedSingle;
            InitializeComponent();
        }

        [Browsable(false)]
        public Size WindowSize
        {
            get
            {
                var s = new APIRect();
                NativeMethods.GetWindowRect(Handle, ref s);
                return new Size(s.Width, s.Height);
            }
        }

        [Category("Appearance - Borders"), Description("The border color")]
        [DefaultValue(typeof (Color), "Black")]
        public Color BorderColor
        {
            get { return borderColor; }

            set
            {
                borderColor = value;
                Refresh();
                Invalidate();
                UpdateStyles();
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;

                if (BorderStyle == BorderStyle.None)
                    return cp;

                cp.ExStyle &= (~WS_EX_CLIENTEDGE);
                cp.Style &= (~WS_BORDER);

                return cp;
            }
        }

        [Browsable(true),
         EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Appearance - Borders"), Description("The border style")]
        public BorderStyle BorderStyle
        {
            get { return borderStyle; }
            set
            {
                if (borderStyle != value)
                {
                    if (!Enum.IsDefined(typeof (BorderStyle), value))
                    {
                        throw new InvalidEnumArgumentException("value", (int) value, typeof (BorderStyle));
                    }
                    borderStyle = value;
                    UpdateStyles();
                    Refresh();
                }
            }
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), Obsolete("Do not use!", true)]
        public override Image BackgroundImage
        {
            get { return base.BackgroundImage; }
            set { base.BackgroundImage = value; }
        }


        [Browsable(false)]
        public int ClientWidth
        {
            get { return WindowSize.Width - (BorderWidth*2); }
        }

        [Browsable(false)]
        public int ClientHeight
        {
            get { return WindowSize.Height - (BorderWidth*2); }
        }

        [Browsable(false)]
        public int BorderWidth
        {
            get
            {
                switch (borderStyle)
                {
                    case BorderStyle.None:
                        {
                            return 0;
                        }
                    case BorderStyle.Sunken:
                        {
                            return 2;
                        }
                    case BorderStyle.SunkenThin:
                        {
                            return 1;
                        }
                    case BorderStyle.Raised:
                        {
                            return 2;
                        }

                    case BorderStyle.Etched:
                        {
                            return 2;
                        }
                    case BorderStyle.Bump:
                        {
                            return 6;
                        }
                    case BorderStyle.FixedSingle:
                        {
                            return 1;
                        }
                    case BorderStyle.FixedDouble:
                        {
                            return 2;
                        }
                    case BorderStyle.RaisedThin:
                        {
                            return 1;
                        }
                    case BorderStyle.Dotted:
                        {
                            return 1;
                        }
                    case BorderStyle.Dashed:
                        {
                            return 1;
                        }
                }


                return Height;
            }
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            // 
            // BaseControl
            // 
            this.Size = new System.Drawing.Size(272, 264);
        }

        #endregion

        public event EventHandler Load = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        protected virtual void OnLoad(EventArgs e)
        {
            if (Load != null)
                Load(this, e);
            Refresh();
        }

        protected override unsafe void WndProc(ref Message m)
        {
            if (m.Msg == (int) WindowMessage.WM_NCPAINT)
            {
                RenderBorder();
            }
            else if (m.Msg == (int) WindowMessage.WM_SHOWWINDOW)
            {
                if (RunOnce)
                {
                    RunOnce = false;
                    OnLoad(null);
                    base.WndProc(ref m);
                    UpdateStyles();
                }
                else
                {
                    UpdateStyles();
                    base.WndProc(ref m);
                }
            }
            else if (m.Msg == (int) WindowMessage.WM_NCCREATE)
            {
                base.WndProc(ref m);
            }
            else if (m.Msg == (int) WindowMessage.WM_NCCALCSIZE)
            {
                if (m.WParam == (IntPtr) 0)
                {
                    var pRC = (APIRect*) m.LParam;
                    //pRC->left -=3;
                    base.WndProc(ref m);
                }
                else if (m.WParam == (IntPtr) 1)
                {
                    var pNCP = (_NCCALCSIZE_PARAMS*) m.LParam;


                    int t = pNCP->NewRect.top + BorderWidth;
                    int l = pNCP->NewRect.left + BorderWidth;
                    int b = pNCP->NewRect.bottom - BorderWidth;
                    int r = pNCP->NewRect.right - BorderWidth;

                    base.WndProc(ref m);

                    pNCP->NewRect.top = t;
                    pNCP->NewRect.left = l;
                    pNCP->NewRect.right = r;
                    pNCP->NewRect.bottom = b;

                    return;
                }
            }
            else
            {
                base.WndProc(ref m);
            }
        }

        private void RenderBorder()
        {
            IntPtr hdc = NativeMethods.GetWindowDC(Handle);
            var s = new APIRect();
            NativeMethods.GetWindowRect(Handle, ref s);

            using (Graphics g = Graphics.FromHdc(hdc))
            {
                DrawingTools.DrawBorder((BorderStyle2) (int) BorderStyle, BorderColor, g,
                                        new Rectangle(0, 0, s.Width, s.Height));
            }
            NativeMethods.ReleaseDC(Handle, hdc);
        }


        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
        }

//		protected override void OnHandleCreated(System.EventArgs e)
//		{
//			base.OnHandleCreated (e);
//		//	this.UpdateStyles ();
//			Console.WriteLine ("gapa");
//		}
//
//		protected override void OnHandleDestroyed(System.EventArgs e)
//		{			
//			base.OnHandleDestroyed (e);
//			Console.WriteLine ("apa");
//		}
//
//		protected override void OnParentChanged(System.EventArgs e)
//		{
//			base.OnParentChanged (e);
//		}
    }
}