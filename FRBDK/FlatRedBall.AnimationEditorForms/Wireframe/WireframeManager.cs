using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.SpecializedXnaControls;
using RenderingLibrary.Content;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using FlatRedBall.IO;
using FlatRedBall.Content.AnimationChain;
using RenderingLibrary.Math.Geometry;
using FlatRedBall.AnimationEditorForms.Data;
using InputLibrary;
using System.Windows.Forms;
using Camera = RenderingLibrary.Camera;
using FlatRedBall.AnimationEditorForms.Controls;
using Microsoft.Xna.Framework;
using FlatRedBall.AnimationEditorForms.Wireframe;
using FlatRedBall.AnimationEditorForms.Textures;
using FlatRedBall.AnimationEditorForms.CommandsAndState;
using FlatRedBall.SpecializedXnaControls.RegionSelection;
using FlatRedBall.AnimationEditorForms.Preview;

namespace FlatRedBall.AnimationEditorForms
{
    public class WireframeManager
    {
        #region Fields

        static WireframeManager mSelf;

        ImageRegionSelectionControl mControl;

        SystemManagers mManagers;

        LineRectangle mSpriteOutline;
        LineGrid mLineGrid;
        WireframeEditControls mWireframeControl;

        public Color OutlineColor = new Microsoft.Xna.Framework.Color(1, 1, 1, .3f);

        StatusTextController mStatusText;

        InspectableTexture mInspectableTexture = new InspectableTexture();

        RectangleSelector mPushedRegion;

        #endregion

        #region Properties

