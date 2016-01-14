using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using FlatRedBall;

using FlatRedBall.Gui;

using FlatRedBall.Input;

using FlatRedBall.IO;

using FlatRedBall.Math;
using EditorObjects.Data;
using FlatRedBall.Content.Gui;

namespace EditorObjects
{
    public class EditorWindow : Form
    {
        #region Fields

        public static EditorWindow LastInstance;

        static FlatRedBall.Math.Geometry.Point TopLeftPixel;

        RuntimeOptions mRuntimeOptions = new RuntimeOptions();

        // Increase this to throttle the framerate
        public static float MinimumFrameLength = 0;

        static double mLastSystemTime;

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
                //		SpriteManager.FindDevice();

                bool updateFieldOfView = forceResize ||
                    SpriteManager.Cameras[0].DestinationRectangle.Height != DisplayRectangle.Height;

                if (SpriteManager.Camera.Orthogonal == false)
                {
                    float ratioChange = DisplayRectangle.Height / (float)SpriteManager.Cameras[0].DestinationRectangle.Height;


                    SpriteManager.Cameras[0].DestinationRectangle = this.DisplayRectangle;

                    if (updateFieldOfView)
                    {
                        double yAt600 = Math.Sin(Math.PI / 8.0);
                        double xAt600 = Math.Cos(Math.PI / 8.0);
                        double desiredYAt600 = yAt600 * (double)DisplayRectangle.Height / 600.0;
                        float desiredAngle = (float)Math.Atan2(desiredYAt600, xAt600);
                        SpriteManager.Cameras[0].FieldOfView = 2 * desiredAngle;
                    }

                }
                else
                {
                    double unitPerPixel = SpriteManager.Camera.OrthogonalHeight / SpriteManager.Cameras[0].DestinationRectangle.Height;

                    SpriteManager.Camera.OrthogonalHeight = (float)(DisplayRectangle.Height * unitPerPixel);
                    SpriteManager.Camera.OrthogonalWidth = (float)(DisplayRectangle.Width * unitPerPixel);
                    SpriteManager.Cameras[0].DestinationRectangle = this.DisplayRectangle;

                    SpriteManager.Cameras[0].FixAspectRatioYConstant();
                    if (updateFieldOfView)
                    {
                        double yAt600 = Math.Sin(Math.PI / 8.0);
                        double xAt600 = Math.Cos(Math.PI / 8.0);
                        double desiredYAt600 = yAt600 * (double)DisplayRectangle.Height / 600.0;
                        float desiredAngle = (float)Math.Atan2(desiredYAt600, xAt600);
                        SpriteManager.Cameras[0].FieldOfView = 2*desiredAngle;
                    }
                }

                // Shifts are no longer needed - top left is 0,0
                //GuiManager.ShiftBy(
                //    -(float)(TopLeftPixel.X + SpriteManager.Cameras[0].XEdge),
                //    -(float)(SpriteManager.Cameras[0].YEdge - TopLeftPixel.Y),
                //    false // Don't shift SpriteFrame GUI
                //    );

                TopLeftPixel.X = -SpriteManager.Cameras[0].XEdge;
                TopLeftPixel.Y = SpriteManager.Cameras[0].YEdge;

