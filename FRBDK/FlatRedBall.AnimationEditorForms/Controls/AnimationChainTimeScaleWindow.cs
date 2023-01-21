using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

namespace FlatRedBall.AnimationEditorForms.Controls
{
    #region Enums

    public enum ScaleMode
    {
        KeepProportional,
        SetAllFramesSame
    }

    #endregion

    public partial class AnimationChainTimeScaleWindow : Form
    {
        #region Fields

        float mValue;
        int mFrameCount;

        #endregion

        public ScaleMode ScaleMode
        {
            get
            {
                if (this.KeepFramesProportionalRadio.Checked)
                {
                    return AnimationEditorForms.Controls.ScaleMode.KeepProportional;
                }
                else
                {
                    return AnimationEditorForms.Controls.ScaleMode.SetAllFramesSame;
                }
            }
        }

        public float Value
        {
            get { return mValue; }
            set
            {
                mValue = value;

                if (TimeTextBox != null)
                {
                    TimeTextBox.Text = mValue.ToString(CultureInfo.InvariantCulture);
                }
            }
        }

        public int FrameCount
        {
            get { return mFrameCount; }
            set
            {
                mFrameCount = value;

                UpdateSetAllRatioText();
            }
        }

        public AnimationChainTimeScaleWindow()
        {
            InitializeComponent();

            StartPosition = FormStartPosition.Manual;
            Location = new Point(AnimationChainTimeScaleWindow.MousePosition.X - this.Width/2, 
                AnimationChainTimeScaleWindow.MousePosition.Y - this.Height/2);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(TimeTextBox.Text) && TimeTextBox.Text != CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
            {
                if (!float.TryParse(TimeTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out mValue))
                {
                    MessageBox.Show("Only numbers are allowed");
                }

                UpdateSetAllRatioText();
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        private void UpdateSetAllRatioText()
        {
            if (FrameCount == 0)
            {
                // do nothing...
            }
            else
            {
                this.SetAllFramesRadioButton.Text = "Set each frame time to " +
                    (Value / FrameCount) + " seconds";
            }
        }

        private void TimeTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        private void TimeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else if(e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }

        private void AnimationChainTimeScaleWindow_Shown(object sender, EventArgs e)
        {
            this.TimeTextBox.Focus();

        }
    }
}
