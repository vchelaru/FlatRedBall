using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TileGraphicsPlugin.Controls
{
    public partial class ControlForAddingCollision : UserControl
    {
        public bool HasCollision
        {
            get
            {
                return this.HasCollisionCheckBox.Checked;
            }
            set
            {
                this.HasCollisionCheckBox.Checked = value;

                this.groupBox1.Visible = HasCollision;

                //if (HasCollision)
                //{
                //    Height = 64;
                //}
                //else
                //{
                //    Height = 30;
                //}
            }
        }

        public bool RectangleSelected { get { return this.RectangleRadioButton.Checked; } }
        public bool CircleSelected { get { return this.CircleRadioButton.Checked; } }

        public ControlForAddingCollision()
        {
            InitializeComponent();
        }

        private void HasCollisionCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            HasCollision = HasCollision;
        }
    }
}
