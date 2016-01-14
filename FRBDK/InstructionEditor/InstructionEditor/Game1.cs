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
using System.Windows.Forms;
using FlatRedBall.Graphics;
using EditorObjects;

namespace InstructionEditor
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;

        FormMethods mFormMethods;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
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
            FlatRedBall.Graphics.Renderer.UseRenderTargets = true;
            SpriteManager.Camera.FarClipPlane = 20000;

            // Since the application will be using the FlatRedBall GUI the
            // mouse needs to be visible and the GUI needs to be enabled.
            IsMouseVisible = true;
            GuiManager.IsUIEnabled = true;

            this.Window.AllowUserResizing = true;

            mFormMethods = new FormMethods();

            FlatRedBallServices.GraphicsOptions.BackgroundColor = Color.Gray;

            base.Initialize();
        }


        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            Form myForm = (Form)Form.FromHandle(this.Window.Handle);

            if (myForm.Focused)
            {

                FlatRedBallServices.Update(gameTime);

                EditorData.Update();
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            Form myForm = (Form)Form.FromHandle(this.Window.Handle);

            if (myForm.Focused)
            {
                FlatRedBallServices.Draw();

                base.Draw(gameTime);
            }
        }
    }
}
