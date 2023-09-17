using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;

namespace OfficialPlugins.ContentPreview.Views
{
    /// <summary>
    /// Interaction logic for WavPreviewView.xaml
    /// </summary>
    public partial class WavPreviewView : UserControl
    {
        private Stream _stream;
        private SoundPlayer _soundPlayer;


        private FilePath _wavFilePath;
        public FilePath WavFilePath
        {
            get => _wavFilePath;
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
            _stream?.Dispose();
            _soundPlayer?.Dispose();

            if(value.Exists())
            {
                // This locks the file
                //var stream = System.IO.File.OpenRead(value.FullPath);
                var bytes = System.IO.File.ReadAllBytes(value.FullPath);

                _stream= new MemoryStream(bytes);
                _soundPlayer = new SoundPlayer(_stream);

            }
            else
            {
                _stream = null;
                _soundPlayer = null;
            }

            _wavFilePath = value;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e) => PlaySound();

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _soundPlayer?.Stop();
        }

        internal void PlaySound()
        {
            try
            {
                _soundPlayer?.Play();
            }
            catch(Exception e)
            {
                GlueCommands.Self.PrintError(Localization.Texts.ErrorPlayingWavFile + "\n" + e);
            }
        }
    }
}
