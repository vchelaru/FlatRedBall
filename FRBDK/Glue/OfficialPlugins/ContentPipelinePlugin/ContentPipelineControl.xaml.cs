using System;
using System.Windows;
using System.Windows.Controls;

namespace OfficialPlugins.ContentPipelinePlugin
{
    /// <summary>
    /// Interaction logic for ContentPipelineControl.xaml
    /// </summary>
    public partial class ContentPipelineControl : UserControl
    {
        public event EventHandler CheckBoxClicked;
        public event EventHandler RefreshClicked;

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
