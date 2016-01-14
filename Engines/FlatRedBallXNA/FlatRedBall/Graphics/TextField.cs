using System;
using System.Text;
using System.Collections.Generic;

using FlatRedBall;

using FlatRedBall.Gui;


using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;


namespace FlatRedBall.Graphics
{
	/// <summary>
	/// Summary description for TextField.
	/// </summary>
	public class TextField
	{
		#region Fields
		internal float mTop;
        internal float mBottom;
        internal float mLeft;
        internal float mRight;

		internal float mWidth;
        internal float mHeight;
		internal float mZ;

		private string mText;
		public float TextHeight;

		public Sprite Parent;
		public bool RelativeToCamera;

        public Window WindowParent;


		internal List<string> mLines;

		internal HorizontalAlignment mAlignment;


        float mTextScale = 1;

        float mRed;
        float mGreen;
        float mBlue;
        float mAlpha = 255;

        Text mTextObject;

		#endregion

		#region Properties
		public string DisplayText
		{
			get	{	return mText;	}
			set
			{
				mText = value;
				mLines.Clear();
			}
		}

        public Text TextObject
        {
            get { return mTextObject; }
            set { mTextObject = value; }
        }

        public float Red
        {
            get { return mRed; }
            set { mRed = value; }
        }

        public float Green
        {
            get { return mGreen; }
            set { mGreen = value; }
        }

        public float Blue
        {
            get { return mBlue; }
            set { mBlue = value; }
        }

        public float Alpha
        {
            get { return mAlpha; }
            set { mAlpha = value; }
        }

        public int LineCount
        {
            get { return mLines.Count; }
        }

        public float Z
        {
            get { return mZ; }
            set { mZ = value; }
        }

        public float Left
        {
            get { return mLeft; }
        }

        public float Right
        {
            get { return mRight; }
        }

        public float Top
        {
            get { return mTop; }
        }

        public float Bottom
        {
            get { return mBottom; }
        }

		#endregion

		#region Methods

		public TextField()
		{
			mAlignment = HorizontalAlignment.Left;
			Parent = null;
			mText = "";
			RelativeToCamera = false;

			TextHeight = 2;

			mLines = new List<string>();
		}
	

		public void SetDimensions(float Top, float Bottom, float Left, float Right, float Z)
		{
			mTop = Top;
			mBottom = Bottom;
			mLeft = Left;
			mRight = Right;
			mWidth = mRight - mLeft;
			mHeight = mTop - mBottom;
			mZ = Z;

			mLines.Clear();

		}


		public void SetDimensions<T>(T objectToSetTo, float borderWidth) where T : IScalable, IPositionable
		{
			mTop = (float)(objectToSetTo.Y + objectToSetTo.ScaleY) - borderWidth;
			mBottom = (float)(objectToSetTo.Y - objectToSetTo.ScaleY) + borderWidth;
			mLeft =(float)(objectToSetTo.X - objectToSetTo.ScaleX) + borderWidth;
			mRight = (float)(objectToSetTo.X + objectToSetTo.ScaleX) - borderWidth;

			mWidth = mRight - mLeft;
			mHeight = mTop - mBottom;
			mZ = (float)objectToSetTo.Z;

			mLines.Clear();

		}
	
		public void SetDimensions(Window windowToSetTo)
        {
            SetDimensions(windowToSetTo, 0);
        }


        public void SetDimensions(Window windowToSetTo, float extraBorder)
		{
            mTop = -mTextScale - extraBorder;
            mLeft = 0 + extraBorder;

            mBottom = -windowToSetTo.ScaleY * 2 + extraBorder ;
			mRight = windowToSetTo.ScaleX * 2 - extraBorder;



			mWidth = mRight - mLeft;
			mHeight = mTop - mBottom;

            WindowParent = windowToSetTo;

			mLines.Clear();

		}

        static StringBuilder StaticStringBuilder = new StringBuilder(100);
        public void UpdateTextObjectDisplayText(int firstRow, int numberOfRows)
        {
            FillLines();

            int upperBound = firstRow + numberOfRows;

            StaticStringBuilder.Remove(0, StaticStringBuilder.Length);

            for (int i = firstRow; i < upperBound && i < mLines.Count; i++)
            {
                StaticStringBuilder.AppendLine(mLines[i]);

            }

            TextObject.DisplayText = StaticStringBuilder.ToString();
        }


        internal void FillLines()
        {
            
            mLines.Clear();

            string splitText = "";

            if (this.mTextObject != null)
            {
                splitText = TextManager.InsertNewLines(mText, mTextObject.Spacing, mWidth, mTextObject.Font);
            }
            else
            {
                splitText = TextManager.InsertNewLines(this.mText, TextHeight/2.0f, mWidth, null);
            }

            mLines.AddRange(splitText.Split('\n'));
        }

		#endregion
	}
}
