using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;

using FlatRedBall.AI;
using FlatRedBall.AI.Pathfinding;

using FlatRedBall.Content;
using FlatRedBall.Content.AI.Pathfinding;
using FlatRedBall.Content.Polygon;

using FlatRedBall.Graphics;

using FlatRedBall.Gui;

using FlatRedBall.IO;

using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using EditorObjects.Gui;
#if FRB_MDX
using Microsoft.DirectX;
#else
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using EditorObjects.EditorSettings;
#endif
namespace AIEditor.Gui
{
    public class Menu : MenuStrip
    {
        #region Fields

        string mNameOfNodeNetwork;

        #endregion

        #region Event Methods

        #region File

        void NewClicked(Window callingWindow)
        {
            EditorData.CreateNew();
        }


        void WarnAboutSavingScene(Window callingWindow)
        {
            string message = "You are attempting to save a Scene (.scnx).  Scenes do not include any node network information.  If you are trying to save a node network, select the Save Node Network menu item";

            OkCancelWindow ocw = GuiManager.ShowOkCancelWindow(message, "Warning");
            ocw.OkClick += OpenFileWindowSaveScene;
        }

        void OpenFileWindowSaveScene(Window callingWindow)
        {
            FileWindow fileWindow = GuiManager.AddFileWindow();

            fileWindow.SetToSave();

            fileWindow.Filter = "Scene XML File (*.scnx)|*.scnx";

            fileWindow.OkClick += SaveSceneOk;

        }

		void LoadSceneOk(Window callingWindow)
		{
			string fileName = ((FileWindow)callingWindow).Results[0];

			EditorData.LoadScene(fileName);
		}

		void OpenFileWindowSaveNodeNetwork(Window callingWindow)
		{
			FileWindow fileWindow = GuiManager.AddFileWindow();

			fileWindow.SetToSave();

			fileWindow.Filter = "Node Network Files (*.nntx)|*.nntx|XML files (*.xml)|*.xml";

			fileWindow.OkClick += SaveNodeNetworkOk;

			if (!string.IsNullOrEmpty(EditorData.LastLoadedFile))
			{
				string directory = FileManager.GetDirectory(EditorData.LastLoadedFile);

				fileWindow.SetDirectory(directory);
			}

		}


		void OpenFileWindowLoadScene(Window callingWindow)
		{
			FileWindow fileWindow = GuiManager.AddFileWindow();

			fileWindow.SetFileType("scnx");

			fileWindow.OkClick += LoadSceneOk;

			if (!string.IsNullOrEmpty(EditorData.LastLoadedFile))
			{
				string directory = FileManager.GetDirectory(EditorData.LastLoadedFile);

				fileWindow.SetDirectory(directory);
			}
		}


		void OpenFileWindowLoadNodeNetwork(Window callingWindow)
		{
			FileWindow fileWindow = GuiManager.AddFileWindow();

			fileWindow.SetToLoad();

			fileWindow.Filter = "Node Network Files (*.nntx)|*.nntx|XML files (*.xml)|*.xml";

			fileWindow.OkClick += LoadNodeNetworkOk;

			if (!string.IsNullOrEmpty(EditorData.LastLoadedFile))
			{
				string directory = FileManager.GetDirectory(EditorData.LastLoadedFile);

				fileWindow.SetDirectory(directory);
			}
		}

		void OpenFileWindowLoadPolygonList(Window callingWindow)
		{
			FileWindow fileWindow = GuiManager.AddFileWindow();
			fileWindow.SetToLoad();
			fileWindow.Filter = "PLYLSTX files (*.plylstx)|*.plylstx";
			fileWindow.OkClick += LoadPolygonListOk;

			if (!string.IsNullOrEmpty(EditorData.LastLoadedFile))
			{
				string directory = FileManager.GetDirectory(EditorData.LastLoadedFile);

				fileWindow.SetDirectory(directory);
			}
		}

		void OpenFileWindowLoadShapeCollection(Window callingWindow)
		{
			FileWindow fileWindow = GuiManager.AddFileWindow();
			fileWindow.SetToLoad();
			fileWindow.Filter = "Shape Collection file (*.shcx)|*.shcx";
			fileWindow.OkClick += LoadShapeCollectionOk;

			if (!string.IsNullOrEmpty(EditorData.LastLoadedFile))
			{
				string directory = FileManager.GetDirectory(EditorData.LastLoadedFile);

				fileWindow.SetDirectory(directory);
			}
		}


		void LoadPolygonListOk(Window callingWindow)
		{
			string fileName = ((FileWindow)callingWindow).Results[0];

			EditorData.LoadPolygonList(fileName);
		}

