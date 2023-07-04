$GLUE_VERSIONS$
using FlatRedBall.Audio;
using NAudio.Wave;
using System;
using System.IO;
using NAudio.Utils;

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

        //public float LoopStartSeconds
        //{
        //    get
        //    {
        //        return BytesToSeconds(loopStream.LoopStartBytes);
        //    }
        //}

        //public int LoopStartBytes
        //{
        //    get => loopStream.LoopStartBytes;
        //    set
        //    {
        //        loopStream.LoopStartBytes = value;
        //    }
        //}

        //public int LoopEndBytes
        //{
        //    get => loopStream.LoopEndBytes;
        //    set
        //    {
        //        loopStream.LoopEndBytes = value;
        //    }
        //}

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

        public TimeSpan Position
        {
            get
            {
                //var rawPosition = (float) loopStream..GetPositionTimeSpan().TotalSeconds;
                //loopStream.Position 

                // This returns the position read in the stream, but not how far
                // the song has played:
                //var loopPositionBytes = loopStream.Position;
                //return BytesToSeconds(loopPositionBytes);

                var rawPosition = (float)waveOut.GetPositionTimeSpan().TotalSeconds;
                return TimeSpan.FromSeconds(rawPosition % Duration.TotalSeconds);
            }
            set
            {
                var bytes = SecondsToBytes((float)value.TotalSeconds);
                loopStream.Position = bytes;
            }
        }

        private float BytesToSeconds(long bytes)
        {
            var timespan = TimeSpan.FromMilliseconds(
                (double)(bytes /
                (waveOut.OutputWaveFormat.Channels * waveOut.OutputWaveFormat.BitsPerSample / 8)) * 1000.0 / (double)waveOut.OutputWaveFormat.SampleRate);

            return (float)timespan.TotalSeconds;
        }

        private long SecondsToBytes(float seconds)
        {
            return (long)((seconds * (float)waveOut.OutputWaveFormat.SampleRate * (waveOut.OutputWaveFormat.Channels * waveOut.OutputWaveFormat.BitsPerSample / 8)));
        }

        public TimeSpan Duration => reader.TotalTime;
    }
}
