$GLUE_VERSIONS$
using FlatRedBall.Audio;
using NAudio.Wave;
using System;
using System.IO;
using NAudio.Utils;

namespace FlatRedBall.NAudio
{
#if !WEB

    public class NAudio_Song : IDisposable
#if ISongInFrb
        , ISong
#endif
    {
        public bool IsDisposed { get; private set; }
        AudioFileReader reader;

        WaveOutEvent waveOut;
        LoopStream loopStream;

        public event EventHandler PlaybackStopped;
        public event EventHandler Looped;

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
                // prevent it from going negative. Although technically supported, it's confusing...
                volume = System.Math.Max(value, 0);
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
            var fullFile = fileName;
            if (FlatRedBall.IO.FileManager.IsRelative(fullFile))
            {
                fullFile = FlatRedBall.IO.FileManager.RelativeDirectory + fileName;
            }
            var extension = FlatRedBall.IO.FileManager.GetExtension(fullFile);
            if (extension == "mp3")
            {
#if DEBUG
                if(!System.IO.File.Exists(fullFile))
                {
                    throw new FileNotFoundException($"Could not find NAudio song file: {fullFile}");
                }
#endif
                this.reader = new AudioFileReader(fullFile);
                loopStream = new LoopStream(reader);
            }
            else
            {
                throw new NotImplementedException($"The extension {extension} is not supported");
            }

            loopStream.Looped += (not, used) => Looped?.Invoke(this, null);

            this.Name = fileName;

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
            CheckDisposed();
            this.waveOut.Play();
        }

        private void CheckDisposed()
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException($"The NAudio_Song {Name} is disposed, so it cannot be played.");
            }
        }

        public void StartOver()
        {
            CheckDisposed();

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

            IsDisposed = true;
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


#endif
}
