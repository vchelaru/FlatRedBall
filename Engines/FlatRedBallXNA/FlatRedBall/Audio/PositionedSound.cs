using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Audio
{
    #region XML Docs
    /// <summary>
    /// A sound which has a position enabling effects such as doppler effect and 
    /// distance-based volume adjustments.
    /// </summary>
    #endregion
    public class PositionedSound : PositionedObject
    {
        #region Fields

        protected Sound mSound;
        protected AudioEmitter mEmitter;

        #endregion

        #region Properties

        public SoundVariableCollection Variables;

        public bool IsStopped { get { return mSound.IsStopped; } }
        public bool IsPlaying { get { return mSound.IsPlaying; } }
        public bool IsPaused { get { return mSound.IsPaused; } }

        #region Xml Docs
        /// <summary>
        /// Gets or sets a scalar applied to the level of Doppler effect calculated
        /// between this sound and a listener.
        /// </summary>
        #endregion
        public float DopplerScale
        {
            get { return mEmitter.DopplerScale; }
            set { mEmitter.DopplerScale = value; }
        }

        #endregion

        #region Constructor

        public PositionedSound(Cue cue, String cueName, string soundBankFile)
            : base()
        {
            mSound = new Sound(cue, cueName, soundBankFile);
            Variables = mSound.Variables;
            mSound.OnCueRetrieved += new Sound.OnCueRetrievedHandler(UpdateAudio);
            
            mEmitter = new AudioEmitter();
        }

        #endregion

        #region Internal Methods

        internal void UpdateAudio()
        {
            mEmitter.Position = this.Position;
            mEmitter.Forward = Vector3.Transform(Vector3.Forward, this.RotationMatrix);
            mEmitter.Up = Vector3.Transform(Vector3.Up, this.RotationMatrix);
            mEmitter.Velocity = this.Velocity;

            mSound.Cue.Apply3D(AudioManager.SoundListener.Listener, mEmitter);
        }

        #endregion

        #region Public Methods

        #region Xml Docs
        /// <summary>
        /// Begins playback of this sound, or resumes playback (if it has been paused)
        /// </summary>
        #endregion
        public void Play()
        {
            mSound.Play();
        }

        #region Xml Docs
        /// <summary>
        /// Pauses playback of this sound
        /// </summary>
        #endregion
        public void Pause()
        {
            mSound.Pause();
        }

        #region Xml Docs
        /// <summary>
        /// Stops playing this sound immediately
        /// </summary>
        #endregion
        public void Stop()
        {
            mSound.Stop();
        }

        #region Xml Docs
        /// <summary>
        /// Stops playing this sound as authored in the XACT project
        /// </summary>
        #endregion
        public void StopAsAuthored()
        {
            mSound.StopAsAuthored();
        }

        #region Xml Docs
        /// <summary>
        /// Stops playing this sound immediately
        /// </summary>
        #endregion
        public void StopImmediately()
        {
            mSound.StopImmediately();
        }

        #endregion
    }
}
