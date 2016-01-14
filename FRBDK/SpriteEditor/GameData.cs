using System;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Model;
using FlatRedBall.Math;
using FlatRedBall.Collections;
using FlatRedBall.Utilities;
using FlatRedBall.IO;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Input;

using EditorObjects;

using Microsoft.DirectX;
using Microsoft.DirectX.DirectInput;

using SpriteEditor.Gui;
using SpriteEditor.SEPositionedObjects;
using FlatRedBall.Content.AnimationChain;
using SpriteEditor.Instructions;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Content.Math.Geometry;
using SpriteEditor.SpriteGridObjects;
using EditorObjects.EditorSettings;
using System.Drawing;
using FlatRedBall.Instructions.Reflection;
using System.Reflection;


namespace SpriteEditor
{
	/// <summary>
	/// Summary description for GameData.
	/// </summary>
	public static class GameData
	{
		#region Fields

        public const string SceneContentManager = "Scene Content";

		#region reference to FRB engine managers and data
		
        public static SECursor Cursor = null;
		public static EditorCamera Camera = null;

		public static bool showingCursorPosition = false;

		#endregion

		static int mNextSpriteNumber = 1; // used to add numbers at the end of sprite names
	
		public static string FileName = "";

        #region SpriteEditor-specific managers
        public static SESpriteGridManager sesgMan;
        public static SpriteFrameManager sfMan;
        #endregion

        static ShapeCollection mShapeCollection;

        static Scene mScene = new Scene();
 		public static List<string> textureReplacements = new List<string>();

        /*
         * This is a list of textures that are used by the SpriteEditor and should
         * never be unloaded when the SpriteEditor is running.  This is set after
         * the initialization and is used to reset the loaded textures when
         * a new scene is made.
         */
        static List<AnimationChainList> mReferencedAnimationChains = new List<AnimationChainList>();

                
		public static EditorProperties EditorProperties;


		public static Sprite altParentSprite = null;
		

		public static Layer tempTargetBoxLayer;

        static bool showingUndo = false;

        public static string screenshotFile = "";

        public static string lastDeselectCalled;

        /* There are two cameras in a scene.  One camera is the SECamera - this 
         * it the camera from which the scene is viewed.  The other camera
         * displays its bounds on the scene at the current Sprite's Z.
         * This can be used to positon objects relative to the edge of the screen.
         */
        public static Camera BoundsCamera;

        static EditorLogic mEditorLogic = new EditorLogic();

        static WorldAxesDisplay mWorldAxesDisplay;

        //List<PoseChainArray> mPoseChainArrays;
		#endregion

        #region Properties


        static SpriteEditorSceneProperties mProperties = new SpriteEditorSceneProperties();

        public static List<AnimationChainList> AnimationChains
        {
            get { return mReferencedAnimationChains; }
        }

        public static EditorLogic EditorLogic
        {
            get { return mEditorLogic; }
        }

        public static Scene Scene
        {
            get { return mScene; }
            set { mScene = value; }
        }

        public static WorldAxesDisplay WorldAxesDisplay
        {
            get { return mWorldAxesDisplay; }
        }


        public static SpriteEditorSceneProperties SpriteEditorSceneProperties
        {

            get { return mProperties; }
            set
            {
                mProperties = value;
            }
        }

        #endregion

        #region Methods

        #region Initialize
        public static void Initialize()
		{
			#region FRB engine managers and data
			Camera = GameForm.camera;

            GameForm.camera.Name = "Main View Camera";

			Cursor = GameForm.cursor;
			#endregion

            mWorldAxesDisplay = new WorldAxesDisplay();


            SpriteManager.Camera.BackgroundColor = System.Drawing.Color.Gray;

            try
            {
                if (System.IO.File.Exists("settings/guiSettings.txt"))
                {
                    GuiManager.LoadSettingsFromText("settings/guiSettings.txt");
                }
            }
            catch
            {
                // no big deal, just ignore it
            }


			tempTargetBoxLayer = SpriteManager.AddLayer();

            BoundsCamera = new Camera(FlatRedBallServices.GlobalContentManager);
            BoundsCamera.Z = -40;
            BoundsCamera.FieldOfView = (float)System.Math.PI / 4.0f;
            BoundsCamera.AspectRatio = 4.0f / 3.0f;
            BoundsCamera.OrthogonalWidth = 800;
            BoundsCamera.OrthogonalHeight = 600;
            BoundsCamera.Name = "Bounds Camera";
            
			EditorProperties = new EditorProperties();

			sesgMan = new SESpriteGridManager();
            sfMan = new SpriteFrameManager();

			Camera.FarClipPlane = 3900;

			Cursor.Initialize();

            ModelManager.ModelLightSetup = ModelManager.LightSetup.FullAmbient;
        }

        #endregion

        #region Public Methods

        public static void Activity()
		{
            mEditorLogic.Update();
            
           // records changes for the current Sprite, or the current SpriteGrid

            GuiData.Update();

            sesgMan.spriteGridArrayLogic();


            UndoManager.EndOfFrameActivity();


		}


        public static AnimationChainList AddAnimationChainList(string fileName)
        {
            AnimationChainList sameNamedList = null;

            foreach (AnimationChainList list in mReferencedAnimationChains)
            {
                if (list.Name == fileName)
                {
                    sameNamedList = list;
                    break;
                }
            }



            if (sameNamedList == null)
            {
                AnimationChainListSave animationChainListSave = AnimationChainListSave.FromFile(fileName);

                AnimationChainList list = animationChainListSave.ToAnimationChainList(SceneContentManager);

                mReferencedAnimationChains.Add(list);

                GuiData.ListWindow.Highlight(list);

                return list;
            }
            else
            {
                // highlight the list with the same name

                GuiData.ListWindow.Highlight(sameNamedList);
                return sameNamedList;

            }
        }


