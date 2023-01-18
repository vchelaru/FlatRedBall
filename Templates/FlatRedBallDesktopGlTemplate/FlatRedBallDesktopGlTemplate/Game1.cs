using System;
using System.Collections.Generic;
using System.Reflection;

using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Screens;
using Microsoft.Xna.Framework;

using System.Linq;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FlatRedBallDesktopGlTemplate
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

#if WINDOWS_PHONE || ANDROID || IOS

            // Frame rate is 30 fps by default for Windows Phone,
            // so let's keep that for other phones too
            TargetElapsedTime = TimeSpan.FromTicks(333333);
            graphics.IsFullScreen = true;
#elif WINDOWS || DESKTOP_GL
            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;
#endif


#if WINDOWS_8
            FlatRedBall.Instructions.Reflection.PropertyValuePair.TopLevelAssembly = 
            this.GetType().GetTypeInfo().Assembly;
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
    }
}
