using System;
using System.Text;
//using System.Drawing;

using FlatRedBall;
using FlatRedBall.Gui;

using FlatRedBall.Math.Geometry;

#if FRB_MDX
using Microsoft.DirectX;
using FlatRedBall.Math;
using FlatRedBall.Input;
#elif FRB_XNA
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Point = FlatRedBall.Math.Geometry.Point;
using FlatRedBall.Math;
using FlatRedBall.Input;
#endif

namespace EditorObjects
{
	/// <summary>
	/// Summary description for RectangleSelector.
	/// </summary>
	public class RectangleSelector
	{
		#region Fields
		Vector3 mOriginalPoint;

		bool mRectangleStarted = false;

        AxisAlignedRectangle mRectangle;

		#endregion

        #region Methods

        #region Constructor

        public RectangleSelector()
		{
            mRectangle = ShapeManager.AddAxisAlignedRectangle();
            mRectangle.Visible = false;
        }

        #endregion

        #region Public Methods

        public void Control()
		{
			#region pushing on the cursor
			if(GuiManager.Cursor.PrimaryPush)
			{
                mRectangleStarted = false;
                if (GuiManager.Cursor.WindowPushed == null)
                {
                    float x = 0;
                    float y = 0;

                    GuiManager.Cursor.GetCursorPosition(out x, out y, 0);

                    mOriginalPoint.X = x;
                    mOriginalPoint.Y = y;
                    mRectangleStarted = true;
                }
			}

			#endregion

			#region dragging the cursor

			if(GuiManager.Cursor.PrimaryDown)
			{
				float x = 0;
				float y = 0;
				GuiManager.Cursor.GetCursorPosition(out x, out y, 0);

                mRectangle.Visible = true;

                mRectangle.X = (x + mOriginalPoint.X) / 2.0f;
                mRectangle.Y = (y + mOriginalPoint.Y) / 2.0f;


                mRectangle.ScaleX = Math.Abs(x - mOriginalPoint.X) / 2.0f;
                mRectangle.ScaleY = Math.Abs(y - mOriginalPoint.Y) / 2.0f;
			}

			#endregion

            // Don't set it on a click so that there's a 1 frame buffer before it becomes invisible
            if (GuiManager.Cursor.PrimaryDown == false && !GuiManager.Cursor.PrimaryClick)
            {
                mRectangle.Visible = false; 
                mRectangleStarted = false;
            }
		}

        public void GetObjectsOver<T>(AttachableList<T> allObjects, AttachableList<T> objectsOver) where T: PositionedObject, IAttachable, ICursorSelectable
        {
            if (mRectangle.Visible)
            {
                mRectangleStarted = false;
                float x = 0;
                float y = 0;
                GuiManager.Cursor.GetCursorPosition(out x, out y, 0);

                mRectangle.X = (x + mOriginalPoint.X) / 2.0f;
                mRectangle.Y = (y + mOriginalPoint.Y) / 2.0f;


                mRectangle.ScaleX = Math.Abs(x - mOriginalPoint.X) / 2.0f;
                mRectangle.ScaleY = Math.Abs(y - mOriginalPoint.Y) / 2.0f;

                if (System.Math.Abs(x - mOriginalPoint.X) > .1f &&
                    System.Math.Abs(y - mOriginalPoint.Y) > .1f)
                {
                    Polygon spritePolygon = new Polygon();
                    Point[] spritePoints = new Point[5];

                    foreach (T t in allObjects)
                    {
                        if (t.CursorSelectable)
                        {
                            spritePoints[0] = new Point(-t.ScaleX, t.ScaleY);
                            spritePoints[1] = new Point(t.ScaleX, t.ScaleY);
                            spritePoints[2] = new Point(t.ScaleX, -t.ScaleY);
                            spritePoints[3] = new Point(-t.ScaleX, -t.ScaleY);
                            spritePoints[4] = spritePoints[0];

                            spritePolygon.Points = spritePoints;
                            spritePolygon.Position = t.Position;
                            spritePolygon.RotationMatrix = t.RotationMatrix;

                            if (spritePolygon.CollideAgainst(mRectangle))
                            {

                                objectsOver.Add(t);
                            }
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Original Point:").Append(mOriginalPoint);
            stringBuilder.Append("\nRectangle Started:").Append(mRectangleStarted);

            return stringBuilder.ToString();
        }

        #endregion

        #region Private Methods


        #endregion

        #endregion
    }
}
