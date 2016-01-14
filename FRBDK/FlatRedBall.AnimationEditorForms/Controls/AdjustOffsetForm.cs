using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBall.AnimationEditorForms.Controls
{
    public partial class AdjustOffsetForm : Form
    {
        public AdjustOffsetForm()
        {
            InitializeComponent();
        }

        private void adjustOffsetControl1_CancelClick(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        private void adjustOffsetControl1_OkClick(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.adjustOffsetControl1.ApplyOffsets();
            this.Close();
        }

        private void adjustOffsetControl1_Load(object sender, EventArgs e)
        {

        }
    }
}
