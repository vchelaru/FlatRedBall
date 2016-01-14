using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Input;
using FlatRedBall.Math;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PolygonEditor
{
    public class MainGame : FlatRedBallGameBase
    {
        private Texture2D _redBallTexture;
        private Sprite _redBallSprite;

        public MainGame(FlatRedBallControl frbControl)
            : base(frbControl)
        {
        }

        /// <summary>
        /// This method is where you'll set up your camera, create sprites, etc.
        /// </summary>
        protected override void Initialize()
        {
            // This needs to be called first. Do not remove it.
            base.Initialize();

            // Set up the camera you wish to use and clear the view            
            SpriteManager.Camera.DestinationRectangle = new Rectangle(0, 0, RenderWidth, RenderHeight);
            SpriteManager.Camera.UsePixelCoordinates();

            // Load the texture
            _redBallTexture = FlatRedBallServices.Load<Texture2D>(@"Content\redball.bmp");

            // Add a basic shape to make sure it is working.
            _redBallSprite = SpriteManager.AddSprite(_redBallTexture);
            _redBallSprite.PixelSize = 1;
            _redBallSprite.RotationZVelocity = 1f;
        }

        protected override void Update(GameTime gameTime)
        {
            // Update FRB.
            FlatRedBallServices.Update(gameTime);

            // This needs to be called. Do not remove it.
            base.Update(gameTime);

            _redBallSprite.X = FlatRedBall.Gui.GuiManager.Cursor.WorldXAt(0);
            _redBallSprite.Y = FlatRedBall.Gui.GuiManager.Cursor.WorldYAt(0);

        }

        protected override void Draw(GameTime gameTime)
        {
            if (IsRenderingPaused)
            {
                return;
            }
            
            // Draw FRB.
            FlatRedBallServices.Draw();

            // This needs to be called. Do not remove it.
            base.Draw(gameTime);
        }
    }
}