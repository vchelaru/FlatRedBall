using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XnaAndWinforms;
using System.Drawing;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using FlatRedBall.SpecializedXnaControls.RegionSelection;
using RenderingLibrary.Math.Geometry;
using RenderingLibrary;
using FlatRedBall.SpecializedXnaControls.Input;
using RenderingLibrary.Content;
using ToolsUtilities;
using System.Reflection;
using System.Windows.Forms;
using RenderingLibrary.Math;
using InputLibrary;

namespace FlatRedBall.SpecializedXnaControls
{
    public class ImageRegionSelectionControl : GraphicsDeviceControl
    {
        #region Fields

        ImageData maxAlphaImageData;

        Texture2D mCurrentTexture;

        bool mRoundRectangleSelectorToUnit = true;
        List<RectangleSelector> mRectangleSelectors = new List<RectangleSelector>();

        CameraPanningLogic mCameraPanningLogic;

        InputLibrary.Cursor mCursor;
        InputLibrary.Keyboard mKeyboard;
        SystemManagers mManagers;

        TimeManager mTimeManager;

        Sprite mCurrentTextureSprite;

        public Zooming.ZoomNumbers ZoomNumbers
        {
            get;
            private set;
        }

        IList<int> mAvailableZoomLevels;

        #endregion

        #region Properties

        public bool RoundRectangleSelectorToUnit
        {
            get { return mRoundRectangleSelectorToUnit; }
            set
            {
                mRoundRectangleSelectorToUnit = value;

                foreach (var item in mRectangleSelectors)
                {
                    item.RoundToUnitCoordinates = mRoundRectangleSelectorToUnit;
                }
            }
        }

        int? snappingGridSize;
        public int? SnappingGridSize
        {
            get
            {
                return snappingGridSize;
            }
            set
            {
                snappingGridSize = value;
                foreach (var item in mRectangleSelectors)
                {
                    item.SnappingGridSize = snappingGridSize;
                }
            }
        }

        Camera Camera
        {
            get
            {
                return mManagers.Renderer.Camera;
            }
        }

        public SystemManagers SystemManagers
        {
            get { return mManagers; }
        }

        public RectangleSelector RectangleSelector
        {
            get 
            {
                if (mRectangleSelectors.Count != 0)
                {
                    return mRectangleSelectors[0];
                }
                else
                {
                    return null;
                }
            }
        }

        public List<RectangleSelector> RectangleSelectors
        {
            get
            {
                return mRectangleSelectors;
            }
        }

        public Texture2D CurrentTexture
        {
            get { return mCurrentTexture; }
            set
            {
                bool didChange = mCurrentTexture != value;

                if (didChange)
                {
                    mCurrentTexture = value;
                    if (mManagers != null)
                    {
                        bool hasCreateVisuals = mCurrentTextureSprite != null;

                        if (!hasCreateVisuals)
                        {
                            CreateVisuals();
                        }
                        if (mCurrentTexture == null)
                        {
                            mCurrentTextureSprite.Visible = false;
                        }
                        else
                        {
                            mCurrentTextureSprite.Visible = true;
                            mCurrentTextureSprite.Texture = mCurrentTexture;
                            mCurrentTextureSprite.Width = mCurrentTexture.Width;
                            mCurrentTextureSprite.Height = mCurrentTexture.Height;

                        }
                        this.RefreshDisplay();
                    }
                }
            }
        }

        private void CreateVisuals()
        {
            mCurrentTextureSprite = new Sprite(mCurrentTexture);
            mCurrentTextureSprite.Name = "Image Region Selection Main Sprite";
            mManagers.SpriteManager.Add(mCurrentTextureSprite);
        }

        public InputLibrary.Cursor XnaCursor
        {
            get { return mCursor; }
        }

        public bool SelectorVisible
        {
            get
            {
                return mRectangleSelectors.Count != 0 && mRectangleSelectors[0].Visible;
            }
            set
            {
                // This causes problems in VS designer mode.
                if (mRectangleSelectors != null)
                {
                    foreach (var selector in mRectangleSelectors)
                    {
                        selector.Visible = value;
                    }
                }
            }
        }

