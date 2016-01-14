using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Graphics;
using System.Drawing;

namespace EditorObjects.Hud
{
    public class TextWidthHandles
    {
        Line mLeftLine;
        Line mTopSolidLine;
        Line mBottomSolidLine;
        Line mTopSoftLine;
        Line mBottomSoftLine;
        Line mRightLine;

        AxisAlignedRectangle mRightBox;

        public Text Text
        {
            get;
            set;
        }

        public void UpdateVisibility()
        {
            if (Text == null)
            {
                mLeftLine.Visible = false;
                mTopSolidLine.Visible = false;
                mBottomSolidLine.Visible = false;
                mTopSoftLine.Visible = false;
                mBottomSoftLine.Visible = false;
                mRightLine.Visible = false;

                mRightBox.Visible = false;
            }
            else
            {
                mLeftLine.Visible = true;
                mTopSolidLine.Visible = true;
                mBottomSolidLine.Visible = true;

                if (float.IsPositiveInfinity(Text.MaxWidth))
                {
                    mTopSoftLine.Visible = true;
                    mBottomSoftLine.Visible = true;
                    mRightLine.Visible = false;
                }
                else
                {
                    mTopSoftLine.Visible = false;
                    mBottomSoftLine.Visible = false;
                    mRightLine.Visible = true;
                }
            }
        }

        public void UpdatePositions()
        {
            

            float scaleX = Text.MaxWidth/2.0f;

            if (float.IsPositiveInfinity(Text.MaxWidth))
            {
                scaleX = Text.ScaleX;
            }

            scaleX *= 1.01f;

                        
            float horizontalCenter = Text.HorizontalCenter - Text.ScaleX + scaleX;
            float left = horizontalCenter - scaleX;
            float right = horizontalCenter + scaleX;


            mLeftLine.X = left;
            mLeftLine.Y = Text.VerticalCenter;
            mLeftLine.RelativePoint1.Y = Text.ScaleY;
            mLeftLine.RelativePoint2.Y = -Text.ScaleY;
            mLeftLine.RelativePoint1.X = 0;
            mLeftLine.RelativePoint2.X = 0;

            mRightLine.X = right;
            mRightLine.Y = Text.VerticalCenter;
            mRightLine.RelativePoint1.Y = Text.ScaleY;
            mRightLine.RelativePoint2.Y = -Text.ScaleY;
            mRightLine.RelativePoint1.X = 0;
            mRightLine.RelativePoint2.X = 0;

            mTopSolidLine.X = horizontalCenter;
            mTopSolidLine.Y = Text.VerticalCenter + Text.ScaleY;
            mTopSolidLine.RelativePoint1.X = -scaleX;
            mTopSolidLine.RelativePoint2.X = scaleX;

            mBottomSolidLine.X = horizontalCenter;
            mBottomSolidLine.Y = Text.VerticalCenter - Text.ScaleY;
            mBottomSolidLine.RelativePoint1.X = -scaleX;
            mBottomSolidLine.RelativePoint2.X = scaleX;

            mTopSoftLine.Y = mTopSolidLine.Y;
            mTopSoftLine.X = right + 1;

            mBottomSoftLine.Y = mBottomSolidLine.Y;
            mBottomSoftLine.X = right + 1;

            mLeftLine.Z = Text.Z;
            mRightLine.Z = Text.Z;
            mTopSolidLine.Z = Text.Z;
            mBottomSolidLine.Z = Text.Z;
            mTopSoftLine.Z = Text.Z;
            mBottomSoftLine.Z = Text.Z;


        }


        public TextWidthHandles()
        {
            mLeftLine = ShapeManager.AddLine();
            mTopSolidLine = ShapeManager.AddLine();
            mBottomSolidLine = ShapeManager.AddLine();
            mTopSoftLine = ShapeManager.AddLine();
            mBottomSoftLine = ShapeManager.AddLine();
            mRightLine = ShapeManager.AddLine();

            mRightBox = ShapeManager.AddAxisAlignedRectangle();

            mLeftLine.Color = Color.Green;
            mTopSolidLine.Color = Color.Green;
            mBottomSolidLine.Color = Color.Green;
            mRightLine.Color = Color.Green;

#if FRB_MDX
            mTopSoftLine.Color = Color.FromArgb(
                128, Color.Green);

            mBottomSoftLine.Color = mTopSoftLine.Color;
#else
            throw new NotImplementedException();
#endif

            mRightBox.Color = Color.Orange;


        }


        public void Update()
        {
            UpdateVisibility();

            if (Text != null)
            {
                UpdatePositions();
            }
        }

    }
}
