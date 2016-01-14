using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics;
using FlatRedBall.Math;
using System.Collections;
using FlatRedBall.Graphics.Animation;

namespace EditorObjects.Gui
{
    public class SpritePropertyGrid : PropertyGrid<Sprite>
    {
        #region Fields

        public static PositionedObjectList<Camera> ExtraCamerasForScale = new PositionedObjectList<Camera>();

        private ComboBox mCurrentChainNameComboBox;

        private ToggleButton mPixelPerfectTextureCoordinates;

        // Store these in case we need to round the values for pixel-perfect values
        private UpDown mTopTextureCoordinateUpDown;
        private UpDown mBottomTextureCoordinateUpDown;
        private UpDown mLeftTextureCoordinateUpDown;
        private UpDown mRightTextureCoordinateUpDown;

        private FileTextBox mAnimationChainFileTextBox;

        private ComboBox mOrderingMode;

        private Button mSetPixelPerfectScaleButton;

		SpriteList mSpriteList;

        UpDown mLeftPixel;
        UpDown mTopPixel;

        UpDown mPixelWidth;
        UpDown mPixelHeight;

        #endregion

        #region Properties

        public bool ShowWarningOnNonPowerOfTwoTexture
        {
            get;
            set;
        }

        public override Sprite SelectedObject
        {
            get
            {
                return base.SelectedObject;
            }
            set
            {
                base.SelectedObject = value;



                if (MakeVisibleOnSpriteSet)
                {
                    if (!Visible && (SelectedObject != null))
                    {
                        GuiManager.BringToFront(this);
                    }
                    Visible = (SelectedObject != null);
                }

                #region Update the Ordered GUI

                if (SelectedObject != null)
                {
                    // for performance reasons this only updates when the selected changes
                    if (SpriteManager.ZBufferedSprites.Contains(SelectedObject))
                    {
                        mOrderingMode.Text = OrderingMode.ZBuffered.ToString();
                    }
                    else
                    {
                        mOrderingMode.Text = OrderingMode.DistanceFromCamera.ToString();
                    }
                }

                #endregion

            }
        }

        public bool MakeVisibleOnSpriteSet
        {
            get;
            set;
        }

        #endregion

        #region Event Methods

        private void SetAnimationChainOnSprite(Window callingWindow)
        {
            if (mSelectedObject != null)
            {
                mSelectedObject.AnimationChains =
                    AnimationChainListSave.FromFile(((FileTextBox)callingWindow).Text).ToAnimationChainList(this.ContentManagerName);

            }
        }

        private void SetOrderingMode(Window callingWindow)
        {
            if (SelectedObject != null)
            {
                ComboBox asComboBox = callingWindow as ComboBox;

                OrderingMode orderingMode = ((OrderingMode)asComboBox.SelectedObject);

                if (orderingMode == OrderingMode.ZBuffered)
                {
                    SpriteManager.ConvertToZBufferedSprite(SelectedObject);
                }
                else
                {
                    SpriteManager.ConvertToOrderedSprite(SelectedObject);
                }
            }

        }

        private void SetPixelPerfectScaleClick(Window callingWindow)
        {
            OkListWindow okListWindow = new OkListWindow("Which camera would you like to scale according to?", "Select Camera");

            foreach (Camera camera in SpriteManager.Cameras)
            {
                okListWindow.AddItem(camera.Name, camera);
            }

            foreach (Camera camera in ExtraCamerasForScale)
            {
                okListWindow.AddItem(camera.Name, camera);
            }

            okListWindow.OkButtonClick += SetPixelPerfectScaleOk;
        }

        private void SetPixelPerfectScaleOk(Window callingWindow)
        {
            if (SelectedObject != null && SelectedObject.Texture != null)
            {
                OkListWindow okListWindow = callingWindow as OkListWindow;

                Camera camera = okListWindow.GetFirstHighlightedObject() as Camera;

                if (camera == null)
                {
                    GuiManager.ShowMessageBox("No Camera was selected, so Scale has not changed", "No Camera");
                }
                else
                {
                    float pixelsPerUnit = camera.PixelsPerUnitAt(SelectedObject.Z);


                    SelectedObject.ScaleX = .5f * SelectedObject.Texture.Width / pixelsPerUnit;
                    SelectedObject.ScaleY = .5f * SelectedObject.Texture.Height / pixelsPerUnit;
                }
            }


        }

