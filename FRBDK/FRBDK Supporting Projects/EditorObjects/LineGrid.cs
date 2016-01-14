using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Math;
using FlatRedBall.Graphics;
#if FRB_MDX
using System.Drawing;
using FlatRedBall.Graphics;
#elif XNA4
using Color = Microsoft.Xna.Framework.Color;
#elif FRB_XNA
using Color = Microsoft.Xna.Framework.Graphics.Color;
#endif

namespace EditorObjects
{
    public class LineGrid
    {
        #region Fields

        Color mCenterLineColor = Color.White;
        Color mGridColor = Color.DarkGray;

        PositionedObjectList<Line> mHorizontalLines = new PositionedObjectList<Line>();
        PositionedObjectList<Line> mVerticalLines = new PositionedObjectList<Line>();

        int mNumberOfHorizontalLines = 11;
        int mNumberOfVerticalLines = 11;

        // The world coordinate centers
        float mX;
        float mY;
		float mZ;

        float mDistanceBetweenLines = 1;

        Layer mLayer;

        bool mVisible = true;

        #endregion

        #region Properties

        public Color CenterLineColor
        {
            get { return mCenterLineColor; }
            set { mCenterLineColor = value; UpdateGrid(); }
        }

        public float DistanceBetweenLines
        {
            get { return mDistanceBetweenLines; }
            set { mDistanceBetweenLines = value; UpdateGrid(); }
        }

        public Color GridColor
        {
            get { return mGridColor; }
            set { mGridColor = value; UpdateGrid(); }
        }

        public Layer Layer
        {
            set
            {
                for (int i = 0; i < mHorizontalLines.Count; i++)
                {
                    ShapeManager.AddToLayer(mHorizontalLines[i], value, false);
                }
                for (int i = 0; i < mVerticalLines.Count; i++)
                {
                    ShapeManager.AddToLayer(mVerticalLines[i], value, false);

                }

                mLayer = value;

            }
        }

        public int NumberOfHorizontalLines
        {
            get { return mNumberOfHorizontalLines; }
            set 
            {
                if (value < 0)
                {
                    throw new ArgumentException("Cannot have a negative number of horiontal lines");
                }

                mNumberOfHorizontalLines = value; 
                UpdateGrid(); 
            }
        }

        public int NumberOfVerticalLines
        {
            get { return mNumberOfVerticalLines; }
            set 
            {
                if (value < 0)
                {
                    throw new ArgumentException("Cannot have a negative number of vertical lines");
                }

                mNumberOfVerticalLines = value; 
                UpdateGrid(); 
            }
        }

        public bool Visible
        {
            get { return mVisible; }
            set
            {
                mVisible = value;

                UpdateVisibility();
            }
        }

        public float X
        {
            get { return mX; }
            set { mX = value; UpdateGrid(); }
        }

        public float Y
        {
            get { return mY; }
            set { mY = value; UpdateGrid(); }
        }

		public float Z
		{
			get { return mZ; }
			set { mZ = value; UpdateGrid(); }
		}

        #endregion

        #region Methods

        #region Constructor

        public LineGrid()
        {
            CenterLineColor = Color.White;
            GridColor = Color.DarkGray;
            UpdateGrid();
        }

        #endregion

        #region Private Methods

        private void UpdateGrid()
        {
            #region Make sure there are enough horizontal lines

            while (mHorizontalLines.Count < mNumberOfHorizontalLines)
            {
                Line newLine = new Line();
                
                if (mLayer != null)
                {
                    ShapeManager.AddToLayer(newLine, mLayer, false);
                    
                }
                mHorizontalLines.Add(newLine);
            }

            while (mHorizontalLines.Count > mNumberOfHorizontalLines)
            {
                ShapeManager.Remove(mHorizontalLines.Last);
            }

            #endregion

            #region Make sure there are enough vertical lines

            while (mVerticalLines.Count < mNumberOfVerticalLines)
            {
                Line newLine = new Line();
                if (mLayer != null)
                {
                    ShapeManager.AddToLayer(newLine, mLayer, false);
                }
                mVerticalLines.Add(newLine);
            }

            while (mVerticalLines.Count > mNumberOfVerticalLines)
            {
                ShapeManager.Remove(mVerticalLines.Last);
            }

            #endregion

            #region Get top, bottom, left, right of LineGrid

            float bottom = mY - 
                mDistanceBetweenLines *(mNumberOfHorizontalLines - 1) / 2.0f;

            float top = mY + 
                mDistanceBetweenLines *(mNumberOfHorizontalLines - 1) / 2.0f;

            float left = mX - 
                mDistanceBetweenLines *(mNumberOfVerticalLines - 1) / 2.0f;

            float right = mX + 
                mDistanceBetweenLines *(mNumberOfVerticalLines - 1) / 2.0f;

            #endregion

            #region Position, color, and scale the horizontal lines


            for (int i = 0; i < mNumberOfHorizontalLines; i++)
            {
                mHorizontalLines[i].X = mX;
                mHorizontalLines[i].Y = bottom + i * mDistanceBetweenLines;
				mHorizontalLines[i].Z = mZ;
                mHorizontalLines[i].RelativePoint1.X = left - mX;
                mHorizontalLines[i].RelativePoint1.Y = 0;

                mHorizontalLines[i].RelativePoint2.X = right - mX;
                mHorizontalLines[i].RelativePoint2.Y = 0;

                if (i == mNumberOfHorizontalLines / 2)
                    mHorizontalLines[i].Color = CenterLineColor;
                else
                    mHorizontalLines[i].Color = GridColor;

            }

            #endregion

            #region Position, color, and scale the vertical lines


            for (int i = 0; i < mNumberOfVerticalLines; i++)
            {
                mVerticalLines[i].X = left + i * mDistanceBetweenLines; ;
                mVerticalLines[i].Y = mY;
				mVerticalLines[i].Z = mZ;
                mVerticalLines[i].RelativePoint1.X = 0;
                mVerticalLines[i].RelativePoint1.Y = bottom - mY;

                mVerticalLines[i].RelativePoint2.X = 0;
                mVerticalLines[i].RelativePoint2.Y = top - mY;

                if (i == mNumberOfVerticalLines / 2)
                    mVerticalLines[i].Color = CenterLineColor;
                else
                    mVerticalLines[i].Color = GridColor;
            }

            #endregion

            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            for (int i = mHorizontalLines.Count - 1; i > -1; i--)
            {
                mHorizontalLines[i].Visible = mVisible;
            }

            for (int i = mVerticalLines.Count - 1; i > -1; i--)
            {
                mVerticalLines[i].Visible = mVisible;
            }
        }

        #endregion

        #endregion
    }
}
