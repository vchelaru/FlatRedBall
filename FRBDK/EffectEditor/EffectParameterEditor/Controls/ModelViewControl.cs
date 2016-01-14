using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using FlatRedBall.Graphics.Model;
using FlatRedBall.Graphics.Lighting;
using FlatRedBall.Graphics;
using FlatRedBall;

// Define xna color so we can easily differentiate between it and GDI colors
using XnaColor = Microsoft.Xna.Framework.Graphics.Color;
using FlatRedBall.Input;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.IO;
using EffectEditor.HelperClasses;

namespace EffectParameterEditor.Controls
{
    public partial class ModelViewControl : FRBPanel
    {
        #region Delegates

        // Called when a model has been loaded
        public delegate void ModelLoadedDelegate(String name, PositionedModel model);

        #endregion

        #region Fields

        PositionedModel mCurrentModel;

        Camera mMainCamera;
        Vector3 mCameraTarget = Vector3.Zero;
        float mCameraRadius = 40f;

        Queue<String> mModelsToLoad = new Queue<string>();

        ToolStripStatusLabel mStatusStrip;

        Thread mLoadingThread;

        #endregion

        #region Properties

        #region XML Docs
        /// <summary>
        /// Gets or sets the background color of the panel
        /// </summary>
        #endregion
        public XnaColor BackgroundColor
        {
            get
            {
                if (mMainCamera != null) return mMainCamera.BackgroundColor;
                else return XnaColor.Black;
            }
            set {
                if (mMainCamera != null) mMainCamera.BackgroundColor = value;
            }
        }

        #region XML Docs
        /// <summary>
        /// Gets or sets the current model
        /// </summary>
        #endregion
        public PositionedModel CurrentModel
        {
            get
            {
                if (!DesignMode) return mCurrentModel;
                else return null;
            }
            set { if (!DesignMode) mCurrentModel = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets or sets the status strip this control reports to
        /// </summary>
        #endregion
        public ToolStripStatusLabel StatusStrip
        {
            get { return mStatusStrip; }
            set { mStatusStrip = value; }
        }

        #endregion

        #region Methods

        #region Initialize and Update

        protected override void Initialize()
        {
            base.Initialize();

            mMainCamera = SpriteManager.Camera;

            mMainCamera.NearClipPlane = .1f;
            mMainCamera.FarClipPlane = 10000f;

            Renderer.LightingEnabled = true;
            Renderer.Lights.SetDefaultLighting(LightCollection.DefaultLightingSetup.FrontLit);
            SpriteManager.Camera.BackgroundColor = XnaColor.Black;
            
            ModelLoaded("Cone", ModelManager.AddModel(ModelShape.Cone));
            ModelLoaded("Cube", ModelManager.AddModel(ModelShape.Cube));
            ModelLoaded("Cylinder", ModelManager.AddModel(ModelShape.Cylinder));
            ModelLoaded("Sphere", ModelManager.AddModel(ModelShape.Sphere));

            // Start the loading thread
            mLoadingThread = new Thread(new ThreadStart(LoadModels));
            mLoadingThread.Start();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            #region Modify Camera Positioning with Input

            if (Focused)
            {
                // Change radius with scrollwheel
                mCameraRadius -= (InputManager.Mouse.ScrollWheel *
                    (InputManager.Mouse.ButtonDown(Mouse.MouseButtons.MiddleButton)? .1f : 1f));

                // Clamp radius to 0
                if (mCameraRadius < 0f) mCameraRadius = 0f;               

                // Change rotation with left mouse button
                if (InputManager.Mouse.ButtonDown(Mouse.MouseButtons.LeftButton))
                {
                    mMainCamera.RotationY -=
                        (float)InputManager.Mouse.XChange * MathHelper.Pi / 200f;
                    mMainCamera.RotationX -=
                        (float)InputManager.Mouse.YChange * MathHelper.Pi / 200f;

                    // Make sure rotationX isn't out of bounds
                    if (mMainCamera.RotationX > MathHelper.PiOver2 &&
                        mMainCamera.RotationX < MathHelper.TwoPi - MathHelper.PiOver2)
                    {
                        mMainCamera.RotationX = MathHelper.Pi + MathHelper.PiOver2 *
                            ((mMainCamera.RotationX > MathHelper.Pi) ? 1f : -1f);
                    }
                }

                // Change target with right button
                if (InputManager.Mouse.ButtonDown(Mouse.MouseButtons.RightButton))
                {
                    Vector3 xTranslate, yTranslate;
                    float scaling;

                    xTranslate = -(float)InputManager.Mouse.XChange *
                        Vector3.Transform(Vector3.Right, mMainCamera.RotationMatrix);
                    yTranslate = (float)InputManager.Mouse.YChange *
                        Vector3.Transform(Vector3.Up, Matrix.CreateRotationX(mMainCamera.RotationX));
                    scaling = mMainCamera.PixelsPerUnitAt(
                        mMainCamera.Z + -1f * Vector3.Distance(mCameraTarget, mMainCamera.Position)
                        );

                    mCameraTarget += (xTranslate + yTranslate) / scaling;
                }
            }

            #endregion

            // Position Camera based on rotation and radius
            mMainCamera.Position = mCameraTarget + Vector3.Transform(
                Vector3.Backward, mMainCamera.RotationMatrix) * mCameraRadius;
        }

        #endregion

        internal ModelLoadedDelegate ModelLoaded;

        #region XML Docs
        /// <summary>
        /// Loads a model from file
        /// </summary>
        /// <param name="fileName">The filename of the model to load</param>
        #endregion
        public void AddModel(string fileName)
        {
            // Add the load event to the list
            mModelsToLoad.Enqueue(fileName);
        }

        delegate void SetStatusCallback(String statusText);
        private void SetStatus(String statusText)
        {
            if (this.InvokeRequired)
            {
                SetStatusCallback d = new SetStatusCallback(SetStatus);
                this.Invoke(d, new object[] { statusText });
            }
            else
            {
                if (this.mStatusStrip != null)
                {
                    this.mStatusStrip.Text = statusText;
                }
            }
        }

        #region Threaded Tasks

        #region XML Docs
        /// <summary>
        /// Loads all models currently queued for loading
        /// </summary>
        #endregion
        public void LoadModels()
        {
            // loop until disposing
            while (!Disposing && !IsDisposed)
            {
                while (mModelsToLoad.Count > 0)
                {
                    // Load the next model
                    string modelToLoad = mModelsToLoad.Dequeue();

                    SetStatus("Loading \"" + System.IO.Path.GetFileName(modelToLoad) + "\"...");

                    // Load the model from disk
                    Model xnaModel = FlatRedBallServices.LoadModelFromFile(
                        modelToLoad, "Global");

                    // When loaded, add to model manager
                    PositionedModel model = ModelManager.AddModel(xnaModel);

                    // Report the successful load
                    string modelName = System.IO.Path.GetFileNameWithoutExtension(modelToLoad);
                    ModelLoaded(modelName, model);

                    SetStatus(String.Empty);
                }
            }
        }

        #endregion

        #endregion
    }
}
