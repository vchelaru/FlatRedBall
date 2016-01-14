using FlatRedBall.Arrow.GlueView;
using FlatRedBall.Content.Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FlatRedBall.Arrow.Controls
{
    /// <summary>
    /// Interaction logic for CameraSettingsForm.xaml
    /// </summary>
    public partial class CameraSettingsForm : Window
    {
        #region Events

        public event EventHandler CameraChanged;

        #endregion

        public CameraSave CameraSave
        {
            get
            {
                return (CameraSave)DataGridUi.Instance;
            }
            set
            {
                DataGridUi.Instance = value;

                AddIncludesAndExcludes();
            }
        }

        public CameraSettingsForm()
        {
            InitializeComponent();

            DataGridUi.PropertyChange += HandlePropertyChange;
        }

        private void HandlePropertyChange(string arg1, WpfDataUi.EventArguments.PropertyChangedArgs arg2)
        {
            if (CameraChanged != null)
            {
                CameraChanged(this, null);
            }

        }


        private void AddIncludesAndExcludes()
        {
            DataGridUi.MembersToIgnore.Add("X");
            DataGridUi.MembersToIgnore.Add("Y");
            DataGridUi.MembersToIgnore.Add("Z");

            DataGridUi.MembersToIgnore.Add("NearClipPlane");
            DataGridUi.MembersToIgnore.Add("FarClipPlane");

            DataGridUi.MembersToIgnore.Add("AspectRatio");

        }

    }
}
