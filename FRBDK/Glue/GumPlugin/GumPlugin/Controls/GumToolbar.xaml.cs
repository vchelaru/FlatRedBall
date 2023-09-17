using System;
using System.Windows;
using System.Windows.Controls;

namespace GumPlugin.Controls
{
    /// <summary>
    /// Interaction logic for GumToolbar.xaml
    /// </summary>
    public partial class GumToolbar : UserControl
    {
        public event EventHandler GumButtonClicked;


        public GumToolbar()
        {
            InitializeComponent();
        }

        private void HandleButtonClick(object sender, RoutedEventArgs e)
        {
            GumButtonClicked?.Invoke(this, null);
        }
    }
}
