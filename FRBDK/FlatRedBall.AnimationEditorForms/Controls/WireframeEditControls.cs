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
    public partial class WireframeEditControls : UserControl, INotifyPropertyChanged
    {
        #region Fields

        public event EventHandler ZoomChanged;
        ZoomControlLogic mZoomControlLogic;
        public event EventHandler SnapToGridChanged;

        #endregion

        #region Properties

        public List<int> AvailableZoomLevels
        {
            get
            {
                return mZoomControlLogic.AvailableZoomLevels;
            }
        }

        public int GridSize
        {
            get
            {
                int toReturn = 32;
                int.TryParse(GridSizeTextBox.Text, out toReturn);

                return toReturn;
            }
            set
            {
                GridSizeTextBox.Text = value.ToString();
            }
        }

        public bool ShowFullAlpha
        {
            get
            {
                return ShowFullAlphaCheckBox.Checked;
            }
            set
            {
                ShowFullAlphaCheckBox.Checked = value;
                this.OnPropertyChanged(nameof(ShowFullAlpha));
            }
        }

        public bool SnapToGrid
        {
            get
            {
                return SnapToGridCheckBox.Checked;
            }
            set
            {
                SnapToGridCheckBox.Checked = value;
                this.OnPropertyChanged(nameof(SnapToGrid));
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
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion


        public WireframeEditControls()
        {
            PropertyChanged += HandlePropertyChanged;

            InitializeComponent();


            mZoomControlLogic = new ZoomControlLogic(ComboBox);
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(SnapToGrid):
                    this.GridSizeTextBox.Enabled = SnapToGrid;
                    break;
            }
        }

        private void UpdateRectangleSelectorSnapping()
        {
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

            this.OnPropertyChanged(nameof(SnapToGrid));
        }

        private void ShowFullAlpha_CheckedChanged(object sender, EventArgs e)
        {
            this.OnPropertyChanged(nameof(ShowFullAlpha));
        }
    }
}
