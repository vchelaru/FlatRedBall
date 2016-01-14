using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Input;
using Microsoft.DirectX.DirectInput;
using SpriteEditor.Gui;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Gui;
using SpriteEditor.SEPositionedObjects;
using FlatRedBall.Math;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Model;
using EditorObjects;
using Microsoft.DirectX;
using FlatRedBall.Graphics.Animation;
using EditorObjects.EditorSettings;
using FlatRedBall.Debugging;
using FlatRedBall.IO;
using EditorObjects.NodeNetworks;



namespace SpriteEditor
{
    public class EditorLogic
    {
        #region Fields


        SnappingManager mSnappingManager = new SnappingManager();

        public float xToY = 1;

        PositionedObjectList<PositionedModel> mCurrentPositionedModels = new PositionedObjectList<PositionedModel>();
        PositionedObjectList<Text> mCurrentTexts = new PositionedObjectList<Text>();
        SpriteList mCurrentSprites = new SpriteList();
        PositionedObjectList<SpriteFrame> mCurrentSpriteFrames = new PositionedObjectList<SpriteFrame>();
        AnimationChainList mCurrentAnimationChainList;

        EditAxes mAxes = null;

        ReactiveHud mReactiveHud = new ReactiveHud();

        NodeNetworkEditorManager mNodeNetworkEditorManager;



        #endregion

        #region Properties

        public AnimationChainList CurrentAnimationChainList
        {
            get { return mCurrentAnimationChainList; }
            set { mCurrentAnimationChainList = value; }
        }

        public PositionedObjectList<PositionedModel> CurrentPositionedModels
        {
            get { return mCurrentPositionedModels; }
        }

        public PositionedObjectList<Text> CurrentTexts
        {
            get { return mCurrentTexts; }
        }

        public SpriteGrid CurrentSpriteGrid
        {
            get { return SESpriteGridManager.CurrentSpriteGrid; }
        }

        public SpriteList CurrentSprites
        {
            get { return mCurrentSprites; }
        }

        public PositionedObjectList<SpriteFrame> CurrentSpriteFrames
        {
            get { return mCurrentSpriteFrames; }
        }

        public EditAxes EditAxes
        {
            get { return mAxes; }
        }

        public NodeNetworkEditorManager NodeNetworkEditorManager
        {
            get
            {
                return mNodeNetworkEditorManager;
            }
        }

        public SnappingManager SnappingManager
        {
            get { return mSnappingManager; }
        }


        #endregion

        #region Methods

        #region Constructor

        public EditorLogic()
        {
            mAxes = new EditAxes();
            mAxes.Visible = false;
            PositionedObjectMover.ObjectsToIgnore.Add(mAxes.origin);
            PositionedObjectRotator.ObjectsToIgnore.Add(mAxes.origin);
            SpriteManager.AutoIncrementParticleCountValue = 500;

            mNodeNetworkEditorManager = new NodeNetworkEditorManager();
        }

        #endregion


        #region Public Methods


        public void Update()
        {
            
            if (GameData.EditorProperties.SortYSecondary)
                SpriteManager.SortSecondaryY();

            mSnappingManager.Update();


            KeyboardShortcuts();


            MouseCameraControl();


            SortingLogic();


            MouseObjectControl();

            mAxes.Update();

            #region SpriteGrid control
            if (SpriteEditorSettings.EditingSpriteGrids)
            {
                GameData.sesgMan.Activity();
            }
            #endregion

            #region wrap up the variable settings on the current Sprite
            if ((SpriteEditorSettings.EditingSprites || SpriteEditorSettings.EditingSpriteGrids) &&
                CurrentSprites.Count != 0)
            {
                SetSpriteVariablesFromStoredVariables();

                if (GuiData.ToolsWindow.constrainDimensions.IsPressed)
                {
                    foreach (Sprite sprite in CurrentSprites)
                        sprite.SetScaleXRatioToY();
                }
            }
            else if (SpriteEditorSettings.EditingSpriteFrames &&
                CurrentSpriteFrames.Count != 0)
            {
                foreach (SpriteFrame sf in CurrentSpriteFrames)
                {
                    sf.X += mAxes.changeVector.X;
                    sf.Y += mAxes.changeVector.Y;
                    sf.Z += mAxes.changeVector.Z;
                }
            }



            #endregion


            #region if the mouse button is up, then we make sure the static position is not true and we aren't grabbing any Sprites

            Cursor cursor = GameData.Cursor;

            if (cursor.PrimaryDown == false && cursor.SecondaryDown == false)
            {
                cursor.StaticPosition = false;
            }

            #endregion

            mReactiveHud.Activity();

            mNodeNetworkEditorManager.Activity();
        }

