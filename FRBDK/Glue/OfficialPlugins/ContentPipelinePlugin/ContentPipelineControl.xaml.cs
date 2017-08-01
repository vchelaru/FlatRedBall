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

namespace OfficialPlugins.ContentPipelinePlugin
{
    /// <summary>
    /// Interaction logic for ContentPipelineControl.xaml
    /// </summary>
    public partial class ContentPipelineControl : UserControl
    {
        public event EventHandler CheckBoxClicked;
        public event EventHandler RefreshClicked;

        public bool UseContentPipeline
        {
            get
            {
                return CheckBox.IsChecked == true;
            }
            set
            {
                CheckBox.IsChecked = value;
            }
        }

        public ContentPipelineControl()
        {
            InitializeComponent();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxClicked?.Invoke(this, null);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RefreshClicked?.Invoke(this, null);
        }
    }
}