        public int ZoomValue
        {
            get
            {
                return MathFunctions.RoundToInt(mManagers.Renderer.Camera.Zoom * 100);
            }
            set
            {
                if (mManagers != null && mManagers.Renderer != null)
                {
                    mManagers.Renderer.Camera.Zoom = value / 100.0f;
                }
            }
        }

        public IList<int> AvailableZoomLevels
        {
            set
            {
                mAvailableZoomLevels = value;
            }
        }

        public int ZoomIndex
        {
            get
            {
                if (mAvailableZoomLevels != null)
                {
                    return mAvailableZoomLevels.IndexOf(ZoomValue);
                }
                return -1;
            }
        }

        /// <summary>
        /// Creates and destroys the internal rectangle selectors to match the desired count.
        /// </summary>
        public int DesiredSelectorCount
        {
            set
            {
                while (value > this.mRectangleSelectors.Count)
                {
                    CreateNewSelector();
                }

                while (value < this.mRectangleSelectors.Count)
                {
                    var selector = mRectangleSelectors.Last();

                    selector.RemoveFromManagers();
                    mRectangleSelectors.RemoveAt(mRectangleSelectors.Count - 1);
                }
            }
        }

        #endregion

        #region Events

        public new event EventHandler RegionChanged;
        public event EventHandler EndRegionChanged;

        public event EventHandler MouseWheelZoom;
        public event Action Panning;
        #endregion

        #region Methods

        protected override void Initialize()
        {
            CustomInitialize();

            base.Initialize();

        }

        public void CreateDefaultZoomLevels()
        {

        }

        public void CustomInitialize()
        {
            if (!DesignMode)
            {
                mTimeManager = new TimeManager();


                mManagers = new SystemManagers();
                mManagers.Initialize(GraphicsDevice);
                mManagers.Name = "Image Region Selection";
                Assembly assembly = Assembly.GetAssembly(typeof(GraphicsDeviceControl));// Assembly.GetCallingAssembly();

                string targetFntFileName = FileManager.UserApplicationDataForThisApplication + "Font18Arial.fnt";
                string targetPngFileName = FileManager.UserApplicationDataForThisApplication + "Font18Arial_0.png";
                FileManager.SaveEmbeddedResource(
                    assembly,
                    "XnaAndWinforms.Content.Font18Arial.fnt",
                    targetFntFileName);

                FileManager.SaveEmbeddedResource(
                    assembly,
                    "XnaAndWinforms.Content.Font18Arial_0.png",
                    targetPngFileName);



                var contentLoader = new ContentLoader();
                contentLoader.SystemManagers = mManagers;

                LoaderManager.Self.ContentLoader = contentLoader;
                LoaderManager.Self.Initialize("Content/InvalidTexture.png", targetFntFileName, Services, mManagers);

                CreateNewSelector();

                mCursor = new InputLibrary.Cursor();
                mCursor.Initialize(this);

                mKeyboard = new InputLibrary.Keyboard();
                mKeyboard.Initialize(this);

                mCameraPanningLogic = new CameraPanningLogic(this, mManagers, mCursor, mKeyboard);
                mCameraPanningLogic.Panning += HandlePanning;



                MouseWheel += new MouseEventHandler(MouseWheelRegion);
                ZoomNumbers = new Zooming.ZoomNumbers();
            }
        }

        private RegionSelection.RectangleSelector CreateNewSelector()
        {
            var newSelector = new RectangleSelector(mManagers);
            newSelector.AddToManagers(mManagers);
            newSelector.Visible = false;
            newSelector.RegionChanged += (not, used) => RegionChanged?.Invoke(newSelector, null);
            newSelector.EndRegionChanged += (not, used) => EndRegionChanged?.Invoke(newSelector, null); 
            newSelector.SnappingGridSize = snappingGridSize;
            newSelector.RoundToUnitCoordinates = mRoundRectangleSelectorToUnit;

            mRectangleSelectors.Add(newSelector);

            return newSelector;
        }

