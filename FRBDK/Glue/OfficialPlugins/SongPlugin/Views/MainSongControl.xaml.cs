using FlatRedBall.IO;
using NAudio.Vorbis;
using NAudio.Wave;
using OfficialPlugins.SongPlugin.ViewModels;
using System;
using System.Collections.Generic;
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

namespace OfficialPlugins.SongPlugin.Views
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class MainSongControl : UserControl
    {
        MainSongControlViewModel ViewModel => DataContext as MainSongControlViewModel;

        private MediaPlayer mediaPlayer = new MediaPlayer();

        VorbisWaveReader nAudioFileReader;
        WaveOutEvent nAudioOutputDevice = new WaveOutEvent();

        FilePath filePath;
        public FilePath FilePath
        {
            set
            {
                filePath = value;
                if (value?.Exists() == true)
                {
                    StopPlaying();
                    if(value.Extension == "ogg")
                    {
                        nAudioFileReader?.Dispose();
                        nAudioOutputDevice?.Dispose();

                        nAudioOutputDevice = new WaveOutEvent();
                        nAudioFileReader = new VorbisWaveReader(value.FullPath);
                        nAudioOutputDevice.Init(nAudioFileReader);

                        ViewModel.Duration = nAudioFileReader.TotalTime;
                    }
                    else if(value.Extension == "mp3")
                    {
                        mediaPlayer.Open(new Uri(value.FullPath));

                        if(mediaPlayer.NaturalDuration.HasTimeSpan)
                        {
                            ViewModel.Duration = mediaPlayer.NaturalDuration.TimeSpan;
                        }
                        else
                        {
                            // maybe we should fall back to naudio
                            var tempReader = new Mp3FileReader(value.FullPath);
                            ViewModel.Duration = tempReader.TotalTime;
                            tempReader.Dispose();   
                                
                        }
                    }
                }
            }
        }

        public MainSongControl()
        {
            InitializeComponent();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if(filePath?.Extension == "ogg")
            {
                nAudioOutputDevice.Play();
            }
            else
            {
                mediaPlayer.Play();
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            nAudioOutputDevice?.Stop();
            mediaPlayer?.Stop();
        }

        internal void StopPlaying()
        {
            nAudioOutputDevice?.Stop();
            mediaPlayer?.Stop();
        }
    }
}
