using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.AnimationEditorForms.Data;
using FlatRedBall.AnimationEditorForms.Preview;

namespace FlatRedBall.AnimationEditorForms.Controls
{


    public partial class PreviewControls : UserControl
    {
        #region Events

        public event EventHandler OnionSkinVisibleChange;
        public event EventHandler SpriteAlignmentChange;
        public event EventHandler ShowGuidesChange;

        #endregion

        public float OffsetMultiplier
        {
            get
            {
                float toReturn = 1;
                float temp = 1;
                if (float.TryParse(OffsetMultTextBox.Text, out temp))
                {
                    // success!
                    toReturn = temp;
                }

                return toReturn;
            }
            set
            {
                OffsetMultTextBox.Text = value.ToString();
            }
        }

        public event EventHandler ZoomChanged;
        ZoomControlLogic mZoomControlLogic;

        public int PercentageValue
        {
            get
            {
                return mZoomControlLogic.PercentageValue;
            }
            set
            {
                mZoomControlLogic.PercentageValue = value;
            }
        }

        public SpriteAlignment SpriteAlignment
        {
            get
            {
                return (SpriteAlignment)SpriteAlignmentComboBox.SelectedItem;
            }
            set
            {
                SpriteAlignmentComboBox.SelectedItem = value;
            }
        }

        public bool IsOnionSkinVisible
        {
            get
            {
                return this.OnionSkinCheckBox.Checked;
            }
            set
            {
                this.OnionSkinCheckBox.Checked = value;
            }
        }

        public bool IsShowGuidesChecked
        {
            get => this.checkBox1.Checked;
            set => this.checkBox1.Checked = value;
        }

        public PreviewControls()
        {
            InitializeComponent();

            OffsetMultiplier = 1;

            PopulateComboBox();

            mZoomControlLogic = new ZoomControlLogic(ZoomComboBox);
        }

        private void PopulateComboBox()
        {
            var values = Enum.GetValues(typeof(SpriteAlignment));

            bool hasSet = false;

            foreach (var value in values)
            {
                SpriteAlignmentComboBox.Items.Add(value);
            }

            // This is the FRB default
            SpriteAlignmentComboBox.SelectedItem = SpriteAlignment.Center;
        }

        private void OnionSkinCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (OnionSkinVisibleChange != null)
            {
                OnionSkinVisibleChange(this, null);
            }
        }

        private void SpriteAlignmentComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SpriteAlignmentChange != null)
            {
                SpriteAlignmentChange(this, null);
            }
        }

        private void ZoomComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.ZoomChanged != null)
            {
                ZoomChanged(this, null);
            }
        }

        public void ZoomIn()
        {
            int index = this.ZoomComboBox.SelectedIndex;
            index--;

            index = System.Math.Max(0, index);

            mZoomControlLogic.PercentageValue = mZoomControlLogic.AvailableZoomLevels[index];
            if (ZoomChanged != null)
            {
                ZoomChanged(this, null);
            }
        }

        public void ZoomOut()
        {
            int index = this.ZoomComboBox.SelectedIndex;
            index++;

            index = System.Math.Min(mZoomControlLogic.AvailableZoomLevels.Count - 1, index);

            mZoomControlLogic.PercentageValue = mZoomControlLogic.AvailableZoomLevels[index];
            if (ZoomChanged != null)
            {
                ZoomChanged(this, null);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            float value = 1;
            if (float.TryParse(OffsetMultTextBox.Text, out value))
            {
                // success!
                PreviewManager.Self.OffsetMultiplier = value;
            }


        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (ShowGuidesChange != null)
            {
                ShowGuidesChange(this, null);
            }
        }
    }
}
