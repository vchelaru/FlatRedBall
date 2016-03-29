using System;
using System.Collections.Generic;
using System.Text;
using ToolTemplate.Gui;
using FlatRedBall.Math.Splines;
using FlatRedBall;
using EditorObjects;
using FlatRedBall.Content.Math.Splines;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.IO;
using FlatRedBall.IO.Remote;
using Microsoft.Xna.Framework;
using System.Windows.Forms;
using SplineEditor.Commands;
using SplineEditor.States;

namespace ToolTemplate
{
    #region XML Docs
    /// <summary>
    /// Stores all data that the tool will edit.
    /// </summary>
    /// <remarks>
    /// Examples include Scenes, Polygon Lists, PositionedModel Lists, or any other
    /// custom data that you may edit.
    /// </remarks>
    #endregion
    public static class EditorData
    {
        #region Fields

        private static EditorLogic mEditorLogic;

        public const string ContentManagerName = "Tool ContentManager";

        static SplineList mSplineList = new SplineList();

        static Camera mBoundsCamera;

        static Scene mLoadedScene = null;
        public const string SceneContentManager = "Scene Content Manager";

        #endregion

        #region Properties

        public static Camera BoundsCamera
        {
            get { return mBoundsCamera; }
        }

        public static EditorLogic EditorLogic
        {
            get { return mEditorLogic; }
        }

        public static SplineList SplineList
        {
            get { return mSplineList; }
        }

        #endregion

        #region Methods

        public static void Initialize()
        {
            // Create the mBoundsCamera before the GUI since the GUI will create a PropertyGrid for this object.
            mBoundsCamera = new Camera(FlatRedBallServices.GlobalContentManager);
            mBoundsCamera.DrawsWorld = false;
            mBoundsCamera.DrawsToScreen = false;

            // This needs to happen after the bounds camera is created, but before GuiData is initialized.
            mEditorLogic = new EditorLogic();
            
            GuiData.Initialize();


            // make resizable
            new EditorObjects.FormMethods();
        }

        public static void InitializeSplineAfterCreation(Spline spline)
        {
            spline.Visible = true;

            spline.Name = "Spline";

            FlatRedBall.Utilities.StringFunctions.MakeNameUnique<Spline>(
                spline, mSplineList);

            // Now select this Spline
            EditorData.EditorLogic.CurrentSpline = spline;
        }

        public static void LoadScene(string fileName)
        {
            if (mLoadedScene != null)
            {
                UnloadScene();
            }

            mLoadedScene = FlatRedBallServices.Load<Scene>(fileName, SceneContentManager);
            mLoadedScene.AddToManagers();
        }

        public static void LoadSplines(string fileName)
        {
            LoadSplines(fileName, null, null);
        }

        public static void LoadSplines(string fileName, string userName, string password)
        {
            bool isFtp = FtpManager.IsFtp(fileName);

            SplineSaveList ssl = null;

            if (isFtp)
            {
                ssl = FtpManager.XmlDeserialize<SplineSaveList>(fileName, userName, password);
            }
            else
            {
                ssl = SplineSaveList.FromFile(fileName);
            }

            foreach (var spline in mSplineList)
            {
                spline.Visible = false;
            }
            mSplineList.Clear();

            ssl.AddToList(mSplineList);

            // Set the colors on all of the Splines
            foreach (Spline spline in mSplineList)
            {
                spline.PointColor = Color.DarkGray;
                spline.PathColor = Color.DarkMagenta;
                spline.SplinePointRadiusInPixels = AppState.Self.Preview.SplinePointRadius;

                spline.CalculateVelocities();
                spline.CalculateAccelerations();

                // Don't do this, let the .splx control this
                //spline.Visible = true;
            }

			mSplineList.Name = fileName;

            AppCommands.Self.Gui.RefreshTreeView();
            AppCommands.Self.Gui.RefreshPropertyGrid();


			FlatRedBallServices.Owner.Text = "SplineEditor - Editing " + fileName;
        }

        public static void ProcessCommandLineArguments(string[] commandLineArguments)
        {
            foreach (string s in commandLineArguments)
            {
                if (FileManager.GetExtension(s) == "splx")
                {
                    AppCommands.Self.File.LoadSplineFromFileName(s);
                }
            }
        }

        public static void UnloadScene()
        {
            mLoadedScene.RemoveFromManagers();

            FlatRedBallServices.Unload(SceneContentManager);

            mLoadedScene = null;
        }

        public static void Update()
        {
            CameraMethods.MouseCameraControl(SpriteManager.Camera);

            mEditorLogic.Update();
        }

        #endregion



    }
}