        public static void AddModel(string fileName)
        {
            try
            {
                PositionedModel model = ModelManager.AddModel(fileName, GameData.SceneContentManager);

                model.X = SpriteManager.Camera.X;
                model.Y = SpriteManager.Camera.Y;

                // If the model is already part of the Model Manager, this call does nothing
                ModelManager.AddModel(model);

                model.Name = GetUniqueNameForObject<PositionedModel>
                    (model.Name, model);

                Scene.PositionedModels.Add(model);
            }
            catch (Exception e)
            {
                GuiManager.ShowMessageBox("Error loading model:\n\n" + e.ToString(), "Error");
            }

            //model.GetVerts(0);
        }


        public static Sprite AddSprite(string spriteTexture, string nameToUse)
        {
            EditorSprite spriteCreated = new EditorSprite();

            #region Create the spriteCreated depending on the type in spriteTexture
            try
            {
                #region Is the texture null?

                if (string.IsNullOrEmpty(spriteTexture))
                {
                    spriteCreated.Texture = null;
                    spriteCreated.Red = GraphicalEnumerations.MaxColorComponentValue ;
                    spriteCreated.ColorOperation = Microsoft.DirectX.Direct3D.TextureOperation.SelectArg2;
                }

                #endregion

                #region Create the Sprite with a .achx
                else if (spriteTexture.EndsWith(".ach") || spriteTexture.EndsWith(".achx") || 
                    spriteTexture.EndsWith(".gif"))
                {

                    if (spriteTexture.EndsWith(".gif"))
                    {
                        AnimationChain animationChain = AnimationChain.FromGif(spriteTexture, SceneContentManager);

                        spriteCreated.SetAnimationChain(animationChain);
                    }
                    else
                    {

                        AnimationChainListSave listSave = AnimationChainListSave.FromFile(spriteTexture);

                        spriteCreated.AnimationChains = listSave.ToAnimationChainList(SceneContentManager);
                    }
                    if (spriteCreated.AnimationChains.Count != 0)
                        spriteCreated.SetAnimationChain(spriteCreated.AnimationChains[0]);

                    spriteCreated.Animate = true;
                }
                #endregion

                #region Create the Sprite with an image
                else
                {
                    spriteCreated.Texture =
                        FlatRedBallServices.Load<Texture2D>(spriteTexture, SceneContentManager);
                }

                #endregion
            }
            catch (Microsoft.DirectX.Direct3D.InvalidDataException)
            {
                throw new Microsoft.DirectX.Direct3D.InvalidDataException();
            }
            #endregion

            SpriteManager.AddSprite(spriteCreated);



            KeepSpriteNameUnique<Sprite>(spriteCreated, nameToUse, mScene.Sprites);

            spriteCreated.X = Camera.X;
            spriteCreated.Y = Camera.Y;

            // If the camera's in ortho mode then we're likely making a 2D game, so set the PixelSize to .5
            if (SpriteManager.Camera.Orthogonal)
            {
                spriteCreated.PixelSize = .5f;
            }

            SetStoredAddToSE(spriteCreated, EditorProperties.PixelSize);

            return spriteCreated;
        }


        public static SpriteFrame AddSpriteFrame(string texture)
        {

            #region Create the new SpriteFrame ( INCLUDES EARLY OUT )

            FlatRedBall.ManagedSpriteGroups.SpriteFrame spriteFrame = null;

            try
            {
                spriteFrame = GameData.CreateSpriteFrame(texture, "");
            }
            catch (InvalidDataException)
            {
                GuiManager.ShowMessageBox("Could not read " + texture + ".  SpriteFrame not created.", "Error Creating SpriteFrame");
                return null;
            }

            #endregion

            SpriteEditorSettings.EditingSpriteFrames = true;


            GuiData.ToolsWindow.paintButton.Unpress();
            Cursor.ClickObject<FlatRedBall.ManagedSpriteGroups.SpriteFrame>(spriteFrame, 
                EditorLogic.CurrentSpriteFrames,false, true);

            ListUnlistedAnimationChains();

            // The GUI reacts to selected objects, but we want the SpriteFrame to be resized immediately
            // after being added. To do this, force a selection:
            GuiData.SpriteFramePropertyGrid.SelectedObject =
                GameData.EditorLogic.CurrentSpriteFrames[0];
            // Then resize it.


            #region Prepare newly-created SpriteFrame

            // Vic says:  Why do we set it to .4999?
            // There are two parts to this answer:
            // 1.   The most common SpriteFrame TextureBorderWidth
            //      is 0.5.  Therefore, the SpriteEditor will set a
            //      value close to this for all newly-created SpriteFrames.
            // 2.   If we set a TextureBorderWidth of .5, the center Sprite will
            //      have 0 texture coordinate "width" and "height".  This is okay
            //      in XNA, MDX, and OGL, but not in Silverlight.  Silverlight requires
            //      there be some spacing between the texture coordiantes.  
            spriteFrame.TextureBorderWidth = .4999f;

            GuiData.SpriteFramePropertyGrid.MakeCurrentSpriteFramePixelPerfect();


            #endregion



            return spriteFrame;

        }


