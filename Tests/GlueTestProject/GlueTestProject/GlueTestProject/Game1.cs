using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Utilities;

using GlueTestProject.Screens;
using FlatRedBall.Localization;
using System.Reflection;
using TMXGlueLib.DataTypes;

namespace GlueTestProject
{
    public partial class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
			
#if !MONOGAME
            graphics.PreferredBackBufferHeight = 600;
#endif

#if WINDOWS_8
            FlatRedBall.Instructions.Reflection.PropertyValuePair.TopLevelAssembly = 
                this.GetType().GetTypeInfo().Assembly;
#endif
        }

        protected override void Initialize()
        {
            FlatRedBallServices.InitializeFlatRedBall(this, graphics);
			CameraSetup.SetupCamera(SpriteManager.Camera, graphics);
            CustomPreGlobalContentInitialize();
			GlobalContent.Initialize();

            LocalizationManager.CurrentLanguage = 1;

            CustomInitialize();

            ReducedTileMapInfo.FastCreateFromTmx = true;

			FlatRedBall.Screens.ScreenManager.Start(typeof(GlueTestProject.Screens.FirstScreen));

            base.Initialize();
        }


        protected override void Update(GameTime gameTime)
        {
            
#if UNIT_TESTS
            ((Form)Form.FromHandle(Window.Handle)).WindowState = FormWindowState.Minimized;
#endif
            FlatRedBallServices.Update(gameTime);

            CustomActivity();

            FlatRedBall.Screens.ScreenManager.Activity();

            string debugText = "";

            if (FlatRedBall.Screens.ScreenManager.CurrentScreen != null)
            {
                debugText = FlatRedBall.Screens.ScreenManager.CurrentScreen + " " + 
                    FlatRedBall.Screens.ScreenManager.CurrentScreen.PauseAdjustedSecondsSince(0).ToString("0.00");
            }

            if (GlobalContent.IsInitialized == false)
            {
                debugText += "\nGlobal Content still initializing...";
            }
            else
            {
                debugText += "\nGlobal Content done initializing";
            }

            FlatRedBall.Debugging.Debugger.TextCorner = FlatRedBall.Debugging.Debugger.Corner.BottomRight;
            FlatRedBall.Debugging.Debugger.Write(debugText);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            FlatRedBallServices.Draw();

            base.Draw(gameTime);
        }
    }
}
