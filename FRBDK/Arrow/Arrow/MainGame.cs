using System.Windows.Forms;
using FlatRedBall;
using FlatRedBall.Arrow.GlueView;
using FlatRedBall.Glue;
using FlatRedBall.Math;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Arrow;

namespace FlatRedBallWpf
{
    public class MainGame : FlatRedBallGameBase
    {
        public MainWindow MainWindow { get; set; }
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

            FlatRedBall.Gui.GuiManager.Cursor.CustomIsActive = ()=> MainWindow.IsActive;

            HighlightManager.Self.Initialize();
            SelectionManager.Self.Initialize();
            EditingManager.Self.Initialize(mFrbControl);

            // Set up the camera you wish to use and clear the view            
            SpriteManager.Camera.DestinationRectangle = new Rectangle(0, 0, RenderWidth, RenderHeight);
            SpriteManager.Camera.UsePixelCoordinates();
            SpriteManager.Camera.BackgroundColor = Color.Gray;


            FlatRedBall.Input.Mouse.ModifyMouseState += HandleModifyMouseState;
        }

        private void HandleModifyMouseState(ref Microsoft.Xna.Framework.Input.MouseState mouseState)
        {
            var point = Control.MousePosition;

            var screen = mFrbControl.PointFromScreen(new System.Windows.Point(point.X, point.Y));
            var newMouseState = new Microsoft.Xna.Framework.Input.MouseState(
                MathFunctions.RoundToInt(screen.X),
                MathFunctions.RoundToInt(screen.Y),
                mouseState.ScrollWheelValue,
                mouseState.LeftButton,
                mouseState.MiddleButton,
                mouseState.RightButton,
                mouseState.XButton1,
                mouseState.XButton2);
            mouseState = newMouseState;
        }

        protected override void Update(GameTime gameTime)
        {


            FlatRedBallServices.Update(gameTime);
            GluxManager.Update();
            HighlightManager.Self.Activity();
            EditingManager.Self.Activity();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            if (IsRenderingPaused)
            {
                return;
            }
            
            FlatRedBallServices.Draw();

            base.Draw(gameTime);
        }
    }
}