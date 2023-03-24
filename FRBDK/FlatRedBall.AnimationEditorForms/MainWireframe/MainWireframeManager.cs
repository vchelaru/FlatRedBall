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
using FlatRedBall.AnimationEditorForms.ViewModels;
using System.ComponentModel;
using ToolsUtilities;
using FileManager = ToolsUtilities.FileManager;
using FilePath = global::ToolsUtilities.FilePath;
using FlatRedBall.AnimationEditorForms.Managers;

namespace FlatRedBall.AnimationEditorForms
{
    public class WireframeManager
    {
        #region Fields


        static WireframeManager mSelf;

        ImageRegionSelectionControl mControl;

        SystemManagers mManagers;

        LineRectangle mSpriteOutline;
        LineRectangle selectionPreviewRectangle;

        LineGrid mLineGrid;
        WireframeEditControls mWireframeControl;

        public Color OutlineColor = new Microsoft.Xna.Framework.Color(1f,1f,1f,1f);
        // premult:
        public Color LineGridColor = new Microsoft.Xna.Framework.Color(.3f, .3f, .3f, .3f);
        public Color MagicWandPreviewColor = new Color(1, 1, 0, 1);

        StatusTextController mStatusText;

        InspectableTexture mInspectableTexture = new InspectableTexture();

        RectangleSelector mPushedRegion;

        Keyboard keyboard;

        public Dictionary<FilePath, Vector3> CameraPositionsForTexture { get; set; } = new Dictionary<FilePath, Vector3>();

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

        public WireframeEditControlsViewModel WireframeEditControlsViewModel
        {
            get; private set;
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

                TrySnapToGridClicking();

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

        private void TrySnapToGridClicking()
        {
            var gridSize = WireframeEditControlsViewModel.GridSize;
            if (WireframeEditControlsViewModel.IsSnapToGridChecked &&
                gridSize > 0 &&
                mControl.CurrentTexture != null)
            {

                float worldX = mControl.XnaCursor.GetWorldX(mManagers);
                float worldY = mControl.XnaCursor.GetWorldY(mManagers);

                int pixelX = (int)worldX;
                int pixelY = (int)worldY;


                var minX = gridSize * ((pixelX) / gridSize);
                var minY = gridSize * ((pixelY) / gridSize);

                bool isCtrlDown =
                    keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) ||
                    keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl);

                var didChange = false;
                if (isCtrlDown)
                {
                    CreateNewFrameFromTextureCoordinates(minX, minY, minX + gridSize, minY + gridSize);
                    didChange = true;
                }
                else
                {
                    // do nothing...yet?
                    //SetCurrentFrameFromMagicWand(minX, minY, maxX, maxY);
                }

                if(didChange)
                {
                    RefreshAll();

                    if (AnimationFrameChange != null)
                    {
                        AnimationFrameChange(this, null);
                    }
                }

            }
        }

        private void TryHandleMagicWandClicking()
        {
            
            if (WireframeEditControlsViewModel.IsMagicWandSelected &&
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
                    bool isCtrlDown =
                        keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) ||
                        keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl);

                    if (isCtrlDown)
                    {
                        CreateNewFrameFromTextureCoordinates(minX, minY, maxX, maxY);
                    }
                    else
                    {
                        SetCurrentFrameFromMagicWand(minX, minY, maxX, maxY);
                    }
                    RefreshAll();


