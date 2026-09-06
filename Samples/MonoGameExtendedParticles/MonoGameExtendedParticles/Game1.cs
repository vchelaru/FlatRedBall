using FlatRedBall;
using FlatRedBall.Screens;
using Microsoft.Xna.Framework;

namespace MonoGameExtendedParticles
{
    public class Game1 : Game
    {
        readonly GraphicsDeviceManager graphics;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            FlatRedBallServices.InitializeFlatRedBall(this, graphics);
            Camera.Main.UsePixelCoordinates();
            ScreenManager.Start(typeof(Screens.GameScreen));

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            FlatRedBallServices.Update(gameTime);
            ScreenManager.Activity();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            FlatRedBallServices.Draw();

            base.Draw(gameTime);
        }
    }
}
