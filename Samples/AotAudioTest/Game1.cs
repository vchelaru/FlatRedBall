using System;
using System.IO;
using FlatRedBall;
using FlatRedBall.Audio;
using FlatRedBall.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;

namespace AotAudioTest
{
    /// <summary>
    /// Smallest possible FlatRedBall game for checking audio, including audio following the
    /// Windows default output device. Space plays a blip, M toggles a looping song. Every audio
    /// call is wrapped and logged to aotaudiotest.log next to the exe, so a failure under
    /// Native AOT shows up there instead of vanishing.
    /// </summary>
    public class Game1 : Game
    {
        const string Controls = "AotAudioTest - Space: sound effect, M: song on/off";

        readonly GraphicsDeviceManager graphics;
        readonly string logPath = Path.Combine(AppContext.BaseDirectory, "aotaudiotest.log");
        readonly bool autoTest;

        SoundEffect beep;
        WavSong song;
        double elapsedSeconds;
        int autoTestStep;

        public Game1(string[] args)
        {
            // --autotest presses Space and M on a timer and exits, for checking a build without a human.
            autoTest = Array.IndexOf(args, "--autotest") >= 0;

            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.PreferredBackBufferWidth = 640;
            graphics.PreferredBackBufferHeight = 240;
            IsMouseVisible = true;
            Window.Title = Controls;

            File.WriteAllText(logPath, "");
            Log("ctor, dynamic code supported: " + System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported);
        }

        protected override void Initialize()
        {
            try
            {
                FlatRedBallServices.InitializeFlatRedBall(this, graphics);
                Log("InitializeFlatRedBall ok");

                var contentFolder = Path.Combine(AppContext.BaseDirectory, "Content");
                using (var stream = File.OpenRead(Path.Combine(contentFolder, "beep.wav")))
                {
                    beep = SoundEffect.FromStream(stream);
                }
                using (var stream = File.OpenRead(Path.Combine(contentFolder, "song.wav")))
                {
                    song = new WavSong("song", SoundEffect.FromStream(stream));
                }
                Log("wav files loaded");
            }
            catch (Exception e)
            {
                Log("Initialize failed: " + e);
                throw;
            }

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            FlatRedBallServices.Update(gameTime);
            elapsedSeconds += gameTime.ElapsedGameTime.TotalSeconds;

            var keyboard = InputManager.Keyboard;
            bool spacePushed = keyboard.KeyPushed(Keys.Space);
            bool mPushed = keyboard.KeyPushed(Keys.M);

            if (autoTest)
            {
                if (autoTestStep == 0 && elapsedSeconds > 1) { spacePushed = true; autoTestStep++; }
                else if (autoTestStep == 1 && elapsedSeconds > 2) { mPushed = true; autoTestStep++; }
                else if (autoTestStep == 2 && elapsedSeconds > 4) { mPushed = true; autoTestStep++; }
                else if (autoTestStep == 3 && elapsedSeconds > 5) { Log("autotest finished"); Exit(); }
            }

            if (spacePushed)
            {
                Run("play sound effect", () => AudioManager.Play(beep));
            }

            if (mPushed)
            {
                Run("toggle song", () =>
                {
                    // Decide from what OpenAL is really doing, not AudioManager.CurrentlyPlayingISong:
                    // after a device disconnect OpenAL stops the source but the manager still thinks the song plays.
                    if (song.IsPlaying)
                    {
                        AudioManager.StopSong();
                    }
                    else
                    {
                        AudioManager.StopSong(); // clears stale manager state; harmless when nothing plays
                        AudioManager.PlaySong(song, forceRestart: true, isSongGlobalContent: false);
                    }
                });
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            FlatRedBallServices.Draw();

            base.Draw(gameTime);
        }

        void Run(string what, Action action)
        {
            try
            {
                action();
                Log(what + ": ok (song playing: " + (AudioManager.CurrentlyPlayingISong != null) + ")");
            }
            catch (Exception e)
            {
                Log(what + " FAILED: " + e);
            }
        }

        void Log(string message)
        {
            try
            {
                File.AppendAllText(logPath, DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine);
            }
            catch
            {
                // Logging must never take the game down.
            }
        }
    }
}