        public static WireframeManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new WireframeManager();
                }
                return mSelf;
            }
        }

        public Texture2D Texture
        {
            get
            {
                return mControl.CurrentTexture;
            }
            set
            {
                mControl.CurrentTexture = value;
                mInspectableTexture.Texture = value;
            }
        }

        public int ZoomValue
        {
            get
            {
                if (mControl != null)
                {
                    return this.mControl.ZoomValue;
                }
                else
                {
                    return 100;
                }
            }
            set
            {
                if (mControl != null)
                {
                    
                    this.mControl.ZoomValue = value;
                    mWireframeControl.PercentageValue = value;
                }
            }
        }

        public SystemManagers Managers
        {
            get { return mManagers; }
        }

        #endregion

        #region Events

        public event EventHandler AnimationChainChange;
        public event EventHandler AnimationFrameChange;

        #endregion

        #region Methods

        #region Input Methods
        void HandleMouseWheelZoom(object sender, EventArgs e)
        {
            mWireframeControl.PercentageValue = mControl.ZoomValue;


        }

        void HandleClick(object sender, EventArgs e)
        {
            if (e is MouseEventArgs && ((MouseEventArgs)e).Button == MouseButtons.Left)
            {
                TryHandleSpriteSheetClicking();

                TryHandleMagicWandClicking();

                if (AnimationFrameChange != null)
                {
                    AnimationFrameChange(this, null);
                }
                if (AnimationChainChange != null)
                {
                    AnimationChainChange(this, null);
                }
            }
        }

        private void TryHandleMagicWandClicking()
        {
            if (mWireframeControl.IsMagicWandSelected &&
                mControl.CurrentTexture != null)
            {
                float worldX = mControl.XnaCursor.GetWorldX(mManagers);
                float worldY = mControl.XnaCursor.GetWorldY(mManagers);

                int pixelX = (int)worldX;
                int pixelY = (int)worldY;

                int minX, minY, maxX, maxY;

                this.mInspectableTexture.GetOpaqueWandBounds(pixelX, pixelY, out minX, out minY, out maxX, out maxY);

                if (maxX >= minX && maxY >= minY)
                {
                    // Selection found!

                    AnimationFrameSave frame = SelectedState.Self.SelectedFrame;

                    Texture2D texture = GetTextureForFrame(frame);

                    mControl.RectangleSelector.Visible = texture != null;

                    this.RefreshAll();

                    if (texture != null)
                    {
                        mControl.RectangleSelector.Left = minX;
                        mControl.RectangleSelector.Width = maxX - minX;

                        mControl.RectangleSelector.Top = minY;
                        mControl.RectangleSelector.Height = maxY - minY;

                        HandleRegionChanged(null, null);


                        //frame.LeftCoordinate = minX / (float)texture.Width;
                        //frame.RightCoordinate = (maxX + 1) / (float)texture.Width;
                        //frame.TopCoordinate = minY / (float)texture.Height;
                        //frame.BottomCoordinate = (maxY + 1) / (float)texture.Height;
                    }

                    RefreshAll();


                    if (AnimationFrameChange != null)
                    {
                        AnimationFrameChange(this, null);
                    }

                }
            }
        }

        private void TryHandleSpriteSheetClicking()
        {
            if (PropertyGridManager.Self.UnitType == UnitType.SpriteSheet &&
                mControl.CurrentTexture != null &&
                SelectedState.Self.SelectedTileMapInformation != null &&
                SelectedState.Self.SelectedTileMapInformation.TileWidth != 0 &&
                SelectedState.Self.SelectedTileMapInformation.TileHeight != 0 &&
                SelectedState.Self.SelectedFrame != null
                )
            {
                float worldX = mControl.XnaCursor.GetWorldX(mManagers);
                float worldY = mControl.XnaCursor.GetWorldY(mManagers);

                if (worldX > 0 && worldX < mControl.CurrentTexture.Width &&
                    worldY > 0 && worldY < mControl.CurrentTexture.Height)
                {
                    int xIndex = (int)(worldX / SelectedState.Self.SelectedTileMapInformation.TileWidth);
                    int yIndex = (int)(worldY / SelectedState.Self.SelectedTileMapInformation.TileHeight);

                    PropertyGridManager.Self.SetTileX(SelectedState.Self.SelectedFrame, xIndex);
                    PropertyGridManager.Self.SetTileY(SelectedState.Self.SelectedFrame, yIndex);


                    this.RefreshAll();
                }
            }
        }

        #endregion

        public void Initialize(ImageRegionSelectionControl control, SystemManagers managers, WireframeEditControls wireframeControl)
        {
            mManagers = managers;
            mManagers.Renderer.SamplerState = SamplerState.PointClamp;


            mControl = control;


            mManagers.Renderer.Camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;

            mWireframeControl = wireframeControl;
            mWireframeControl.WandSelectionChanged += ReactToMagicWandChange;

            mControl.RegionChanged += new EventHandler(HandleRegionChanged);

            mControl.MouseWheelZoom += new EventHandler(HandleMouseWheelZoom);
            mControl.AvailableZoomLevels = mWireframeControl.AvailableZoomLevels;

            mControl.XnaUpdate += new Action(HandleXnaUpdate);
            mControl.Panning += HandlePanning;

            mSpriteOutline = new LineRectangle(managers);
            managers.ShapeManager.Add(mSpriteOutline);
            mSpriteOutline.Visible = false;
            mSpriteOutline.Color = OutlineColor;

            mLineGrid = new LineGrid(managers);
            managers.ShapeManager.Add(mLineGrid);
            mLineGrid.Visible = false;
            mLineGrid.Color = OutlineColor;

            mControl.Click += new EventHandler(HandleClick);

            mStatusText = new StatusTextController(managers);
            mControl_XnaInitialize();
        }

        private void HandlePanning()
        {
            ApplicationEvents.Self.CallAfterWireframePanning();
        }

        void ReactToMagicWandChange(object sender, EventArgs e)
        {
            RefreshAll();
        }

        void mControl_XnaInitialize()
        {
            RefreshAll();

        }

        void HandleXnaUpdate()
        {
            mStatusText.AdjustTextSize();
            if (mStatusText.Visible)
            {
                MoveOriginToTopLeft(mManagers.Renderer.Camera);
            }

            if (mControl.XnaCursor.IsInWindow)
            {
                StatusBarManager.Self.SetCursorPosition(
                    mControl.XnaCursor.GetWorldX(mManagers),
                    mControl.XnaCursor.GetWorldY(mManagers));
            }
        }

        void HandleRegionChanged(object sender, EventArgs e)
        {
            AnimationFrameSave frame = SelectedState.Self.SelectedFrame;

            if (frame != null)
            {
                Texture2D texture = mControl.CurrentTexture;

                frame.LeftCoordinate = mControl.RectangleSelector.Left / (float)texture.Width;
                frame.RightCoordinate = mControl.RectangleSelector.Right / (float)texture.Width;
                frame.TopCoordinate = mControl.RectangleSelector.Top / (float)texture.Height;
                frame.BottomCoordinate = mControl.RectangleSelector.Bottom / (float)texture.Height;

                TreeViewManager.Self.RefreshTreeNode(frame);
            }
            else if (SelectedState.Self.SelectedChain != null)
            {
                if (mPushedRegion != null)
                {
                    Texture2D texture = mControl.CurrentTexture;

                    int changedLeft = FlatRedBall.Math.MathFunctions.RoundToInt( mPushedRegion.Left - mPushedRegion.OldLeft);
                    int changedTop = FlatRedBall.Math.MathFunctions.RoundToInt( mPushedRegion.Top - mPushedRegion.OldTop);
                    int changedBottom = FlatRedBall.Math.MathFunctions.RoundToInt( mPushedRegion.Bottom - mPushedRegion.OldBottom);
                    int changedRight = FlatRedBall.Math.MathFunctions.RoundToInt(mPushedRegion.Right - mPushedRegion.OldRight);

                    foreach (var containedFrame in SelectedState.Self.SelectedChain.Frames)
                    {
                        // shift this by pixels!

                        containedFrame.LeftCoordinate += changedLeft / (float)texture.Width;
                        containedFrame.RightCoordinate += changedRight / (float)texture.Width;

                        containedFrame.TopCoordinate += changedTop / (float)texture.Height;
                        containedFrame.BottomCoordinate += changedBottom / (float)texture.Height;
                    }

                    UpdateSelectorsToAnimation(skipPushed:true, texture:texture);
                    PreviewManager.Self.ReactToAnimationChainSelected();
                }
            }
            // This is causing spamming of the save - we only want to do this on a mouse click
            //if (AnimationChainChange != null)
            //{
            //    AnimationChainChange(this, null);
            //}
            if (AnimationFrameChange != null)
            {
                AnimationFrameChange(this, null);
            }
        }
        
        public void RefreshAll()
        {
            /////////////Early Out
            if (mControl == null)
            {
                return;
            }
            //////////End Early Out
            if (SelectedState.Self.SelectedFrame != null)
            {
                UpdateToSelectedFrame();
            }

            else if (SelectedState.Self.SelectedChain != null && SelectedState.Self.SelectedChain.Frames.Count != 0) 
            {
                UpdateToSelectedAnimation();
            }
            else
            {
                mControl.RectangleSelector.Visible = false;
                Texture = null;
                mSpriteOutline.Visible = false;
                mLineGrid.Visible = false;

            }
            mStatusText.UpdateText();
        }

        private void UpdateToSelectedAnimation(bool skipPushed = false)
        {
            if (mControl.RectangleSelector != null)
            {
                mControl.RectangleSelector.Visible = false;
            }

            AnimationFrameSave afs = null;

            afs = SelectedState.Self.SelectedChain.Frames[0];
            if (afs.TextureName != null)
            {
                Texture2D texture = GetTextureForFrame(afs);
                Texture = texture;

                ShowSpriteOutlineForTexture(texture);
                UpdateLineGridToTexture(texture);


                bool anyDiffer = SelectedState.Self.SelectedChain.Frames.Any(item => item.TextureName != afs.TextureName);

                if (!anyDiffer)
                {
                    UpdateSelectorsToAnimation(skipPushed, texture);

                }
                else
                {
                    mControl.DesiredSelectorCount = 0;
                }
            }

            // Do we need to check if it's changed?
            //if (Texture != textureBefore)
            {
                ApplicationEvents.Self.CallWireframeTextureChange();
            }

        }

        private void UpdateSelectorsToAnimation(bool skipPushed, Texture2D texture)
        {
            // Everything here is 
            mControl.DesiredSelectorCount = SelectedState.Self.SelectedChain.Frames.Count;
            for (int i = 0; i < SelectedState.Self.SelectedChain.Frames.Count; i++)
            {
                var frame = SelectedState.Self.SelectedChain.Frames[i];

                var rectangleSelector = mControl.RectangleSelectors[i];
                if (skipPushed == false || rectangleSelector != mPushedRegion)
                {
                    bool hasAlreadyBeenInitialized = rectangleSelector.Tag != null;
                    UpdateRectangleSelectorToFrame(frame, texture, mControl.RectangleSelectors[i]);
                    rectangleSelector.ShowHandles = false;
                    if (!hasAlreadyBeenInitialized)
                    {

                        rectangleSelector.Tag = frame;
                        rectangleSelector.ShowHandles = false;
                        rectangleSelector.RoundToUnitCoordinates = true;
                        rectangleSelector.AllowMoveWithoutHandles = true;
                        rectangleSelector.Pushed += HandleRegionPushed;
                        // Only the first cursor will reset back to the arrow, otherwise the others shouldn't
                        rectangleSelector.ResetsCursorIfNotOver = i == 0;
                    }
                }
            }
        }

        private void HandleRegionPushed(object sender, EventArgs e)
        {
            this.mPushedRegion = sender as RectangleSelector;
        }

        private void UpdateToSelectedFrame()
        {
            AnimationFrameSave frame = SelectedState.Self.SelectedFrame;

            Texture2D texture = GetTextureForFrame(frame);
            Texture2D textureBefore = Texture;
            Texture = texture;


            mControl.DesiredSelectorCount = 1;
            if (texture != null)
            {
                var rectangleSelector = mControl.RectangleSelector;
                
                mControl.DesiredSelectorCount = 1;

                UpdateRectangleSelectorToFrame(frame, texture, rectangleSelector);

                ShowSpriteOutlineForTexture(texture);
            }
            else
            {
                mControl.RectangleSelector.Visible = false;

            }

            UpdateLineGridToTexture(texture);

            if (PropertyGridManager.Self.UnitType == UnitType.SpriteSheet || mWireframeControl.IsMagicWandSelected)
            {
                this.mControl.RectangleSelector.ShowHandles = false;
            }
            else
            {
                this.mControl.RectangleSelector.ShowHandles = true;
            }


            this.mControl.RoundRectangleSelectorToUnit = PropertyGridManager.Self.UnitType == UnitType.Pixel;

            if (Texture != textureBefore)
            {
                ApplicationEvents.Self.CallWireframeTextureChange();
            }
        }

        private static void UpdateRectangleSelectorToFrame(AnimationFrameSave frame, Texture2D texture, RectangleSelector rectangleSelector)
        {
            rectangleSelector.Visible = texture != null;

            if (texture != null)
            {
                float leftPixel = frame.LeftCoordinate * texture.Width;
                float rightPixel = frame.RightCoordinate * texture.Width;
                float topPixel = frame.TopCoordinate * texture.Height;
                float bottomPixel = frame.BottomCoordinate * texture.Height;

                rectangleSelector.Left = leftPixel;
                rectangleSelector.Top = topPixel;
                rectangleSelector.Width = rightPixel - leftPixel;
                rectangleSelector.Height = bottomPixel - topPixel;
            }
        }

        private void UpdateLineGridToTexture(Texture2D texture)
        {
            float rowWidth = 0;
            float columnWidth = 0;
            if (SelectedState.Self.SelectedTileMapInformation != null)
            {
                TileMapInformation tmi = SelectedState.Self.SelectedTileMapInformation;
                rowWidth = tmi.TileHeight;
                columnWidth = tmi.TileWidth;
            }

            if (texture != null && rowWidth != 0 && columnWidth != 0 && PropertyGridManager.Self.UnitType == UnitType.SpriteSheet)
            {
                mLineGrid.Visible = true;
                mLineGrid.ColumnWidth = columnWidth;
                mLineGrid.RowWidth = rowWidth;

                mLineGrid.ColumnCount = (int)(texture.Width / columnWidth);
                mLineGrid.RowCount = (int)(texture.Height / rowWidth);
                // This puts it in front of everything - maybe eventually we want to use layers?
                mLineGrid.Z = 1;
            }

            else
            {
                mLineGrid.Visible = false;
            }
        }

        private void ShowSpriteOutlineForTexture(Texture2D texture)
        {
            if (texture != null)
            {
                mSpriteOutline.Visible = true;
                mSpriteOutline.Width = texture.Width;
                mSpriteOutline.Height = texture.Height;
            }
            else
            {
                mSpriteOutline.Visible = false;
            }
        }

        public string GetTextureFileNameForFrame(AnimationFrameSave frame)
        {
            string returnValue = null;
            if (ProjectManager.Self.AnimationChainListSave != null)
            {
                string achxFolder = FileManager.GetDirectory(ProjectManager.Self.FileName);

                if (frame != null && !string.IsNullOrEmpty(frame.TextureName))
                {
                    string fileName = achxFolder + frame.TextureName;

                    returnValue = fileName;

                }
                else
                {
                    returnValue = null;
                }
            }
            return returnValue;

        }

        public Texture2D GetTextureForFrame(AnimationFrameSave frame)
        {
            string fileName = GetTextureFileNameForFrame(frame);


            Texture2D texture = null;
            if (!string.IsNullOrEmpty(fileName) && System.IO.File.Exists(fileName))
            {
                texture = LoaderManager.Self.LoadContent<Texture2D>(fileName);
            }
            return texture;
        }

        public const int Border = 10;
        string mLastTexture = "";
        public void HandleAnimationChainChanged()
        {
            RenderingLibrary.Camera camera = mManagers.Renderer.Camera;

            string newName = null;
            if (Texture != null)
            {
                newName = Texture.Name;
            }
            if (newName != mLastTexture)
            {
                MoveOriginToTopLeft(camera);
            }

            mLastTexture = newName;

            mControl.BringSpriteInView();
        }

        private static void MoveOriginToTopLeft(RenderingLibrary.Camera camera)
        {
            // top-left origin.
            //float desiredX = (-Border + camera.ClientWidth / 2.0f) / camera.Zoom;
            //if (desiredX != camera.X)
            //{
            //    camera.X = desiredX;
            //}

            //camera.Y = (-Border + camera.ClientHeight / 2.0f) / camera.Zoom;

            camera.X = 0;
            camera.Y = 0;
        }



        #endregion
    }
}
