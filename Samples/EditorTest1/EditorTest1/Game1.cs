using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Screens;
using Microsoft.Xna.Framework;

using System.Linq;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EditorTest1
{
    public partial class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;

        
        partial void GeneratedInitializeEarly();
        partial void GeneratedInitialize();
        partial void GeneratedUpdate(Microsoft.Xna.Framework.GameTime gameTime);
        partial void GeneratedDrawEarly(Microsoft.Xna.Framework.GameTime gameTime);
        partial void GeneratedDraw(Microsoft.Xna.Framework.GameTime gameTime);

        public Game1() : base()
        {
            graphics = new GraphicsDeviceManager(this);
            // HiDef is required for web, but proably for all other platforms too, so let's set it 
            // outside of any #if's
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
#if  ANDROID || IOS
            graphics.IsFullScreen = true;
#elif WINDOWS || DESKTOP_GL
            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;
#endif
        }

        protected override void Initialize()
        {
            #if IOS
            var bounds = UIKit.UIScreen.MainScreen.Bounds;
            var nativeScale = UIKit.UIScreen.MainScreen.Scale;
            var screenWidth = (int)(bounds.Width * nativeScale);
            var screenHeight = (int)(bounds.Height * nativeScale);
            graphics.PreferredBackBufferWidth = screenWidth;
            graphics.PreferredBackBufferHeight = screenHeight;
            #endif
        
            GeneratedInitializeEarly();

            FlatRedBallServices.InitializeFlatRedBall(this, graphics);

            // Set by LiveGameProcess (GlueUnitTests) before launching this exe repeatedly to drive live-edit
            // tests. The window is still hidden here - MonoGame's SdlGameWindow.CreateWindow (invoked as
            // part of InitializeFlatRedBall's resolution setup, above) destroys and recreates the SDL
            // window centered but hidden; nothing calls Sdl.Window.Show until Game.Run()'s loop actually
            // starts, after Initialize()/LoadContent() finish - so repositioning it here moves it off the
            // combined virtual desktop before it's ever painted. Setting Window.Position any earlier (e.g.
            // in Program.cs before Run()) does not work: that CreateWindow() call above would destroy the
            // already-positioned window and recreate a fresh centered one, discarding it.
            if (Environment.GetEnvironmentVariable("FRB_LIVE_GAME_TEST_OFFSCREEN") == "1")
            {
                var offscreenX = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN)
                    + NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
                Window.Position = new Point(offscreenX, Window.Position.Y);
            }

            GeneratedInitialize();

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            FlatRedBallServices.Update(gameTime);

            FlatRedBall.Screens.ScreenManager.Activity();

            GeneratedUpdate(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GeneratedDrawEarly(gameTime);

            FlatRedBallServices.Draw();

            GeneratedDraw(gameTime);

            base.Draw(gameTime);
        }

        static class NativeMethods
        {
            public const int SM_XVIRTUALSCREEN = 76;
            public const int SM_CXVIRTUALSCREEN = 78;

            [DllImport("user32.dll")]
            public static extern int GetSystemMetrics(int nIndex);
        }
    }
}
