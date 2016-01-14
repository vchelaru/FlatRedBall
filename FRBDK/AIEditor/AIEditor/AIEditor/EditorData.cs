using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;

using FlatRedBall.AI.Pathfinding;

using FlatRedBall.Gui;

using FlatRedBall.Input;

using FlatRedBall.Math;

using EditorObjects;

using AIEditor.Gui;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Content.Polygon;
using FlatRedBall.Content.AI.Pathfinding;

#if FRB_XNA
using Keys = Microsoft.Xna.Framework.Input.Keys;
using FlatRedBall.IO;
using EditorObjects.EditorSettings;
#endif

namespace AIEditor
{
    public static class EditorData
    {
        #region Fields

        public const string ContentManagerName = "PolygonEditor Content Manager";

        static Scene mScene = new Scene();

        //static PositionedObjectList<Polygon> mPolygonList;
        static ShapeCollection mShapeCollection = new ShapeCollection();

        static NodeNetwork mNodeNetwork = new NodeNetwork();

        static EditingLogic mEditingLogic;

        public static EditorProperties EditorProperties = new EditorProperties();

        #region XML Docs
        /// <summary>
        /// Displays the X and Y axes to help the user find the origin.
        /// </summary>
        #endregion
        static WorldAxesDisplay mWorldAxesDisplay;

        #endregion

        #region Properties

        public static EditingLogic EditingLogic
        {
            get { return mEditingLogic; }
        }

		public static string LastLoadedFile
		{
			get;
			set;
		}

        public static NodeNetwork NodeNetwork
        {
            get { return mNodeNetwork; }
            set
            {
                // Need to clean up the old NodeNetwork
                mNodeNetwork = value;
                mNodeNetwork.Visible = true;

                GuiData.NodeNetworkPropertyGrid.SelectedObject = mNodeNetwork;
            }
        }

        public static Scene Scene
        {
            get { return mScene; }
            //set
            //{
            //    if (mScene != null)
            //    {
            //        FlatRedBallServices.Unload(ContentManagerName);
            //        SpriteManager.RemoveScene(mScene, true);

            //    }
            //    mScene = value;

            //    SpriteManager.AddScene(mScene);

            //}
        }

        public static ShapeCollection ShapeCollection
        {
            get { return mShapeCollection; }
        }

        #endregion

        #region Methods

        #region Public Methods

        public static void CloseCurrentScene()
        {
            mScene.RemoveFromManagers(true);

        }


        public static void CreateNew()
        {
            RemoveNodeNetwork();
        }


        public static void Initialize()
        {
            mWorldAxesDisplay = new WorldAxesDisplay();
            mEditingLogic = new EditingLogic();

#if FRB_MDX
			Form1.TitleText = "AIEditor - Editing unsaved file";
#else
            FlatRedBallServices.Owner.Text = "AIEditor - Editing unsaved file";
#endif

        }


        public static void LoadNodeNetwork(string fileName, bool copyYToZ, bool flipY, bool makeYZero)
        {
            RemoveNodeNetwork();

            FlatRedBall.Content.AI.Pathfinding.NodeNetworkSave nodeNetworkSave =
                FlatRedBall.Content.AI.Pathfinding.NodeNetworkSave.FromFile(fileName);

            string possibleCompanionFile = FileManager.RemoveExtension(fileName) + "." + AIEditorPropertiesSave.Extension;

            if (FileManager.FileExists(possibleCompanionFile))
            {
                AIEditorPropertiesSave aieps = AIEditorPropertiesSave.Load(possibleCompanionFile);

                if (aieps.Camera != null)
                {
                    aieps.Camera.SetCamera(SpriteManager.Camera);
                }
                //if(aieps.BoundsCamera != null)
                //{
                //    aieps.BoundsCamera.SetCamera(


            }

            #region Modify loaded NodeNetwork if necessary (copyYToZ, flipY, makeYZero)

            if (copyYToZ)
            {
                foreach (PositionedNodeSave pns in nodeNetworkSave.PositionedNodes)
                {
                    pns.Y = pns.Z;
                }
            }

            if (flipY)
            {

                foreach (PositionedNodeSave pns in nodeNetworkSave.PositionedNodes)
                {
                    pns.Y = -pns.Y;
                }
            }

            if (makeYZero)
            {
                foreach (PositionedNodeSave pns in nodeNetworkSave.PositionedNodes)
                {
                    pns.Z = 0;
                }
            }

            #endregion

            LastLoadedFile = fileName;
#if FRB_MDX
			Form1.TitleText = "AIEditor - Editing " + fileName;
#else
            FlatRedBallServices.Owner.Text = "AIEditor - Editing " + fileName;
#endif
            string error;

            NodeNetwork = nodeNetworkSave.ToNodeNetwork(out error);

            if (!string.IsNullOrEmpty(error))
            {
                System.Windows.Forms.MessageBox.Show(error);
            }
        }


        public static void LoadPolygonList(string fileName)
        {
            ShapeManager.Remove<Polygon>(
                mShapeCollection.Polygons);

            PolygonSaveList polygonSaveList = PolygonSaveList.FromFile(fileName);
            mShapeCollection.Polygons.AddRange(
            polygonSaveList.ToPolygonList());

            ShapeManager.AddPolygonList(mShapeCollection.Polygons);

			LastLoadedFile = fileName;
        }


        public static void LoadScene(string fileName)
        {
            if (mScene != null)
            {
                FlatRedBallServices.Unload(ContentManagerName);
                SpriteManager.RemoveScene(mScene, true);

            }

            FlatRedBall.Content.SpriteEditorScene ses = FlatRedBall.Content.SpriteEditorScene.FromFile(fileName);

            mScene = ses.ToScene(EditorData.ContentManagerName);

            SpriteManager.AddScene(mScene);

			LastLoadedFile = fileName;
        }


        public static void LoadShapeCollection(string fileName)
        {
            bool replace = true;

            if (replace)
            {
                mShapeCollection.RemoveFromManagers();
                mShapeCollection.Clear();

                mShapeCollection =
                    FlatRedBallServices.Load<ShapeCollection>(fileName, ContentManagerName);
                mShapeCollection.AddToManagers();

				LastLoadedFile = fileName;
            }
        }


        public static void Update()
        {
            mEditingLogic.Update();

            PerformKeyboardShortcuts();

            //PerformMouseCameraControl();

            mNodeNetwork.UpdateShapes();

        }

        #endregion

        #region Private Methods

        private static void PerformKeyboardShortcuts()
        {
            if (InputManager.ReceivingInput != null)
                return;

            InputManager.Keyboard.ControlPositionedObject(SpriteManager.Camera,
                SpriteManager.Camera.Z * -.6f);

            GuiData.ToolsWindow.ListenForShortcuts();

            if ((InputManager.Keyboard.KeyDown(Keys.LeftControl) ||
                InputManager.Keyboard.KeyDown(Keys.RightControl)) &&
                InputManager.Keyboard.KeyPushed(Keys.C))
            {
                mEditingLogic.CopyCurrentPositionedNodes();

            }
        }

        private static void RemoveNodeNetwork()
        {
            if (mNodeNetwork != null)
            {
                mNodeNetwork.Visible = false;
            }

            // The editor depends on the NodeNetwork not being null
            mNodeNetwork = new NodeNetwork();
        }

        #endregion

        #endregion
    }
}
