using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using FlatRedBall.Math;
using FlatRedBall.IO;

namespace FlatRedBall.Audio
{
    #region SoundGlobalVariableCollection class


#if !WINDOWS_PHONE && !MONOGAME

    #region Xml Docs
    /// <summary>
    /// Used to manage global variables in the audio engine
    /// </summary>
    #endregion
    public struct SoundGlobalVariableCollection
    {
        #region Fields

        internal AudioEngine mAudioEngine;

        #endregion

        #region Constructor

        internal SoundGlobalVariableCollection(AudioEngine audioEngine)
        {
            mAudioEngine = audioEngine;
        }

        #endregion

        #region Public Methods

        public float this[String variable]
        {
            get { return mAudioEngine.GetGlobalVariable(variable); }
            set { mAudioEngine.SetGlobalVariable(variable, value); }
        }

        #endregion
    }
#endif
    #endregion

    public static partial class AudioManager
    {

#if !WINDOWS_PHONE && !MONOGAME
        private static AudioEngine mXnaAudioEngine;
        //private static WaveBank mXnaWaveBank;
        //private static SoundBank mXnaSoundBank;

        private static Dictionary<string, SoundBank> mXnaSoundBanks = new Dictionary<string, SoundBank>();
        private static Dictionary<string, WaveBank> mXnaWaveBanks = new Dictionary<string, WaveBank>();

        public static PositionedSoundListener SoundListener;
        private static PositionedObjectList<PositionedSound> PositionedSounds;

        public static SoundGlobalVariableCollection GlobalVariables;
#endif
        private static string mDefaultWaveBank;
        private static string mDefaultSoundBank;


#if !WINDOWS_PHONE && !MONOGAME
        #region XML Docs
        /// <summary>
        /// Adds a sound bank
        /// </summary>
        /// <param name="soundBankFile">The sound bank to add</param>
        #endregion
        public static void AddSoundBank(string soundBankFile)
        {
            if (!mXnaSoundBanks.ContainsKey(soundBankFile))
            {
                string standardisedSoundBankFile = FileManager.Standardize(soundBankFile);
                mXnaSoundBanks.Add(soundBankFile, new SoundBank(mXnaAudioEngine, standardisedSoundBankFile));
            }
        }

        #region XML Docs
        /// <summary>
        /// Adds a wave bank
        /// </summary>
        /// <param name="waveBankFile">The wave bank to add</param>
        #endregion
        public static void AddWaveBank(string waveBankFile)
        {
            if (!mXnaWaveBanks.ContainsKey(waveBankFile))
            {
                string standardisedWaveBankFile = FileManager.Standardize(waveBankFile);

                WaveBank waveBank;
                try
                {
                    waveBank = new WaveBank(mXnaAudioEngine, standardisedWaveBankFile);
                }
                catch (InvalidOperationException)
                {
                    //Perhaps it's a streaming WaveBank... 
                    waveBank = new WaveBank(mXnaAudioEngine, standardisedWaveBankFile, 0, (short)16);

                    //NM: Wait for the song to be preapred if necessary
                    //I have timed this and the time to prepare is negligible and it ensures the song will actually play and not just fail silently.
                    while (!waveBank.IsPrepared)
                    {
                        //you need to call update at least once before playing a streaming sound
                        mXnaAudioEngine.Update();
                    }
                }

                mXnaWaveBanks.Add(waveBankFile, waveBank);
            }
        }

