using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Audio;

namespace FlatRedBall.Audio
{
    #region Xml Docs
    /// <summary>
    /// Used to manage variables in a sound
    /// </summary>
    #endregion
    public struct SoundVariableCollection
    {
        #region Fields

        private Cue mCue;

        // A cue can only be used once.  How lame is that?
        // So when the new cue is created, the variables are
        // all tossed.  Therefore, this is going to store off
        // the variables so that the user can set them once and
        // forget them.
        internal Dictionary<string, float> mVariableValues;

        #endregion

        #region Properties


        public float this[String variable]
        {
            get { return mCue.GetVariable(variable); }
            set 
            { 
                mCue.SetVariable(variable, value);

                if (mVariableValues.ContainsKey(variable))
                {
                    mVariableValues[variable] = value;
                }
                else
                {
                    mVariableValues.Add(variable, value);
                }
            
            }
        }

        internal Cue Cue
        {
            get { return mCue; }
            set
            {
                mCue = value;

                foreach (KeyValuePair<string, float> kvp in mVariableValues)
                {
                    mCue.SetVariable(kvp.Key, kvp.Value);
                }

            }
        }

        #endregion

        #region Constructor

        internal SoundVariableCollection(Cue cue)
        {
            mVariableValues = new Dictionary<string,float>();

            mCue = cue;
        }

        #endregion

        #region Public Methods


        #endregion
    }

    public class Sound
    {
        #region Delegates

        internal delegate void OnCueRetrievedHandler();

        #endregion

        #region Fields

        protected Cue mCue;
        protected String mCueName;
        protected string mSoundBankFile;

        public SoundVariableCollection Variables;

        #endregion

        #region Properties

        internal Cue Cue { get { return mCue; } }

        public bool IsStopped { get { return mCue.IsStopped; } }
        public bool IsPlaying { get { return mCue.IsPlaying; } }
        public bool IsPaused { get { return mCue.IsPaused; } }

        #endregion

        #region Construction

        internal Sound(Cue cue, String cueName, string soundBankFile)
        {
            mCue = cue;
            mCueName = cueName;
            mSoundBankFile = soundBankFile;
            Variables = new SoundVariableCollection(mCue);
        }

        #endregion

        #region Public Methods

        internal event OnCueRetrievedHandler OnCueRetrieved;

        #region Xml Docs
        /// <summary>
        /// Begins playback of this sound, or resumes playback (if it has been paused)
        /// </summary>
        #endregion
        public void Play()
        {
			if (mCue.IsDisposed || mCue.IsStopped)
			{
				// Get the cue again, since it has gone out of scope
				mCue = AudioManager.GetCue(mCueName, mSoundBankFile);
                
				// Setting this will reset the variables.
				Variables.Cue = mCue;
				if (OnCueRetrieved != null)
				{
					OnCueRetrieved();
				}
			}

            if (mCue.IsPaused)
            {
                mCue.Resume();
            }
            else if (!mCue.IsPlaying && mCue.IsPrepared)
            {
                if (OnCueRetrieved != null)
                {
                    OnCueRetrieved();
                }
                mCue.Play();
            }
        }

        #region Xml Docs
        /// <summary>
        /// Pauses playback of this sound
        /// </summary>
        #endregion
        public void Pause()
        {
            if (mCue.IsPlaying)
            {
                mCue.Pause();
            }
        }

        #region Xml Docs
        /// <summary>
        /// Stops playing this sound as authored
        /// </summary>
        #endregion
        public void Stop()
        {
            if (mCue.IsPlaying || mCue.IsPaused)
            {
                mCue.Stop(AudioStopOptions.AsAuthored);
            }
        }

        #region Xml Docs
        /// <summary>
        /// Stops playing this sound immediately
        /// </summary>
        #endregion
        public void StopImmediately()
        {
            if (mCue.IsPlaying || mCue.IsPaused)
            {
                mCue.Stop(AudioStopOptions.Immediate);
            }
        }

        #region Xml Docs
        /// <summary>
        /// Stops playing this sound as authored in the XACT project
        /// </summary>
        #endregion
        public void StopAsAuthored()
        {
            if (mCue.IsPlaying || mCue.IsPaused)
            {
                mCue.Stop(AudioStopOptions.AsAuthored);
            }
        }

        #endregion
    }
}
