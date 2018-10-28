using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.AnimationEditorForms.ViewModels;

namespace FlatRedBall.AnimationEditorForms.Controls
{
    public partial class WireframeEditControls : UserControl, INotifyPropertyChanged
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

        public WireframeEditControlsViewModel DataContext
        {
            get { return wireframeEditControlsWpf1.DataContext as WireframeEditControlsViewModel; }
            set { wireframeEditControlsWpf1.DataContext = value; }
        }

        #endregion


        #region Event

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion


        public WireframeEditControls()
        {
            InitializeComponent();


            mZoomControlLogic = new ZoomControlLogic(ComboBox);
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

        private void ShowFullAlpha_CheckedChanged(object sender, EventArgs e)
        {
            this.OnPropertyChanged(nameof(ShowFullAlpha));
        }
    }
}
