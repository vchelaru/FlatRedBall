using FlatRedBall.ContentExtensions.Encryption;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlatRedBall.ContentExtensions.ContentTypes
{
    public class StreamedSong : IDisposable
    {
        Mp3FileReader reader;
        WaveOut waveOut;
        Stream stream;

        public Song XnaSong
        {
            get;
            private set;
        }

        string customName;

        public string Name
        {
            get
            {
                if (XnaSong != null)
                {
                    return XnaSong.Name;
                }
                else
                {
                    return customName;
                }
            }
        }

        List<IDisposable> ownedDisposables = new List<IDisposable>();

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
            get { return volume; }
            set
            {
                volume = value;
                if (waveOut != null)
                {
                    waveOut.Volume = volume;
                }
            }
        }

        bool isRepeating;
        public bool IsRepeating
        {
            get
            {
                return isRepeating;
            }
            set
            {
                isRepeating = value;
            }
        }

        public StreamedSong(string xnbFile, ContentManager contentManager)
        {
            XnaSong = contentManager.Load<Song>(xnbFile);
        }

        public StreamedSong(string aesEncryptedFileName, string aesKey, string aesInitialVector)
        {
            var manager = new AesEncryptionManager();
            manager.EncryptionKey = aesKey;
            manager.InitialVector = aesInitialVector;

            using (var encryptedStream = System.IO.File.OpenRead(aesEncryptedFileName))
            {
                int decryptedSize;
                var decryptedBytes = manager.DecryptFromStream(encryptedStream, (int)encryptedStream.Length,
                    out decryptedSize);

                stream = new MemoryStream(decryptedBytes, 0, decryptedSize);

                ownedDisposables.Add(stream);
            }

            this.customName = aesEncryptedFileName;
        }

        public StreamedSong(Stream stream, string name)
        {
            this.stream = stream;
            this.customName = name;
        }

        public void Play()
        {
            if (XnaSong != null)
            {
                PlayXnaSong();
            }
            else
            {
                PlayNAudioSong();
            }
        }

        private void PlayNAudioSong()
        {
            bool isAlreadyPlaying = waveOut != null;

            if (isAlreadyPlaying)
            {
                throw new InvalidOperationException("Song is already playing");
            }

            // This makes it behave like XNA - which is crappy, but Baron needs it to behave like XNA for now:
            stream.Position = 0;

            reader = new Mp3FileReader(stream);
            waveOut = new WaveOut();

            waveOut.Init(reader);
            waveOut.Volume = volume;

            waveOut.Play();

            waveOut.PlaybackStopped += HandlePlaybackStopped;
        }

        private void PlayXnaSong()
        {
            MediaPlayer.Play(XnaSong);
        }

        private void HandlePlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (IsRepeating)
            {
                this.stream.Position = 0;
                waveOut.Play();
            }
            else
            {
                TryDisposeContainedObjects();
            }
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
            // When a song stops, we want to reset the volume back to 1 so the next song plays full volume and sfx aren't impacted
            if (waveOut != null)
            {
                waveOut.Volume = 1;
            }
            TryDisposeContainedObjects();
        }

        public void Dispose()
        {
            TryDisposeContainedObjects();
        }

    }
}
