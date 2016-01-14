using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBall.AnimationEditorForms.Controls
{
    public partial class WireframeEditControls : UserControl
    {
        #region Fields

        public event EventHandler ZoomChanged;
        ZoomControlLogic mZoomControlLogic;

        #endregion

        #region Properties

        public List<int> AvailableZoomLevels
        {
            get
            {
                return mZoomControlLogic.AvailableZoomLevels;
            }
        }

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

        public bool IsMagicWandSelected
        {
            get
            {
                return MagicWandCheckBox.Checked;
            }
            set
            {
                MagicWandCheckBox.Checked = value;
            }
        }

        #endregion

        
        #region Event

        public event EventHandler WandSelectionChanged;

        #endregion


        public WireframeEditControls()
        {
            InitializeComponent();

            mZoomControlLogic = new ZoomControlLogic(ComboBox);
        }



        private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.ZoomChanged != null)
            {
                ZoomChanged(this, null);
            }
        }

        private void MagicWandCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (WandSelectionChanged != null)
            {
                WandSelectionChanged(this, null);
            }
        }
    }
}