        private void SortingLogic()
        {
            ListWindow listWindow = GuiData.ListWindow;

            if (listWindow.EditingSprites)
            {
                GameData.Scene.Sprites.SortZInsertionAscending();
            }
            else if (listWindow.EditingSpriteFrames)
            {
                GameData.Scene.SpriteFrames.SortZInsertionAscending();
            }
            else if (listWindow.EditingTexts)
            {
                GameData.Scene.Texts.SortZInsertionAscending();
            }
        }


        #endregion


        #region Private Methods

        void KeyboardCameraControl()
        {

            // N is used to nudge objects, so the camera shouldn't move when N is down
            if (InputManager.Keyboard.KeyDown(Key.N) == false)
            {
                EditorObjects.CameraMethods.KeyboardCameraControl(GameData.Camera);
            }
        }


        void KeyboardShortcuts()
        {


            if ((InputManager.ReceivingInput != null && InputManager.ReceivingInput.TakingInput) || 
				InputManager.ReceivingInputJustSet) return;


            KeyboardCameraControl();

            #region F for Frame or Focus

            if (InputManager.Keyboard.KeyPushed(Key.F))
            {

                if (CurrentSprites.Count != 0)
                {
                    CameraMethods.FocusOn(CurrentSprites[0]);
                }
                else if (CurrentPositionedModels.Count != 0)
                {
                    CameraMethods.FocusOn(CurrentPositionedModels[0]);
                }
            }

            #endregion

            #region Nudge control
            if (InputManager.Keyboard.KeyDown(Key.N))
            {
                
                GameData.Camera.XVelocity = GameData.Camera.YVelocity = GameData.Camera.ZVelocity = 0;
                if (GameData.EditorLogic.CurrentSprites.Count != 0)
                {
                    float distanceToMove = 1 / GameData.Camera.PixelsPerUnitAt((GameData.EditorLogic.CurrentSprites[0]).Z);


                    if (InputManager.Keyboard.KeyPushed(Key.Up))
                    {
                        (GameData.EditorLogic.CurrentSprites[0]).Y += distanceToMove;
                    }
                    else if (InputManager.Keyboard.KeyPushed(Key.Down))
                    {
                        (GameData.EditorLogic.CurrentSprites[0]).Y -= distanceToMove;

                    }
                    else if (InputManager.Keyboard.KeyPushed(Key.Left))
                    {
                        (GameData.EditorLogic.CurrentSprites[0]).X -= distanceToMove;

                    }
                    else if (InputManager.Keyboard.KeyPushed(Key.Right))
                    {
                        (GameData.EditorLogic.CurrentSprites[0]).X += distanceToMove;
                    }
                    else if (InputManager.Keyboard.KeyPushed(Key.Equals))
                        (GameData.EditorLogic.CurrentSprites[0]).Z += .01f;
                    else if (InputManager.Keyboard.KeyPushed(Key.Minus))
                        (GameData.EditorLogic.CurrentSprites[0]).Z -= .01f;

                }
                else if (CurrentSpriteFrames.Count != 0)
                {
                    float distanceToMove = 1 / GameData.Camera.PixelsPerUnitAt((GameData.EditorLogic.CurrentSpriteFrames[0]).Z);

                    if (InputManager.Keyboard.KeyPushed(Key.Up))
                    {
                        CurrentSpriteFrames[0].Y += distanceToMove;
                    }
                    else if (InputManager.Keyboard.KeyPushed(Key.Down))
                    {
                        CurrentSpriteFrames[0].Y -= distanceToMove;

                    }
                    else if (InputManager.Keyboard.KeyPushed(Key.Left))
                    {
                        CurrentSpriteFrames[0].X -= distanceToMove;

                    }
                    else if (InputManager.Keyboard.KeyPushed(Key.Right))
                    {
                        CurrentSpriteFrames[0].X += distanceToMove;
                    }
                    else if (InputManager.Keyboard.KeyPushed(Key.Equals))
                        CurrentSpriteFrames[0].Z += .01f;
                    else if (InputManager.Keyboard.KeyPushed(Key.Minus))
                        CurrentSpriteFrames[0].Z -= .01f;
                }
                else if (CurrentTexts.Count != 0)
                {
                    float distanceToMove = 1 / GameData.Camera.PixelsPerUnitAt((GameData.EditorLogic.CurrentTexts[0]).Z);

                    if (InputManager.Keyboard.KeyPushed(Key.Up))
                    {
                        CurrentTexts[0].Y += distanceToMove;
                    }
                    else if (InputManager.Keyboard.KeyPushed(Key.Down))
                    {
                        CurrentTexts[0].Y -= distanceToMove;

                    }
                    else if (InputManager.Keyboard.KeyPushed(Key.Left))
                    {
                        CurrentTexts[0].X -= distanceToMove;

                    }
                    else if (InputManager.Keyboard.KeyPushed(Key.Right))
                    {
                        CurrentTexts[0].X += distanceToMove;
                    }
                    else if (InputManager.Keyboard.KeyPushed(Key.Equals))
                        CurrentTexts[0].Z += .01f;
                    else if (InputManager.Keyboard.KeyPushed(Key.Minus))
                        CurrentTexts[0].Z -= .01f;
                }
            }
            #endregion

            #region tool key shortucts - move, rotate, scale, attach, detach, copy

            #region m - Move
            if (InputManager.Keyboard.KeyPushed(Key.M))
            {
                GuiData.ToolsWindow.MoveButton.Press();
            }
            #endregion

            #region r - Rotate
            if (InputManager.Keyboard.KeyPushed(Key.R))
            {
                GuiData.ToolsWindow.RotateButton.Press();
            }
            #endregion

            #region x - Scale
            if (InputManager.Keyboard.KeyPushed(Key.X))
            {
                GuiData.ToolsWindow.ScaleButton.Press();
            }
            #endregion

            #region a - Attach
            if (InputManager.Keyboard.KeyPushed(Key.A))
                GuiData.ToolsWindow.attachSprite.Toggle();
            #endregion

            #region d - Detach
            if (InputManager.Keyboard.KeyPushed(Key.D))
                GuiData.ToolsWindow.detachSprite(null);
            #endregion

            #region CTRL + c - Copy
            if (InputManager.Keyboard.ControlCPushed() &&
                (CurrentPositionedModels.Count != 0 || GameData.EditorLogic.CurrentSprites.Count != 0 || CurrentSpriteFrames.Count != 0 || CurrentTexts.Count != 0))
            {
                GuiData.ToolsWindow.DuplicateClick() ;
            }
            #endregion

            #endregion

            #region showing position by pressing C
            else if (InputManager.Keyboard.KeyPushed(Key.C))
            {
                if (GameData.showingCursorPosition) GameData.showingCursorPosition = false;
                else GameData.showingCursorPosition = true;
            }
            #endregion

            #region space - editing in solo mode
            if (InputManager.Keyboard.KeyPushed(Key.Space))
                GameData.Cursor.ToggleSoloEdit();
            #endregion

            #region DEL
            if (InputManager.Keyboard.KeyPushed(Key.Delete))
            {
                DeleteSelectedObject();
            }
            #endregion

            #region pressing shift to set scale dimention proportions
            if (InputManager.Keyboard.KeyPushed(Key.LeftShift) || InputManager.Keyboard.KeyPushed(Key.RightShift))
            {
                if (GameData.EditorLogic.CurrentSprites.Count != 0)
                    xToY = (float)(GameData.EditorLogic.CurrentSprites[0].ScaleX / GameData.EditorLogic.CurrentSprites[0].ScaleY);
                else if (CurrentSpriteFrames.Count != 0)
                    xToY = CurrentSpriteFrames[0].ScaleX / CurrentSpriteFrames[0].ScaleY;
            }
            #endregion

            #region file shortcuts

            #region CTRL + S
            if (InputManager.Keyboard.KeyPushed(Key.S) && (InputManager.Keyboard.KeyDown(Key.LeftControl) || (InputManager.Keyboard.KeyDown(Key.RightControl))))
            {

                SpriteEditor.Gui.GuiData.MenuStrip.SaveSceneClick(GameData.FileName);

            }
            #endregion

            #region CTRL + N
            if (InputManager.Keyboard.KeyPushed(Key.N) && (InputManager.Keyboard.KeyDown(Key.LeftControl) || (InputManager.Keyboard.KeyDown(Key.RightControl))))
            {
                SpriteEditor.Gui.MenuStrip.NewSceneClick(null);
            }
            #endregion

            #endregion

            #region Sys Rq/ Print Screen

            if (InputManager.Keyboard.KeyPushed(Key.SysRq))
            {
                FileWindow fw = GuiManager.AddFileWindow();
                fw.SetToSave();
                fw.OkClick += new GuiMessage(GameData.SetScreenshotFile);

                List<string> fileTypes = new List<string>();
                fileTypes.Add("bmp");
                fileTypes.Add("jpg");
                fileTypes.Add("tga");
                fileTypes.Add("png");
                fileTypes.Add("dds");

                fw.SetFileType(fileTypes);
                fw.CurrentFileType = "png";
            }
            #endregion
        }

