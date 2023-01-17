using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary;
using XnaAndWinforms;
using RenderingLibrary.Math.Geometry;
using FlatRedBall.Content.AnimationChain;
using RenderingLibrary.Graphics;
using FlatRedBall.AnimationEditorForms.Controls;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.AnimationEditorForms.Data;
using FlatRedBall.AnimationEditorForms.Wireframe;
using InputLibrary;
using FlatRedBall.SpecializedXnaControls.Input;
using FlatRedBall.AnimationEditorForms.CommandsAndState;
using FlatRedBall.SpecializedXnaControls;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Content.Math.Geometry;

namespace FlatRedBall.AnimationEditorForms.Preview
{
    public class PreviewManager
    {
        #region Fields

        static PreviewManager mSelf;

        PreviewControls mPreviewControls;

        CameraController cameraController;

        ShapePreviewManager shapePreviewManager;
        SystemManagers mManagers;

        GraphicsDeviceControl mControl;

        RenderingLibrary.Graphics.Sprite Sprite;
        List<RenderingLibrary.Graphics.Sprite> mOnionSkinSprites = new List<RenderingLibrary.Graphics.Sprite>();

        LineRectangle outlineRectangle;


        int mMaxWidth;
        int mMaxHeight;
        
        // store these off so that if the screen resizes, we can adjust the camera to keep the top-left static
        float mLastLeft;
        float mLastTop;

        Ruler mLeftRuler;
        Ruler mTopRuler;

        Cursor mCursor;
        Keyboard mKeyboard;

        #endregion

        #region Properties

        public SpriteAlignment SpriteAlignment
        {
            get
            {
                return mPreviewControls.SpriteAlignment;
            }
        }

        public static PreviewManager Self
        {
            get
            {
                if (mSelf == null) mSelf = new PreviewManager();
                return mSelf;
            }
        }

        public int ZoomValue
        {
            get => cameraController.ZoomValue;
            set => cameraController.ZoomValue = value;
        }

        public float OffsetMultiplier
        {
            get
            {
                return mPreviewControls.OffsetMultiplier;
            }
            set
            {
                mPreviewControls.OffsetMultiplier = value;
            }
        }

        public IEnumerable<float> HorizontalGuides
        {
            get
            {
                return mLeftRuler.GuideValues;
            }
            set
            {
                mLeftRuler.GuideValues = value;
            }
        }

        public IEnumerable<float> VerticalGuides
        {
            get
            {
                return mTopRuler.GuideValues;
            }
            set
            {
                mTopRuler.GuideValues = value;
            }
        }

        RenderingLibrary.Camera Camera
        {
            get { return mManagers.Renderer.Camera; }
        }

        #endregion

        #region Initialize

        public void Initialize(GraphicsDeviceControl graphicsDeviceControl, PreviewControls previewControls)
        {

            mPreviewControls = previewControls;
            mPreviewControls.OnionSkinVisibleChange += new EventHandler(HandleOnionSkinChange);
            mPreviewControls.SpriteAlignmentChange += new EventHandler(HandleSpriteAlignmentChange);
            mControl = graphicsDeviceControl;
            mControl.MouseWheel += new System.Windows.Forms.MouseEventHandler(HandleMouseWheel);
            HandleXnaInitialize();


        }

        void HandleXnaInitialize()
        {
            mManagers = new SystemManagers();
            mManagers.Initialize(mControl.GraphicsDevice);

            mManagers.Renderer.SamplerState = SamplerState.PointClamp;

            mManagers.Name = "Preview Window Managers";
            var shapeManager = mManagers.ShapeManager;

            Sprite = new RenderingLibrary.Graphics.Sprite(null);
            Sprite.Name = "Animation PreviewManager Main Sprite";

            outlineRectangle = new LineRectangle(mManagers);
            mManagers.ShapeManager.Add(outlineRectangle);
            // Move it in front of the Sprite
            outlineRectangle.Z = 1;

            mManagers.SpriteManager.Add(Sprite);

            mControl.Resize += new EventHandler(HandleResize);
            mControl.XnaDraw += new Action(HandleXnaDraw);
            mControl.XnaUpdate += new Action(HandleXnaUpdate);
            MoveCameraToProperLocation();

            // We'll use Cursor.Self which is initialized and updated elsewhere
            // Actually looks like that's not the case.  We'll make a new one.
            mCursor = new Cursor();
            mCursor.Initialize(mControl);

            mKeyboard = new Keyboard();
            mKeyboard.Initialize(mControl);

            mLeftRuler = new Ruler(mControl, mManagers, mCursor);
            mLeftRuler.RulerSide = RulerSide.Left;

            mTopRuler = new Ruler(mControl, mManagers, mCursor);
            mTopRuler.RulerSide = RulerSide.Top;

            mManagers.Renderer.Camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;
            mManagers.Renderer.Camera.X = -50;
            mManagers.Renderer.Camera.Y = -50;

            cameraController = new CameraController(Camera, mManagers, mCursor, mKeyboard, mControl, mTopRuler, mLeftRuler);

            shapePreviewManager = new ShapePreviewManager(mCursor, mKeyboard, mManagers);
        }

