using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FlatRedBall;
using FlatRedBall.Screens;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Content.Particle;
using FlatRedBall.IO;

namespace ParticleEditorPlugin
{
    public class ParticlePreviewer : Microsoft.Xna.Framework.Game
    {
        #region Properties
        /// <summary>
        /// Static instance accessor: singleton pattern
        /// </summary>
        public static ParticlePreviewer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ParticlePreviewer();
                }
                return instance;
            }
        }

        /// <summary>
        /// Whether or not "TryRun" has been called.
        /// </summary>
        public bool Running
        {
            get
            {
                return running;
            }
        }
        #endregion

        #region Fields
        /// <summary>
        /// The content manager name to use for this "game"
        /// </summary>
        public const string ContentManagerName = "PreviewerContentManager";

        private static ParticlePreviewer instance;
        private Emitter previewEmitter;
        private GraphicsDeviceManager graphics;
        private bool running = false;
        private SpriteList particles = new SpriteList();
        #endregion

        /// <summary>
        /// Private constructor, only one of these should be launchable
        /// </summary>
        private ParticlePreviewer()
        {
            graphics = new GraphicsDeviceManager(this);
        }

        /// <summary>
        /// Attempts to run the game instance or wraps the exception with an additional message on failure.
        /// </summary>
        public void TryRun()
        {
            if (!Running)
            {
                try
                {
                    running = true;
                    this.Run();
                }
                catch (Exception ex)
                {
                    running = false;
                    throw new Exception("Failed to run particle previewer: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Disposes of the currently displaying 
        /// emitter and creates a new one from the supplied 
        /// save object. Requires an absolute path so it knows where
        /// to load sprites and other paths from.
        /// </summary>
        /// <param name="emitterSave">The emitter save to inflate into an emitter runtime object.</param>
        /// <param name="absolutePath">The base path to use when loading sprite textures or other resources.</param>
        public void DisplayEmitter(EmitterSave emitterSave, string absolutePath)
        {
            if (previewEmitter != null)
            {
                SpriteManager.RemoveEmitter(previewEmitter);
            }

            FileManager.RelativeDirectory = absolutePath;
            previewEmitter = emitterSave.ToEmitter(ContentManagerName);
            SpriteManager.AddEmitter(previewEmitter);
        }

        /// <summary>
        /// Performs FlatRedBall initialization tasks.
        /// </summary>
        protected override void Initialize()
        {
            FlatRedBallServices.InitializeFlatRedBall(this, graphics);
            FlatRedBallServices.GraphicsOptions.SetResolution(640, 480);
            FlatRedBall.Debugging.Debugger.NumberOfLinesInCommandLine = 1;


            this.IsMouseVisible = true;
            Camera.Main.UsePixelCoordinates(false, 640, 480);
            Camera.Main.BackgroundColor = Color.CornflowerBlue;

            base.Initialize();
        }

        /// <summary>
        /// Updates the game each cycle
        /// </summary>
        /// <param name="gameTime">Container for information about time elapsed since last update.</param>
        protected override void Update(GameTime gameTime)
        {
            FlatRedBallServices.Update(gameTime);
            FlatRedBall.Screens.ScreenManager.Activity();

            if (previewEmitter != null)
            {
                previewEmitter.TimedEmit(particles);
                FlatRedBall.Debugging.Debugger.CommandLineWrite("Particles: " + particles.Count);
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Performs FlatRedBall drawing services.
        /// </summary>
        /// <param name="gameTime">Container for information about time elapsed since last update.</param>
        protected override void Draw(GameTime gameTime)
        {
            FlatRedBallServices.Draw();
            base.Draw(gameTime);
        }

        /// <summary>
        /// Updates the previewer state that the game is not running.
        /// </summary>
        /// <param name="sender">The object that sent this request</param>
        /// <param name="e">Args that accompany this request.</param>
        protected override void OnExiting(object sender, EventArgs args)
        {
            running = false;
            base.OnExiting(sender, args);
        }
    }
}