        #region XML Docs
        /// <summary>
        /// Removes a sound bank
        /// </summary>
        /// <param name="soundBankFile">The sound bank to remove</param>
        #endregion
        public static void RemoveSoundBank(string soundBankFile)
        {
            if (mXnaSoundBanks.ContainsKey(soundBankFile))
            {
                SoundBank sb = mXnaSoundBanks[soundBankFile];
                mXnaSoundBanks.Remove(soundBankFile);
                sb.Dispose();

                if (mDefaultSoundBank == soundBankFile)
                {
                    if (mXnaSoundBanks.Count > 0)
                    {
                        mDefaultSoundBank = mXnaSoundBanks.Keys.GetEnumerator().Current;
                    }
                    else
                    {
                        mDefaultSoundBank = string.Empty;
                    }
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// Removes a wave bank
        /// </summary>
        /// <param name="waveBankFile">The wave bank to remove</param>
        #endregion
        public static void RemoveWaveBank(string waveBankFile)
        {
            if (mXnaWaveBanks.ContainsKey(waveBankFile))
            {
                WaveBank wb = mXnaWaveBanks[waveBankFile];
                mXnaWaveBanks.Remove(waveBankFile);
                wb.Dispose();

                if (mDefaultWaveBank == waveBankFile)
                {
                    if (mXnaWaveBanks.Count > 0)
                    {
                        mDefaultWaveBank = mXnaWaveBanks.Keys.GetEnumerator().Current;
                    }
                    else
                    {
                        mDefaultWaveBank = string.Empty;
                    }
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// Whether or not the given sound bank file has been loaded
        /// </summary>
        /// <param name="soundBankFile">The sound bank to check</param>
        /// <returns>Whether or not the sound bank has been loaded</returns>
        #endregion
        public static bool HasSoundBank(string soundBankFile)
        {
            return mXnaSoundBanks.ContainsKey(soundBankFile);
        }

        #region XML Docs
        /// <summary>
        /// Whether or not the given wave bank file has been loaded
        /// </summary>
        /// <param name="soundBankFile">The wave bank to check</param>
        /// <returns>Whether or not the wave bank has been loaded</returns>
        #endregion
        public static bool HasWaveBank(string soundBankFile)
        {
            return mXnaWaveBanks.ContainsKey(soundBankFile);
        }

        #region Xml Docs
        /// <summary>
        /// Gets a sound
        /// </summary>
        /// <param name="soundName">The name of the sound in the XACT project</param>
        /// <returns>A new sound</returns>
        #endregion
        public static Sound GetSound(String soundName)
        {
            return GetSound(soundName, mDefaultSoundBank);
        }

        #region Xml Docs
        /// <summary>
        /// Gets a sound
        /// </summary>
        /// <param name="soundName">The name of the sound in the XACT project</param>
        /// <param name="soundBankFile">The name of the sound bank to retrieve the sound from</param>
        /// <returns>A new sound</returns>
        #endregion
        public static Sound GetSound(String soundName, string soundBankFile)
        {
            return new Sound(GetCue(soundName, soundBankFile), soundName, soundBankFile);
        }


        #region Xml Docs
        /// <summary>
        /// Gets a positioned sound
        /// </summary>
        /// <param name="soundName">The name of the sound in the XACT project</param>
        /// <returns>A new positioned sound</returns>
        #endregion
        public static PositionedSound GetPositionedSound(String soundName)
        {
            return GetPositionedSound(soundName, mDefaultSoundBank);
        }

        #region Xml Docs
        /// <summary>
        /// Gets a positioned sound
        /// </summary>
        /// <param name="soundName">The name of the sound in the XACT project</param>
        /// <param name="soundBankFile">The name of the sound bank to retrieve the sound from</param>
        /// <returns>A new positioned sound</returns>
        #endregion
        public static PositionedSound GetPositionedSound(String soundName, string soundBankFile)
        {
            PositionedSound newPosSound = new PositionedSound(GetCue(soundName, soundBankFile), soundName, soundBankFile);
            PositionedSounds.Add(newPosSound);
            return newPosSound;
        }

        /// <summary>
        /// Removes the specified sound from the audio manager
        /// Warning: The sound will stop being positioned when removed
        /// </summary>
        /// <param name="sound">The sound to remove</param>
        public static void RemovePositionedSound(PositionedSound sound)
        {
            sound.RemoveSelfFromListsBelongingTo();
        }
#endif



#if !WINDOWS_PHONE && !MONOGAME

        #region XML Docs
        /// <summary>
        /// Plays a sound immediately
        /// </summary>
        /// <param name="soundName">The name of the sound in the XACT project</param>
        #endregion
        public static Cue PlaySound(String soundName)
        {
            return PlaySound(soundName, mDefaultSoundBank);
        }
#endif

#if !WINDOWS_PHONE && !MONOGAME
        #region XML Docs
        /// <summary>
        /// Plays a sound immediately
        /// </summary>
        /// <param name="soundName">The name of the sound in the XACT project</param>
        /// <param name="soundBankFile">The name of the sound bank to retrieve the sound from</param>
        #endregion
        public static Cue PlaySound(String soundName, string soundBankFile)
        {
            Cue soundCue = null;

            try
            {
                soundCue = mXnaSoundBanks[soundBankFile].GetCue(soundName);

                //NM: Wait for the sound to be preapred if necessary
                //I have timed this and the time to prepare is negligible and it ensures the song will actually play and not just fail silently.
                while (!soundCue.IsPrepared)
                {
                    mXnaAudioEngine.Update();
                }

                soundCue.Play();
            }
            catch (IndexOutOfRangeException e)
            {
                throw new IndexOutOfRangeException(
                    "\"" + soundName + "\" is not a cue name in the specified sound bank.",
                    e);
            }
            catch (KeyNotFoundException e)
            {
                throw new KeyNotFoundException(
                    "The sound bank \"" + soundBankFile + "\" has not been loaded.  Did you forget to add the sound bank to the Audio Manager?",
                    e);
            }

            return soundCue;
        }
#endif


#if !WINDOWS_PHONE && !MONOGAME
        internal static Cue GetCue(String soundName, string soundBankFile)
        {
            try
            {
                if (!mXnaSoundBanks.ContainsKey(soundBankFile))
                    throw new KeyNotFoundException(
                    "\"" + soundName + "\" is not a cue name in the specified sound bank.");

                return mXnaSoundBanks[soundBankFile].GetCue(soundName);
            }
            catch (IndexOutOfRangeException e)
            {
                throw new IndexOutOfRangeException(
                    "\"" + soundName + "\" is not a cue name in the specified sound bank.",
                    e);
            }
        }
#endif


        #region Xml Docs
        /// <summary>
        /// Initializes the audio manager with an XACT audio project
        /// </summary>
        /// <param name="settingsFile">The settings file for the audio project (xgs)</param>
        /// <param name="waveBankFile">The wave bank file for the audio project (xwb)</param>
        /// <param name="soundBankFile">The sound bank file for the audio project (xsb)</param>
        #endregion
        public static void Initialize(
            String settingsFile, String waveBankFile, String soundBankFile)
        {
            mSettingsFile = settingsFile;

            mDefaultWaveBank = waveBankFile;
            mDefaultSoundBank = soundBankFile;

            FlatRedBall.IO.FileManager.ThrowExceptionIfFileDoesntExist(settingsFile);

#if !WINDOWS_PHONE && !MONOGAME
            mXnaAudioEngine = new AudioEngine(
                FileManager.Standardize(settingsFile));

            AddWaveBank(mDefaultWaveBank);
            AddSoundBank(mDefaultSoundBank);

            GlobalVariables = new SoundGlobalVariableCollection(mXnaAudioEngine);
#endif

            FlatRedBallServices.Suspending += new EventHandler(OnSuspending);
            FlatRedBallServices.Unsuspending += new EventHandler(OnUnsuspending);

            mIsInitialized = true;
        }

    }
}
