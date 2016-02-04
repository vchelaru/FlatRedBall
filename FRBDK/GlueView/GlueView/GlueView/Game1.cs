using System;
using System.Collections.Generic;
using System.Linq;
using InteractiveInterface;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using FlatRedBall;
using FlatRedBall.Glue;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using FlatRedBall.IO;
using System.Runtime.Remoting.Channels.Http;
using EditorObjects;
using FlatRedBall.Graphics;
using GlueView.Gui;
using FlatRedBall.Glue.Elements;
using System.Windows.Forms;
using RemotingHelper;
using FlatRedBall.Glue.Reflection;
using System.Reflection;
using FlatRedBall.Instructions.Reflection;

namespace GlueView
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        ToolForm mToolForm;

        FormMethods mFormMethods;
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferHeight = 600;
        }


        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            FlatRedBallServices.InitializeFlatRedBall(this, graphics);

            this.TargetElapsedTime = TimeSpan.FromSeconds( 1 / 30.0 );

            mToolForm = new ToolForm();
            mToolForm.Owner = (Form)(Form.FromHandle(FlatRedBallServices.WindowHandle));
            Plugin.PluginManager.Initialize();

            IsMouseVisible = true;

            IsFixedTimeStep = false;

            GuiData.Initialize();

            RegisterAdditionalAssemblies();

            AvailableAssetTypes.Self.Initialize(FileManager.RelativeDirectory);

            #region Set up resizing

            mFormMethods = new FormMethods(0, 0, 0, 0);

            #endregion

            SpriteManager.Camera.BackgroundColor = Color.Gray;
            FlatRedBall.Math.Geometry.Polygon.TolerateEmptyPolygons = true;
            ExposedVariableManager.Initialize();

            try
            {
                RemotingServer.SetupPort(8686);
                RemotingServer.SetupInterface<SelectionInterface>();
                RemotingServer.SetupInterface<RegisterInterface>();
            }
            catch(Exception e)
            {
                System.Windows.Forms.MessageBox.Show("GlueView could not start, please make sure GlueView is not already running on your computer \n \n Exception: \n"+e.ToString());
                FlatRedBall.FlatRedBallServices.Game.Exit(); 
            }

            mToolForm.Show();
            
            GluxManager.ContentManagerName = "GlueView";


            EditorLogic.Initialize();

            CommandLineManager.Self.ProcessCommandLineArgs(this.mToolForm);

            Form form = Form.FromHandle(FlatRedBallServices.WindowHandle) as Form;
            form.Resize += HandleResize;
            
            base.Initialize();
        }

        private void RegisterAdditionalAssemblies()
        {
            var assembly = Assembly.GetAssembly(typeof(PlatformSpecificType));
            PropertyValuePair.AdditionalAssemblies.Add(assembly);
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
            // I don't think GView needs a ScreenManager activity does it?
            //Screens.ScreenManager.Activity();
            EditorLogic.Activity();

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