        public static void AddSpriteGrid(string texture)
        {

            Sprite blueprintSprite = new Sprite();
            blueprintSprite.Texture =
                FlatRedBallServices.Load<Texture2D>(texture, GameData.SceneContentManager);

            SpriteEditor.Gui.MenuStrip.WarnAboutNonPowerOfTwoTexture(blueprintSprite.Texture);


            blueprintSprite.X = Camera.X;
            blueprintSprite.Y = Camera.Y;

            GuiData.ListWindow.Add(blueprintSprite.Texture);



            SpriteGridCreationOptions sgco = new SpriteGridCreationOptions();

            sgco.Plane = SpriteGrid.Plane.XY;

            GuiData.spriteGridPropertiesWindow.SelectedObject = sgco;

            // This sets the GridSpacing based off of the Camera values
            GuiData.spriteGridPropertiesWindow.Show(true, blueprintSprite);

            float spacing = sgco.GridSpacing;


            sgco.XLeftBound = MathFunctions.RoundFloat(SpriteManager.Camera.X - spacing, spacing);
            sgco.XRightBound = MathFunctions.RoundFloat(SpriteManager.Camera.X + spacing, spacing);

            if (sgco.XLeftBound == sgco.XRightBound)
            {
                sgco.XRightBound += spacing*2;
            }

            sgco.YBottomBound = MathFunctions.RoundFloat(SpriteManager.Camera.Y - spacing, spacing);
            sgco.YTopBound = MathFunctions.RoundFloat(SpriteManager.Camera.Y + spacing, spacing);

            if (sgco.YBottomBound == sgco.YTopBound)
            {
                sgco.YTopBound += spacing*2;
            }

            sgco.ZCloseBound = -spacing;
            sgco.ZFarBound = spacing;         


            GuiData.spriteGridPropertiesWindow.UpdateToObject();

            ListUnlistedAnimationChains();
        }


        public static Texture2D AddTexture(string textureFileName)
        {
            Texture2D frbt =
                FlatRedBallServices.Load<Texture2D>(textureFileName, SceneContentManager);

            GuiData.ListWindow.Add(frbt);
            GuiData.ListWindow.Highlight(frbt);
            Texture2D highlightedTexture = GuiData.ListWindow.HighlightedTexture;
            if (highlightedTexture != null)
            {
                GuiData.ToolsWindow.currentTextureDisplay.SetOverlayTextures(highlightedTexture, null);
            }
            else
            {
                GuiData.ToolsWindow.currentTextureDisplay.SetOverlayTextures((Texture2D)null, (Texture2D)null);
            }

            return frbt;
        }

		/// <summary>
		/// This scaled and adjusts relative positions when using a root control Sprite.
		/// The method assumes that the Sprite s is parallel or perpendicular to its parent Sprite.
		/// </summary>
		/// <param name="s">The Sprite to scale and move.</param>
		/// <param name="ScaleX">The parent Sprite's ScaleX percentage change.</param>
		/// <param name="ScaleY">The parent Sprite's ScaleY percentage change.</param>
		/// <param name="sclZ">The parent Sprite's sclZ percentage change (won't scale parent).</param>
		public static void ApplyRelativeScale(Sprite s, float ScaleX, float ScaleY)
		{
			if(s == mEditorLogic.EditAxes .origin)	return; // don't want to scale the edit axes

			if(InputManager.Keyboard.KeyDown(Key.LeftShift) == false && InputManager.Keyboard.KeyDown(Key.RightShift) == false)
			{
				/*
				 * After rotating the scaleVector, some values may become negative.  However, if they were positive in the original
				 * we want them to stay positive.  That way, if the control point is growing, we want the attached Sprite to grow as well.
				 * The locatorVector does this, but assumes that all rotations are multiples of PI/2 on all axes
				 */

                Sprite topParent = (Sprite)s.TopParent;
                Sprite parent = (Sprite)s.Parent;

                float xDifference = s.X - topParent.X;
                float yDifference = s.Y - topParent.Y;

                s.Detach();

                if ((s.RotationZ > (float)System.Math.PI * 0.25f && s.RotationZ < (float)System.Math.PI * 0.75f) ||
                    (s.RotationZ > (float)System.Math.PI * 1.25f && s.RotationZ < (float)System.Math.PI * 1.75f))
                {
                    s.ScaleX *= ScaleY + 1;
                    s.ScaleY *= ScaleX + 1;
                }
                else
                {
                    s.ScaleX *= ScaleX + 1;
                    s.ScaleY *= ScaleY + 1;
                }

                s.X = topParent.X + xDifference * (ScaleX + 1);
                s.Y = topParent.Y + yDifference * (ScaleY + 1);

                s.AttachTo(parent, true);

				for(int i = s.Children.Count - 1; i > -1; i--)
					ApplyRelativeScale((Sprite)(s.Children[i]), ScaleX, ScaleY);
			}
			else
			{
			
				s.ScaleX *= ScaleX + 1;
				s.ScaleY *= ScaleY + 1;

				s.RelativeX *= ScaleX + 1;
				s.RelativeY *= ScaleY + 1;

                for (int i = s.Children.Count - 1; i > -1; i--)
                    ApplyRelativeScale((Sprite)(s.Children[i]), ScaleX, ScaleY);
            }
	    }

        /// <summary>
        /// Detaches and makes all markers invisible so that they do not get in the way of operations dealing with attachment.
        /// </summary>
        public static void ClearAttachedMarkers()
        {
            mEditorLogic.EditAxes.origin.Detach();
            mEditorLogic.EditAxes.Visible = false;
        }


