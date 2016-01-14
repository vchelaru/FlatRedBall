using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using FlatRedBall;

using FlatRedBall.Gui;

using FlatRedBall.Input;

using FlatRedBall.IO;

using FlatRedBall.Math;

namespace PlatformerSample
{
    public class GameWindow : Form
    {
        #region Fields

        static FlatRedBall.Math.Geometry.Point TopLeftPixel;

        Game1 mGameData;

        #endregion

        #region Events and Delegates

        protected override void OnResize(System.EventArgs e)
        {
            OnResize(false);
        }

        protected void OnResize(bool forceResize)
        {

            if (WindowState == FormWindowState.Minimized)
                return; // don't want to change the camera when we minimize the screen

            if (SpriteManager.IsInitialized)
            {
                FlatRedBallServices.ForceClientSizeUpdates();
            }
        }

        void GameForm_Activated(object sender, EventArgs e)
        {
            FlatRedBallServices.Update(null);

            if (FlatRedBallServices.IsInitialized)
            {
                GuiManager.Cursor.ResetCursor();
            }

        }

        public void ExitMessage(Window callingWindow)
        {
            Exit();
        }

        public void Exit()
        {
            SpriteManager.Exiting = true;
            System.Windows.Forms.Cursor.Show();
            this.Dispose();

        }
        #endregion

        #region Methods

        public GameWindow()
        {
            FileManager.RelativeDirectory = System.Windows.Forms.Application.StartupPath + "/";

            FlatRedBall.IO.FileManager.CurrentDirectory = FlatRedBall.IO.FileManager.StartupPath;

            this.ClientSize = new System.Drawing.Size(800, 600);
            this.MinimizeBox = true;

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;

            this.AllowDrop = true;

            this.Activated += new EventHandler(GameForm_Activated);

       
            FlatRedBallServices.InitializeFlatRedBall(this);


            //this.FormBorderStyle = FormBorderStyle.None;
            //this.TopMost = true;
            SpriteManager.Camera.FixAspectRatioYConstant();

            TopLeftPixel.X = -SpriteManager.Cameras[0].XEdge;
            TopLeftPixel.Y = SpriteManager.Cameras[0].YEdge;
            OnResize(true);


        }

        public void Run()
        {
            this.Show();
            this.Activate();

            mGameData = new Game1();
            mGameData.Initialize();


            while (this.IsDisposed == false && Created)
            {

                if (Form.ActiveForm != this)
                {
                    System.Threading.Thread.Sleep(500);
                    Application.DoEvents();
                    continue;
                }

                FrameUpdate();

                mGameData.Update();

                if (!Created)
                    break;


                Draw();

                Application.DoEvents();



            }

        }

        public virtual void FrameUpdate()
        {
            FlatRedBallServices.Update();
        }

        private void Draw()
        {
            FlatRedBallServices.Draw();
        }

        #endregion

    }
}
