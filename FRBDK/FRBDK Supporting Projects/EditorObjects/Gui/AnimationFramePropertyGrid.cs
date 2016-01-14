using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Graphics.Animation;

namespace EditorObjects.Gui
{
    public class AnimationFramePropertyGrid : PropertyGrid<AnimationFrame>
    {
        #region Fields

        UpDown mLeftPixel;
        UpDown mTopPixel;

        UpDown mPixelWidth;
        UpDown mPixelHeight;

        Button mHideShowTextureCoordinates;

        #endregion

        #region Event Methods

        private void ChangeLeftPixel(Window callingWindow)
        {
            if (mSelectedObject != null && mSelectedObject.Texture != null)
            {

                mSelectedObject.LeftCoordinate =
                    mLeftPixel.CurrentValue / mSelectedObject.Texture.Width;
                mSelectedObject.RightCoordinate = mSelectedObject.LeftCoordinate + mPixelWidth.CurrentValue / mSelectedObject.Texture.Width;

            }
        }

        private void ChangeTopPixel(Window callingWindow)
        {
            if (mSelectedObject != null && mSelectedObject.Texture != null)
            {
                mSelectedObject.TopCoordinate =
                    mTopPixel.CurrentValue / mSelectedObject.Texture.Height;

                mSelectedObject.BottomCoordinate = mSelectedObject.TopCoordinate + mPixelHeight.CurrentValue / mSelectedObject.Texture.Height;


            }
        }

        private void ChangePixelWidth(Window callingWindow)
        {
            if (mSelectedObject != null && mSelectedObject.Texture != null)
            {
                mSelectedObject.RightCoordinate =
                    (mLeftPixel.CurrentValue + mPixelWidth.CurrentValue) / mSelectedObject.Texture.Width;

            }
        }

        private void ChangePixelHeight(Window callingWindow)
        {
            if (mSelectedObject != null && mSelectedObject.Texture != null)
            {
                mSelectedObject.BottomCoordinate =
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
                        SelectedObject.LeftCoordinate);
                    mLeftPixel.CurrentValue = leftPixel;
                }

                if (!mTopPixel.IsWindowOrChildrenReceivingInput)
                {
                    int topPixel = (int)(.5f + SelectedObject.Texture.Height *
                        SelectedObject.TopCoordinate);
                    mTopPixel.CurrentValue = topPixel;
                }

                if (!mPixelWidth.IsWindowOrChildrenReceivingInput)
                {
                    int width = (int)(.5f + SelectedObject.Texture.Width *
                        Math.Abs(SelectedObject.RightCoordinate - SelectedObject.LeftCoordinate));

                    mPixelWidth.CurrentValue = width;
                }

                if (!mPixelHeight.IsWindowOrChildrenReceivingInput)
                {
                    int height = (int)(.5f + SelectedObject.Texture.Height *
                        Math.Abs(SelectedObject.BottomCoordinate - SelectedObject.TopCoordinate));

                    mPixelHeight.CurrentValue = height;
                }
            }
        }

        #endregion

		#region Methods

		public AnimationFramePropertyGrid(Cursor cursor)
            : base(cursor)
        {
            Name = "AnimationFrame Properties";

            ExcludeMember("Empty");

            ExcludeMember("TextureName");

            #region Set member display names

            SetMemberDisplayName("frameTime", "Frame Length (ms):");
            SetMemberDisplayName("texture", "Texture:");
            SetMemberDisplayName("xFlip", "Flip Vertical:");
            SetMemberDisplayName("yFlip", "Flip Horizontal:");

            #endregion



			IncludeMember("Texture", "Texture");

            #region Custom Pixel texture coordinate values

            TextureCoordinatePropertyGridHelper.CreatePixelCoordinateUi(
                this,
                "TopCoordinate",
                "BottomCoordinate",
                "LeftCoordinate",
                "RightCoordinate",
                "Tex Coords",
                out mTopPixel,
                out mLeftPixel,
                out mPixelHeight,
                out mPixelWidth);

            mLeftPixel.ValueChanged += ChangeLeftPixel;
            mTopPixel.ValueChanged += ChangeTopPixel;
            mPixelWidth.ValueChanged += ChangePixelWidth;
            mPixelHeight.ValueChanged += ChangePixelHeight;

            //MoveWindowToTop(textDisplay);

            MoveWindowToTop(mPixelHeight);
            MoveWindowToTop(mPixelWidth);
            MoveWindowToTop(mTopPixel);
            MoveWindowToTop(mLeftPixel);


            #endregion

            mHideShowTextureCoordinates = new Button(GuiManager.Cursor);
            mHideShowTextureCoordinates.Text = "Show Texture Coords";
            mHideShowTextureCoordinates.ScaleX = 9.0f;
            mHideShowTextureCoordinates.Click += new GuiMessage(ClickHideShowTextureCoordinates);
            AddWindow(mHideShowTextureCoordinates, "Tex Coords");

            this.AfterUpdateDisplayedProperties += UpdateCustomUI;

		}

        void ClickHideShowTextureCoordinates(Window callingWindow)
        {
            if (GetUIElementForMember("LeftCoordinate") == null)
            {
                ShowTextureCoordinateUI();
                mHideShowTextureCoordinates.Text = "Hide Texture Coords";
            }
            else
            {
                HideTextureCoordinateUI();
                mHideShowTextureCoordinates.Text = "Show Texture Coords";
            }
        }

        private void ShowTextureCoordinateUI()
        {
            IncludeMember("LeftCoordinate", "Tex Coords");
            IncludeMember("RightCoordinate", "Tex Coords");
            IncludeMember("TopCoordinate", "Tex Coords");
            IncludeMember("BottomCoordinate", "Tex Coords");

            ((UpDown)GetUIElementForMember("LeftCoordinate")).Sensitivity = .01f;
            ((UpDown)GetUIElementForMember("RightCoordinate")).Sensitivity = .01f;
            ((UpDown)GetUIElementForMember("TopCoordinate")).Sensitivity = .01f;
            ((UpDown)GetUIElementForMember("BottomCoordinate")).Sensitivity = .01f;

        }

        private void HideTextureCoordinateUI()
        {
            ExcludeMember("LeftCoordinate");
            ExcludeMember("RightCoordinate");
            ExcludeMember("TopCoordinate");
            ExcludeMember("BottomCoordinate");
        }

		public Button ShowRefreshTextureButton()
		{
			Button button = new Button(mCursor);

			button.ScaleX = 7;
			button.Text = "Refresh Texture";

			this.AddWindow(button, "Texture");

			return button;

		}


		#endregion


	}
}