        public static void MakeNewScene()
        {
            EditorWindow.LastInstance.Text = "SpriteEditor - untitled scene";

            SpriteManager.RemoveSpriteList(GameData.Scene.Sprites);

            FlatRedBallServices.Unload(GameData.SceneContentManager);

            GameData.Scene.RemoveFromManagers();

            GameData.Scene.SpriteGrids.Clear();
            SESpriteGridManager.CurrentSpriteGrid = null;
            sesgMan.SpriteGridGrabbed = null;
            sesgMan.newlySelectedCurrentSprite = null;
            sesgMan.newlySelectedCurrentSpriteGrid = null;
            SESpriteGridManager.oldPosition = Vector3.Empty;
            sesgMan.ClickGrid(null, null);


            GameData.DeselectCurrentSpriteFrames();

            GuiData.ListWindow.ClearTextures();
            mReferencedAnimationChains.Clear();

            FlatRedBallServices.Unload(GameData.SceneContentManager);

            GuiData.ToolsWindow.SnapSprite.Unpress();
            GameData.EditorProperties.ConstrainDimensions = false;
            GameData.EditorProperties.PixelSize = 0f;
            GameData.EditorProperties.SnapToGrid = false;

            Camera.X = 0f;
            Camera.Y = 0f;
            Camera.Z = -40f;
            GameData.EditorLogic.EditAxes.Visible = false;

            if (mShapeCollection != null)
            {
                mShapeCollection.RemoveFromManagers();
                mShapeCollection = null;
            }

            mProperties = new SpriteEditorSceneProperties();
        }


        public static void SelectModel(PositionedModel modelToSelect)
        {
            Cursor.ClickObject<PositionedModel>(modelToSelect, mEditorLogic.CurrentPositionedModels, false);
        }


        public static void SelectText(Text textToSelect)
        {
            Cursor.ClickObject<Text>(textToSelect, mEditorLogic.CurrentTexts, false);
        }


        public static void SetScreenshotFile(Window callingWindow)
        {
            SpriteManager.SaveScreenshot(((FileWindow)callingWindow).Results[0]);

        }


        #endregion

        #region Add

        public static void AddShapeCollection(string fileName)
        {
            if (mShapeCollection != null)
            {
                mShapeCollection.RemoveFromManagers();
            }

            ShapeCollectionSave scs = ShapeCollectionSave.FromFile(fileName);
            mShapeCollection = scs.ToShapeCollection();
            mShapeCollection.AddToManagers();
        }

        #endregion

        #region Delete and Replace
        public static void DeleteCurrentSprites()
        {
            SpriteList spritesToRemove = new SpriteList();

            /*
             * May need to delete more Sprites if the selected Sprite is part of a group.  But this all depends on whether
             * we are in group or hierarchy control mode. 
             */

            // need to detach the axes, cursorOverBox, targetBox
            ClearAttachedMarkers();

            #region find out which Sprites are being removed depending on alt and group/hierarchy edit mode
            if (GuiData.ToolsWindow.groupHierarchyControlButton.IsPressed) // hierarchy control
            {
                foreach (Sprite s in mEditorLogic.CurrentSprites)
                {
                    if (spritesToRemove.Contains(s) == false)
                        spritesToRemove.AddOneWay(s);

                    SpriteList tempSpriteArray = new SpriteList();

                    s.GetAllDescendantsOneWay(tempSpriteArray);

                    foreach (Sprite childSprite in tempSpriteArray)
                        if (spritesToRemove.Contains(childSprite) == false)
                            spritesToRemove.AddOneWay(childSprite);
                }
            }
            else
            {
                PositionedObjectList<Sprite> parentSprites = mEditorLogic.CurrentSprites.GetTopParents();

                foreach (Sprite s in parentSprites)
                {
                    if (spritesToRemove.Contains(s) == false)
                        spritesToRemove.AddOneWay(s);

                    SpriteList tempSpriteArray = new SpriteList();

                    s.GetAllDescendantsOneWay(tempSpriteArray);

                    foreach (Sprite childSprite in tempSpriteArray)
                        if (spritesToRemove.Contains(childSprite) == false)
                            spritesToRemove.AddOneWay(childSprite);
                }
            }
            #endregion

            DeleteSprites(spritesToRemove, InputManager.Keyboard.KeyDown(Key.LeftAlt) || InputManager.Keyboard.KeyDown(Key.Right));
        }

        public static void DeleteCurrentSpriteGrid()
        {
            if (EditorLogic.CurrentSpriteGrid != null)
            {
                sesgMan.DeleteGrid(EditorLogic.CurrentSpriteGrid);
            }
            // the sesgMan handles setting the current to null for us.
            //EditorLogic.CurrentSpriteGrid = null;
        }

        public static void DeleteModel(PositionedModel positionedModel)
        {
            if (mEditorLogic.CurrentPositionedModels.Contains(positionedModel))
                DeselectObject(positionedModel);

            ModelManager.RemoveModel(positionedModel);
        }

        public static void DeleteText(Text text)
        {
            if (mEditorLogic.CurrentTexts.Contains(text))
                DeselectObject(text);

            TextManager.RemoveText(text);
        }


        #region XML Docs
        /// <summary>
        /// Performs all of the necessary actions to delete a Sprite from the SpriteEditor.
        /// </summary>
        /// <remarks>
        /// These actions include removing the Sprites (and if necessary, their children) from the list boxes, 
        /// recording undo information, and actually removing the Sprite from the SpriteManager's memory
        /// </remarks>
        /// <param name="spritesToRemove"></param>
        #endregion
        public static void DeleteSprites(SpriteList spritesToRemove, bool removeChildren)
        {
            ClearAttachedMarkers();

            foreach(Sprite sprite in spritesToRemove)
            {
                if(EditorLogic.CurrentSprites.Contains(sprite))
                {
                    DeselectCurrentSprites();
                    break;
                }
            }

            SpriteManager.RemoveSpriteList(spritesToRemove);
        }


