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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Alsing.Windows.Forms.CoreLib
{
    public class SplitViewParentControl : BaseControl
    {
        protected SplitViewChildControl _ActiveView;
        private long _ticks; //splitter doubleclick timer
        public bool DisableScrollBars;
        protected bool DoOnce;
        protected SplitViewChildControl LowerLeft;
        protected SplitViewChildControl LowerRight;
        protected SplitViewControl splitView;
        protected SplitViewChildControl UpperLeft;
        protected SplitViewChildControl UpperRight;

        #region Private Properties

        private List<SplitViewChildControl> _Views;

        protected List<SplitViewChildControl> Views
        {
            get { return _Views; }
            set { _Views = value; }
        }

        #endregion

        public SplitViewParentControl()
        {
            OnCreate();

            InitializeComponent();
            InitializeComponentInternal();
            splitView.Resizing += SplitView_Resizing;
            splitView.HideLeft += SplitView_HideLeft;
            splitView.HideTop += SplitView_HideTop;


            LowerRight = GetNewView();
            LowerRight.AllowDrop = true;
            LowerRight.BorderColor = Color.White;
            LowerRight.BorderStyle = BorderStyle.None;
            LowerRight.Location = new Point(0, 0);
            LowerRight.Size = new Size(100, 100);

            Views = new List<SplitViewChildControl>();
            LowerRight.TopThumb.MouseDown += TopThumb_MouseDown;
            LowerRight.LeftThumb.MouseDown += LeftThumb_MouseDown;
            Views.Add(LowerRight);
            LowerRight.TopThumbVisible = true;
            LowerRight.LeftThumbVisible = true;
            splitView.Controls.Add(LowerRight);
            splitView.LowerRight = LowerRight;

            SplitView = true;
            ScrollBars = ScrollBars.Both;
            BorderStyle = BorderStyle.None;
            ChildBorderColor = SystemColors.ControlDark;
            ChildBorderStyle = BorderStyle.FixedSingle;
            BackColor = SystemColors.Window;
            Size = new Size(100, 100);
            _ActiveView = LowerRight;
        }

        /// <summary>
        /// Gets or Sets the active view
        /// </summary>
        [Browsable(false)]
        public ActiveView ActiveView
        {
            get
            {
                if (_ActiveView == UpperLeft)
                    return ActiveView.TopLeft;

                if (_ActiveView == UpperRight)
                    return ActiveView.TopRight;

                if (_ActiveView == LowerLeft)
                    return ActiveView.BottomLeft;

                if (_ActiveView == LowerRight)
                    return ActiveView.BottomRight;

                return 0;
            }
            set
            {
                if (value != ActiveView.BottomRight)
                {
                    ActivateSplits();
                }


                if (value == ActiveView.TopLeft)
                    _ActiveView = UpperLeft;

                if (value == ActiveView.TopRight)
                    _ActiveView = UpperRight;

                if (value == ActiveView.BottomLeft)
                    _ActiveView = LowerLeft;

                if (value == ActiveView.BottomRight)
                    _ActiveView = LowerRight;
            }
        }

        private void InitializeComponent() {}

        /// <summary>
        /// Resets the Splitview.
        /// </summary>
        public void ResetSplitview()
        {
            splitView.ResetSplitview();
        }

        private void SplitView_Resizing(object sender, EventArgs e)
        {
            LowerRight.TopThumbVisible = false;
            LowerRight.LeftThumbVisible = false;
        }

        private void SplitView_HideTop(object sender, EventArgs e)
        {
            LowerRight.TopThumbVisible = true;
        }

        private void SplitView_HideLeft(object sender, EventArgs e)
        {
            LowerRight.LeftThumbVisible = true;
        }

        protected virtual void ActivateSplits()
        {
            if (UpperLeft == null)
            {
                UpperLeft = GetNewView();
                UpperRight = GetNewView();
                LowerLeft = GetNewView();

                splitView.Controls.AddRange(new Control[]
                                            {
                                                UpperLeft,
                                                LowerLeft,
                                                UpperRight
                                            });

                splitView.UpperRight = LowerLeft;
                splitView.UpperLeft = UpperLeft;
                splitView.LowerLeft = UpperRight;

                CreateViews();
            }
        }


        protected void TopThumb_MouseDown(object sender, MouseEventArgs e)
        {
            ActivateSplits();

            long t = DateTime.Now.Ticks - _ticks;
            _ticks = DateTime.Now.Ticks;


            if (t < 3000000)
            {
                splitView.Split5050h();
            }
            else
            {
                splitView.InvokeMouseDownh();
            }
        }

        protected void LeftThumb_MouseDown(object sender, MouseEventArgs e)
        {
            ActivateSplits();

            long t = DateTime.Now.Ticks - _ticks;
            _ticks = DateTime.Now.Ticks;


            if (t < 3000000)
            {
                splitView.Split5050v();
            }
            else
            {
                splitView.InvokeMouseDownv();
            }
        }

        protected virtual void OnCreate() {}

        protected virtual void CreateViews()
        {
            if (UpperRight != null)
            {
                Views.Add(UpperRight);
                Views.Add(UpperLeft);
                Views.Add(LowerLeft);
            }
        }

        protected virtual SplitViewChildControl GetNewView()
        {
            return null;
        }

        protected void View_Enter(object sender, EventArgs e)
        {
            _ActiveView = (SplitViewChildControl) sender;
        }

        protected void View_Leave(object sender, EventArgs e)
        {
            //	((EditViewControl)sender).RemoveFocus ();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == (int) WindowMessage.WM_SETFOCUS)
            {
                if (_ActiveView != null)
                    _ActiveView.Focus();
            }
        }

        #region PUBLIC PROPERTY SPLITVIEWV

        [Browsable(false)]
        public int SplitviewV
        {
            get { return splitView.SplitviewV; }
            set
            {
                if (splitView == null)
                    return;

                splitView.SplitviewV = value;
            }
        }

        #endregion

        #region PUBLIC PROPERTY SPLITVIEWH

        [Browsable(false)]
        public int SplitviewH
        {
            get { return splitView.SplitviewH; }
            set
            {
                if (splitView == null)
                    return;
                splitView.SplitviewH = value;
            }
        }

        #endregion

        #region public property ScrollBars

        private ScrollBars _ScrollBars;

        [Category("Appearance"),
         Description("Determines what Scrollbars should be visible")]
        [DefaultValue(ScrollBars.Both)]
        public ScrollBars ScrollBars
        {
            get { return _ScrollBars; }

            set
            {
                if (_Views == null)
                    return;

                if (DisableScrollBars)
                    value = ScrollBars.None;

                foreach (SplitViewChildControl evc in _Views)
                {
                    evc.ScrollBars = value;
                }
                _ScrollBars = value;
            }
        }

        #endregion

        #region public property SplitView

        //member variable
        private bool _SplitView;

        [Category("Appearance"),
         Description("Determines if the controls should use splitviews")]
        [DefaultValue(true)]
        public bool SplitView
        {
            get { return _SplitView; }

            set
            {
                _SplitView = value;

                if (splitView == null)
                    return;

                if (!SplitView)
                {
                    splitView.Visible = false;
                    Controls.Add(LowerRight);
                    LowerRight.HideThumbs();
                    LowerRight.Dock = DockStyle.Fill;
                }
                else
                {
                    splitView.Visible = true;
                    splitView.LowerRight = LowerRight;
                    LowerRight.Dock = DockStyle.None;
                    LowerRight.ShowThumbs();
                }
            }
        }

        #endregion //END PROPERTY SplitView

        #region PUBLIC PROPERTY CHILDBODERSTYLE

        /// <summary>
        /// Gets or Sets the border styles of the split views.
        /// </summary>
        [Category("Appearance - Borders")]
        [Description("Gets or Sets the border styles of the split views.")]
        [DefaultValue(BorderStyle.FixedSingle)]
        public BorderStyle ChildBorderStyle
        {
            get { return ((SplitViewChildControl) Views[0]).BorderStyle; }
            set
            {
                foreach (SplitViewChildControl ev in Views)
                {
                    ev.BorderStyle = value;
                }
            }
        }

        #endregion

        #region PUBLIC PROPERTY CHILDBORDERCOLOR

        /// <summary>
        /// Gets or Sets the border color of the split views.
        /// </summary>
        [Category("Appearance - Borders")]
        [Description("Gets or Sets the border color of the split views.")]
        [DefaultValue(typeof (Color), "ControlDark")]
        public Color ChildBorderColor
        {
            get { return ((SplitViewChildControl) Views[0]).BorderColor; }
            set
            {
                foreach (SplitViewChildControl ev in Views)
                {
                    if (ev != null)
                    {
                        ev.BorderColor = value;
                    }
                }
            }
        }

        #endregion

        #region roger generated code

        private void InitializeComponentInternal()
        {
            splitView = new SplitViewControl();
            SuspendLayout();
            // 
            // splitView
            // 
            splitView.BackColor = Color.Empty;
            splitView.Dock = DockStyle.Fill;
            splitView.LowerLeft = null;
            splitView.LowerRight = null;
            splitView.Name = "splitView";
            splitView.Size = new Size(248, 216);
            splitView.SplitviewH = -4;
            splitView.SplitviewV = -4;
            splitView.TabIndex = 0;
            splitView.Text = "splitView";
            splitView.UpperLeft = null;
            splitView.UpperRight = null;
            // 
            // SplitViewParentControl
            // 
            Controls.AddRange(new Control[]
                              {
                                  splitView
                              });
            Name = "SplitViewParentControl";
            Size = new Size(248, 216);
            ResumeLayout(false);
        }

        #endregion
    }
}

namespace Alsing.Windows.Forms
{
    /// <summary>
    /// Represents which split view is currently active in the syntaxbox
    /// </summary>
    public enum ActiveView
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }
}