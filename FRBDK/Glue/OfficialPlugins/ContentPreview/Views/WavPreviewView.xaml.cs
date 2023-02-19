using FlatRedBall.Glue.Plugins.ExportedImplementations;
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


        FilePath wavFilePath;
        public FilePath WavFilePath
        {
            get => wavFilePath;
            set
            {
                ForceRefreshToFilePath(value);
            }
        }

        public WavPreviewView()
        {
            InitializeComponent();
        }

        public void ForceRefreshToFilePath(FilePath value)
        {
            Stream?.Dispose();
            SoundPlayer?.Dispose();

            if(value.Exists())
            {
                // This locks the file
                //var stream = System.IO.File.OpenRead(value.FullPath);
                var bytes = System.IO.File.ReadAllBytes(value.FullPath);

                Stream= new MemoryStream(bytes);
                SoundPlayer = new SoundPlayer(Stream);

            }
            else
            {
                Stream = null;
                SoundPlayer = null;
            }

            wavFilePath = value;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e) => PlaySound();

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            SoundPlayer?.Stop();
        }

        internal void PlaySound()
        {
            try
            {
                SoundPlayer?.Play();
            }
            catch(Exception e)
            {
                GlueCommands.Self.PrintError($"Error playing wav file:\n" + e.ToString());
            }
        }
    }
}
