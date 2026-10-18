using System;
using FlatRedBall.Audio;
using Microsoft.Xna.Framework.Audio;

namespace AotAudioTest
{
    /// <summary>
    /// An <see cref="ISong"/> backed by a looping <see cref="SoundEffectInstance"/>. MonoGame's
    /// DesktopGL <c>Song</c> only plays .ogg and nothing here can encode one, so the song is a .wav
    /// played through the same OpenAL device as the sound effect.
    /// </summary>
    public class WavSong : ISong
    {
        readonly SoundEffect soundEffect;
        readonly SoundEffectInstance instance;

        public WavSong(string name, SoundEffect soundEffect)
        {
            Name = name;
            this.soundEffect = soundEffect;
            instance = soundEffect.CreateInstance();
            instance.IsLooped = true;
        }

        public string Name { get; set; }

        public bool IsPlaying => instance.State == SoundState.Playing;

        public bool IsRepeating
        {
            get => instance.IsLooped;
            set => instance.IsLooped = value;
        }

        public float Volume
        {
            get => instance.Volume;
            set => instance.Volume = value;
        }

        public event EventHandler PlaybackStopped;

        public void Play() => instance.Play();

        public void StartOver()
        {
            instance.Stop();
            instance.Play();
        }

        public void Stop()
        {
            instance.Stop();
            PlaybackStopped?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            instance.Dispose();
            soundEffect.Dispose();
        }
    }
}