                //GuiManager.RefreshTextSize();

            }
        }

        void GameForm_Activated(object sender, EventArgs e)
        {
            FlatRedBallServices.Update(null);
            GuiManager.Cursor.ResetCursor();

        }

        private void GameForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            try
            {
                EditorObjects.Gui.LayoutManager.SaveWindowLayout();
            }
            catch(Exception saveException)
            {

                System.Windows.Forms.MessageBox.Show("Error saving the screen layout. " +
                    "A file has been saved containing error information.  You can help solve this problem " +
                    "by posting the error contained in this file on the FlatRedBall forums.  The file can be " +
                    "found in My Documents");

                StringBuilder errorInformation = new StringBuilder();

                errorInformation.AppendLine("Error saving window layout.  Error information: ");
                errorInformation.AppendLine(e.ToString());

                errorInformation.AppendLine("The following windows are contained in the GuiManager");

                foreach (Window w in GuiManager.Windows)
                {
                    errorInformation.AppendLine(w.GetType() + " " + w.Name + " " + w.ToString());

                }

                FileManager.SaveText(errorInformation.ToString(),
                    FileManager.MyDocuments + "WindowLayoutError.txt");
            }

            OkCancelWindow toExit = GuiManager.AddOkCancelWindow();
            toExit.Message = "Any unsaved changes will be lost. Are you sure you want to exit?";
            toExit.ScaleX = 9.4f;
            toExit.ScaleY = 7;
            toExit.OkClick += new GuiMessage(ExitMessage);
            InputManager.ReceivingInput = toExit;
            GuiManager.AddDominantWindow(toExit);

            e.Cancel = true;
        }

        private void GameForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Assign the file names to a string array, in 
                // case the user has selected multiple files.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                try
                {
                    System.Drawing.Point p = this.PointToClient(new System.Drawing.Point(e.X, e.Y));

                    foreach (string fileName in files)
                    {
                        ProcessDroppedFile(fileName);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                    return;
                }
            }
        }

        public void ExitMessage(Window callingWindow)
        {

            Exit();
        }

        public void Exit()
        {
            #region Save the RuntimeOptions
            mRuntimeOptions.FullScreen = this.WindowState == FormWindowState.Maximized;

            if (this.WindowState != FormWindowState.Maximized)
            {
                mRuntimeOptions.WindowWidth = this.Width;
                mRuntimeOptions.WindowHeight = this.Height;
            }

            if (System.IO.Directory.Exists(FileWindow.ApplicationFolderForThisProgram) == false)
            {
                System.IO.Directory.CreateDirectory(FileWindow.ApplicationFolderForThisProgram);
            }

            mRuntimeOptions.Save(FileWindow.ApplicationFolderForThisProgram + @"RuntimeOptions.xml");

            #endregion

            SpriteManager.Exiting = true;
            System.Windows.Forms.Cursor.Show();
            this.Dispose();

        }
        #endregion

        #region Methods

        public EditorWindow()
        {
            LastInstance = this;

            FileManager.RelativeDirectory = System.Windows.Forms.Application.StartupPath + "/";

            FlatRedBall.IO.FileManager.CurrentDirectory = FlatRedBall.IO.FileManager.StartupPath;

            #region Set the window size

            // See if the RuntimeOptions file is valid

            this.ClientSize = new System.Drawing.Size(800, 600);


            #endregion


            FlatRedBallServices.InitializeFlatRedBall(this);

            this.MinimizeBox = true;

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;

            this.MinimumSize = new System.Drawing.Size(
                MinimumSize.Width, 40);


            #region Set the Activated, Closing, and DragEnter events

            this.Activated += new EventHandler(GameForm_Activated);
            this.Closing += new System.ComponentModel.CancelEventHandler(GameForm_Closing);
            this.AllowDrop = true;
            this.DragEnter += new System.Windows.Forms.DragEventHandler(GameForm_DragDrop);

            #endregion

            #region Prepare the camera and TopLeftPixel for resizing

            SpriteManager.Camera.FixAspectRatioYConstant();
            SpriteManager.Camera.FarClipPlane = 7000;

            TopLeftPixel.X = -SpriteManager.Cameras[0].XEdge;
            TopLeftPixel.Y = SpriteManager.Cameras[0].YEdge;


            #endregion

        }

        public void Run(string[] args)
        {
            //try
            {

                this.Show();
                this.Activate();

                #region Process Command Line Arguments
                foreach (string argument in args)
                {
                    ProcessCommandLineArgument(argument);
                }
                #endregion

                LoadRuntimeOptions();

                while (this.IsDisposed == false && Created)
                {
                    #region If the form is inactive, sleep and do Windows events
                    if (Form.ActiveForm != this)
                    {
                        System.Threading.Thread.Sleep(500);
                        Application.DoEvents();
                        continue;
                    }
                    #endregion

                    #region Throttle the frame time

                    if (MinimumFrameLength != 0)
                    {
                        double timeSinceLastFrame =
                            TimeManager.GetSystemTime() - mLastSystemTime;


                        if (timeSinceLastFrame < MinimumFrameLength)
                        {
                            System.Threading.Thread.Sleep((int)(1000 * (MinimumFrameLength - timeSinceLastFrame)));
                        }

                        mLastSystemTime = TimeManager.GetSystemTime();
                    }

                    #endregion

                    FrameUpdate();

                    if (!Created)
                        break;


                    Draw();

                    Application.DoEvents();
                }
            }
			//catch (Exception e)
			{
			//	System.Windows.Forms.MessageBox.Show(e.ToString());
			}

        }

        private void LoadRuntimeOptions()
        {
            try
            {
                if (FileManager.FileExists(FileWindow.ApplicationFolderForThisProgram + @"RuntimeOptions.xml"))
                {
                    mRuntimeOptions = RuntimeOptions.FromFile(FileWindow.ApplicationFolderForThisProgram + @"RuntimeOptions.xml");

                    // It's possible the runtime options may have a 0 width or height.  If so, let's fix that here
                    if (mRuntimeOptions.WindowHeight <= 1)
                    {
                        mRuntimeOptions.WindowHeight = 480;
                    }
                    if (mRuntimeOptions.WindowWidth <= 1)
                    {
                        mRuntimeOptions.WindowWidth = 640;
                    }


                    this.Size = new System.Drawing.Size(
                        mRuntimeOptions.WindowWidth,
                        mRuntimeOptions.WindowHeight);

                    if (mRuntimeOptions.FullScreen)
                    {
                        this.WindowState = FormWindowState.Maximized;
                    }



                    OnResize(true);

                }
            }
            catch (Exception e)
            {
                // Who cares, not a big deal, just carry on
            }
        }

        public virtual void FrameUpdate()
        {
            FlatRedBallServices.Update(null);
        }

        public virtual void ProcessCommandLineArgument(string argument)
        {

        }

        protected virtual void ProcessDroppedFile(string fileName)
        {

        }

        private void Draw()
        {
            FlatRedBallServices.Draw();
        }

        #endregion

    }
}
