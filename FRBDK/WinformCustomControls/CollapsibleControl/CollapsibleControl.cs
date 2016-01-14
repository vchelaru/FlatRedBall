using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel.Design;

namespace FlatRedBall.Winforms
{
    public enum ExpandedState
    {
        Expanded,
        Collapsed
    }


    //[DesignTimeVisible(true)]
    //[Category("Containers")]
    //[Designer("System.Windows.Forms.Design.ParentControlDesigner, System.Design", typeof(IDesigner))] 
    public partial class CollapsibleControl : UserControl
    {
        public bool ExpandedState;

        const int CollapsedHeight = 28;

        public event EventHandler CollapseChange;

        bool mIsCollapsed = false;
        public int ExpandedHeight
        {
            get;
            set;
        }

        public string Label
        {
            get
            {
                return this.label1.Text;
            }
            set
            {
                label1.Text = value;
            }
        }

        public bool IsCollapsed
        {
            get
            {
                return mIsCollapsed;
            }
            set
            {
                if (mIsCollapsed != value)
                {
                    mIsCollapsed = value;

                    if (mIsCollapsed)
                    {
                        this.MinimumSize = new Size();
                        this.Height = CollapsedHeight;
                    }
                    else
                    {
                        this.Height = ExpandedHeight;
                    }
                    if (CollapseChange != null)
                    {
                        CollapseChange(this, null);
                    }
                }
            }
        }

        public CollapsibleControl()
        {
            InitializeComponent();

            ExpandedHeight = 100;
        }

        private void panel1_Click(object sender, EventArgs e)
        {
            this.IsCollapsed = !this.IsCollapsed;
        }

        private void label1_Click(object sender, EventArgs e)
        {
            this.IsCollapsed = !this.IsCollapsed;
        }
    }
}