        public static void DeleteSpriteFrame(SpriteFrame sfToDelete, bool recordUndo) // pressing CTRL+Z after adding a Sprite calls this function.  Don't want to record the undo when actually undoing.  Need to implement later.
        {
            if (mEditorLogic.CurrentSpriteFrames.Count != 0 && mEditorLogic.CurrentSpriteFrames.Contains(sfToDelete))
            {
                ClearAttachedMarkers();
                DeselectCurrentObject(sfToDelete, mEditorLogic.CurrentSpriteFrames);
            }

            SpriteManager.RemoveSpriteFrame(sfToDelete);
        }

        #region XML Docs
        /// <summary>
        /// Removes the argument texture from memory, from list boxes, and from any GUI displaying the texture.
        /// </summary>
        /// <remarks>
        /// This method does not remove or retexture any objects which reference this texture.  This is handled
        /// in other methods like RemoveObjectsReferencing.
        /// </remarks>
        /// <param name="textureToDelete">The texture to remove.</param>
        #endregion
        public static void DeleteTexture(Texture2D textureToDelete)
        {
            GuiData.ListWindow.Remove(textureToDelete);

            if (GuiData.ToolsWindow.currentTextureDisplay.UpOverlayTexture == textureToDelete)
                GuiData.ToolsWindow.currentTextureDisplay.SetOverlayTextures(null, null) ;

            GuiData.TextureCoordinatesSelectionWindow.Visible = false;

            FlatRedBallServices.Unload(textureToDelete, SceneContentManager);

        }

        #region XML Docs
        /// <summary>
        /// Removes all Sprites and SpriteFrames referencing the argument Texture2D.  Sets all 
        /// SpriteGrid TextureLocationArray references to the argument texture back to the base texture or removes the
        /// SpriteGrid if the base texture is the same as the argument.
        /// </summary>
        /// <param name="texture">The texture being removed.</param>
        #endregion
        public static void RemoveObjectsReferencing(Texture2D texture)
        {
            SpriteList spritesToRemove = new SpriteList();
            // will need to store all removed objects for undo
            foreach(Sprite s in mScene.Sprites)
            {
                if (s.Texture == texture)
                {
                    spritesToRemove.Add(s);
                }
            }
            DeleteSprites(spritesToRemove, false);

            for (int i = GameData.Scene.SpriteGrids.Count - 1; i > -1; i-- )
            {
                if (GameData.Scene.SpriteGrids[i].Blueprint.Texture == texture)
                {
                    /* The blueprint texture matches the texture being removed.  Remove the 
                     * SpriteGrid from the SE.
                     */
                    sesgMan.DeleteGrid(GameData.Scene.SpriteGrids[i]);
                }
                else
                {
                    GameData.Scene.SpriteGrids[i].ReplaceTexture(
                        texture,
                        GameData.Scene.SpriteGrids[i].Blueprint.Texture);
                }
                    
            }


            for (int i = Scene.SpriteFrames.Count - 1; i > -1; i--)
            {
                if (Scene.SpriteFrames[i].Texture == texture)
                    DeleteSpriteFrame(Scene.SpriteFrames[i], true);
            }


        }

        #region XML Docs
        /// <summary>
        /// Replaces the oldTexture with the newTexture
        /// </summary>
        /// <remarks>
        /// Calls the SpriteManager's ReplaceTexture method to perform regular FlatRedBall
        /// texture replacement.  Also replaces all textures on the SpriteGrid, texture ListBox,
        /// and toolsWindow.  
        /// </remarks>
        /// <param name="oldTexture">The old texture to replace.</param>
        /// <param name="newTexture">The new texture to replace with.</param>
        #endregion
        public static void ReplaceTexture(Texture2D oldTexture, Texture2D newTexture)
        {
            foreach (SpriteGrid sg in Scene.SpriteGrids)
                sg.ReplaceTexture(oldTexture, newTexture);

            if (GuiData.ToolsWindow.currentTextureDisplay.UpOverlayTexture == oldTexture)
            {
                GuiData.ToolsWindow.currentTextureDisplay.SetOverlayTextures(newTexture, null);
            }

            FlatRedBallServices.ReplaceFromFileTexture2D(oldTexture, newTexture, SceneContentManager);

            GuiData.ListWindow.ReplaceTexture(oldTexture, newTexture);

            GuiData.TextureCoordinatesSelectionWindow.ReplaceTexture(oldTexture, newTexture);

        }

        #endregion

        #region sprite and sprite group methods (copy, click, adjust after creation)

        public static void AttachObjects(PositionedObject child, PositionedObject parent)
        {
            if (parent == null || parent == child)
            {
                return;
            }
            
            if (child.Parent != null)
            {
                child.Detach();
            }

            if (child.IsParentOf(parent))
            {
                // A parent is trying to attach to its child.  Not allowed!
                System.Windows.Forms.MessageBox.Show("The current object is a parent of " + parent.Name + ". This operation is not allowed");
                return;
            }

            child.AttachTo(parent, true);

            GuiData.ToolsWindow.attachSprite.Unpress();
            GuiData.ToolsWindow.detachSpriteButton.Enabled = true;
            GuiData.ToolsWindow.setRootAsControlPoint.Enabled = true;
        
        }