		void LoadNodeNetworkOk(Window callingWindow)
		{
			mNameOfNodeNetwork = ((FileWindow)callingWindow).Results[0];

			PropertyGrid<AxisFlippingSettings> axisFlippingSettings = GuiManager.AddPropertyGrid<AxisFlippingSettings>();
			axisFlippingSettings.HasCloseButton = true;
			axisFlippingSettings.SelectedObject = new AxisFlippingSettings();

			// when loading change the members
			axisFlippingSettings.SetMemberDisplayName("CopyYToZ", "CopyZToY");
			axisFlippingSettings.SetMemberDisplayName("FlipY", "Flip Y");
			axisFlippingSettings.SetMemberDisplayName("MakeYZero", "Set Z = 0");

			Button okButton = new Button(GuiManager.Cursor);
			okButton.Text = "Ok";
			okButton.ScaleX = 1.5f;
			okButton.Click += LoadNodeNetworkSettingsOk;
			okButton.Click += RemoveParentWindow;

			axisFlippingSettings.Closing += GuiManager.RemoveWindow;
			axisFlippingSettings.AddWindow(okButton);
		}

		void LoadNodeNetworkSettingsOk(Window callingWindow)
		{
			AxisFlippingSettings axisFlippingSettings =
				((PropertyGrid<AxisFlippingSettings>)callingWindow.Parent).SelectedObject;

			EditorData.LoadNodeNetwork(
				mNameOfNodeNetwork,
				axisFlippingSettings.CopyYToZ,
				axisFlippingSettings.FlipY,
				axisFlippingSettings.MakeYZero);
		}

		void LoadShapeCollectionOk(Window callingWindow)
		{
			string fileName = ((FileWindow)callingWindow).Results[0];

			EditorData.LoadShapeCollection(fileName);
		}


        void SaveNodeNetworkOk(Window callingWindow)
        {
            mNameOfNodeNetwork = ((FileWindow)callingWindow).Results[0];

            PropertyGrid<AxisFlippingSettings> axisFlippingSettings = new PropertyGrid<AxisFlippingSettings>(GuiManager.Cursor);
            axisFlippingSettings.HasCloseButton = false;
            axisFlippingSettings.Name = "Save Options:";
            GuiManager.AddDominantWindow(axisFlippingSettings);
            axisFlippingSettings.HasCloseButton = true;
            axisFlippingSettings.SelectedObject = new AxisFlippingSettings();

            Button okButton = new Button(GuiManager.Cursor);
            okButton.Text = "Save";
            okButton.ScaleX = 3f;
            okButton.ScaleY = 2;
            okButton.Click += SaveNodeNetworkSettingsOk;
            okButton.Click += RemoveParentWindow;

            axisFlippingSettings.Closing += GuiManager.RemoveWindow;
            axisFlippingSettings.AddWindow(okButton);

        }

        void SaveSceneOk(Window callingWindow)
        {
            string fileName = ((FileWindow)callingWindow).Results[0];

            SpriteEditorScene ses = SpriteEditorScene.FromScene(EditorData.Scene);
            ses.Save(fileName);

        }

        void SaveNodeNetworkSettingsOk(Window callingWindow)
        {
            FlatRedBall.Content.AI.Pathfinding.NodeNetworkSave nodeNetworkSave =
                FlatRedBall.Content.AI.Pathfinding.NodeNetworkSave.FromNodeNetwork(EditorData.NodeNetwork);

            AxisFlippingSettings axisFlippingSettings =
                ((PropertyGrid<AxisFlippingSettings>)callingWindow.Parent).SelectedObject;


            if (axisFlippingSettings.FlipY)
            {

                foreach (PositionedNodeSave pns in nodeNetworkSave.PositionedNodes)
                {
                    pns.Y = -pns.Y;
                }
            }

            if (axisFlippingSettings.CopyYToZ)
            {
                foreach (PositionedNodeSave pns in nodeNetworkSave.PositionedNodes)
                {
                    pns.Z = pns.Y;
                }
            }

            if (axisFlippingSettings.MakeYZero)
            {
                foreach (PositionedNodeSave pns in nodeNetworkSave.PositionedNodes)
                {
                    pns.Y = 0;
                }
            }

            string companionFile = FileManager.RemoveExtension(mNameOfNodeNetwork) + "." + AIEditorPropertiesSave.Extension;

            AIEditorPropertiesSave aieps = new AIEditorPropertiesSave();
            aieps.SetFromRuntime(SpriteManager.Camera, null, false);
            aieps.Save(companionFile);
            nodeNetworkSave.Save(mNameOfNodeNetwork);
            if (!System.IO.File.Exists(mNameOfNodeNetwork))
            {
                GuiManager.ShowMessageBox("Error saving file " + mNameOfNodeNetwork, "Error saving");
            }
        }

        void CloseScene(Window callingWindow)
        {
            EditorData.CloseCurrentScene();
        }

        #endregion

        #region Edit
        void ScaleNodeNetwork(Window callingWindow)
        {
            Vector3OkWindow okWindow = new Vector3OkWindow(GuiManager.Cursor);
            GuiManager.AddWindow(okWindow);

            okWindow.Vector3Value = new Vector3(1, 1, 1);

            okWindow.OkClick += new GuiMessage(ScaleOkClick);
        }