        #endregion

        #region Methods

        #region Input methods

        void HandleMouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            cameraController.HandleMouseWheel(mCursor, e.Delta, mPreviewControls);
        }



        #endregion


        void HandleSpriteAlignmentChange(object sender, EventArgs e)
        {

        }

        void HandleOnionSkinChange(object sender, EventArgs e)
        {
            UpdateOnionSkinSprites();
        }

        void HandleXnaDraw()
        {
            mManagers.SpriteManager.Activity(TimeManager.Self.CurrentTime);
            MoveSpriteAccordingToAlignmentAndOffset(Sprite, SelectedState.Self.SelectedFrame);
            UpdateOutlineRectangleToSprite();
            DoShapeUpdateActivity();
            mManagers.Renderer.Draw(mManagers);


        }



        private void DoShapeUpdateActivity()
        {
            // Event though we may not be rendering the main Sprite, we want to use the main Sprite's animation:
            var animation = Sprite.Animation;

            if (animation != null && Sprite.Animate)
            {
                int index = Sprite.Animation.CurrentFrameIndex;

                AnimationChainSave chain = SelectedState.Self.SelectedChain;

                AnimationFrameSave frame = null;

                if (chain != null && chain.Frames.Count > index)
                {
                    if (frame == null)
                    {
                        frame = chain.Frames[index];
                    }
                }

                shapePreviewManager.UpdateShapesToFrame(frame);
            }
        }

        private void UpdateOutlineRectangleToSprite()
        {
            
            outlineRectangle.Width = Sprite.EffectiveWidth;
            outlineRectangle.Height = Sprite.EffectiveHeight;
            outlineRectangle.Color = WireframeManager.Self.OutlineColor;
            outlineRectangle.Visible = Sprite.Visible;

            outlineRectangle.X = Sprite.X;

            outlineRectangle.Y = Sprite.Y;
        }

        void HandlePanning()
        {
            // Should this raise this event?
            // This isn't the wireframe panning, but
            // the preview panning
            //ApplicationEvents.Self.CallAfterWireframePanning();
        }

        void HandleXnaUpdate()
        {
            mCursor.Activity(TimeManager.Self.CurrentTime);
            mKeyboard.Activity();
            // I think we only want to do this if we're actually in the window
            // No, we want to always do it otherwise the user can't delete guides.
            // We will make checks internally
            //if (mCursor.IsInWindow)
            {
                if (mCursor.IsInWindow)
                {
                    System.Windows.Forms.Cursor cursorToSet = System.Windows.Forms.Cursors.Arrow;

                    System.Windows.Forms.Cursor.Current = cursorToSet;
                    this.mControl.Cursor = cursorToSet;




                }
                this.mLeftRuler.HandleXnaUpdate(mCursor.IsInWindow);
                this.mTopRuler.HandleXnaUpdate(mCursor.IsInWindow);
            }



            if (mCursor.IsInWindow)
            {
                StatusBarManager.Self.SetCursorPosition(
                    mCursor.GetWorldX(mManagers),
                    mCursor.GetWorldY(mManagers));

            }
            var shouldUpdate = shapePreviewManager.Update();

            if(shouldUpdate)
            {
                UpdateShapes();
            }
        }

        void HandleResize(object sender, EventArgs e)
        {
            // Now that the camera is top-left justified, no need
            // to do anything.
            //MoveCameraToProperLocation();
        }

