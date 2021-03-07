using FlatRedBall.Glue;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace GlueFormsCore.Controls
{
    class WinformsSplitContainer : System.Windows.Forms.SplitContainer
    {
        private System.Windows.Forms.SplitContainer topPanelContainer;
        private FlatRedBall.Glue.Controls.TabControlEx tcRight;
        private FlatRedBall.Glue.Controls.TabControlEx tcTop;
        private FlatRedBall.Glue.Controls.TabControlEx tcLeft;
        private FlatRedBall.Glue.Controls.TabControlEx tcBottom;
        private FlatRedBall.Glue.Controls.TabControlEx tcCenter;

        public System.Windows.Forms.SplitContainer rightPanelContainer;
        private System.Windows.Forms.SplitContainer leftPanelContainer;

        public WinformsSplitContainer()
        {
            this.topPanelContainer = new System.Windows.Forms.SplitContainer();
            this.tcCenter = new FlatRedBall.Glue.Controls.TabControlEx();
            this.tcRight = new FlatRedBall.Glue.Controls.TabControlEx();
            this.tcTop = new FlatRedBall.Glue.Controls.TabControlEx();
            this.tcLeft = new FlatRedBall.Glue.Controls.TabControlEx();
            this.tcBottom = new FlatRedBall.Glue.Controls.TabControlEx();
            this.rightPanelContainer = new System.Windows.Forms.SplitContainer();
            this.leftPanelContainer = new System.Windows.Forms.SplitContainer();

            // 
            // bottomPanelContainer.Panel1
            // 
            this.Panel1.Controls.Add(this.topPanelContainer);

            this.Panel2.Controls.Add(this.tcBottom);
            this.Panel2Collapsed = true;
            this.Size = new System.Drawing.Size(764, 579);
            this.SplitterDistance = 520;
            this.TabIndex = 7;


            // 
            // topPanelContainer
            // 
            this.topPanelContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.topPanelContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topPanelContainer.Location = new System.Drawing.Point(0, 0);
            this.topPanelContainer.Name = "topPanelContainer";
            this.topPanelContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // topPanelContainer.Panel1
            // 
            this.topPanelContainer.Panel1.Controls.Add(this.tcTop);
            this.topPanelContainer.Panel1Collapsed = true;
            // 
            // topPanelContainer.Panel2
            // 
            this.topPanelContainer.Panel2.Controls.Add(this.leftPanelContainer);
            this.topPanelContainer.Size = new System.Drawing.Size(764, 579);
            this.topPanelContainer.SplitterDistance = 82;
            this.topPanelContainer.TabIndex = 6;

            // 
            // tcTop
            // 
            this.tcTop.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.tcTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcTop.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tcTop.IgnoreFirst = false;
            this.tcTop.Location = new System.Drawing.Point(0, 0);
            this.tcTop.Name = "tcTop";
            this.tcTop.SelectedIndex = 0;
            this.tcTop.Size = new System.Drawing.Size(146, 78);
            this.tcTop.TabIndex = 0;
            this.tcTop.ControlAdded += new System.Windows.Forms.ControlEventHandler(this.tcPanel1_ControlAdded);
            this.tcTop.ControlRemoved += new System.Windows.Forms.ControlEventHandler(this.tcPanel1_ControlRemoved);

            // 
            // tcBottom
            // 
            this.tcBottom.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.tcBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcBottom.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tcBottom.IgnoreFirst = false;
            this.tcBottom.Location = new System.Drawing.Point(0, 0);
            this.tcBottom.Name = "tcBottom";
            this.tcBottom.SelectedIndex = 0;
            this.tcBottom.Size = new System.Drawing.Size(146, 42);
            this.tcBottom.TabIndex = 1;
            this.tcBottom.ControlAdded += new System.Windows.Forms.ControlEventHandler(this.tcPanel2_ControlAdded);
            this.tcBottom.ControlRemoved += new System.Windows.Forms.ControlEventHandler(this.tcPanel2_ControlRemoved);

            // 
            // tcLeft
            // 
            this.tcLeft.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.tcLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcLeft.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tcLeft.IgnoreFirst = false;
            this.tcLeft.Location = new System.Drawing.Point(0, 0);
            this.tcLeft.Name = "tcLeft";
            this.tcLeft.SelectedIndex = 0;
            this.tcLeft.Size = new System.Drawing.Size(134, 96);
            this.tcLeft.TabIndex = 1;
            this.tcLeft.ControlAdded += new System.Windows.Forms.ControlEventHandler(this.tcPanel1_ControlAdded);
            this.tcLeft.ControlRemoved += new System.Windows.Forms.ControlEventHandler(this.tcPanel1_ControlRemoved);

            // 
            // MainTabControl
            // 
            this.tcCenter.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.tcCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcCenter.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tcCenter.IgnoreFirst = false;
            this.tcCenter.Location = new System.Drawing.Point(0, 0);
            this.tcCenter.Name = "MainTabControl";
            this.tcCenter.SelectedIndex = 0;
            this.tcCenter.Size = new System.Drawing.Size(542, 575);
            this.tcCenter.TabIndex = 4;
            // 
            // tcRight
            // 
            this.tcRight.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.tcRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcRight.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tcRight.IgnoreFirst = false;
            this.tcRight.Location = new System.Drawing.Point(0, 0);
            this.tcRight.Margin = new System.Windows.Forms.Padding(0);
            this.tcRight.Name = "tcRight";
            this.tcRight.Padding = new System.Drawing.Point(6, 0);
            this.tcRight.SelectedIndex = 0;
            this.tcRight.Size = new System.Drawing.Size(210, 575);
            this.tcRight.TabIndex = 2;
            this.tcRight.ControlAdded += new System.Windows.Forms.ControlEventHandler(this.ControlAddedToRightView);
            this.tcRight.ControlRemoved += new System.Windows.Forms.ControlEventHandler(this.ControlRemovedFromRightView);

            // 
            // rightPanelContainer
            // 
            this.rightPanelContainer.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.rightPanelContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.rightPanelContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightPanelContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.rightPanelContainer.Location = new System.Drawing.Point(0, 0);
            this.rightPanelContainer.Margin = new System.Windows.Forms.Padding(0);
            this.rightPanelContainer.Name = "rightPanelContainer";
            // 
            // rightPanelContainer.Panel1
            // 
            this.rightPanelContainer.Panel1.Controls.Add(this.tcCenter);
            // 
            // rightPanelContainer.Panel2
            // 
            this.rightPanelContainer.Panel2.Controls.Add(this.tcRight);
            this.rightPanelContainer.Size = new System.Drawing.Size(764, 579);
            this.rightPanelContainer.SplitterDistance = 546;
            this.rightPanelContainer.TabIndex = 4;
            // 
            // leftPanelContainer
            // 
            this.leftPanelContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.leftPanelContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftPanelContainer.Location = new System.Drawing.Point(0, 0);
            this.leftPanelContainer.Name = "leftPanelContainer";
            // 
            // leftPanelContainer.Panel1
            // 
            this.leftPanelContainer.Panel1.Controls.Add(this.tcLeft);
            this.leftPanelContainer.Panel1Collapsed = true;
            // 
            // leftPanelContainer.Panel2
            // 
            this.leftPanelContainer.Panel2.Controls.Add(this.rightPanelContainer);
            this.leftPanelContainer.Size = new System.Drawing.Size(764, 579);
            this.leftPanelContainer.SplitterDistance = 138;
            this.leftPanelContainer.TabIndex = 9;


            PluginManager.SetTabs(tcTop, tcBottom, tcLeft, tcRight, tcCenter);

        }

        internal void UpdateSizesFromSettings()
        {
            rightPanelContainer.Panel2MinSize = 125;
            try
            {
                leftPanelContainer.SplitterDistance = EditorData.GlueLayoutSettings.LeftPanelSplitterPosition;
                topPanelContainer.SplitterDistance = EditorData.GlueLayoutSettings.TopPanelSplitterPosition;
                rightPanelContainer.SplitterDistance = EditorData.GlueLayoutSettings.RightPanelSplitterPosition;
                this.SplitterDistance = EditorData.GlueLayoutSettings.BottomPanelSplitterPosition;
            }
            catch
            {
                // do nothing
            }
        }

        internal void ReactToFormClosing()
        {
            EditorData.GlueLayoutSettings.LeftPanelSplitterPosition = leftPanelContainer.SplitterDistance;
            EditorData.GlueLayoutSettings.TopPanelSplitterPosition = topPanelContainer.SplitterDistance;
            EditorData.GlueLayoutSettings.RightPanelSplitterPosition = rightPanelContainer.SplitterDistance;
        }


        private void tcPanel1_ControlAdded(object sender, ControlEventArgs e)
        {
            SplitContainer parent = (SplitContainer)((Control)sender).Parent.Parent;

            parent.Panel1Collapsed = false;
        }

        private void tcPanel1_ControlRemoved(object sender, ControlEventArgs e)
        {
            SplitContainer parent = (SplitContainer)((Control)sender).Parent.Parent;
            TabControlEx tc = (TabControlEx)sender;
            bool show = tc.TabCount > 1;

            if (show)
                parent.Panel1Collapsed = false;
            else
                parent.Panel1Collapsed = true;

        }

        private void tcPanel2_ControlAdded(object sender, ControlEventArgs e)
        {
            SplitContainer parent = (SplitContainer)((Control)sender).Parent.Parent;

            parent.Panel2Collapsed = false;
        }

        private void tcPanel2_ControlRemoved(object sender, ControlEventArgs e)
        {
            SplitContainer parent = (SplitContainer)((Control)sender).Parent.Parent;
            TabControlEx tc = (TabControlEx)sender;
            bool show = tc.TabCount > 1;

            if (show)
                parent.Panel2Collapsed = false;
            else
                parent.Panel2Collapsed = true;

        }


        private void ControlAddedToRightView(object sender, ControlEventArgs e)
        {
            SplitContainer parent = (SplitContainer)((Control)sender).Parent.Parent;

            parent.Panel2Collapsed = false;


        }

        private void ControlRemovedFromRightView(object sender, ControlEventArgs e)
        {
            SplitContainer parent = (SplitContainer)((Control)sender).Parent.Parent;
            TabControlEx tc = (TabControlEx)sender;
            bool show = tc.TabCount > 1;

            if (show)
                parent.Panel2Collapsed = false;
            else
                parent.Panel2Collapsed = true;

        }
    }
}
