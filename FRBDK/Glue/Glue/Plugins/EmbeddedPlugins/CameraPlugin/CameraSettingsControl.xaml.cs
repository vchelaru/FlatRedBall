using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CameraPlugin
{
    /// <summary>
    /// Interaction logic for CameraSettingsControl.xaml
    /// </summary>
    public partial class CameraSettingsControl : UserControl
    {
        public CameraSettingsControl()
        {
            InitializeComponent();
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tBox = (TextBox)sender;
                DependencyProperty prop = TextBox.TextProperty;

                BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
                if (binding != null) { binding.UpdateSource(); }
            }
        }

        //private void StretchRadioButtonChecked(object sender, RoutedEventArgs e)
        //{
        //    StretchAreaMediaElement.Position = TimeSpan.FromMilliseconds(1000);
        //    StretchAreaMediaElement.Play();
        //}

        //private void IncreaseAreaRadioButtonChecked(object sender, RoutedEventArgs e)
        //{
        //    IncreaseAreaMediaElement.Position = TimeSpan.FromMilliseconds(1000);
        //    IncreaseAreaMediaElement.Play();
        //}

        //private void StretchRadioButtonGumChecked(object sender, RoutedEventArgs e)
        //{
        //    StretchAreaGumMediaElement.Position = TimeSpan.FromMilliseconds(1000);
        //    StretchAreaGumMediaElement.Play();
        //}

        //private void IncreaseAreaRadioButtonGumChecked(object sender, RoutedEventArgs e)
        //{
        //    IncreaseAreaGumMediaElement.Position = TimeSpan.FromMilliseconds(1000);
        //    IncreaseAreaGumMediaElement.Play();
        //}


        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;

        }

        private void HandleMediaEnded(object sender, RoutedEventArgs e)
        {
            (sender as MediaElement).Position = TimeSpan.FromMilliseconds(1);
            //(sender as MediaElement).Play();
        }
    }
}