        void ScaleOkClick(Window callingWindow)
        {
            Vector3 scaleValue = ((Vector3OkWindow)callingWindow).Vector3Value;

            NodeNetwork nodeNetwork = EditorData.NodeNetwork;

            for (int i = 0; i < nodeNetwork.Nodes.Count; i++)
            {
                PositionedNode node = nodeNetwork.Nodes[i];

                node.Position.X *= scaleValue.X;
                node.Position.Y *= scaleValue.Y;
                node.Position.Z *= scaleValue.Z;
            }

            GuiManager.RemoveWindow(callingWindow);
        }

        #endregion

        #region Add

        void AddNode(Window callingWindow)
        {
            PositionedNode node = EditorData.NodeNetwork.AddNode();
            node.Position.X = SpriteManager.Camera.Position.X;
            node.Position.Y = SpriteManager.Camera.Position.Y;

            EditorData.NodeNetwork.Visible = true;
            EditorData.NodeNetwork.UpdateShapes();
            
        }

        void AddSprite(Window callingWindow)
        {
            FileWindow fileWindow = GuiManager.AddFileWindow();
            fileWindow.SetToLoad();
            fileWindow.SetFileType("graphic");

            fileWindow.OkClick += AddSpriteOk;
        }

        void AddSpriteOk(Window callingWindow)
        {
            Texture2D texture = 
                FlatRedBallServices.Load<Texture2D>(((FileWindow)callingWindow).Results[0], EditorData.ContentManagerName);

            Sprite sprite = SpriteManager.AddSprite(texture);
            sprite.PixelSize = .5f;
            sprite.PixelSize = -1;
            sprite.X = sprite.ScaleX;
            sprite.Y = -sprite.ScaleY;
            sprite.Name = 
                FileManager.RemoveExtension(FileManager.RemovePath(texture.Name)) + EditorData.Scene.Sprites.Count;

            EditorData.Scene.Sprites.Add(sprite);
        }

        #endregion



        #region General Purpose
        void RemoveParentWindow(Window callingWindow)
        {
            GuiManager.RemoveWindow(callingWindow.Parent);
        }


        #endregion


        void ShowCameraPropertiesWindow(Window callingWindow)
        {
            GuiData.CameraPropertyGrid.Visible = true;
            GuiManager.BringToFront(GuiData.CameraPropertyGrid);
        }

        void ShowEditorPropertiesWindow(Window callingWindow)
        {
            GuiData.EditorPropertiesGrid.Visible = true;
            GuiManager.BringToFront(GuiData.EditorPropertiesGrid);
        }

        void ShowNodeNetworkPropertiesWindow(Window callingWindow)
        {
            GuiData.NodeNetworkPropertyGrid.Visible = true;
            GuiManager.BringToFront(GuiData.NodeNetworkPropertyGrid);
        }

        void ShowScenePropertiesWindow(Window callingWindow)
        {
            GuiData.ScenePropertyGrid.Visible = true;
            GuiManager.BringToFront(GuiData.ScenePropertyGrid);
        }

        void ShowShapeCollectionPropertiesWindow(Window callingWindow)
        {
            GuiData.ShapeCollectionPropertyGrid.Visible = true;
            GuiManager.BringToFront(GuiData.ShapeCollectionPropertyGrid);
        }

        #endregion

        #region Methods

        public Menu()
            : base(GuiManager.Cursor)
        {
            GuiManager.AddWindow(this);

            #region File
            MenuItem item = AddItem("File");

            item.AddItem("New").Click += NewClicked;
            item.AddItem("-------------------");
            item.AddItem("Load NodeNetwork").Click += OpenFileWindowLoadNodeNetwork;
            item.AddItem("Load Scene").Click += OpenFileWindowLoadScene;
            item.AddItem("Load PolygonList").Click += OpenFileWindowLoadPolygonList;
            item.AddItem("Load Shape Collection").Click += OpenFileWindowLoadShapeCollection;
            item.AddItem("-------------------");
            item.AddItem("Save NodeNetwork").Click += OpenFileWindowSaveNodeNetwork;
            item.AddItem("Save Scene").Click += WarnAboutSavingScene;
            item.AddItem("-------------------");
            item.AddItem("Close Scene").Click += new GuiMessage(CloseScene);

            #endregion

            #region Edit

            item = AddItem("Edit");
            item.AddItem("Scale NodeNetwork").Click += new GuiMessage(ScaleNodeNetwork);

            #endregion

            #region Add

            item = AddItem("Add");

            item.AddItem("New Sprite").Click += AddSprite;
            item.AddItem("New Node").Click += AddNode;

            #endregion

            #region Window
            item = AddItem("Window");
            item.AddItem("Camera Properties").Click += ShowCameraPropertiesWindow;
            item.AddItem("Editor Properties").Click += ShowEditorPropertiesWindow;
            item.AddItem("Node Network Properties").Click += ShowNodeNetworkPropertiesWindow;
            item.AddItem("Scene Properties").Click += ShowScenePropertiesWindow;
            item.AddItem("Shape Collection Properties").Click += ShowShapeCollectionPropertiesWindow;
            #endregion
        }

        #endregion
    }
}