		/// <summary>
		/// Returns a unique name for the passed Sprite, or a unique name if the spriteInMemory is null.
		/// </summary>
        /// <remarks>
        /// This function is used to create a unique name for a new named object.
        /// This is done by looping through the argument array of named objects
        /// to see if the an object with the same name already exists.
        /// 
        /// Since the object being checked could already be in the argument array,
        /// the function has to make sure that the argument and object in the array are
        /// not the same.  If there there is an object in the array of the same name and it
        /// is not the same object as the argument object, the number at the end is incremented.
        /// 
        /// If no objects of the same name are found in the array, tempObject will be null and
        /// the method will return the nameToCheck
        /// </remarks>
		/// <param name="nameToCheck"></param>
		/// <param name="spriteInMemory"></param>
        /// <param name="attachableList"></param>
		/// <returns></returns>
		public static string GetUniqueNameForObject<T>(string nameToCheck, T objectToCheckTheNameOf) where T: class, INameable
		{
			object temporaryObject = null;
			do
			{
                // See if there is already an object
                // using the objectToCheckTheNameOf's Name.
                // If so, increment the number at the end and
                // try again.
                temporaryObject = null;

                foreach (Sprite s in mScene.Sprites)
                {
                    if (s.Name == nameToCheck && s != objectToCheckTheNameOf)
                    {
                        temporaryObject = s;
                        break;
                    }
                }

                foreach (SpriteFrame sf in Scene.SpriteFrames)
                {
                    if (sf.Name == nameToCheck && sf != objectToCheckTheNameOf)
                    {
                        temporaryObject = sf;
                        break;
                    }
                }

                foreach (SpriteGrid spriteGrid in Scene.SpriteGrids)
                {
                    if (spriteGrid.Name == nameToCheck && spriteGrid != objectToCheckTheNameOf)
                    {
                        temporaryObject = spriteGrid;
                        break;
                    }

                }

                foreach (PositionedModel pm in Scene.PositionedModels)
                {
                    if (pm.Name == nameToCheck && pm != objectToCheckTheNameOf)
                    {
                        temporaryObject = pm;
                        break;
                    }
                }

				// we have one sprite.  Let's see if it's the only sprite
				if(temporaryObject == null)
					return nameToCheck;

				nameToCheck = StringFunctions.IncrementNumberAtEnd(nameToCheck);
				
				
			}while(temporaryObject != null);
			
			return nameToCheck;

		}


        /*
         * // This method may not be needed anymore because of the generic solution above
        public string checkSpriteFrameName(string nameToCheck, SpriteFrame spriteFrameInMemory)
        {
            SpriteFrame tempSpriteFrame = null;

            foreach (SpriteFrame sf in sfMan.SpriteFrames)
            {
                if (sf.Name == nameToCheck && sf != spriteFrameInMemory)
                {
                    nameToCheck = StringFunctions.IncrementNumberAtEnd(nameToCheck);
                    
                }
            }

            return nameToCheck;

        }
        */

        public static SpriteFrame copySpriteFrame(SpriteFrame spriteFrameToCopy, float pixelSize)
        {
            SpriteFrame sf = spriteFrameToCopy.Clone();
            return sf;
        }


		public static Sprite copySpriteHierarchy(Sprite spriteToCopy, Sprite parentSprite,  float pixelSize,
            SpriteList newSpritesOneWay, Dictionary<string, int> appendedNumbers)
		{
            // Create the new Sprite by cloning the spriteToCopy
            Sprite newSprite = null;

            if (spriteToCopy is EditorSprite)
                newSprite = spriteToCopy.Clone<EditorSprite>();
            else
                newSprite = spriteToCopy.Clone<Sprite>();


            newSpritesOneWay.AddOneWay(newSprite);

                // Add the new Sprite to the SpriteManager
            if (SpriteManager.OrderedSprites.Contains(spriteToCopy))
            {
                SpriteManager.AddSprite(newSprite);
            }
            else
            {
                SpriteManager.AddSprite(newSprite);
                SpriteManager.ConvertToZBufferedSprite(newSprite);
            }

            #region Fix the new Sprite's name
            // Take the number off of the end and see if it exists in the appendedNumbers.
            string nameWithoutNumbers = StringFunctions.RemoveNumberAtEnd(spriteToCopy.Name);

            // if it's there, then use the number associated with the name to assign the name
            if (appendedNumbers.ContainsKey(nameWithoutNumbers))
            {
                newSprite.Name = GetUniqueNameForObject(
                    nameWithoutNumbers + appendedNumbers[nameWithoutNumbers], (Sprite)null);

                appendedNumbers[nameWithoutNumbers] =
                    StringFunctions.GetIntAfter(nameWithoutNumbers, newSprite.Name);

            }
            else
            {
                // there is no entry for the nameWithoutNumbers, so find the name and add it with the appropriate number
                newSprite.Name = GetUniqueNameForObject(spriteToCopy.Name, (Sprite)null);

                if (StringFunctions.HasNumberAtEnd(newSprite.Name))
                {
                    // we know that the newSprite.name already exists so save a little work next time if there is another Sprite
                    // to copy by simply incrementing the number.  
                    appendedNumbers.Add(nameWithoutNumbers, 1 + StringFunctions.GetIntAfter(nameWithoutNumbers, newSprite.Name));
                }

            }
            #endregion

            SetStoredAddToSE(newSprite as ISpriteEditorObject, pixelSize);

            if (parentSprite != null)
            {
                newSprite.Detach();
                newSprite.AttachTo(parentSprite, false);
            }

            for (int i = 0; i < spriteToCopy.Children.Count; i++)
            {
                copySpriteHierarchy(spriteToCopy.Children[i] as Sprite, newSprite, pixelSize, newSpritesOneWay, appendedNumbers);
            }
            return newSprite;
		}

