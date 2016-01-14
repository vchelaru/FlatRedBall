using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Gui;
using FlatRedBall.Content.AnimationChain;

using FlatRedBall.Math;
using FlatRedBall.Graphics;
#if FRB_MDX
using Microsoft.DirectX.Direct3D;
#endif

using Sprite = FlatRedBall.Sprite;

namespace EditorObjects.Gui
{

    public class SpriteFramePropertyGrid : PropertyGrid<SpriteFrame>
	{
		#region Fields

		UpDown mTextureBorderWidthInPixels;

        private ComboBox mCurrentChainNameComboBox;

        private FileTextBox mAnimationChainFileTextBox;

        private ComboBox mOrderingMode;

		#endregion

		#region Properties

        bool IsAsymmetric
        {
            get
            {
                if (SelectedObject == null)
                {
                    return false;
                }
                else
                {

                    SpriteFrame.BorderSides borderSides = SelectedObject.Borders;
                    return ((borderSides & SpriteFrame.BorderSides.Right) == SpriteFrame.BorderSides.Right &&
                    (borderSides & SpriteFrame.BorderSides.Left) == 0)

                        ||

                    ((borderSides & SpriteFrame.BorderSides.Left) == SpriteFrame.BorderSides.Left &&
                    (borderSides & SpriteFrame.BorderSides.Right) == 0)

                        ||

                    ((borderSides & SpriteFrame.BorderSides.Top) == SpriteFrame.BorderSides.Top &&
                    (borderSides & SpriteFrame.BorderSides.Bottom) == 0)

                        ||

                    ((borderSides & SpriteFrame.BorderSides.Bottom) == SpriteFrame.BorderSides.Bottom &&
                    (borderSides & SpriteFrame.BorderSides.Top) == 0);
                }
            }
        }

