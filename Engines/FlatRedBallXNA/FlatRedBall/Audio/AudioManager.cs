using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using FlatRedBall.Math;
using Microsoft.Xna.Framework.Media;
using System.Reflection;
using FlatRedBall.IO;
using FlatRedBall.Instructions;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace FlatRedBall.Audio
{
    #region SoundEffectPlayingBehavior

    public enum SoundEffectPlayingBehavior
    {
        PlayAlways,
        OncePerFrame
    }

    #endregion

    public static partial class AudioManager
    {
        #region Fields / Properties

        private static List<SoundEffectPlayInfo> mSoundEffectPlayInfos = new List<SoundEffectPlayInfo>();

        private static List<string> mSoundsPlayedThisFrame = new List<string>();

        private static string mSettingsFile;


        private static bool mIsInitialized = false;
        private static Song mCurrentSong = null;
        private static Song mPreviousSong = null;
        private static bool mPreviousSongUsesGlobalContent = false;

        static Song mSongLastRequested;
        static string mLastSongNameRequested;

        static bool mAreSongsEnabled;
        static bool mAreSoundEffectsEnabled;

        static bool? mDoesUserWantCustomSoundtrack = null;

        /// <summary>
        /// The default volume for playing sounds, applied when calling Play(SoundEffect). Ranges between 0 and 1.
        /// </summary>
        public static float MasterSoundVolume
        {
            get;
            set;
        } = 1.0f;

        static SongPlaylist Playlist;

        static int playlistIndex = 0;
#if MONODROID
        //This is for a performance issue with SoundPool
        private static SoundEffect _droidLoop;
        private static DateTime _lastPlay = DateTime.Now;
#endif


        #endregion

        #region Properties

        public static bool IsCustomMusicPlaying
        {
            get
            {
#if SILVERLIGHT
                return false;
#else
                return Microsoft.Xna.Framework.Media.MediaPlayer.GameHasControl == false;
#endif
            }
        }

        public static SoundEffectPlayingBehavior SoundEffectPlayingBehavior
        {
            get;
            set;
        }

        public static bool IsInitialized
        {
            get { return mIsInitialized; }
        }

        /// <summary>
        /// Controls whether the Play function will produce any audio when called.
        /// This defaults to true, and it can be set to false in response to a setting
        /// in the game's options screen. Setting this to false will not stop any currently-playing
        /// SoundEffects as this is an XNA limitation - SoundEffect is fire-and-forget.
        /// </summary>
        public static bool AreSoundEffectsEnabled
        {
            get
            {
                return mAreSoundEffectsEnabled;
            }
            set
            {
                mAreSoundEffectsEnabled = value;
            }
        }

        public static bool AreSongsEnabled
        {
            get { return mAreSongsEnabled; }
            set
            {
                mAreSongsEnabled = value;

                if (CurrentlyPlayingSong != null && !mAreSongsEnabled)
                {
                    StopSong();
                }
            }
        }

        public static string SettingsFile
        {
            get { return mSettingsFile; }
        }

        /// <summary>
        ///  Represents the "active" song.  This may or may not be actually playing.
        ///  This may still be non-null when no song is playing if the code has stopped
        ///  music from playing.  The AudioManager remembers this to resume playing later.
        /// </summary>

        public static Song CurrentSong
        {
            get { return mCurrentSong; }
        }

        static bool mIsSongUsingGlobalContent;

        static Song mCurrentlyPlayingSongButUsePropertyPlease;
        /// <summary>
        /// Represents the song that is currently playing.  If no song is playing this is null.
        /// </summary>
        public static Song CurrentlyPlayingSong
        {
            get
            {
                return mCurrentlyPlayingSongButUsePropertyPlease;
            }
            private set
            {
                if (value != mCurrentlyPlayingSongButUsePropertyPlease)
                {
                    mCurrentlyPlayingSongButUsePropertyPlease = value;
                }
            }
        }

#if DEBUG
        /// <summary>
        /// Reports the total number of sound effects that have been played by the AudioManager since the start of the program's execution.
        /// This can be used to count sound effect plays as a rough form of profiling.
        /// </summary>
        public static int NumberOfSoundEffectPlays
        {
            get;
            set;
        }

#endif

        #endregion

        #region Event Handlers

        static void OnUnsuspending(object sender, EventArgs e)
        {
            if (mCurrentSong != null)
            {
                PlaySong(mCurrentSong, true, mIsSongUsingGlobalContent);
            }
        }

        static void OnSuspending(object sender, EventArgs e)
        {
            StopSong();
        }

        #endregion

        static AudioManager()
        {
            AreSoundEffectsEnabled = true;
            AreSongsEnabled = true;
#if !MONOGAME
            SoundListener = new PositionedSoundListener();
            PositionedSounds = new PositionedObjectList<PositionedSound>();
#endif
            Microsoft.Xna.Framework.Media.MediaPlayer.MediaStateChanged += HandleMediaStateChanged;


        }

        #region Song Methods

        /// <summary>
        /// Plays the current song.  PlaySong with an argument must
        /// be called before this can be called.  This can be used to
        /// resume music when the game is unpaused or if audio options are
        /// being turned on/off
        /// </summary>
        public static void PlaySong()
        {
            Playlist = null;
            PlaySong(mCurrentSong, false, mIsSongUsingGlobalContent);
        }

        /// <summary>
        /// Plays the argument song, optionally restarting it if it is already playing.
        /// </summary>
        /// <param name="toPlay">The song to play.</param>
        /// <param name="forceRestart">Whether the song should be restarted. If the toPlay parameter differs from the currently-playing song then it will 
        /// restart regardless of the forceRestart value. This value only matters when the currently-playing song is passed.</param>
        /// <param name="isSongGlobalContent">Whether the song uses a Global content manager. This is important if StopAndDisposeCurrentSongIfNameDiffers is called.
        /// StopAndDisposeCurrentSongIfNameDiffers is called by Glue, so the isSongGlobalContent param matters even if your code is not directly calling this function.</param>
        public static void PlaySong(Song toPlay, bool forceRestart, bool isSongGlobalContent)
        {
            Playlist = null;
            PlaySongInternal(toPlay, forceRestart, isSongGlobalContent);
        }

        public static void PlaySongs(SongPlaylist songPlaylist)
        {
            Playlist = songPlaylist;
#if !__IOS__
            MediaPlayer.IsRepeating = false;
#endif
            playlistIndex = 0;

            PlaySongFromPlaylistInternal();
        }

        private static void PlaySongFromPlaylistInternal()
        {
            if (playlistIndex >= Playlist.Songs.Length)
            {
                playlistIndex = 0;
            }

            if (playlistIndex < Playlist.Songs.Length)
            {
                PlaySongInternal(Playlist.Songs[playlistIndex], forceRestart: true, isSongGlobalContent: Playlist.AreSongsGlobalContent);
            }
        }

        private static void PlaySongInternal(Song toPlay, bool forceRestart, bool isSongGlobalContent)
        {
            bool shouldPlay = true;

            shouldPlay = IsCustomMusicPlaying == false || (mDoesUserWantCustomSoundtrack.HasValue && mDoesUserWantCustomSoundtrack.Value == false);

            if (toPlay.Name != mLastSongNameRequested || forceRestart ||
                CurrentlyPlayingSong == null)
            {
                mSongLastRequested = toPlay;
                mIsSongUsingGlobalContent = isSongGlobalContent;
                mLastSongNameRequested = toPlay.Name;
            }
            else
            {
                shouldPlay = false;
            }

            if (shouldPlay && AreSongsEnabled)
            {
                mCurrentSong = toPlay;

                CurrentlyPlayingSong = mCurrentSong;
                mIsSongUsingGlobalContent = isSongGlobalContent;



#if ANDROID
				try
				{
                	MediaPlayer.Play(toPlay);
				}
				// November 19, 2014
				// For some reason the
				// automated test project
				// would always crash when
				// trying to play a song on
				// a certain screen.  I was able
				// to avoid the crash by putting a
				// breakpoint and waiting a while before
				// continuing execution.  I suppose it means
				// that the song needs a little bit of time before
				// it is played.  So I just added a catch and put 
				catch(Java.IO.IOException e)
				{
					string message = e.Message;

					if(message.Contains("0x64"))
					{
						// This needs a second before it starts up
						int msToWait = 100;

						System.Threading.Thread.Sleep(msToWait);
						MediaPlayer.Play(toPlay);
					}
					else
					{
						throw e;
					}
				}
#else
                Microsoft.Xna.Framework.Media.MediaPlayer.Play(toPlay);

#endif
            }
        }

        public static void StopSong()
        {
            Microsoft.Xna.Framework.Media.MediaPlayer.Stop();

            CurrentlyPlayingSong = null;
        }

        public static bool StopAndDisposeCurrentSongIfNameDiffers(string nameToCompareAgainst)
        {
            bool wasDisposed = false;

            if (CurrentlyPlayingSong != null && nameToCompareAgainst != mLastSongNameRequested)
            {
                Song songToDispose = CurrentlyPlayingSong;
                StopSong();

                if (!mIsSongUsingGlobalContent)
                {
                    songToDispose.Dispose();
                }

                wasDisposed = true;
            }
            return wasDisposed;
        }


        public static void PlaySongThenResumeCurrent(Song toPlay, bool songUsesGlobalContent)
        {
            mPreviousSong = mCurrentSong;
            mPreviousSongUsesGlobalContent = mIsSongUsingGlobalContent;

            mCurrentSong = toPlay;
            mIsSongUsingGlobalContent = songUsesGlobalContent;
            try
            {
                PlaySong(toPlay, false, songUsesGlobalContent);

#if UWP
                MethodInfo playMethod = typeof(AudioManager).GetMethod("PlaySong", new Type[1] { typeof(Song) });
#else
                MethodInfo playMethod = typeof(AudioManager).GetMethod("PlaySong", BindingFlags.Public | BindingFlags.Static, null, new Type[1] { typeof(Song) }, null);
#endif
                InstructionManager.Add(new StaticMethodInstruction(playMethod,
                                                                    new object[1] { mPreviousSong },
                                                                    TimeManager.CurrentTime + toPlay.Duration.TotalSeconds));
            }
            catch
            {
                // stupid DRM
            }
        }

        private static void HandleMediaStateChanged(object sender, EventArgs e)
        {
            if (CurrentlyPlayingSong != null && Microsoft.Xna.Framework.Media.MediaPlayer.State == MediaState.Stopped)
            {
                CurrentlyPlayingSong = null;

                var shouldMoveToNext =
                    Microsoft.Xna.Framework.Media.MediaPlayer.IsRepeating == false && Playlist != null;

                if (shouldMoveToNext)
                {
                    playlistIndex++;
                    PlaySongFromPlaylistInternal();
                }
            }
            // This can happen through  looping
            else if (Microsoft.Xna.Framework.Media.MediaPlayer.State == MediaState.Playing)
            {
                CurrentlyPlayingSong = Microsoft.Xna.Framework.Media.MediaPlayer.Queue.ActiveSong;
            }
        }

        #endregion

        #region Public Methods



        /// <summary>
        /// Returns whether the argument SoundEffect is playing. If true, then the SoundEffect is already
        /// playing. If false, the SoundEffect is not playing. This will only check SoundEffects which were 
        /// played through the AudioManager.Play method.
        /// </summary>
        /// <param name="soundEffect">The SoundEffect to test.</param>
        /// <returns>Whether the sound effect is playing.</returns>
        public static bool IsSoundEffectPlaying(SoundEffect soundEffect)
        {
            for (int i = 0; i < mSoundEffectPlayInfos.Count; i++)
            {
                if (mSoundEffectPlayInfos[i].SoundEffect == soundEffect)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Plays the argument sound effect using a default Volume, pitch, and pan.
        /// </summary>
        /// <param name="soundEffect"></param>
        public static void Play(SoundEffect soundEffect)
        {
            Play(soundEffect, MasterSoundVolume);
        }

        /// <summary>
        /// Plays the argument sound effect.
        /// </summary>
        /// <param name="soundEffect">The sound effect to play</param>
        /// <param name="volume">Volume, ranging from 0.0f (silence) to 1.0f (full volume). 1.0f is full volume</param>
        /// <param name="pitch">Pitch, ranging from -1.0f (one octave down) to 1.0f (one octave up). 0.0f means no change </param>
        /// <param name="pan">Volume, ranging from -1.0f (full left) to 1.0f (full right). 0.0f is centered </param>
        public static void Play(SoundEffect soundEffect, float volume, float pitch = 0, float pan = 0)
        {
#if DEBUG && !MONOGAME
            if (soundEffect.IsDisposed)
            {
                throw new ArgumentException("Argument SoundEffect is disposed");
            }
#endif

            if (AreSoundEffectsEnabled)
            {
                bool shouldPlay = SoundEffectPlayingBehavior == Audio.SoundEffectPlayingBehavior.PlayAlways ||
                    mSoundsPlayedThisFrame.Contains(soundEffect.Name) == false;

                if (shouldPlay)
                {
#if MONODROID
                    _lastPlay = DateTime.Now;
#endif


#if ANDROID && !DEBUG

					try
					{
						if (volume < 1 || pitch != 0.0f || pan != 0.0f)
					    {
						    soundEffect.Play(volume, pitch, pan);
					    }
						else
						{
							soundEffect.Play();
						}
					}
					catch
					{
					// Sept 28, 2015
					// Monogame 3.4 (and probably 3.5) does not support 
					// playing Sounds on ARM 64 devices. It crashes. We will
					// catch it in release in case someone releases a FRB game
					// and doesn't test it on these devices - better to be quiet
					// than to crash. In debug it will crash like normal (see below)
					}


#else
                    if (volume < 1 || pitch != 0.0f || pan != 0.0f)
                    {
                        soundEffect.Play(volume, pitch, pan);
                    }
                    else
                    {
                        soundEffect.Play();
                    }


#endif


#if DEBUG
                    NumberOfSoundEffectPlays++;
#endif
                    if (SoundEffectPlayingBehavior == Audio.SoundEffectPlayingBehavior.OncePerFrame)
                    {
                        mSoundsPlayedThisFrame.Add(soundEffect.Name);
                    }

                    SoundEffectPlayInfo sepi = new SoundEffectPlayInfo();
                    sepi.LastPlayTime = TimeManager.CurrentTime;
                    sepi.SoundEffect = soundEffect;
                    mSoundEffectPlayInfos.Add(sepi);
                }
            }
        }

        /// <summary>
        /// Checks if the argument sound effect if playing. If not, plays the sound effect.
        /// </summary>
        /// <param name="soundEffect">The sound effect to play.</param>
        public static void PlayIfNotPlaying(SoundEffect soundEffect)
        {
            if (!IsSoundEffectPlaying(soundEffect))
            {
                Play(soundEffect);
            }
        }
        public static int GetNumberOfTimesCurrentlyPlaying(SoundEffect soundEffect) =>
            mSoundEffectPlayInfos.Count(item => item.SoundEffect == soundEffect);
#endregion

        #region Manager methods


        internal static void UpdateDependencies()
        {
#if !MONOGAME
            SoundListener.UpdateDependencies(TimeManager.CurrentTime);
            SoundListener.UpdateAudio();

            for (int i = 0; i < PositionedSounds.Count; i++)
            {
                PositionedSounds[i].UpdateDependencies(TimeManager.CurrentTime);
                PositionedSounds[i].UpdateAudio();
            }

            if (mXnaAudioEngine != null) mXnaAudioEngine.Update();
#endif

            mSoundsPlayedThisFrame.Clear();


        }

        public static void Update()
        {
            for (int i = mSoundEffectPlayInfos.Count - 1; i > -1; i--)
            {
                SoundEffectPlayInfo sepi = mSoundEffectPlayInfos[i];
                if (TimeManager.SecondsSince(sepi.LastPlayTime + sepi.SoundEffect.Duration.TotalSeconds) > 0)
                {
                    mSoundEffectPlayInfos.RemoveAt(i);
                }

            }


            // TODO:  Execute instructions
#if !MONOGAME
            for (int i = 0; i < PositionedSounds.Count; i++)
            {
                PositionedSounds[i].TimedActivity(
                    TimeManager.SecondDifference,
                    TimeManager.SecondDifferenceSquaredDividedByTwo,
                    TimeManager.LastSecondDifference);
            }
#endif

            // TODO [msmith] Get this working again once we add the data into the asset folder.
            /* TODO MDS_TEMP
#if MONODROID
            if ((DateTime.Now - _lastPlay).Milliseconds > 200)
            {
                if (_droidLoop != null)
                {
                    _droidLoop.Play(0, 0, 0);
                    _lastPlay = DateTime.Now;
                }else
                {
                    _droidLoop = FlatRedBallServices.Load<SoundEffect>(@"performanceloop");
                }
            }
#endif
			*/
        }


#endregion

    }

    public class SongPlaylist
    {
        internal Song[] Songs;

        public bool AreSongsGlobalContent { get; private set; }

        public SongPlaylist(IEnumerable<Song> songs, bool areSongsGlobalContent)
        {
            Songs = songs.ToArray();
            AreSongsGlobalContent = areSongsGlobalContent;
        }
    }


}