        private void MoveCameraToProperLocation()
        {
            const float border = 45;
            float zoom = mManagers.Renderer.Camera.Zoom;

            float differenceX = mControl.Width - mManagers.Renderer.Camera.ClientWidth;
            if (differenceX != 0)
            {
                mManagers.Renderer.Camera.X += differenceX / 2.0f;
            }

            mManagers.Renderer.Camera.X = 0;
            mManagers.Renderer.Camera.Y = 0;
        }

        public void RefreshAll()
        {
            ReactToAnimationChainSelected();
            UpdateSpriteToAnimationFrame();

            UpdateShapes();
        }

        private void UpdateShapes()
        {
            var frame = AppState.Self.CurrentFrame;
            shapePreviewManager.UpdateShapesToFrame(frame);
        }

        public void ReactToAnimationChainSelected()
        {
            UpdateMaxDimensions();

            if (SelectedState.Self.SelectedChain != null && SelectedState.Self.SelectedChain.Frames.Count != 0)
            {
                Sprite.Visible = true;
                Sprite.Animate = true;
                Sprite.Animation = GetTextureFlipAnimationForAnimationChain();


            }
            else
            {
                Sprite.Visible = false;
            }

            UpdateOnionSkinSprites();
        }

        private void UpdateMaxDimensions()
        {
            mMaxWidth = 0;
            mMaxHeight = 0;
            if(SelectedState.Self.SelectedChain != null)
            {
                foreach (var frame in SelectedState.Self.SelectedChain.Frames)
                {
                    Texture2D texture = WireframeManager.Self.GetTextureForFrame(frame);
                    if (texture != null)
                    {
                        int textureWidth = texture.Width;
                        int textureHeight = texture.Height;

                        mMaxWidth = System.Math.Max(
                            Math.MathFunctions.RoundToInt((frame.RightCoordinate - frame.LeftCoordinate) * textureWidth),
                            mMaxWidth);
                        mMaxHeight = System.Math.Max(
                            Math.MathFunctions.RoundToInt((frame.BottomCoordinate - frame.TopCoordinate) * textureHeight),
                            mMaxHeight);
                    }
                }
            }


        }

        private TimedSpriteSheetAnimation GetTextureFlipAnimationForAnimationChain()
        {
            if (SelectedState.Self.SelectedChain != null)
            {

                TimedSpriteSheetAnimation tssa = new TimedSpriteSheetAnimation();

                foreach (AnimationFrameSave afs in SelectedState.Self.SelectedChain.Frames)
                {
                    AnimationFrame animationFrame = AnimationFrameSaveToRenderingLibraryAnimationFrame(afs);

                    tssa.Frames.Add(animationFrame);
                }

                return tssa;
            }
            else
            {
                return null;
            }
        }

        private AnimationFrame AnimationFrameSaveToRenderingLibraryAnimationFrame(AnimationFrameSave afs)
        {
            
            AnimationFrame af = new AnimationFrame();
            af.Texture = WireframeManager.Self.GetTextureForFrame(afs);
            
            af.FlipHorizontal = afs.FlipHorizontal;
            af.FlipVertical = afs.FlipVertical;
            af.SourceRectangle = GetSourceRetangleForFrame(afs, af.Texture);
            af.FrameTime = afs.FrameLength;
            return af;
        }

        public void ReactToAnimationFrameSelected()
        {
            if (SelectedState.Self.SelectedFrame != null)
            {
                UpdateSpriteToAnimationFrame();
                UpdateShapes();
            }
            else
            {
                // We made it null, which means the user
                // may have selected the chain that owns the frame
                ReactToAnimationChainSelected();
            }
        }

        private void UpdateSpriteToAnimationFrame()
        {
            AnimationFrameSave afs = SelectedState.Self.SelectedFrame;
            if (afs != null)
            {
                RenderingLibrary.Graphics.Sprite sprite = Sprite;
                sprite.Visible = true;
                UpdateSpriteToAnimationFrame(afs, sprite);
            }

            UpdateOnionSkinSprites();

        }

        private void UpdateSpriteToAnimationFrame(AnimationFrameSave afs, RenderingLibrary.Graphics.Sprite sprite)
        {
            sprite.Animate = false;

            sprite.Texture = WireframeManager.Self.GetTextureForFrame(afs);
            if (sprite.Texture != null)
            {
                sprite.SourceRectangle = GetSourceRetangleForFrame(afs, sprite.Texture);
                if(sprite.SourceRectangle.HasValue)
                {
                    sprite.Width = sprite.SourceRectangle.Value.Width;
                    sprite.Height = sprite.SourceRectangle.Value.Height;
                }
            }
            sprite.FlipHorizontal = afs.FlipHorizontal;
            sprite.FlipVertical = afs.FlipVertical;
            sprite.Animation = Sprite.Animation;

            MoveSpriteAccordingToAlignmentAndOffset(sprite, afs);
        }