		public override SpriteFrame SelectedObject
        {
            get
            {
                return base.SelectedObject;
            }
            set
            {
                base.SelectedObject = value;

                if (!Visible && (SelectedObject != null))
                {
                    GuiManager.BringToFront(this);
                }

                Visible = (SelectedObject != null);

                if (SelectedObject != null)
                {
                    OnBordersChanged(null);
                }

                #region Update the Ordered GUI

                if (SelectedObject != null)
                {
                    Sprite centerSprite = SelectedObject.CenterSprite;

                    // for performance reasons this only updates when the selected changes
                    if (centerSprite != null && SpriteManager.ZBufferedSprites.Contains(centerSprite))
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

        public bool ShowWarningOnNonPowerOfTwoTexture
        {
            get;
            set;
        }

		private bool UseTextureHeight
		{
			get
			{
				return ObjectDisplaying != null &&
					((ObjectDisplaying.Borders & SpriteFrame.BorderSides.Top) == SpriteFrame.BorderSides.Top ||
					(ObjectDisplaying.Borders & SpriteFrame.BorderSides.Bottom) == SpriteFrame.BorderSides.Bottom);
			}
		}

        #endregion

        #region Event Methods
		private void ChangeTextureBorderWidthInPixels(Window callingWindow)
		{
			if (ObjectDisplaying != null && ObjectDisplaying.Texture != null)
			{
				if (UseTextureHeight)
				{
					ObjectDisplaying.TextureBorderWidth = mTextureBorderWidthInPixels.CurrentValue /
						ObjectDisplaying.Texture.Height;
				}
				else
				{
					ObjectDisplaying.TextureBorderWidth = mTextureBorderWidthInPixels.CurrentValue /
						ObjectDisplaying.Texture.Width;
				}
			}
		}

        private void OnBordersChanged(Window callingWindow)
        {
            SpriteFrame.BorderSides borderSides = SelectedObject.Borders;

            if(IsAsymmetric)
            {
                // Since the SpriteFrame doesn't use symmetrical BorderSides, we can set the allowed texture coords
                // to 1 instead of .5
                UpDown upDown = GetUIElementForMember("TextureBorderWidth") as UpDown;
                upDown.MaxValue = 1;
            }
            else
            {
                // This is symmetrical, so we can't allow the texture border width to be bigger than .5
                UpDown upDown = GetUIElementForMember("TextureBorderWidth") as UpDown;
                upDown.MaxValue = .5f;
            }
        }

        private void OnTextureChange(Window callingWindow)
        {
            if (ShowWarningOnNonPowerOfTwoTexture)
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

        private void SetPixelPerfectClick(Window callingWindow)
        {
            // EARLY OUT
            if (SelectedObject.Texture == null)
            {
                return;
            }
            // end early out


            #region Get the pixelsPerUnit
            Camera camera = SpriteManager.Camera;

            float pixelsPerUnit = camera.PixelsPerUnitAt(SelectedObject.Z);
            #endregion

            #region Get whether the SpriteFrame has side and top/bottom borders

            bool hasSideBorders = (SelectedObject.Borders & SpriteFrame.BorderSides.Left) == SpriteFrame.BorderSides.Left ||
                (SelectedObject.Borders & SpriteFrame.BorderSides.Right) == SpriteFrame.BorderSides.Right;

            bool hasTopBottomBorders = (SelectedObject.Borders & SpriteFrame.BorderSides.Top) == SpriteFrame.BorderSides.Top ||
                (SelectedObject.Borders & SpriteFrame.BorderSides.Bottom) == SpriteFrame.BorderSides.Bottom;

            #endregion

            float textureBorderWidthToUse = SelectedObject.TextureBorderWidth;

            if (textureBorderWidthToUse >= .499f)
            {
                textureBorderWidthToUse = .5f;
            }

            if (hasSideBorders)
            {
                SelectedObject.SpriteBorderWidth = SelectedObject.Texture.Width *
                    textureBorderWidthToUse / pixelsPerUnit;
            }
            if (hasTopBottomBorders)
            {
                SelectedObject.SpriteBorderWidth = SelectedObject.Texture.Height *
                    textureBorderWidthToUse / pixelsPerUnit;
            }


            // Width should only be set if it can't be scaled left/right.  That means 
            if (!hasSideBorders)
            {
                SelectedObject.ScaleX = .5f * SelectedObject.Texture.Width / pixelsPerUnit;
            }
            else
            {
                SelectedObject.ScaleX = Math.Max(SelectedObject.SpriteBorderWidth, SelectedObject.ScaleX);
            }

            if (!hasTopBottomBorders)
            {
                SelectedObject.ScaleY = .5f * SelectedObject.Texture.Height / pixelsPerUnit;
            }
            else
            {
                SelectedObject.ScaleY = Math.Max(SelectedObject.SpriteBorderWidth, SelectedObject.ScaleY);
            }
        }

        private void ShowAnimationPopup(Window callingWindow)
        {
            GuiManager.ToolTipText = mAnimationChainFileTextBox.Text;
        }

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
                    foreach (Sprite sprite in SelectedObject.AllSprites)
                    {
                        SpriteManager.ConvertToZBufferedSprite(sprite);
                    }
                }
                else
                {
                    foreach (Sprite sprite in SelectedObject.AllSprites)
                    {
                        SpriteManager.ConvertToOrderedSprite(sprite);
                    }
                }
            }

        }

        #endregion

        #region Methods

		#region Constructor

		public SpriteFramePropertyGrid(Cursor cursor)
            : base(cursor)
        {
            ExcludeAllMembers();

            #region Include Basic Members

            IncludeMember("X", "Basic");
            IncludeMember("Y", "Basic");
            IncludeMember("Z", "Basic");

            IncludeMember("RotationX", "Basic");
            IncludeMember("RotationY", "Basic");
            IncludeMember("RotationZ", "Basic");

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

            #region Scale Members

            IncludeMember("ScaleX", "Scale");
            IncludeMember("ScaleY", "Scale");

            Button setPixelPerfectScale = new Button(mCursor);
            setPixelPerfectScale.ScaleX = 5;
            setPixelPerfectScale.ScaleY = 2;

            setPixelPerfectScale.Text = "Set Pixel\nPerfect";

            setPixelPerfectScale.Click += SetPixelPerfectClick;

            AddWindow(setPixelPerfectScale, "Scale");

            #endregion

            IncludeMember("Texture", "Texture");
            SetMemberChangeEvent("Texture", OnTextureChange);

            #region Animation members

            IncludeMember("AnimationChains",    "Animation");
            IncludeMember("CurrentChainName", "Animation");
            IncludeMember("Animate", "Animation");
            IncludeMember("AnimationSpeed", "Animation");

            #endregion

            #region Include Color Members

            IncludeMember("ColorOperation", "Color");

#if !FRB_XNA
            ComboBox colorOperationComboBox = GetUIElementForMember("ColorOperation") as ComboBox;

            for (int i = colorOperationComboBox.Count - 1; i > -1; i--)
            {
                TextureOperation textureOperation =
                    ((TextureOperation)colorOperationComboBox[i].ReferenceObject);

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

            #endregion

            #region Include Blend Members

            IncludeMember("BlendOperation", "Blend");
            IncludeMember("Alpha", "Blend");

            #endregion

            #region Include Border Members

            IncludeMember("TextureBorderWidth", "Border");
            IncludeMember("SpriteBorderWidth", "Border");
            IncludeMember("Borders", "Border");
			IncludeMember("PixelSize", "Border");

            #endregion

            #region Customize "Borders"

            SetMemberChangeEvent("Borders", OnBordersChanged);

            #endregion  

            #region Customize TextureBorderWidth

            UpDown upDown = GetUIElementForMember("TextureBorderWidth") as UpDown;
            upDown.MinValue = 0;
            upDown.MaxValue = .5f;

			mTextureBorderWidthInPixels = new UpDown(mCursor);
			mTextureBorderWidthInPixels.ScaleX = 6;
			mTextureBorderWidthInPixels.ValueChanged += ChangeTextureBorderWidthInPixels;
			mTextureBorderWidthInPixels.RoundTo = 1;
			AddWindow(mTextureBorderWidthInPixels, "Border");


			#endregion


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


			AfterUpdateDisplayedProperties += new GuiMessage(UpdateTextureBorderWidthUI);

			RemoveCategory("Uncategorized");

            SelectCategory("Basic");

		}

		#endregion

        public override void UpdateDisplayedProperties()
        {
            base.UpdateDisplayedProperties();

            SpritePropertyGrid.UpdateAnimationChainUi(SelectedObject,
                mCurrentChainNameComboBox, mAnimationChainFileTextBox);
        }

		void UpdateTextureBorderWidthUI(Window callingWindow)
		{
			if (ObjectDisplaying != null && !mTextureBorderWidthInPixels.IsWindowOrChildrenReceivingInput && ObjectDisplaying.Texture != null)
			{
				if (UseTextureHeight)
				{
					mTextureBorderWidthInPixels.CurrentValue =
						ObjectDisplaying.TextureBorderWidth * ObjectDisplaying.Texture.Height;
				}
				else
				{
					mTextureBorderWidthInPixels.CurrentValue =
						ObjectDisplaying.TextureBorderWidth * ObjectDisplaying.Texture.Width;
				}
			}
		}

        public void MakeCurrentSpriteFramePixelPerfect()
        {
            SetPixelPerfectClick(null);
        }

        #endregion
    }
}
