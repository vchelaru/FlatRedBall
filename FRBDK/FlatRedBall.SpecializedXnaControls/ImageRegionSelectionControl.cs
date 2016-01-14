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
                mCurrentTexture = value;
                if (mManagers != null)
                {
                    if (mCurrentTextureSprite == null)
                    {
                        mCurrentTextureSprite = new Sprite(mCurrentTexture);
                        mManagers.SpriteManager.Add(mCurrentTextureSprite);
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

                    selector.RemoveFromManagers(mManagers);
                    mRectangleSelectors.RemoveAt(mRectangleSelectors.Count - 1);
                }
            }
        }

        #endregion

        #region Events

        public event EventHandler RegionChanged;
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

        private RegionSelection.RectangleSelector CreateNewSelector()
        {
            var newSelector = new RectangleSelector(mManagers);
            newSelector.AddToManagers(mManagers);
            newSelector.Visible = false;
            newSelector.RegionChanged += new EventHandler(RegionChangedInternal);

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

        void RegionChangedInternal(object sender, EventArgs e)
        {
            if (RegionChanged != null)
            {
                RegionChanged(this, null);
            }
        }

        void Update()
        {
            mTimeManager.Activity();

            mCursor.Activity(mTimeManager.CurrentTime);
            mKeyboard.Activity();


            foreach (var item in mRectangleSelectors)
            {
                item.Activity(mCursor, mKeyboard, this, mManagers);
            }
        }

        protected override void Draw()
        {
            
            this.Update();

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
                const float pixelBorder = 10;

                bool isAbove = mCurrentTextureSprite.Y + mCurrentTexture.Height < Camera.AbsoluteTop;
                bool isBelow = mCurrentTextureSprite.Y > Camera.AbsoluteBottom;

                bool isLeft = mCurrentTextureSprite.X + mCurrentTexture.Width < Camera.AbsoluteLeft;
                bool isRight = mCurrentTextureSprite.X> Camera.AbsoluteRight;

                // If it's both above and below, that means the user has zoomed in a lot so that the Sprite is bigger than
                // the camera view.  
                // If it's neither, then the entire Sprite is in view.
                // If it's only one or the other, that means that part of the Sprite is hanging off the edge, and we can adjust.
                bool adjustY = (isAbove || isBelow) && !(isAbove && isBelow);
                bool adjustX = (isLeft || isRight) && !(isLeft && isRight);

                if (adjustY)
                {
                    bool isTallerThanCamera = mCurrentTexture.Height * Camera.Zoom > Camera.ClientHeight;

                    if ((isTallerThanCamera && isAbove) || (!isTallerThanCamera && isBelow))
                    {
                        // Move Camera so Sprite is on bottom
                        Camera.Y = mCurrentTextureSprite.Y + mCurrentTexture.Height - (Camera.ClientHeight / 2.0f - pixelBorder) / Camera.Zoom;
                    }
                    else
                    {
                        // Move Camera so Sprite is on top
                        Camera.Y = mCurrentTextureSprite.Y + (Camera.ClientHeight / 2.0f - pixelBorder) / Camera.Zoom;
                    }
                }

                if (adjustX)
                {
                    bool isWiderThanCamera = mCurrentTexture.Width * Camera.Zoom > Camera.ClientWidth;

                    if ((isWiderThanCamera && isLeft) || (!isWiderThanCamera && isRight))
                    {
                        Camera.X = mCurrentTextureSprite.X + mCurrentTexture.Width - (Camera.ClientWidth / 2.0f - pixelBorder) / Camera.Zoom;
                    }
                    else
                    {
                        Camera.X = mCurrentTextureSprite.X + (Camera.ClientWidth / 2.0f - pixelBorder) / Camera.Zoom;
                    }
                }
            }
        }

        #endregion

    }
}
