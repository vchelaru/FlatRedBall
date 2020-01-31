using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace FlatRedBall.NAudio
{
    public class NAudio_Sfx: IDisposable
    {
        private AudioFileReader reader;
        private WaveOut waveOut;

        float volume = 1;
        public float Volume
        {
            get => volume;
            set
            {
                volume = value;
                if (waveOut != null)
                {
                    waveOut.Volume = volume;
                }
            }
        }

        public NAudio_Sfx(string fileName)
        {
            reader = new AudioFileReader(fileName);
            reader.Position = 0;
            waveOut = new WaveOut();
            waveOut.Volume = volume;
            waveOut.Init(reader);
            waveOut.PlaybackStopped += OnPlaybackStopped;
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            reader.Position = 0;
        }

        public void Play()
        {
            waveOut.Play();
        }

        private void TryDisposeContainedObjects()
        {

            bool needsToDispose = waveOut != null;

            if (needsToDispose)
            {
                waveOut.Dispose();
                reader.Dispose();
                waveOut = null;
                reader = null;
            }
        }

        public void Stop()
        {
            if (waveOut != null)
            {
                waveOut.Volume = volume;
            }
            TryDisposeContainedObjects();
        }

        public void Dispose()
        {
            TryDisposeContainedObjects();
        }
    }
}