        private void ShowCantEditAbsoluteValuesMessage(Window callingWindow)
        {
            if (SelectedObject.Parent != null)
            {

                GuiManager.ShowMessageBox(
                    "The Sprite that you are editing is attached to another parent object.  " +
                    "To edit the position or rotation of this object, use the Relative category.",
                    "Can't edit absolute values");
            }
        }

        private void ShowAnimationPopup(Window callingWindow)
        {
            GuiManager.ToolTipText = mAnimationChainFileTextBox.Text;
        }

		private void SplitSpriteClick(Window callingWindow)
		{
			if (ObjectDisplaying != null)
			{
				float textureWidth = ObjectDisplaying.RightTextureCoordinate - ObjectDisplaying.LeftTextureCoordinate;
				int wide = (int)(.5 + 
					Math.Ceiling(textureWidth));

				float textureHeight = ObjectDisplaying.BottomTextureCoordinate - ObjectDisplaying.TopTextureCoordinate;
				int height = (int)(.5 + 
					Math.Ceiling(textureHeight));

				float scaleXOverTextureWidth = ObjectDisplaying.ScaleX / textureWidth;

				float scaleYOverTextureHeight = ObjectDisplaying.ScaleY / textureHeight;

				float currentLeftTextureCoordinate = ObjectDisplaying.LeftTextureCoordinate;
				float currentRightTextureCoordinate = Math.Min(ObjectDisplaying.RightTextureCoordinate, 1);
				float currentTopTextureCoordinate = ObjectDisplaying.TopTextureCoordinate;
				float currentBottomTextureCoordinate = Math.Min(ObjectDisplaying.BottomTextureCoordinate, 1) ;
				float maxRightTextureCoordinate = ObjectDisplaying.RightTextureCoordinate;
				float maxBottomTextureCoordinate = ObjectDisplaying.BottomTextureCoordinate;

				float startLeft = ObjectDisplaying.X - ObjectDisplaying.ScaleX;
				float startTop = ObjectDisplaying.Y + ObjectDisplaying.ScaleY;

				for (int x = 0; x < wide; x++)
				{
					currentTopTextureCoordinate = ObjectDisplaying.TopTextureCoordinate;
					currentBottomTextureCoordinate = Math.Min(ObjectDisplaying.BottomTextureCoordinate, 1);

					for (int y = 0; y < height; y++)
					{
						Sprite newSprite = null;
						if (x == 0 && y == 0)
						{
							newSprite = ObjectDisplaying;
						}
						else
						{
							newSprite = ObjectDisplaying.Clone();
							SpriteManager.AddSprite(newSprite);
							mSpriteList.Add(newSprite);

							FlatRedBall.Utilities.StringFunctions.MakeNameUnique<Sprite>(
								newSprite, mSpriteList);
						}

						newSprite.LeftTextureCoordinate = currentLeftTextureCoordinate;
						newSprite.RightTextureCoordinate = currentRightTextureCoordinate;
						newSprite.TopTextureCoordinate = currentTopTextureCoordinate;
						newSprite.BottomTextureCoordinate = currentBottomTextureCoordinate;

						newSprite.ScaleX = scaleXOverTextureWidth * (newSprite.RightTextureCoordinate - newSprite.LeftTextureCoordinate);
						newSprite.ScaleY = scaleYOverTextureHeight * (newSprite.BottomTextureCoordinate - newSprite.TopTextureCoordinate);

						newSprite.X = startLeft +
							2 * scaleXOverTextureWidth * (newSprite.LeftTextureCoordinate - ObjectDisplaying.LeftTextureCoordinate) +
							newSprite.ScaleX;

						newSprite.Y = startTop -
							2 * scaleYOverTextureHeight * (newSprite.TopTextureCoordinate - ObjectDisplaying.TopTextureCoordinate) -
							newSprite.ScaleY;

						currentTopTextureCoordinate = currentBottomTextureCoordinate;
						currentBottomTextureCoordinate = currentTopTextureCoordinate + 1;

						// Since FSB only allows texture coordinates between 0 and 1, let's make sure that we're not exceeding that.
						if (newSprite.LeftTextureCoordinate >= 1)
						{
							int leftInt = (int)newSprite.LeftTextureCoordinate;
							newSprite.LeftTextureCoordinate -= leftInt;
							newSprite.RightTextureCoordinate -= leftInt;
						}

						if (newSprite.TopTextureCoordinate >= 1)
						{
							int topInt = (int)newSprite.TopTextureCoordinate;
							newSprite.TopTextureCoordinate -= topInt;
							newSprite.BottomTextureCoordinate -= topInt;
						}

					}

					currentLeftTextureCoordinate = currentRightTextureCoordinate;
					currentRightTextureCoordinate = currentLeftTextureCoordinate + 1;
				}

				//// let's just add 3 new Sprites.
				//Sprite sprite = SpriteManager.AddSprite("redball.bmp");
				//mSpriteList.Add(sprite);
				//sprite.XVelocity = 1;
				//sprite.Name = "test";
			}

		}