        private void UpdateOnionSkinSprites()
        {
            bool shouldShow = SelectedState.Self.SelectedFrame != null &&
                SelectedState.Self.SelectedChain.Frames.Count > 1 &&
                mPreviewControls.IsOnionSkinVisible;

            if (shouldShow)
            {
                if (mOnionSkinSprites.Count == 0)
                {
                    RenderingLibrary.Graphics.Sprite sprite = new RenderingLibrary.Graphics.Sprite(null);
                    sprite.Color = new Microsoft.Xna.Framework.Color(1, 1, 1, .5f);
                    sprite.Z = -1;
                    mManagers.SpriteManager.Add(sprite);
                    mOnionSkinSprites.Add(sprite);
                }

                int indexToShow = SelectedState.Self.SelectedChain.Frames.IndexOf(SelectedState.Self.SelectedFrame) - 1;
                if (indexToShow == -1)
                {
                    indexToShow = SelectedState.Self.SelectedChain.Frames.Count - 1;
                }

                AnimationFrameSave frame = SelectedState.Self.SelectedChain.Frames[indexToShow];

                UpdateSpriteToAnimationFrame(frame, mOnionSkinSprites[0]);
            }
            else
            {
                while (mOnionSkinSprites.Count != 0)
                {
                    RenderingLibrary.Graphics.Sprite sprite = mOnionSkinSprites[0];
                    mManagers.SpriteManager.Remove(sprite);
                    mOnionSkinSprites.RemoveAt(0);
                }
            }

        }

        public void ReactToAnimationFrameChange()
        {
            UpdateSpriteToAnimationFrame();
        }

        private Microsoft.Xna.Framework.Rectangle? GetSourceRetangleForFrame(AnimationFrameSave afs, Microsoft.Xna.Framework.Graphics.Texture2D texture2D)
        {
            if (afs == null || texture2D == null)
            {
                return null;
            }
            else
            {
                Microsoft.Xna.Framework.Rectangle rectangle = new Microsoft.Xna.Framework.Rectangle();
                rectangle.X = Math.MathFunctions.RoundToInt(afs.LeftCoordinate * texture2D.Width);
                rectangle.Width = Math.MathFunctions.RoundToInt(afs.RightCoordinate * texture2D.Width) - rectangle.X;

                rectangle.Y = Math.MathFunctions.RoundToInt(afs.TopCoordinate * texture2D.Height);
                rectangle.Height = Math.MathFunctions.RoundToInt(afs.BottomCoordinate * texture2D.Height) - rectangle.Y;

                return rectangle;
            }
        }

        private void MoveSpriteAccordingToAlignmentAndOffset(RenderingLibrary.Graphics.Sprite sprite, AnimationFrameSave frame)
        {
            // Event though we may not be rendering the main Sprite, we want to use the main Sprite's animation:
            IAnimation animation = Sprite.Animation;

            if (sprite != null && sprite.Visible && Sprite.Animation != null)
            {
                int index = sprite.Animation.CurrentFrameIndex;

                float animationXOffset = 0;
                float animationYOffset = 0;

                AnimationChainSave chain = SelectedState.Self.SelectedChain;



                if (chain != null && chain.Frames.Count > index)
                {
                    if (frame == null)
                    {
                        frame = chain.Frames[index];
                    }

                    animationXOffset = frame.RelativeX * OffsetMultiplier;
                    animationYOffset = frame.RelativeY * OffsetMultiplier;
                }

                if (SpriteAlignment == Data.SpriteAlignment.Center)
                {
                    float xOffset = (-sprite.EffectiveWidth) / 2.0f;
                    float yOffset = (-sprite.EffectiveHeight) / 2.0f;

                    sprite.X = xOffset + animationXOffset;
                    sprite.Y = yOffset - animationYOffset;
                }
                else
                {
                    sprite.X = 0 + animationXOffset;
                    sprite.Y = 0 - animationYOffset;
                }

            }
        }

        #endregion
    }
}
