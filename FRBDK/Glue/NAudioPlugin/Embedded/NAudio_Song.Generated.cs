$GLUE_VERSIONS$
using FlatRedBall.Audio;
using NAudio.Wave;
using System;
using System.IO;

namespace FlatRedBall.NAudio
{
    public class NAudio_Song : IDisposable
#if ISongInFrb
        , ISong
#endif
    {
        AudioFileReader reader;

        WaveOutEvent waveOut;
        LoopStream loopStream;

        public event EventHandler PlaybackStopped;

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
                if (reader != null)
                {
                    reader.Volume = volume;
                }
            }
        }
        public bool IsRepeating
        {
            get => this.loopStream.EnableLooping;
            set
            {
                this.loopStream.EnableLooping = value;
            }
        }

        public string Name { get; set; }

        public NAudio_Song(string fileName)
        {
            var extension = FlatRedBall.IO.FileManager.GetExtension(fileName);
            if (extension == "mp3")
            {
                this.reader = new AudioFileReader(fileName);
                loopStream = new LoopStream(reader);
            }
            else
            {
                throw new NotImplementedException($"The extension {extension} is not supported");
            }

            waveOut = new WaveOutEvent();
            waveOut.Init(loopStream);
            waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
        }

        private void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            PlaybackStopped?.Invoke(this, null);
        }

        public void Play()
        {
            this.waveOut.Play();
        }

        public void StartOver()
        {
            if (reader != null)
            {
                this.reader.Position = 0;
            }
            this.waveOut.Play();
        }

        private void TryDisposeContainedObjects()
        {

            bool needsToDispose = waveOut != null;

            if (needsToDispose)
            {
                waveOut?.Dispose();
                reader?.Dispose();
                loopStream?.Dispose();
                waveOut = null;
                reader = null;
                loopStream = null;
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