        private void OnTextureChange(Window callingWindow)
        {
            if (ShowWarningOnNonPowerOfTwoTexture && SelectedObject.Texture != null)
            {
                if (MathFunctions.IsPowerOfTwo(SelectedObject.Texture.Width) == false ||
                    MathFunctions.IsPowerOfTwo(SelectedObject.Texture.Height) == false)
                {
                    GuiManager.ShowMessageBox("The texture " + SelectedObject.Texture.Name + " is not power of two. " +
                        "Its dimensions are " + SelectedObject.Texture.Width + " x " + SelectedObject.Texture.Height,
                        "Texture dimensions not power of two");
                }
            }
        }

        private void ChangeLeftPixel(Window callingWindow)
        {
            if (mSelectedObject != null && mSelectedObject.Texture != null)
            {

                mSelectedObject.LeftTextureCoordinate =
                    mLeftPixel.CurrentValue / mSelectedObject.Texture.Width;
                mSelectedObject.RightTextureCoordinate = mSelectedObject.LeftTextureCoordinate + mPixelWidth.CurrentValue / mSelectedObject.Texture.Width;

            }
        }

        private void ChangeTopPixel(Window callingWindow)
        {
            if (mSelectedObject != null && mSelectedObject.Texture != null)
            {
                mSelectedObject.TopTextureCoordinate =
                    mTopPixel.CurrentValue / mSelectedObject.Texture.Height;

                mSelectedObject.BottomTextureCoordinate = mSelectedObject.TopTextureCoordinate + mPixelHeight.CurrentValue / mSelectedObject.Texture.Height;


            }
        }

        private void ChangePixelWidth(Window callingWindow)
        {
            if (mSelectedObject != null && mSelectedObject.Texture != null)
            {
                mSelectedObject.RightTextureCoordinate =
                    (mLeftPixel.CurrentValue + mPixelWidth.CurrentValue) / mSelectedObject.Texture.Width;

            }
        }

        private void ChangePixelHeight(Window callingWindow)
        {
            if (mSelectedObject != null && mSelectedObject.Texture != null)
            {
                mSelectedObject.BottomTextureCoordinate =
                    (mTopPixel.CurrentValue + mPixelHeight.CurrentValue) / mSelectedObject.Texture.Height;

            }
        }

