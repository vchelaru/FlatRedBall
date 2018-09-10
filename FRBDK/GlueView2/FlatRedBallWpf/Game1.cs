using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Input;
using FlatRedBall.Math;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBallWpf
{
    public class Game1 : FlatRedBallGameBase
    {


        public Game1(FlatRedBallControl frbControl)
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
        }

        protected override void Update(GameTime gameTime)
        {
            // Update FRB.
            FlatRedBallServices.Update(gameTime);

            FlatRedBall.Screens.ScreenManager.Activity();

            // This needs to be called. Do not remove it.
            base.Update(gameTime);



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