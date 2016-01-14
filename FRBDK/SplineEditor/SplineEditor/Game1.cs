using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using FlatRedBall;
using FlatRedBall.Content.AnimationChain;

using FlatRedBall.Graphics.Animation;
using FlatRedBall.Input;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Gui;
using FlatRedBall.Graphics;
using EditorObjects;
using System.Windows.Forms;

namespace ToolTemplate
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        //Text mDebugText;

        string[] mCommandLineArguments;
        public Game1(string[] commandLineArguments)
        {
            graphics = new GraphicsDeviceManager(this);

            mCommandLineArguments = commandLineArguments;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            FlatRedBallServices.InitializeFlatRedBall(this, this.graphics);

            bool runAsFastAsPossible = false;
            if (runAsFastAsPossible)
            {
                this.IsFixedTimeStep = false;
                graphics.SynchronizeWithVerticalRetrace = false;

            }
            else
            {
                this.TargetElapsedTime = TimeSpan.FromSeconds(.05f);
            }

            Form form = Form.FromHandle(FlatRedBallServices.WindowHandle) as Form;
            form.Resize += HandleResize;


            // Since the application will be using the FlatRedBall GUI the
            // mouse needs to be visible and the GUI needs to be enabled.
            IsMouseVisible = true;

            EditorData.Initialize();

            EditorData.ProcessCommandLineArguments(mCommandLineArguments);

            base.Initialize();

        }

        private void HandleResize(object sender, EventArgs e)
        {
            Form form = Form.FromHandle(FlatRedBallServices.WindowHandle) as Form;
            if (form.WindowState == FormWindowState.Minimized)
            {
                FlatRedBallServices.SuspendEngine();
                // Do some stuff
            }
            else
            {
                FlatRedBallServices.UnsuspendEngine();
            }
        }


        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            
            FlatRedBallServices.Update(gameTime);

            EditorData.Update();
            
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            FlatRedBallServices.Draw();
            base.Draw(gameTime);
        }
    }
}