        public void DeleteSelectedObject()
        {
            #region Delete Model
            if (SpriteEditorSettings.EditingModels)
            {
                while (CurrentPositionedModels.Count != 0)
                {
                    GameData.DeleteModel(CurrentPositionedModels[0]);
                }
            }
            #endregion
            #region Delete texture
            if (SpriteEditorSettings.EditingTextures)
            {

                if (GuiData.ListWindow.HighlightedDisplayRegion != null)
                {
                    GuiData.ListWindow.Remove(GuiData.ListWindow.HighlightedDisplayRegion);
                }
                else
                {

                    Texture2D selectedTexture =
                         GuiData.ListWindow.HighlightedTexture;

                    if (selectedTexture != null)
                    {
                        // find out if any objects reference the textures
                        bool isTextureReferenced = false;

                        #region Loop through all objects to see if they reference this texture

                        foreach (Sprite s in GameData.Scene.Sprites)
                        {
                            if (s.Texture == selectedTexture)
                            {
                                isTextureReferenced = true;
                                break;
                            }
                        }

                        if (!isTextureReferenced)
                        {
                            foreach (SpriteFrame sf in GameData.Scene.SpriteFrames)
                            {
                                if (sf.Texture == selectedTexture)
                                {
                                    isTextureReferenced = true;
                                    break;
                                }
                            }
                        }

                        if (!isTextureReferenced)
                        {
                            foreach (SpriteGrid sg in GameData.Scene.SpriteGrids)
                            {
                                if (sg.IsTextureReferenced(selectedTexture))
                                {
                                    isTextureReferenced = true;
                                    break;
                                }
                            }
                        }
                        #endregion

                        if (isTextureReferenced)
                        {
                            List<Texture2D> allTextures = new List<Texture2D>();

                            List<Texture2D> textures = GuiData.ListWindow.GetTextures();

                            foreach (Texture2D texture in textures)
                            {
                                allTextures.Add(texture);
                            }

                            DeleteTextureWindow dtw = new DeleteTextureWindow(
                                selectedTexture, allTextures);
                        }
                        else
                        {
                            GameData.DeleteTexture(selectedTexture);
                        }
                    }
                }
            }
            #endregion
            #region Delete Sprites
            else if ((GameData.EditorLogic.CurrentSprites.Count != 0) && SESpriteGridManager.CurrentSpriteGrid == null)
            {
                GameData.DeleteCurrentSprites();
            }
            #endregion
            #region Delete SpriteGrid
            else if (SpriteEditorSettings.EditingSpriteGrids && SESpriteGridManager.CurrentSpriteGrid != null)
            {
                GameData.sesgMan.DeleteGrid(SESpriteGridManager.CurrentSpriteGrid);
            }
            #endregion
            #region Delete SpriteFrame
            else if (SpriteEditorSettings.EditingSpriteFrames && CurrentSpriteFrames.Count != 0)
            {
                for (int i = CurrentSpriteFrames.Count - 1; i > -1; i--)
                {
                    GameData.DeleteSpriteFrame(CurrentSpriteFrames[i], true);
                }
            }
            #endregion
            #region Delete Texts
            else if (CurrentTexts.Count != 0)
            {
                for (int i = CurrentTexts.Count - 1; i > -1; i--)
                {
                    GameData.DeleteText(CurrentTexts[i]);
                }
            }
            #endregion
        }