        public static SpriteFrame CopySpriteFrameHierarchy(SpriteFrame spriteFrameToCopy, SpriteFrame parentSpriteFrame,
            float pixelSize)
        {
            // Create the new Sprite by cloning the spriteToCopy
            SpriteFrame newSpriteFrame = spriteFrameToCopy.Clone();

            // Add the new SpriteFrame to the SpriteManager
            SpriteManager.AddSpriteFrame(newSpriteFrame);

            // Fix its name
            newSpriteFrame.Name = GetUniqueNameForObject<SpriteFrame>(spriteFrameToCopy.Name, null);

            // if the Sprite references a null texture, we don't want to try to add that null texture to the texture box
            if (newSpriteFrame.Texture != null)
                GuiData.ListWindow.Add(newSpriteFrame.Texture);

            Scene.SpriteFrames.Add(newSpriteFrame);

            /*
           
            for (int i = 0; i < spriteFrameToCopy.Children.Count; i++)
            {
                copySpriteFrameHierarchy(((SpriteFrame)spriteFrameToCopy.Children[i]), newSpriteFrame, pixelSize);
            }
*/            return newSpriteFrame;
        }

        public static PositionedModel CopyModelHierarchy(PositionedModel modelToCopy, PositionedModel parentModel,
            float pixelSize)
        {
            // Create the new Sprite by cloning the spriteToCopy
            PositionedModel newModel = modelToCopy.Clone( );

            // Add the new SpriteFrame to the SpriteManager
            ModelManager.AddModel(newModel);

            // Fix its name
            newModel.Name = GetUniqueNameForObject<SpriteFrame>(modelToCopy.Name, null);

            Scene.PositionedModels.Add(newModel);

            return newModel;

        }

        public static Text CopyTextHierarchy(Text textToCopy, Text parentText, float pixelSize)
        {
            Text newText = textToCopy.Clone();

            TextManager.AddText(newText);

            newText.Name = textToCopy.Name;
            StringFunctions.MakeNameUnique<Text>(newText, Scene.Texts);

            Scene.Texts.Add(newText);

            return newText;
        }

        public static SpriteFrame CreateSpriteFrame(string texture, string nameToUse)
        {
            SpriteFrame spriteFrame = SpriteManager.AddSpriteFrame(null, SpriteFrame.BorderSides.All);

            try
            {
                if (texture.EndsWith(".ach") || texture.EndsWith(".achx"))
                {
                    AnimationChainListSave listSave = AnimationChainListSave.FromFile(texture);

                    spriteFrame.AnimationChains = listSave.ToAnimationChainList(SceneContentManager);

                    if (spriteFrame.AnimationChains.Count != 0)
                        spriteFrame.CurrentChainName = (spriteFrame.AnimationChains[0].Name);

                    spriteFrame.Animate = true;
                }
                else if (texture.EndsWith(".tga") || texture.EndsWith(".png"))
                {
                    spriteFrame.Texture =
                        FlatRedBallServices.Load<Texture2D>(texture, SceneContentManager);
                }
                else
                {
                    spriteFrame.Texture =
                        FlatRedBallServices.Load<Texture2D>(texture, SceneContentManager);
                }
            }
            catch (Microsoft.DirectX.Direct3D.InvalidDataException)
            {
                throw new Microsoft.DirectX.Direct3D.InvalidDataException();
            }

            if (string.IsNullOrEmpty(nameToUse) == false)
            {
                spriteFrame.Name = nameToUse;
            }
            else
            {
                spriteFrame.Name = FileManager.RemovePath( FileManager.RemoveExtension(texture));
            }

            StringFunctions.MakeNameUnique<SpriteFrame>(spriteFrame, mScene.SpriteFrames);

            spriteFrame.X = Camera.X;
            spriteFrame.Y = Camera.Y;

            mScene.SpriteFrames.Add(spriteFrame);

            return spriteFrame;
        }

        public static Text CreateText()
        {
            Text text = TextManager.AddText("Text");
            text.X = Camera.X;
            text.Y = Camera.Y;
            text.Name = "Text";
            mScene.Texts.Add(text);

            // If the view is orthogonal, then we're going to assume that it's a pixel-perfect
            // screen.
            if (SpriteManager.Camera.Orthogonal)
            {
                text.Scale = .5f *
                    text.Font.LineHeightInPixels / 1;// camera.PixelsPerUnitAt(ref this.Position);
                text.Spacing = text.Scale;
                
                text.NewLineDistance = (float)System.Math.Round(text.Scale * 1.5f);

                text.DisplayText = text.DisplayText; // refresh what's viewable

            }

            StringFunctions.MakeNameUnique<Text>(text, mScene.Texts);

            return text;
        }

        public static void DeselectCurrentObjects<T>(AttachableList<T> currentObjects) where T : PositionedObject, ICursorSelectable
        {
            for (int i = currentObjects.Count - 1; i > -1; i--)
            {
                DeselectCurrentObject(currentObjects[i], currentObjects);
            }
            currentObjects.Clear();
            
        }

        public static void DeselectCurrentObject<T>(T currentObject, AttachableList<T> currentList) where T : PositionedObject
        {
            ClearAttachedMarkers();

            if (currentObject as EditorSprite != null)
                mEditorLogic.SetIndividualVariablesFromStoredVariables(currentObject as EditorSprite);

            currentList.Remove(currentObject);
            
        }

        public static void DeselectSprite(Sprite spriteToDeselect)
        {
            if (spriteToDeselect == mEditorLogic.CurrentSprites[0])
            {
                ClearAttachedMarkers();
            }
            mEditorLogic.CurrentSprites.Remove(spriteToDeselect);


            GuiData.ListWindow.SpriteListBox.DeselectObject(spriteToDeselect);

        }


		public static void DeselectCurrentSprites()
        {

            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();


            lastDeselectCalled = "";

            lastDeselectCalled +=
               st.ToString();

            ClearAttachedMarkers();

            mEditorLogic.CurrentSprites.Clear();

			GuiData.ToolsWindow.setRootAsControlPoint.Enabled = false;
            GuiData.ToolsWindow.convertToSpriteFrame.Enabled = false;
            GuiData.ToolsWindow.convertToSpriteGridButton.Enabled = false;
			GuiData.ToolsWindow.attachSprite.Enabled = false;
		}