        private void UpdateCustomUI(Window callingWindow)
        {
            if (SelectedObject != null && SelectedObject.Texture != null)
            {
                if (!mLeftPixel.IsWindowOrChildrenReceivingInput)
                {
                    int leftPixel = (int)(.5f + SelectedObject.Texture.Width *
                        SelectedObject.LeftTextureCoordinate);
                    mLeftPixel.CurrentValue = leftPixel;
                }

                if (!mTopPixel.IsWindowOrChildrenReceivingInput)
                {
                    int topPixel = (int)(.5f + SelectedObject.Texture.Height *
                        SelectedObject.TopTextureCoordinate);
                    mTopPixel.CurrentValue = topPixel;
                }

                if (!mPixelWidth.IsWindowOrChildrenReceivingInput)
                {
                    int width = (int)(.5f + SelectedObject.Texture.Width *
                        Math.Abs(SelectedObject.RightTextureCoordinate - SelectedObject.LeftTextureCoordinate));

                    mPixelWidth.CurrentValue = width;
                }

                if (!mPixelHeight.IsWindowOrChildrenReceivingInput)
                {
                    int height = (int)(.5f + SelectedObject.Texture.Height *
                        Math.Abs(SelectedObject.BottomTextureCoordinate - SelectedObject.TopTextureCoordinate));

                    mPixelHeight.CurrentValue = height;
                }
            }
        }


        #endregion

        #region Methods

        #region Constructor

        public SpritePropertyGrid(Cursor cursor)
            : base(cursor)
        {
            MakeVisibleOnSpriteSet = true;

            InitialExcludeIncludeMembers();

            #region Category Cleanup

            RemoveCategory("Uncategorized");

            SelectCategory("Basic");

            #endregion

            #region UI Replacement

            #region Replace the CurrentChainName UI element

            mCurrentChainNameComboBox = new ComboBox(this.mCursor);
            mCurrentChainNameComboBox.ScaleX = 5;
            ReplaceMemberUIElement("CurrentChainName", mCurrentChainNameComboBox);

            #endregion

            mAnimationChainFileTextBox = new FileTextBox(this.mCursor);
            mAnimationChainFileTextBox.ScaleX = 8;
            mAnimationChainFileTextBox.TextBox.CursorOver += ShowAnimationPopup;
            this.AddWindow(mAnimationChainFileTextBox, "Animation");
            mAnimationChainFileTextBox.SetFileType("achx");
            this.SetLabelForWindow(mAnimationChainFileTextBox, "Animation File");

            mAnimationChainFileTextBox.FileSelect += SetAnimationChainOnSprite;

            #endregion

			this.MinimumScaleY = 10;

            SetMemberDisplayName("CursorSelectable", "Active");
        }