        void MouseCameraControl()
        {
            if (GuiData.ToolsWindow.IsDownZ)
            {
                EditorObjects.CameraMethods.MouseCameraControl(GameData.Camera);
                SpriteManager.Camera.CameraCullMode = CameraCullMode.UnrotatedDownZ;
            }
            else
            {
                EditorObjects.CameraMethods.MouseCameraControl3D(GameData.Camera);
                SpriteManager.Camera.CameraCullMode = CameraCullMode.None;
            }

            // Grabbing an object and moving to the edge of the screen scrolls the camera. 
            if (GuiManager.Cursor.ObjectGrabbed != null)
            {

            }

        }


        void MouseObjectControl()
        {
            SECursor cursor = GameData.Cursor;

            if (cursor.WindowOver == null && GuiManager.DominantWindowActive == false &&
                cursor.IsInWindow())
            {

                if (GuiData.ToolsWindow.attachSprite.IsPressed == false)
                    mAxes.Control(cursor, GameData.Camera, xToY);



                cursor.Activity();

                if (SpriteEditorSettings.EditingSprites)
                {
                    foreach (EditorSprite sprite in mCurrentSprites)
                    {
                        testGridSnappingMove(sprite);
                    }
                }
            }

        }


        public void SetIndividualVariablesFromStoredVariables<T>(T s) where T : PositionedObject, ISpriteEditorObject
        {
            PositionedObject asPositionedObject = s as PositionedObject;
            if (EditAxes.changeVector.X != 0 ||
                EditAxes.changeVector.Y != 0 ||
                EditAxes.changeVector.Z != 0)
            {

                #region has a parent
                if (asPositionedObject.Parent != null && GuiData.ToolsWindow.groupHierarchyControlButton.IsPressed)
                {
                    Matrix rotMat = asPositionedObject.Parent.RotationMatrix;
                    rotMat.Invert();

                    mAxes.changeVector.TransformCoordinate(rotMat);

                    s.TopParent.X += EditAxes.changeVector.X;
                    s.TopParent.Y += EditAxes.changeVector.Y;
                    s.TopParent.Z += EditAxes.changeVector.Z;
                }
                else
                {
                    ((ISpriteEditorObject)(s.TopParent)).X += EditAxes.changeVector.X;
                    ((ISpriteEditorObject)(s.TopParent)).Y += EditAxes.changeVector.Y;
                    ((ISpriteEditorObject)(s.TopParent)).Z += EditAxes.changeVector.Z;
                }
                #endregion
            }

        }


        void SetSpriteVariablesFromStoredVariables()
        {
            if (CurrentSprites[0] is ISpriteEditorObject)
            {


                foreach (EditorSprite s in CurrentSprites)
                {
                    SetIndividualVariablesFromStoredVariables(s);
                }
            }
        }


        public void testGridSnappingMove(ISpriteEditorObject spriteToTest)
        {
            if ((mAxes.CursorPushedOnAxis || spriteToTest != null) && GameData.EditorProperties.SnapToGrid)
            {
                double x = (spriteToTest).X;
                double y = (spriteToTest).Y;
                double z = (spriteToTest).Z;

                spriteToTest.X = ((int)(x / GameData.EditorProperties.SnappingGridSize)) * GameData.EditorProperties.SnappingGridSize;
                spriteToTest.Y = ((int)(y / GameData.EditorProperties.SnappingGridSize)) * GameData.EditorProperties.SnappingGridSize;
                spriteToTest.Z = ((int)(z / GameData.EditorProperties.SnappingGridSize)) * GameData.EditorProperties.SnappingGridSize;

            }
        }


        #endregion

        #endregion
    }
}