        public static void DeselectCurrentSpriteFrames()
        {
            while (mEditorLogic.CurrentSpriteFrames.Count != 0)
                DeselectSpriteFrame(mEditorLogic.CurrentSpriteFrames[0]);
        }


        public static void DeselectCurrentTexts()
        {
            while (mEditorLogic.CurrentTexts.Count != 0)
                DeselectCurrentObject<Text>(mEditorLogic.CurrentTexts[0], mEditorLogic.CurrentTexts);
        }


        public static void DeselectSpriteFrame(SpriteFrame spriteFrameToDeselect)
        {
            DeselectCurrentObject<SpriteFrame>(spriteFrameToDeselect, mEditorLogic.CurrentSpriteFrames);
        }


        public static void DeselectObject(object objectToDeselect)
        {
            #region Deslect SpriteFrames

            if (objectToDeselect is SpriteFrame)
            {
                mEditorLogic.CurrentSpriteFrames.Remove(objectToDeselect as SpriteFrame);

                ClearAttachedMarkers();

                if (mEditorLogic.CurrentSpriteFrames.Count == 0)
                {
                    GuiData.ToolsWindow.attachSprite.Enabled = false;
                }
                
                GuiData.ListWindow.SpriteFrameListBox.HighlightItem(null, false);
            }
            #endregion

            #region Deslect PositionedModels

            else if (objectToDeselect is PositionedModel)
            {
                mEditorLogic.CurrentPositionedModels.Remove(objectToDeselect as PositionedModel);

                ClearAttachedMarkers();

                if (mEditorLogic.CurrentPositionedModels.Count == 0)
                {
                    GuiData.ToolsWindow.attachSprite.Enabled = false;
                }
            }

            #endregion

            #region DeselectTexts

            else if (objectToDeselect is Text)
            {
                mEditorLogic.CurrentTexts.Remove(objectToDeselect as Text);

                ClearAttachedMarkers();

                GuiData.ToolsWindow.attachSprite.Enabled = mEditorLogic.CurrentTexts.Count != 0;
            }
            #endregion
        }


        public static void KeepSpriteNameUnique<T>(T spriteToKeepNameUnique, string nameToUse, AttachableList<T> list) where T: class, IAttachable, ITexturable
        {
            string spriteTexture = "Untextured";

            if (spriteToKeepNameUnique.Texture != null)
            {
                spriteTexture = FileManager.RemoveExtension(
                 spriteToKeepNameUnique.Texture.Name);
            }
            spriteTexture = FileManager.RemovePath(spriteTexture);

            if (nameToUse == "")
            {
                string spriteName = spriteTexture + "1";
                string adjustedSpriteName = GetUniqueNameForObject(spriteName, spriteToKeepNameUnique);
                spriteToKeepNameUnique.Name = adjustedSpriteName;
                mNextSpriteNumber++;
                if (adjustedSpriteName != spriteName)
                    mNextSpriteNumber++; // if we need to move ahead, then we might as well move the spriteNum to save some calculations
            }
            else
                spriteToKeepNameUnique.Name = nameToUse;

        }

        /// <summary>
        /// Sets the Stored values to the Sprite's actual values and adds the Sprite and its Texture2D to the ListWindow
        /// </summary>
        /// <param name="spriteToSetAndAdd"></param>
        /// <param name="pixelSize"></param>
        public static void SetStoredAddToSE(ISpriteEditorObject spriteToSetAndAdd, float pixelSize)
        {
            if (pixelSize > 0)
                spriteToSetAndAdd.PixelSize = pixelSize;

            // if the Sprite references a null texture, we don't want to try to add that null texture to the texture box
            if (spriteToSetAndAdd is Sprite)
            {
                if (((Sprite)spriteToSetAndAdd).Texture != null)
                    GuiData.ListWindow.Add(((Sprite)spriteToSetAndAdd).Texture);

                mScene.Sprites.Add((Sprite)spriteToSetAndAdd);

                UndoManager.AddToThisFramesUndo(new AddSpriteUndo(spriteToSetAndAdd as Sprite));
            }

            ListUnlistedAnimationChains();
        }


        #endregion

        private static bool HasSameNamedAnimationChainList(AnimationChainList listToCompareAgainst)
        {
            foreach (AnimationChainList list in mReferencedAnimationChains)
            {
                if (list.Name == listToCompareAgainst.Name)
                    return true;
            }
            return false;
        }

        private static void ListUnlistedAnimationChains()
        {
            #region Loop through all Sprites

            foreach (Sprite sprite in mScene.Sprites)
            {
                if (sprite.AnimationChains != null && sprite.AnimationChains.Count != 0 &&
                    HasSameNamedAnimationChainList(sprite.AnimationChains) == false)
                {
                    
                    mReferencedAnimationChains.Add(sprite.AnimationChains);
                }
            }

            #endregion

            #region Loop through all SpriteFrames
            foreach (SpriteFrame spriteFrame in mScene.SpriteFrames)
            {
                if (spriteFrame.AnimationChains != null &&
                    HasSameNamedAnimationChainList(spriteFrame.AnimationChains) == false)
                {
                    mReferencedAnimationChains.Add(spriteFrame.AnimationChains);
                }
            }
            #endregion

            #region Loop through SpriteGrids

            foreach (SpriteGrid spriteGrid in mScene.SpriteGrids)
            {

            }

            #endregion
        }

        #endregion
	}
}
