using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.IO;

namespace FlatRedBall.NAudio
{
    public class NAudio_Song : IDisposable
    {
        NAudio_LoopReader reader;
        WaveOutEvent waveOut;
        WaveChannel32 waveChannel;

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
                waveChannel.Volume = volume;
            }
        }
        public bool IsRepeating
        {
            get => this.reader.IsRepeating;
            set
            {
                this.reader.IsRepeating = value;
            }
        }

        public NAudio_Song(string fileName)
        {
            this.reader = new NAudio_LoopReader(fileName);
            this.reader.IsRepeating = true;
            this.reader.Position = 0;
            this.waveChannel = new WaveChannel32(this.reader, volume, 0);
            this.waveOut = new WaveOutEvent();
            this.waveOut.Init(this.waveChannel);
        }

        public void Play()
        {
            this.waveOut.Play();
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
                waveChannel.Dispose();
                waveOut = null;
                reader = null;
                waveChannel = null;
            }
        }

        public void Stop()
        {
            if (waveOut != null)
            {
                waveOut.Stop();
            }
        }

        public void Dispose()
        {
            TryDisposeContainedObjects();
        }
    }
}
