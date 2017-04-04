using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBall.Winforms.Container
{
    public partial class CollapsibleContainerStrip : UserControl
    {
        const int SubtractFromWidthForControlWidth = 19;

        public int mTotalHeight;

        public event EventHandler ItemCollapsedOrExpanded;


        public CollapsibleContainerStrip()
        {
            InitializeComponent();

            //this.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            
        }

        public void AddButton(int size)
        {
            Button button = new Button();
            button.Height = size;
            button.Width = this.Width;
            button.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            button.Text = this.Controls.Count.ToString();
            this.Controls.Add(button);
            UpdatePositions();
        }

        public CollapsibleControl AddCollapsibleControl(int expandedHeight= 50, string label = "Collapsible Control")
        {
            SuspendLayout();

            CollapsibleControl newControl = new CollapsibleControl();
            this.Controls.Add(newControl);
            newControl.Width = this.Width - SubtractFromWidthForControlWidth;
            
            //newControl.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            
            newControl.ExpandedHeight = 28 + expandedHeight;
            newControl.Height = newControl.ExpandedHeight;

            newControl.CollapseChange += new EventHandler(OnCollapseChange);
            UpdatePositions();
            ResumeLayout();
            return newControl;
        }

        public CollapsibleControl AddCollapsibleControlFor(System.Windows.Controls.UserControl wpfControl, int expandedHeight, string label)
        {
            if (expandedHeight == -1)
            {
                expandedHeight = (int)wpfControl.Height;
            }

            CollapsibleControl newControl = AddCollapsibleControl(expandedHeight);
            newControl.SuspendLayout();

            newControl.Label = label;

            System.Windows.Forms.Integration.ElementHost wpfHost;
            wpfHost = new System.Windows.Forms.Integration.ElementHost();
            wpfHost.Dock = DockStyle.Fill;
            wpfHost.Child = wpfControl;


            newControl.Controls.Add(wpfHost);
            wpfHost.Dock = DockStyle.Fill;
            wpfHost.BringToFront();

            newControl.ResumeLayout();
            return newControl;
        }

        public CollapsibleControl AddCollapsibleControlFor(Control control, int expandedHeight = 50, string label = "Collapsible Control")
        {
            if (expandedHeight == -1)
            {
                expandedHeight = control.Height;
            }

            CollapsibleControl newControl = AddCollapsibleControl(expandedHeight);
            newControl.SuspendLayout();

            newControl.Label = label;
            newControl.Controls.Add(control);
            control.Dock = DockStyle.Fill;
            control.BringToFront();
            
            int m = 3;
            newControl.ResumeLayout();
            return newControl;
        }

        public Control GetControlByLabel(string label)
        {
            foreach (var control in this.Controls)
            {
                if (control is CollapsibleControl)
                {
                    CollapsibleControl container = control as CollapsibleControl;
                    if (container.Label == label && container.Controls.Count != 0)
                    {
                        return container.Controls[0];
                    }
                }
            }
            return null;
        }

        public CollapsibleControl AddCollapsibleControlForReduced(Control control, int expandedHeight = 50, string label = "Collapsible Control")
        {
            if (expandedHeight == -1)
            {
                expandedHeight = control.Height;
            }
            CollapsibleControl newControl;
            {
                newControl = new CollapsibleControl();

                newControl.Width = this.Width;
                newControl.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
                newControl.ExpandedHeight = 28 + expandedHeight;
                newControl.Height = newControl.ExpandedHeight;
                newControl.CollapseChange += new EventHandler(OnCollapseChange);
                this.Controls.Add(newControl);

                UpdatePositions();
            
            }



            newControl.Label = label;
            newControl.Controls.Add(control);
            control.Dock = DockStyle.Fill;
            control.BringToFront();


            return newControl;
        }

        void OnCollapseChange(object sender, EventArgs e)
        {
            UpdatePositions();

            ItemCollapsedOrExpanded?.Invoke(this, null);
        }

        void UpdatePositions()
        {
            mTotalHeight = 0;

            for (int i = 0; i < Controls.Count; i++)
            {
                if (!(Controls[i] is VScrollBar))
                {
                    Control control = Controls[i];

                    mTotalHeight += control.Height + 3;
                }
            }
            // Let's give it some extra height so the user knows he's at the bottom
            mTotalHeight += 4;
            UpdateScrollBar();

            int currentY = -vScrollBar1.Value;

            for (int i = 0; i < Controls.Count; i++)
            {
                if (!(Controls[i] is VScrollBar))
                {
                    Control control = Controls[i];
                    control.Location = new Point(
                        control.Location.X,
                        currentY);
                    currentY += control.Height + 3;
                }
            }


        }

        private void UpdateScrollBar()
        {
            int amountShown = this.Height;

            if (mTotalHeight > amountShown)
            {
                this.vScrollBar1.Minimum = 0;
                this.vScrollBar1.Maximum = mTotalHeight;
                this.vScrollBar1.LargeChange = (int)(mTotalHeight * (amountShown / (float)mTotalHeight));

                this.vScrollBar1.Value = Math.Min(vScrollBar1.Value, vScrollBar1.Maximum - vScrollBar1.LargeChange);
            }
            else
            {
                this.vScrollBar1.LargeChange = vScrollBar1.Maximum;
                this.vScrollBar1.Value = 0;
            }
        }

        private void CollapsibleContainerStrip_SizeChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < Controls.Count; i++)
            {
                if (!(Controls[i] is VScrollBar))
                {
                    Control control = Controls[i];
                    control.Width = this.Width - SubtractFromWidthForControlWidth;
                }
            }
            UpdatePositions();
        }

        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            UpdatePositions();
        }

        private void CollapsibleContainerStrip_ControlRemoved(object sender, ControlEventArgs e)
        {
            SuspendLayout();
            UpdatePositions();
            ResumeLayout();
        }
    }
}