        private void InitialExcludeIncludeMembers()
        {

            ExcludeAllMembers();

            #region Basic Members
            IncludeMember("X", "Basic");
            SetMemberChangeEvent("X", ShowCantEditAbsoluteValuesMessage);
            IncludeMember("Y", "Basic");
            SetMemberChangeEvent("Y", ShowCantEditAbsoluteValuesMessage);
            IncludeMember("Z", "Basic");
            SetMemberChangeEvent("Z", ShowCantEditAbsoluteValuesMessage);

            IncludeMember("Visible", "Basic");
            IncludeMember("CursorSelectable", "Basic");

            IncludeMember("Name", "Basic");

            mOrderingMode = new ComboBox(mCursor);
            mOrderingMode.AddItem("Distance from Camera", OrderingMode.DistanceFromCamera);
            mOrderingMode.AddItem("ZBuffered", OrderingMode.ZBuffered);
            mOrderingMode.ScaleX = 6.5f;
            AddWindow(mOrderingMode, "Basic");
            SetLabelForWindow(mOrderingMode, "OrderingMode");
            mOrderingMode.ItemClick += SetOrderingMode;

            #endregion

            #region Rotation Members

            IncludeMember("RotationX", "Rotation");
            SetMemberChangeEvent("RotationX", ShowCantEditAbsoluteValuesMessage);
            IncludeMember("RotationY", "Rotation");
            SetMemberChangeEvent("RotationY", ShowCantEditAbsoluteValuesMessage);
            IncludeMember("RotationZ", "Rotation");
            SetMemberChangeEvent("RotationZ", ShowCantEditAbsoluteValuesMessage);

            #endregion

            #region Scale Members
            IncludeMember("ScaleX", "Scale");
            IncludeMember("ScaleY", "Scale");

            IncludeMember("PixelSize", "Scale");
            ((UpDown)GetUIElementForMember("PixelSize")).Sensitivity = .01f;
            // Vic says:  Any value for PixelSize above 0 will result in the
            // Sprite having a valid PixelSize.  Values below 0 are ignored.
            // This isn't the best solution, but there's really no reason for
            // a user to ever set a value below -1, so we'll put a cap on that.
            // Eventualy we may want to make this more robust by "hopping" to -1
            // if the user drags the value down below 0, and "hopping" to 0 if the
            // user drags the value up from -1.
            ((UpDown)GetUIElementForMember("PixelSize")).MinValue = -1;

            mSetPixelPerfectScaleButton = new Button(mCursor);
            mSetPixelPerfectScaleButton.Text = "Set Pixel\nPerfect Scale";
            mSetPixelPerfectScaleButton.ScaleX = 6;
            mSetPixelPerfectScaleButton.ScaleY = 2f;

            AddWindow(mSetPixelPerfectScaleButton, "Scale");
            mSetPixelPerfectScaleButton.Click += SetPixelPerfectScaleClick;


            #endregion

            #region Animation

            IncludeMember("AnimationChains", "Animation");
            IncludeMember("CurrentChainName", "Animation");
            IncludeMember("Animate", "Animation");
            IncludeMember("AnimationSpeed", "Animation");

            #endregion

            #region Texture Members

            IncludeMember("Texture", "Texture");
            SetMemberChangeEvent("Texture", OnTextureChange);

            IncludeMember("TextureAddressMode", "Texture");

            //mTopTextureCoordinateUpDown = IncludeMember("TopTextureCoordinate", "Texture") as UpDown;
            //mTopTextureCoordinateUpDown.Sensitivity = .01f;

            //mBottomTextureCoordinateUpDown = IncludeMember("BottomTextureCoordinate", "Texture") as UpDown;
            //mBottomTextureCoordinateUpDown.Sensitivity = .01f;

            //mLeftTextureCoordinateUpDown = IncludeMember("LeftTextureCoordinate", "Texture") as UpDown;
            //mLeftTextureCoordinateUpDown.Sensitivity = .01f;

            //mRightTextureCoordinateUpDown = IncludeMember("RightTextureCoordinate", "Texture") as UpDown;
            //mRightTextureCoordinateUpDown.Sensitivity = .01f;
            TextureCoordinatePropertyGridHelper.CreatePixelCoordinateUi(
                this,
                "TopCoordinate",
                "BottomCoordinate",
                "LeftCoordinate",
                "RightCoordinate",
                "Texture",
                out mTopPixel,
                out mLeftPixel,
                out mPixelHeight,
                out mPixelWidth);

            mLeftPixel.ValueChanged += ChangeLeftPixel;
            mTopPixel.ValueChanged += ChangeTopPixel;
            mPixelWidth.ValueChanged += ChangePixelWidth;
            mPixelHeight.ValueChanged += ChangePixelHeight;

            this.AfterUpdateDisplayedProperties += UpdateCustomUI;

            IncludeMember("FlipHorizontal", "Texture");
            IncludeMember("FlipVertical", "Texture");


            mPixelPerfectTextureCoordinates = new ToggleButton(this.mCursor);
            mPixelPerfectTextureCoordinates.Text = "Pixel Perfect\nCoordinates";
            mPixelPerfectTextureCoordinates.ScaleX = 5.5f;
            mPixelPerfectTextureCoordinates.ScaleY = 2.1f;

            AddWindow(mPixelPerfectTextureCoordinates, "Texture");

            #endregion

            #region Relative

            IncludeMember("RelativeX", "Relative");
            IncludeMember("RelativeY", "Relative");
            IncludeMember("RelativeZ", "Relative");

            IncludeMember("RelativeRotationX", "Relative");
            IncludeMember("RelativeRotationY", "Relative");
            IncludeMember("RelativeRotationZ", "Relative");

            #endregion

            #region Color

            IncludeMember("ColorOperation", "Color");

#if !FRB_XNA
            ComboBox colorOperationComboBox = GetUIElementForMember("ColorOperation") as ComboBox;

            for (int i = colorOperationComboBox.Count - 1; i > -1; i--)
            {
                Microsoft.DirectX.Direct3D.TextureOperation textureOperation =
                    ((Microsoft.DirectX.Direct3D.TextureOperation)colorOperationComboBox[i].ReferenceObject);

                if (!FlatRedBall.Graphics.GraphicalEnumerations.IsTextureOperationSupportedInFrbXna(
                    textureOperation))
                {
                    colorOperationComboBox.RemoveAt(i);
                }
            }
#endif

            IncludeMember("Red", "Color");
            IncludeMember("Green", "Color");
            IncludeMember("Blue", "Color");

            IncludeMember("BlendOperation", "Blend");
            IncludeMember("Alpha", "Blend");

            #endregion

        }

