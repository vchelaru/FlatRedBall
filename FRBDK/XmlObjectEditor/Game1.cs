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

using System.IO;
using System.Windows.Forms;

using FlatRedBall;

using FlatRedBall.Gui;
using FlatRedBall.IO;

namespace XmlObjectEditor
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        public Game1()
        {
            this.Window.AllowUserResizing = true;
            this.Window.ClientSizeChanged += new EventHandler(Window_ClientSizeChanged);

            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;

            System.Windows.Forms.Form gameForm = (Form)Form.FromHandle(this.Window.Handle);

            gameForm.AllowDrop = true;
            gameForm.DragEnter += new DragEventHandler(gameForm_DragEnter);
            gameForm.DragDrop += new DragEventHandler(gameForm_DragDrop);
        }

        void gameForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        void gameForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (string filename in files)
            {
                if (File.Exists(filename))
                {
                    LoadFile(filename);
                }
            }
        }

        void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            // update the camera
            SpriteManager.Camera.SetSplitScreenViewport(Camera.SplitScreenViewport.FullScreen);
        }

        void LoadFile(string filename)
        {
            string extension = FileManager.GetExtension(filename);

            // model
            if (extension.Equals("x") || extension.Equals("X"))
            {
                // load whatever
            }
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            IsMouseVisible = true;
            GuiManager.IsUIEnabled = true;


            //Renderer.UseRenderTargets = false;

            // Initialize FRB
            FlatRedBallServices.InitializeFlatRedBall(this, graphics);

            SpriteManager.Camera.FarClipPlane = 12000;

            // Add window resize event
            //this.Window.ClientSizeChanged += new EventHandler(Window_ClientSizeChanged);

            // Initialize Editor Data
            EditorData.Initialize();
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }


        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Update FRB
            FlatRedBallServices.Update(gameTime);

            // Update Editor Data
            EditorData.Update();


            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // Draw FRB
            FlatRedBallServices.Draw();

            base.Draw(gameTime);
        }
    }
}
