using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlatRedBall.AnimationEditorForms.Controls
{
    /// <summary>
    /// Interaction logic for AdjustOffsetWindow.xaml
    /// </summary>
    public partial class AdjustOffsetWindow : Window
    {
        public AdjustOffsetWindow()
        {
            InitializeComponent();

        }

        private void AdjustOffsetControlWpf_OkClick()
        {
            AdjustOffsetControl.ViewModel.ApplyOffsets();

            this.DialogResult = true;
        }

        private void AdjustOffsetControlWpf_CancelClick()
        {
            this.DialogResult = false;
        }
    }
}