        #endregion

        #region Public Methods

		public void EnableSplittingSprite(SpriteList listToAddNewSpritesTo)
		{
			mSpriteList = listToAddNewSpritesTo;

			Button splitSpriteButton = new Button(mCursor);
			splitSpriteButton.Text = "Split Sprite";
			splitSpriteButton.ScaleX = 10;
			this.AddWindow(splitSpriteButton, "Actions");
			splitSpriteButton.Click += SplitSpriteClick;
		}

        public override void UpdateDisplayedProperties()
        {
            base.UpdateDisplayedProperties();

            UpdateAnimationChainUi(SelectedObject, mCurrentChainNameComboBox, mAnimationChainFileTextBox);

            #region Update the RoundTo property for the texture coordinate UpDowns

            if (SelectedObject != null && SelectedObject.Texture != null && mTopTextureCoordinateUpDown != null)
            {
                float roundToX = 0;            
                float roundToY = 0;

                if (mPixelPerfectTextureCoordinates.IsPressed)
                {
                    roundToX = 1.0f / SelectedObject.Texture.Width;
                    roundToY = 1.0f / SelectedObject.Texture.Height;
                }

                mTopTextureCoordinateUpDown.RoundTo = roundToY;
                mBottomTextureCoordinateUpDown.RoundTo = roundToY;

                mLeftTextureCoordinateUpDown.RoundTo = roundToX;
                mRightTextureCoordinateUpDown.RoundTo = roundToX;

            }
            #endregion
        }

        public static void UpdateAnimationChainUi(IAnimationChainAnimatable selectedObject,
            ComboBox currentChainNameComboBox, FileTextBox animationChainFileTextBox)
        {
            #region Update mCurrentChainComboBox items

            if (selectedObject != null && selectedObject.AnimationChains.Count != 0)
            {
                for (int i = 0; i < selectedObject.AnimationChains.Count; i++)
                {
                    if (currentChainNameComboBox.Count <= i || currentChainNameComboBox[i].Text != selectedObject.AnimationChains[i].Name)
                    {
                        currentChainNameComboBox.InsertItem(i, selectedObject.AnimationChains[i].Name, selectedObject.AnimationChains[i]);
                    }
                }

                while (currentChainNameComboBox.Count > selectedObject.AnimationChains.Count)
                {
                    currentChainNameComboBox.RemoveAt(currentChainNameComboBox.Count - 1);
                }
            }
            #endregion

            #region Update the Animation File UI
            if (selectedObject != null && selectedObject.AnimationChains != null && selectedObject.AnimationChains.Name != null)
            {
                if (!animationChainFileTextBox.IsWindowOrChildrenReceivingInput)
                {
                    animationChainFileTextBox.Text = selectedObject.AnimationChains.Name;
                }

            }

            #endregion
        }

        #endregion

        #endregion
    }
}
