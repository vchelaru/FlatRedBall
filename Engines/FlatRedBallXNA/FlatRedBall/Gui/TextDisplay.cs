using System;
using FlatRedBall;
using FlatRedBall.Graphics;
#if FRB_MDX

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D=Microsoft.DirectX.Direct3D;
#else

#endif
namespace FlatRedBall.Gui
{
	/// <summary>
	/// Summary description for TextDisplay.
	/// </summary>
	public class TextDisplay : Window
    {
        #region Fields
        string mText;

        //TextField mTextField;

        Text mTextObject;

        #endregion

        #region Properties

        public override float ScaleX
        {
            get
            {
                return mScaleX;
            }
            set
            {
                mScaleX = value;
            }
        }

        public override float ScaleY
        {
            get
            {
                return mScaleY;
            }
            set
            {
                mScaleY = value;
            }
        }

        public string Text
        {
            get { return mText; }
            set 
            { 
                mText = value;
                if (mTextObject != null)
                {
                    mTextObject.DisplayText = value;
                }
            }
        }

        public override bool Visible
        {
            get
            {
                return base.Visible;
            }
            set
            {
                base.Visible = value;

                if (mTextObject != null)
                {
                    mTextObject.Visible = value;
                }
            }
        }

        public float Width
        {
            get { return TextManager.GetWidth(this.Text, GuiManager.TextSpacing); }
        }

        #endregion

        #region Methods

        #region Constructor

        public TextDisplay(Cursor cursor) : base(cursor)
		{
        }

        #endregion

        public override bool IsPointOnWindow(float cameraRelativeX, float cameraRelativeY)
        {
            return (cameraRelativeY > mWorldUnitY - GuiManager.TextHeight/2.0f &&
                cameraRelativeY < mWorldUnitY + GuiManager.TextHeight / 2.0f &&
                cameraRelativeX > mWorldUnitX &&
                cameraRelativeX < mWorldUnitX + Width + 1);
        }

        public override void SetSkin(GuiSkin guiSkin)
        {
            //base.SetSkin(guiSkin);
            if (mTextObject == null)
            {
                mTextObject = TextManager.AddText(mText);
                GuiManagerDrawn  = false;
            }


        }

        public override string ToString()
        {
            return Text;
        }

        public void Wrap(float width)
        {
            mText = 
                TextManager.InsertNewLines(mText, GuiManager.TextSpacing, width, TextManager.DefaultFont);
        }

        #region Internal Methods

        public override void Activity(Camera camera)
        {
            base.Activity(camera);

            if (mTextObject != null)
            {
                mTextObject.X = this.X;
                mTextObject.Y = this.Y;
            }
        }

        internal override void DrawSelfAndChildren(Camera camera)
		{
            if (Visible == false)
				return;

#if false // this is marked as if (false) so we must not be using it anymore?  Not sure, but taking it out to eliminate warnings.
            if (false)//mTextField != null)
            {
                TextManager.mRedForVertexBuffer = TextManager.mGreenForVertexBuffer = TextManager.mBlueForVertexBuffer = 20;

                TextManager.mScaleForVertexBuffer = GuiManager.TextHeight / 2.0f;
                TextManager.mSpacingForVertexBuffer = GuiManager.TextSpacing;

                if (Enabled)
                    TextManager.mAlphaForVertexBuffer = 255;
                else
                    TextManager.mAlphaForVertexBuffer = 115;

                //TextManager.Draw(mTextField);
            }
            else
#endif
            {
                TextManager.mMaxWidthForVertexBuffer = float.PositiveInfinity;

                TextManager.mAlignmentForVertexBuffer = HorizontalAlignment.Left;

                TextManager.mXForVertexBuffer = mWorldUnitX + 1;
                TextManager.mYForVertexBuffer = mWorldUnitY;

#if FRB_MDX
                TextManager.mZForVertexBuffer = camera.Z + 100;
#else
                TextManager.mZForVertexBuffer = camera.Z - 100;

#endif

                TextManager.mScaleForVertexBuffer = GuiManager.TextHeight / 2.0f;
                TextManager.mSpacingForVertexBuffer = GuiManager.TextSpacing;

                TextManager.mRedForVertexBuffer = TextManager.mGreenForVertexBuffer = TextManager.mBlueForVertexBuffer = 20;

                if (Enabled)
                    TextManager.mAlphaForVertexBuffer = 255;
                else
                    TextManager.mAlphaForVertexBuffer = 115;


                TextManager.Draw(ref mText);
            }
		}


        internal override int GetNumberOfVerticesToDraw()
		{
            //if (false)//mTextField != null)
            //{
            //    if (mTextField.DisplayText == null)
            //        return 0;
            //    else
            //        return mTextField.DisplayText.Replace(" ", "").Length * 6;
            //}
            //else
            //{
                if (Text == null)
                    return 0;
                else
                    return Text.Replace(" ", "").Length * 6;
            //}
        }

        #endregion



        #endregion
    }
}
