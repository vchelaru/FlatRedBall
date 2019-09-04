using FlatRedBall;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameStandard
{
    public class GameBase : Microsoft.Xna.Framework.Game
    {
        protected GraphicsDeviceManager graphics;

        public GameBase() : base()
        {
            graphics = new GraphicsDeviceManager(this);
        }

        protected override void Initialize()
        {
            FlatRedBallServices.InitializeFlatRedBall(this, graphics);
            //ScreenManager.Start(typeof(SomeScreen).FullName);
            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            FlatRedBallServices.Update(gameTime);

            FlatRedBall.Screens.ScreenManager.Activity();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            FlatRedBallServices.Draw();

            base.Draw(gameTime);
        }
    }
}