        private void HandlePanning()
        {
            if (Panning != null)
            {
                Panning();
            }
        }


        void PerformActivity()
        {
            mTimeManager.Activity();

            mCursor.Activity(mTimeManager.CurrentTime);
            mKeyboard.Activity();


            foreach (var item in mRectangleSelectors)
            {
                item.Activity(mCursor, mKeyboard, this);
            }
        }

        protected override void Draw()
        {
            
            this.PerformActivity();

            base.Draw();



            mManagers.Renderer.Draw(mManagers);
        }

        void MouseWheelRegion(object sender, MouseEventArgs e)
        {
            if (mAvailableZoomLevels != null)
            {
                int index = ZoomIndex;
                if (index != -1)
                {
                    float value = e.Delta;

                    float worldX = mCursor.GetWorldX(mManagers);
                    float worldY = mCursor.GetWorldY(mManagers);

                    float oldCameraX = Camera.X;
                    float oldCameraY = Camera.Y;

                    float oldZoom = ZoomValue / 100.0f;

                    bool didZoom = false;

                    if (value < 0 && index < mAvailableZoomLevels.Count - 1)
                    {
                        ZoomValue = mAvailableZoomLevels[ index + 1];

                        didZoom = true;
                    }
                    else if (value > 0 && index > 0)
                    {
                        ZoomValue = mAvailableZoomLevels[ index - 1];

                        didZoom = true;
                    }


                    if (didZoom)
                    {
                        AdjustCameraPositionAfterZoom(worldX, worldY, 
                            oldCameraX, oldCameraY, oldZoom, ZoomValue, Camera);

                        if (MouseWheelZoom != null)
                        {
                            MouseWheelZoom(this, null);
                        }
                    }
                }
            }
        }

        public static void AdjustCameraPositionAfterZoom(float oldCursorWorldX, float oldCursorWorldY, 
            float oldCameraX, float oldCameraY, float oldZoom, float newZoom, Camera camera)
        {
            float differenceX = oldCameraX - oldCursorWorldX;
            float differenceY = oldCameraY - oldCursorWorldY;

            float zoomAsFloat = newZoom / 100.0f;

            float modifiedDifferenceX = differenceX * oldZoom / zoomAsFloat;
            float modifiedDifferenceY = differenceY * oldZoom / zoomAsFloat;

            camera.X = oldCursorWorldX + modifiedDifferenceX;
            camera.Y = oldCursorWorldY + modifiedDifferenceY;

            // This makes the zooming behavior feel weird.  We'll do this when the user selects a new 
            // AnimationChain, but not when zooming.
            //BringSpriteInView();
        }

        public void BringSpriteInView()
        {
            if (mCurrentTexture != null)
            {
                if(Camera.CameraCenterOnScreen == CameraCenterOnScreen.TopLeft)
                {
                    var minX = -Camera.ClientWidth / 2.0f;
                    var maxX = mCurrentTexture.Width - Camera.ClientWidth / 2.0f;

                    var minY = -Camera.ClientHeight / 2.0f;
                    var maxY = mCurrentTexture.Height - Camera.ClientHeight / 2.0f;

                    Camera.X = Math.Max(minX, Camera.X);
                    Camera.X = Math.Min(maxX, Camera.X);

                    Camera.Y = Math.Max(minY, Camera.Y);
                    Camera.Y = Math.Min(maxY, Camera.Y);
                }
                else
                {
                    var minX = -Camera.ClientWidth / 2.0f;
                    var maxX = mCurrentTexture.Width + Camera.ClientWidth / 2.0f;

                    var minY = -Camera.ClientHeight / 2.0f;
                    var maxY = mCurrentTexture.Height + Camera.ClientHeight / 2.0f;

                    Camera.X = Math.Max(minX, Camera.X);
                    Camera.X = Math.Min(maxX, Camera.X);

                    Camera.Y = Math.Max(minY, Camera.Y);
                    Camera.Y = Math.Min(maxY, Camera.Y);
                }
            }
        }

        #endregion

    }
}
