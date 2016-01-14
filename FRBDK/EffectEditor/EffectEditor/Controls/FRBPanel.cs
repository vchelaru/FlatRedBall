using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using FlatRedBall;
using FlatRedBall.Graphics.Model;

namespace EffectEditor.Controls
{
    public partial class FRBPanel : GraphicsDeviceControl
    {
        #region Fields

        Stopwatch mTimer;
        TimeSpan mLastStop;

        #endregion

        #region Methods

        protected override void Initialize()
        {
            // Initialize the timer
            mTimer = Stopwatch.StartNew();
            mLastStop = mTimer.Elapsed;

            // Intialize FlatRedBall
            FlatRedBallServices.InitializeFlatRedBall(this.graphicsDeviceService, this.Handle);

            Application.Idle += delegate { Invalidate(); };
            this.Resize += new EventHandler(FRBPanel_Resize);
        }

        void FRBPanel_Resize(object sender, EventArgs e)
        {
            SpriteManager.Camera.DestinationRectangle =
                new Microsoft.Xna.Framework.Rectangle(
                    SpriteManager.Camera.DestinationRectangle.X,
                    SpriteManager.Camera.DestinationRectangle.Y,
                    this.Width,
                    this.Height);
        }

        #region Standard Methods

        protected virtual void Update(GameTime gameTime)
        {
            // Update FRB
            FlatRedBallServices.Update(gameTime);
        }

        protected override void Draw()
        {
            // Update timing
            TimeSpan elapsed = new TimeSpan(mTimer.Elapsed.Ticks - mLastStop.Ticks);
            mLastStop = mTimer.Elapsed;

            // Update Control
            Update(new GameTime(
                mTimer.Elapsed, elapsed, mTimer.Elapsed, elapsed));

            // Draw FRB
            FlatRedBallServices.Draw();
        }

        #endregion

        #endregion
    }
}
