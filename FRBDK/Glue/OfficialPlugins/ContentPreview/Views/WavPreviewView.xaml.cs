using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
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

namespace OfficialPlugins.ContentPreview.Views
{
    /// <summary>
    /// Interaction logic for WavPreviewView.xaml
    /// </summary>
    public partial class WavPreviewView : UserControl
    {
        Stream Stream;
        SoundPlayer SoundPlayer;

        public FilePath WavFilePath
        {
            set
            {
                UpdateToFilePath(value);
            }
        }

        public WavPreviewView()
        {
            InitializeComponent();
        }

        private void UpdateToFilePath(FilePath value)
        {
            Stream?.Dispose();
            SoundPlayer?.Dispose();

            if(value.Exists())
            {
                var stream = System.IO.File.OpenRead(value.FullPath);

                SoundPlayer = new SoundPlayer(stream);

            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            SoundPlayer?.Play();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            SoundPlayer?.Stop();
        }
    }
}
