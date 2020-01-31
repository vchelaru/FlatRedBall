using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.IO;

namespace FlatRedBall.NAudio
{
    public class NAudio_Song : IDisposable
    {
        VorbisWaveReader reader;
        WaveOut waveOut;

        public bool IsPlaying
        {
            get
            {
                return
                    waveOut != null &&
                    waveOut.PlaybackState == PlaybackState.Playing;
            }
        }

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
        public bool IsRepeating { get; set; } = true;

        public NAudio_Song (string fileName) 
        {
            this.reader = new VorbisWaveReader(fileName);
            this.reader.Position = 0;
            this.waveOut = new WaveOut();

            this.waveOut.Init(reader);
            this.waveOut.Volume = volume;
            this.waveOut.PlaybackStopped += HandlePlaybackStopped;
        }

        public void Play()
        {
            this.waveOut.Play();
        }

        private void HandlePlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (IsRepeating)
            {
                StartOver();
            }
            else
            {
                TryDisposeContainedObjects();
            }
        }

        public void StartOver()
        {
            this.reader.Position = 0;
            this.waveOut.Play();
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