                    if (AnimationFrameChange != null)
                    {
                        AnimationFrameChange(this, null);
                    }

                }
            }
        }

        private void CreateNewFrameFromTextureCoordinates(int minX, int minY, int maxX, int maxY)
        {
            AnimationChainSave chain = SelectedState.Self.SelectedChain;


            if (string.IsNullOrEmpty(ProjectManager.Self.FileName))
            {
                MessageBox.Show("You must first save this file before adding frames");
            }
            else if (chain == null)
            {
                MessageBox.Show("First select an Animation to add a frame to");
            }
            else
            {
                AnimationFrameSave afs = new AnimationFrameSave();
                afs.ShapeCollectionSave = new FlatRedBall.Content.Math.Geometry.ShapeCollectionSave();

                var texture = this.mInspectableTexture.Texture;
                var achxFolder = FlatRedBall.IO.FileManager.GetDirectory(ProjectManager.Self.FileName);
                var relative = FlatRedBall.IO.FileManager.MakeRelative(texture.Name, achxFolder);

                afs.TextureName = relative;

                afs.LeftCoordinate = minX / (float)texture.Width;
                afs.RightCoordinate = maxX / (float)texture.Width;
                afs.TopCoordinate = minY / (float)texture.Height;
                afs.BottomCoordinate = maxY / (float)texture.Height;

                afs.FrameLength = .1f; // default to .1 seconds.  


                chain.Frames.Add(afs);

                AppCommands.Self.RefreshTreeNode(chain);

                SelectedState.Self.SelectedFrame = afs;

                AnimationChainChange?.Invoke(this, null);
            }


        }


        private void SetCurrentFrameFromMagicWand(int minX, int minY, int maxX, int maxY)
        {
            // Selection found!

            AnimationFrameSave frame = SelectedState.Self.SelectedFrame;

            Texture2D texture = GetTextureForFrame(frame);

            if(mControl.RectangleSelector != null)
            {
                mControl.RectangleSelector.Visible = texture != null;
            }

            this.RefreshAll();

            if (texture != null)
            {
                mControl.RectangleSelector.Left = minX;
                mControl.RectangleSelector.Top = minY;

                // We add 1 because if the min and max X are equal, that means we'd
                // have a 1x1 pixel area, and the rectangle's width would need to be 1.
                mControl.RectangleSelector.Width = maxX - minX;
                mControl.RectangleSelector.Height = maxY - minY;

                HandleRegionChanged(null, null);
            }
        }

        private void RecordCameraPosition()
        {
            string fileName = null;

            fileName = WireframeEditControlsViewModel.LastSelectedTexturePath?.Standardized;
            
            if(fileName != null)
            {
                CameraPositionsForTexture[fileName] = new Vector3(mManagers.Renderer.Camera.Position, 0);

            }
        }

        private void UpdateToSavedCameraPosition()
        {
            if (WireframeEditControlsViewModel.SelectedTextureFilePath != null && CameraPositionsForTexture.ContainsKey(WireframeEditControlsViewModel.SelectedTextureFilePath))
            {
                var vector3 = CameraPositionsForTexture[WireframeEditControlsViewModel.SelectedTextureFilePath];

                mManagers.Renderer.Camera.Position = new Vector2(vector3.X, vector3.Y);
            }
            else
            {
                mManagers.Renderer.Camera.Position = new Vector2();
            }
        }

        private void TryHandleSpriteSheetClicking()
        {
            var frame = SelectedState.Self.SelectedFrame;
            var tileInformation = SelectedState.Self.SelectedTileMapInformation;
            if (PropertyGridManager.Self.UnitType == UnitType.SpriteSheet &&
                mControl.CurrentTexture != null &&
                tileInformation?.TileWidth != 0 &&
                tileInformation?.TileHeight != 0 &&
                frame != null
                )
            {
                float worldX = mControl.XnaCursor.GetWorldX(mManagers);
                float worldY = mControl.XnaCursor.GetWorldY(mManagers);

                if (worldX > 0 && worldX < mControl.CurrentTexture.Width &&
                    worldY > 0 && worldY < mControl.CurrentTexture.Height)
                {

                    bool isCtrlDown =
                        keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) ||
                        keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl);


                    int xIndex = (int)(worldX / tileInformation.TileWidth);
                    int yIndex = (int)(worldY / tileInformation.TileHeight);

                    if(isCtrlDown)
                    {
                        var textureWidth = mControl.CurrentTexture.Width;
                        var textureHeight = mControl.CurrentTexture.Height;

                        int pixelPositionX = xIndex * tileInformation.TileWidth;
                        var leftCoordinate = pixelPositionX;
                        var rightCoordinate = leftCoordinate + tileInformation.TileWidth;

                        int pixelPositionY = yIndex * tileInformation.TileHeight;
                        var topCoordinate = pixelPositionY;
                        var bottomCoordinate = topCoordinate + tileInformation.TileHeight;

                        CreateNewFrameFromTextureCoordinates(leftCoordinate, topCoordinate, rightCoordinate, bottomCoordinate);
                    }
                    else
                    {
                        PropertyGridManager.Self.SetTileX(frame, xIndex);
                        PropertyGridManager.Self.SetTileY(frame, yIndex);
                    }

                    this.RefreshAll();
                }
            }
        }


        #endregion

        public void Initialize(ImageRegionSelectionControl control, SystemManagers managers, 
            WireframeEditControls wireframeControl, WireframeEditControlsViewModel wireframeEditControlsViewModel)
        {
            var addCursor = new System.Windows.Forms.Cursor(this.GetType(), "Content.AddCursor.cur");

            WinformsCursorManager.Self.Initialize(addCursor);

            mManagers = managers;
            mManagers.Renderer.SamplerState = SamplerState.PointClamp;

            mControl = control;
            mControl.NewSelectorCreated += HandleNewSelectorCreated;

            keyboard = new Keyboard();
            keyboard.Initialize(control);


            mManagers.Renderer.Camera.CameraCenterOnScreen = CameraCenterOnScreen.Center;

            mWireframeControl = wireframeControl;

            mControl.RegionChanged += HandleRegionChanged;

            mControl.MouseWheelZoom += new EventHandler(HandleMouseWheelZoom);
            mControl.AvailableZoomLevels = mWireframeControl.AvailableZoomLevels;
            mControl.Click += HandleImageRegionSelectionControlClick;

            mControl.XnaUpdate += new Action(HandleXnaUpdate);
            mControl.Panning += HandlePanning;

            mSpriteOutline = new LineRectangle(managers);
            managers.ShapeManager.Add(mSpriteOutline);
            mSpriteOutline.Visible = false;
            mSpriteOutline.Color = OutlineColor;

            selectionPreviewRectangle = new LineRectangle(managers);
            managers.ShapeManager.Add(selectionPreviewRectangle);
            selectionPreviewRectangle.Visible = false;
            selectionPreviewRectangle.Color = MagicWandPreviewColor;
            // Move them up one Z to put them above the sprites:
            selectionPreviewRectangle.Z = 1;

            mLineGrid = new LineGrid(managers);
            mLineGrid.Name = "MainWireframeManager LineGrid";
            managers.ShapeManager.Add(mLineGrid);
            mLineGrid.Visible = false;
            mLineGrid.Color = LineGridColor;
            mLineGrid.Z = -1;
            mControl.Click += new EventHandler(HandleClick);

            mStatusText = new StatusTextController(managers);
            mControl_XnaInitialize();

            WireframeEditControlsViewModel = wireframeEditControlsViewModel;
            WireframeEditControlsViewModel.PropertyChanged += HandleWireframePropertyChanged;
        }

        void HandleImageRegionSelectionControlClick(object sender, EventArgs e)
        {
            mControl.Focus();
        }

        private void HandleNewSelectorCreated(RectangleSelector newSelector)
        {


            newSelector.Pushed += HandleSelectorPushed;
        }

        private void HandleSelectorPushed(object sender, EventArgs e)
        {
            this.mPushedRegion = sender as RectangleSelector;
        }

        private void HandleWireframePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(WireframeEditControlsViewModel.SelectedTextureFilePath):

                    UpdateSelectedFrameToSelectedTexture();

                    break;
                case nameof(WireframeEditControlsViewModel.IsMagicWandSelected):
                    ReactToMagicWandChange(this, null);
                    break;
                case nameof(WireframeEditControlsViewModel.IsSnapToGridChecked):
                case nameof(WireframeEditControlsViewModel.GridSize):
                    ReactToSnapToGridChecedChange();

                    break;
            }
        }

        public void UpdateSelectedFrameToSelectedTexture()
        {
            RecordCameraPosition();

            // update the texture before updating the frame (and calling refresh)
            UpdateToSelectedAnimationTextureFile(WireframeEditControlsViewModel.SelectedTextureFilePath);

            UpdateToSavedCameraPosition();

            if (SelectedState.Self.SelectedFrame != null && WireframeEditControlsViewModel.SelectedTextureFilePath != null)
            {
                var achxFolder = FileManager.GetDirectory(SelectedState.Self.SelectedChain.Name);
                achxFolder = FileManager.GetDirectory(ProjectManager.Self.FileName);

                string relativeFileName = FileManager.MakeRelative(WireframeEditControlsViewModel.SelectedTextureFilePath.FullPath, achxFolder);

                SelectedState.Self.SelectedFrame.TextureName = relativeFileName;

                AnimationFrameChange?.Invoke(this, null);

                PropertyGridManager.Self.Refresh();

                RefreshAll();

                TreeViewManager.Self.RefreshTreeNode(SelectedState.Self.SelectedFrame);
            }
        }

        private void HandlePanning()
        {
            ApplicationEvents.Self.CallAfterWireframePanning();
        }

        void ReactToMagicWandChange(object sender, EventArgs e)
        {
            if (SelectedState.Self.SelectedFrame != null)
            {
                UpdateHandlesAndMoveCursor();
            }

            if (WireframeEditControlsViewModel.IsMagicWandSelected)
            {
                mControl.Focus();
            }
        }

        private void ReactToSnapToGridChecedChange()
        {
            if(WireframeEditControlsViewModel.IsSnapToGridChecked)
            {
                mControl.Focus();
            }


            if (WireframeEditControlsViewModel.IsSnapToGridChecked)
            {
                mControl.SnappingGridSize = WireframeEditControlsViewModel.GridSize;
            }
            else
            {
                mControl.SnappingGridSize = null;
            }

            UpdateLineGridToTexture(Texture);
        }

        void mControl_XnaInitialize()
        {
            RefreshAll();

        }

        void HandleXnaUpdate()
        {
            keyboard.Activity();
            mStatusText.AdjustTextSize();

            TimeManager.Self.Activity();

            PerformMagicWandPreviewLogic();

            PerformSnapToGridPreviewLogic();

            if (mControl.XnaCursor.IsInWindow)
            {
                PerformCursorUpdateLogic();


                StatusBarManager.Self.SetCursorPosition(
                    mControl.XnaCursor.GetWorldX(mManagers),
                    mControl.XnaCursor.GetWorldY(mManagers));
            }
        }

        private void PerformSnapToGridPreviewLogic()
        {
            bool isCtrlDown =
                keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) ||
                keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl);

            // CTRL + Click adds frame when snap to grid is on, but only then do we show the outline
            if (isCtrlDown &&
                WireframeEditControlsViewModel.IsSnapToGridChecked && 
                mControl.XnaCursor.IsInWindow && mControl.CurrentTexture != null &&
                WireframeEditControlsViewModel.GridSize > 0)
            {
                float worldX = mControl.XnaCursor.GetWorldX(mManagers);
                float worldY = mControl.XnaCursor.GetWorldY(mManagers);

                var gridSize = WireframeEditControlsViewModel.GridSize;

                var minX =  gridSize * ( ((int)worldX) / gridSize);
                var minY = gridSize * ( ((int)worldY) / gridSize);

                selectionPreviewRectangle.Visible = true;
                selectionPreviewRectangle.X = minX;
                selectionPreviewRectangle.Y = minY;
                selectionPreviewRectangle.Width = gridSize;
                selectionPreviewRectangle.Height = gridSize;
            }
        }

        double lastUpdate;

        private void PerformMagicWandPreviewLogic()
        {
            if(WireframeEditControlsViewModel.IsMagicWandSelected && mControl.XnaCursor.IsInWindow && mControl.CurrentTexture != null)
            {
                var timeSinceLastUpdate = TimeManager.Self.CurrentTime - lastUpdate;
                const double UpateFrequency = 1;

                float worldX = mControl.XnaCursor.GetWorldX(mManagers);
                float worldY = mControl.XnaCursor.GetWorldY(mManagers);

                var isOutsideOfPreview = selectionPreviewRectangle.Visible == false ||
                    worldX < selectionPreviewRectangle.GetAbsoluteLeft() ||
                    worldX > selectionPreviewRectangle.GetAbsoluteRight() ||
                    worldY < selectionPreviewRectangle.GetAbsoluteTop() ||
                    worldY > selectionPreviewRectangle.GetAbsoluteBottom();

                if(timeSinceLastUpdate > UpateFrequency || isOutsideOfPreview)
                {

                    int pixelX = (int)worldX;
                    int pixelY = (int)worldY;

                    int minX, minY, maxX, maxY;

                    this.mInspectableTexture.GetOpaqueWandBounds(pixelX, pixelY, out minX, out minY, out maxX, out maxY);

                    if(minX != maxX && minY != maxY)
                    {
                        lastUpdate = TimeManager.Self.CurrentTime;
                    }

                    selectionPreviewRectangle.Visible = true;
                    selectionPreviewRectangle.X = minX;
                    selectionPreviewRectangle.Y = minY;
                    selectionPreviewRectangle.Width = maxX - minX;
                    selectionPreviewRectangle.Height = maxY - minY;

                }
            }
            else
            {
                selectionPreviewRectangle.Visible = false;
            }
        }

        private void PerformCursorUpdateLogic()
        {
            var cursorToAssign = WinformsCursorManager.Self.PerformCursorUpdateLogic(
                keyboard, mControl.XnaCursor, WireframeEditControlsViewModel, mControl.RectangleSelectors);

            if (System.Windows.Forms.Cursor.Current != cursorToAssign)
            {
                System.Windows.Forms.Cursor.Current = cursorToAssign;
                mControl.Cursor = cursorToAssign;
            }
        }

        void HandleRegionChanged(object sender, EventArgs e)
        {
            // This can get raised multiple times if the cursor is over multiple rectangle selectors.
            // This can happen if multiple frames overlap - such as if the user selects two AnimationChains
            // which use the same textures, but which are flipped:
            // Walk Right
            // Walk Left
            // If we run this code for every instance that the cursor is over, objects may get moved multiple
            // times per frame. Therefore, let's only do this for the one grabbed instance:
            // Update March 24, 2023
            // When creating a new animation through the magic wand, the send is null. In that case sender doesn't
            // equal the mPushedRegion, but we'll allow null to pass through (we won't early out).
            ///////////////////////////Early Out//////////////////////////
            if(sender != null && sender != mPushedRegion)
            {
                return;
            }
            /////////////////////////End Early Out////////////////////////

            var senderRectangleSelector = sender as RectangleSelector;

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
            else if (SelectedState.Self.SelectedChains?.Count > 0 || SelectedState.Self.SelectedChain != null)
            {
                if (mPushedRegion != null)
                {
                    Texture2D texture = mControl.CurrentTexture;

                    int changedLeft = FlatRedBall.Math.MathFunctions.RoundToInt( mPushedRegion.Left - mPushedRegion.OldLeft);
                    int changedTop = FlatRedBall.Math.MathFunctions.RoundToInt( mPushedRegion.Top - mPushedRegion.OldTop);
                    int changedBottom = FlatRedBall.Math.MathFunctions.RoundToInt( mPushedRegion.Bottom - mPushedRegion.OldBottom);
                    int changedRight = FlatRedBall.Math.MathFunctions.RoundToInt(mPushedRegion.Right - mPushedRegion.OldRight);

                    if(changedLeft != 0 || changedTop != 0 || changedRight != 0 || changedBottom != 0)
                    {
                        // only move the regions that are shown
                        foreach(var region in mControl.RectangleSelectors)
                        {
                            var frameForRegion = region.Tag as AnimationFrameSave;

                            frameForRegion.LeftCoordinate += changedLeft / (float)texture.Width;
                            frameForRegion.RightCoordinate += changedRight / (float)texture.Width;

                            frameForRegion.TopCoordinate += changedTop / (float)texture.Height;
                            frameForRegion.BottomCoordinate += changedBottom / (float)texture.Height;
                        }

                        UpdateSelectorsToAnimation(skipUpdatingRectangleSelector:true, texture:texture);
                        PreviewManager.Self.ReactToAnimationChainSelected();
                    }
                }
            }
            // This is causing spamming of the save - we only want to do this on a mouse click
            //if (AnimationChainChange != null)
            //{
            //    AnimationChainChange(this, null); b
            //}
            AnimationFrameChange?.Invoke(this, null);
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

                UpdateHandlesAndMoveCursor();
            }

            else
            {
                UpdateToSelectedAnimation();
            }
            mStatusText.UpdateText();
        }


        internal void FocusSelectionIfOffScreen()
        {
            if(this.mControl.RectangleSelector != null)
            {
                bool isSelectionOnScreen = GetIfSelectionIsOnScreen();

                if (isSelectionOnScreen == false)
                {
                    FocusOnSelection();
                }
            }
        }

        private void FocusOnSelection()
        {
            var camera = mManagers.Renderer.Camera;
            var selector = this.mControl.RectangleSelector;

            camera.AbsoluteLeft = selector.Left - 20;
            camera.AbsoluteTop = selector.Top - 20;
        }

        private bool GetIfSelectionIsOnScreen()
        {
            var camera = mManagers.Renderer.Camera;

            var selector = this.mControl.RectangleSelector;

            bool isOnScreen = selector.Right > camera.AbsoluteLeft &&
                selector.Left < camera.AbsoluteRight &&
                selector.Bottom > camera.AbsoluteTop &&
                selector.Top < camera.AbsoluteBottom;

            return isOnScreen;
        }

        private void UpdateHandlesAndMoveCursor()
        {
            if(this.mControl.RectangleSelector != null)
            {
                if (PropertyGridManager.Self.UnitType == UnitType.SpriteSheet || WireframeEditControlsViewModel.IsMagicWandSelected)
                {
                    this.mControl.RectangleSelector.ShowHandles = false;
                    this.mControl.RectangleSelector.ShowMoveCursorWhenOver = false;
                }
                else
                {
                    this.mControl.RectangleSelector.ShowHandles = true;
                    this.mControl.RectangleSelector.ShowMoveCursorWhenOver = true;
                }
            }
        }

        private void UpdateToSelectedAnimation(bool skipPushed = false)
        {
            PopulateViewModelTextures();

            if (mControl.RectangleSelector != null)
            {
                mControl.RectangleSelector.Visible = false;
            }

            var selectedFilePath = WireframeEditControlsViewModel?.SelectedTextureFilePath;

            UpdateToSelectedAnimationTextureFile(selectedFilePath, skipPushed);

        }

        private void UpdateToSelectedAnimationTextureFile(ToolsUtilities.FilePath selectedFilePath, bool skipPushed = false)
        {
            var fileName = selectedFilePath?.Standardized;

            if (!string.IsNullOrEmpty(fileName))
            {
                Texture2D texture = GetTextureFromFile(fileName);
                Texture = texture;

                ShowSpriteOutlineForTexture(Texture);
                UpdateLineGridToTexture(Texture);
                
                string folder = null;
                bool doAnyFramesUseThisTexture = false;

                if (SelectedState.Self.AnimationChainListSave != null && SelectedState.Self.SelectedChain != null) 
                {
                    if(string.IsNullOrEmpty(SelectedState.Self.AnimationChainListSave.FileName))
                    {
                        throw new InvalidOperationException("The current AnimationChainListSave has an empty file name." +
                            "This should not happen, since the AnimationEditor should force saving the file before getting to this point.");
                    }

                    folder = FlatRedBall.IO.FileManager.GetDirectory(SelectedState.Self.AnimationChainListSave.FileName);

                    doAnyFramesUseThisTexture =
                        SelectedState.Self.SelectedChain.Frames.Any(item => new ToolsUtilities.FilePath(folder + item.TextureName) == selectedFilePath);
                }

                if (doAnyFramesUseThisTexture && texture != null)
                {
                    UpdateSelectorsToAnimation(skipPushed, texture);
                }
                else
                {
                    mControl.DesiredSelectorCount = 0;
                }
            }
            else
            {
                Texture = null;
                mControl.DesiredSelectorCount = 0;

                ShowSpriteOutlineForTexture(Texture);
                UpdateLineGridToTexture(Texture);
            }

            // Do we need to check if it's changed?
            //if (Texture != textureBefore)
            {
                ApplicationEvents.Self.CallWireframeTextureChange();
            }
        }

        private void PopulateViewModelTextures()
        {
            // It may not have yet been initialized...
            WireframeEditControlsViewModel?.AvailableTextures.Clear();


            if(SelectedState.Self.AnimationChainListSave?.FileName != null)
            {
                string folder = FlatRedBall.IO.FileManager.GetDirectory(SelectedState.Self.AnimationChainListSave.FileName);
                var animationsSelectedFirst = SelectedState.Self.AnimationChainListSave.AnimationChains
                    .OrderBy(item => item != SelectedState.Self.SelectedChain);

                var frames = animationsSelectedFirst.SelectMany(item => item.Frames);

                var filePaths = frames
                    .Where(item => !string.IsNullOrEmpty(item.TextureName))
                    .Select(item => new ToolsUtilities.FilePath(folder + item.TextureName))
                    .Union(ProjectManager.Self.ReferencedPngs)
                    .Distinct()
                    .ToList();

                foreach(var filePath in filePaths)
                {
                    WireframeEditControlsViewModel.AvailableTextures.Add(filePath);
                }

                WireframeEditControlsViewModel.SelectedTextureFilePath = filePaths.FirstOrDefault();
            }
        }

        private void UpdateSelectorsToAnimation(bool skipUpdatingRectangleSelector, Texture2D texture)
        {
            string folder = FlatRedBall.IO.FileManager.GetDirectory(SelectedState.Self.AnimationChainListSave.FileName);
            var textureFilePath = new ToolsUtilities.FilePath(texture.Name);

            var framesOnThisTexture = SelectedState.Self.SelectedChains
                .SelectMany(item => item.Frames)
                .Where(item => new ToolsUtilities.FilePath(folder + item.TextureName) == textureFilePath)
                .ToList();

            mControl.DesiredSelectorCount = framesOnThisTexture.Count;

            foreach(var selector in mControl.RectangleSelectors)
            {
                // We'll do it ourselves, to consider hotkeys
                selector.AutoSetsCursor = false;
            }

            for (int i = 0; i < framesOnThisTexture.Count; i++)
            {
                var frame = framesOnThisTexture[i];

                var rectangleSelector = mControl.RectangleSelectors[i];
                if (skipUpdatingRectangleSelector == false || rectangleSelector != mPushedRegion)
                {
                    bool hasAlreadyBeenInitialized = rectangleSelector.Tag != null;
                    UpdateRectangleSelectorToFrame(frame, texture, mControl.RectangleSelectors[i]);
                    rectangleSelector.ShowHandles = false;

                    rectangleSelector.Tag = frame;

                    if (!hasAlreadyBeenInitialized)
                    {

                        rectangleSelector.ShowHandles = false;
                        rectangleSelector.RoundToUnitCoordinates = true;
                        rectangleSelector.AllowMoveWithoutHandles = true;
                        // Only the first cursor will reset back to the arrow, otherwise the others shouldn't
                        rectangleSelector.ResetsCursorIfNotOver = i == 0;
                    }
                }
            }
        }

        private void UpdateToSelectedFrame()
        {
            AnimationFrameSave frame = SelectedState.Self.SelectedFrame;

            Texture2D texture = GetTextureForFrame(frame);
            Texture2D textureBefore = Texture;
            Texture = texture;


            if (texture != null)
            {
                
                mControl.DesiredSelectorCount = 1;
                foreach (var selector in mControl.RectangleSelectors)
                {
                    // We'll do it ourselves, to consider hotkeys
                    selector.AutoSetsCursor = false;
                }

                var rectangleSelector = mControl.RectangleSelector;
                UpdateRectangleSelectorToFrame(frame, texture, rectangleSelector);

                ShowSpriteOutlineForTexture(texture);

                this.WireframeEditControlsViewModel.SelectedTextureFilePath = GetTextureFileNameForFrame(frame).Standardized;

            }
            else
            {
                mControl.DesiredSelectorCount = 0;

            }

            UpdateLineGridToTexture(texture);

            
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
            if(AppState.Self.IsSnapToGridChecked)
            {
                rowWidth = AppState.Self.GridSize;
                columnWidth = AppState.Self.GridSize;
            }
            else if (SelectedState.Self.SelectedTileMapInformation != null)
            {
                TileMapInformation tmi = SelectedState.Self.SelectedTileMapInformation;
                rowWidth = tmi.TileHeight;
                columnWidth = tmi.TileWidth;
            }


            if (texture != null && rowWidth != 0 && columnWidth != 0)
            {
                mLineGrid.Visible = true;
                mLineGrid.ColumnWidth = columnWidth;
                mLineGrid.RowWidth = rowWidth;
                mLineGrid.Name = "Main wireframe LineGrid";
                mLineGrid.ColumnCount = (int)(texture.Width / columnWidth);
                mLineGrid.RowCount = (int)(texture.Height / rowWidth);
                // This puts it in front of everything - maybe eventually we want to use layers?
                //mLineGrid.Z = 1;
                // no no, go behind
                mLineGrid.Z = -1;
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

        public FilePath GetTextureFileNameForFrame(AnimationFrameSave frame)
        {
            FilePath returnValue = null;
            if (ProjectManager.Self.AnimationChainListSave != null)
            {
                string achxFolder = FlatRedBall.IO.FileManager.GetDirectory(ProjectManager.Self.FileName);

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
            var fileName = GetTextureFileNameForFrame(frame);

            return GetTextureFromFile(fileName);
        }

        private static Texture2D GetTextureFromFile(FilePath filePath)
        {
            Texture2D texture = null;
            if (filePath != null && System.IO.File.Exists(filePath.FullPath))
            {
                texture = LoaderManager.Self.LoadContent<Texture2D>(filePath.FullPath);
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
