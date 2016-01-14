using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Math.Geometry;
using FlatRedBall.ManagedSpriteGroups;

namespace EditorObjects.Hud
{
	public class SpriteFrameIndividualSpriteOutline
	{
		#region Fields

		Line[] mVerticalLines = new Line[2];
		Line[] mHorizontalLines = new Line[2];

		bool mVisible;

		#endregion

		#region Properties

		public SpriteFrame SpriteFrame
		{
			get;
			set;
		}

		public bool Visible
		{
			get { return mVisible; }
			set
			{
				mVisible = value;
				UpdateLineVisibility();
			}
		}

		private void UpdateLineVisibility()
		{
			if (SpriteFrame == null)
			{
				mVerticalLines[0].Visible = false;
				mVerticalLines[1].Visible = false;

				mHorizontalLines[0].Visible = false;
				mHorizontalLines[1].Visible = false;
			}
			else
			{
				mVerticalLines[0].Visible = mVisible &&
					(SpriteFrame.Borders & SpriteFrame.BorderSides.Left) == SpriteFrame.BorderSides.Left;
				mVerticalLines[1].Visible = mVisible &&
					(SpriteFrame.Borders & SpriteFrame.BorderSides.Right) == SpriteFrame.BorderSides.Right;

				mHorizontalLines[0].Visible = mVisible &&
					(SpriteFrame.Borders & SpriteFrame.BorderSides.Top) == SpriteFrame.BorderSides.Top;
				mHorizontalLines[1].Visible = mVisible &&
					(SpriteFrame.Borders & SpriteFrame.BorderSides.Bottom) == SpriteFrame.BorderSides.Bottom;
			}
		}

		#endregion

		#region Methods

		#region Constructor

		public SpriteFrameIndividualSpriteOutline()
		{
			for (int i = 0; i < 2; i++)
			{
				mVerticalLines[i] = ShapeManager.AddLine();
				mHorizontalLines[i] = ShapeManager.AddLine() ;
			}
		}

		public void Update()
		{
			float verticalDivisionOffset = SpriteFrame.ScaleX - SpriteFrame.SpriteBorderWidth;
			float horizontalDivisionOffset = SpriteFrame.ScaleY - SpriteFrame.SpriteBorderWidth;

			mVerticalLines[0].RelativePoint1.X = -verticalDivisionOffset;
			mVerticalLines[0].RelativePoint1.Y = SpriteFrame.ScaleY;
			mVerticalLines[0].RelativePoint2.X = -verticalDivisionOffset;
			mVerticalLines[0].RelativePoint2.Y = -SpriteFrame.ScaleY;

			mVerticalLines[1].RelativePoint1.X = verticalDivisionOffset;
			mVerticalLines[1].RelativePoint1.Y = SpriteFrame.ScaleY;
			mVerticalLines[1].RelativePoint2.X = verticalDivisionOffset;
			mVerticalLines[1].RelativePoint2.Y = -SpriteFrame.ScaleY;

			mHorizontalLines[0].RelativePoint1.X = -SpriteFrame.ScaleX;
			mHorizontalLines[0].RelativePoint1.Y = horizontalDivisionOffset;
			mHorizontalLines[0].RelativePoint2.X = SpriteFrame.ScaleX;
			mHorizontalLines[0].RelativePoint2.Y = horizontalDivisionOffset;

			mHorizontalLines[1].RelativePoint1.X = -SpriteFrame.ScaleX;
			mHorizontalLines[1].RelativePoint1.Y = -horizontalDivisionOffset;
			mHorizontalLines[1].RelativePoint2.X = SpriteFrame.ScaleX;
			mHorizontalLines[1].RelativePoint2.Y = -horizontalDivisionOffset;

			mVerticalLines[0].Position = mVerticalLines[1].Position =
				mHorizontalLines[0].Position = mHorizontalLines[1].Position = SpriteFrame.Position;

			mVerticalLines[0].RotationMatrix = mVerticalLines[1].RotationMatrix =
				mHorizontalLines[0].RotationMatrix = mHorizontalLines[1].RotationMatrix = SpriteFrame.RotationMatrix;

			UpdateLineVisibility();
		}

		#endregion

		#endregion
	}
}
