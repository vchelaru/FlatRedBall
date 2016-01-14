using System;
using InteractiveInterface;
using LevelEditor.Gui;
using LevelEditor.Screens;
using Microsoft.Xna.Framework;
using FlatRedBall;
using FlatRedBall.Glue;
using FlatRedBall.IO;
using FlatRedBall.Graphics;
using FlatRedBall.Glue.Elements;
using RemotingHelper;

namespace LevelEditor
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Game
    {
        readonly GraphicsDeviceManager _graphics;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            FlatRedBallServices.InitializeFlatRedBall(this, _graphics);

            IsMouseVisible = true;

            GuiData.Initialize();

            AvailableAssetTypes.Initialize(FileManager.RelativeDirectory + "Content/ContentTypes.csv");

            #region Set up resizing

            Renderer.UseRenderTargets = false;

            #endregion

            try
            {
                RemotingServer.SetupPort(9426);
                RemotingServer.SetupInterface<SelectionInterface>();
                RemotingServer.SetupInterface<RegisterInterface>();
            }
            catch(Exception e)
            {
                System.Windows.Forms.MessageBox.Show(@"Level Editor could not start, please make sure Level Editor is not already running on your computer 
 
 Exception: 
"+e);
                FlatRedBallServices.Game.Exit(); 
            }

            
            GluxManager.ContentManagerName = "LevelEditor";


            EditorLogic.Initialize();
            
            
            base.Initialize();
        }


        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            FlatRedBallServices.Update(gameTime);
            ScreenManager.Activity();

            EditorLogic.Activity();

            // TODO: Add your update logic here
            GluxManager.Update(); 
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
