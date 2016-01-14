using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math.Geometry;
using FlatRedBall;
using FlatRedBall.Math;
using FlatRedBall.Graphics;

#if FRB_MDX
using Microsoft.DirectX;
using Color = System.Drawing.Color;
#else
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Color = Microsoft.Xna.Framework.Graphics.Color;
#endif

namespace EditorObjects.Hud
{
    public class ScalableSelector : PositionedObject
    {
        #region Fields

        Polygon mDarkPolygon;
        Polygon mLightPolygon;

        #endregion

        #region Properties

        public bool Visible
        {
            get { return mDarkPolygon.Visible; }
            set
            {
                mDarkPolygon.Visible = value;
                mLightPolygon.Visible = value;
            }
        }

        #endregion

        #region Methods

        public ScalableSelector()
        {
            mDarkPolygon = Polygon.CreateRectangle(1, 1);
            mDarkPolygon.Color = Color.Black;

            mLightPolygon = Polygon.CreateRectangle(1, 1);

            SpriteManager.AddPositionedObject(this);

            ShapeManager.AddPolygon(mDarkPolygon);
            ShapeManager.AddPolygon(mLightPolygon);

            mDarkPolygon.AttachTo(this, false);
            mLightPolygon.AttachTo(this, false);
        }

        public void Destroy()
        {
            ShapeManager.Remove(mDarkPolygon);
            ShapeManager.Remove(mLightPolygon);
            SpriteManager.RemovePositionedObject(this);
        }

        public void UpdateToObject<T>(T objectToUpdateTo,
            Camera camera) where T : IScalable, IPositionable, IRotatable
        {
            UpdateToObject(
                new Vector3(objectToUpdateTo.X, objectToUpdateTo.Y, objectToUpdateTo.Z),
                objectToUpdateTo.RotationMatrix,
                objectToUpdateTo.ScaleX,
                objectToUpdateTo.ScaleY,
                camera);
        }


        public void UpdateToObject(Text textToUpdateTo, Camera camera)
        {
            Vector3 position = new Vector3(textToUpdateTo.HorizontalCenter, textToUpdateTo.VerticalCenter, textToUpdateTo.Z);

            MathFunctions.RotatePointAroundPoint(textToUpdateTo.Position, ref position, textToUpdateTo.RotationZ);

            UpdateToObject(position, textToUpdateTo.RotationMatrix, textToUpdateTo.ScaleX, textToUpdateTo.ScaleY, camera);
        }


        public void UpdateToObject(Vector3 position, Matrix rotationMatrix, float scaleX, float scaleY, Camera camera)
        {
            this.Position = position;

            this.RotationMatrix = rotationMatrix;

            // Use 2 so that there's a 1-pixel space between the inner and outer
            float extraDistance = (float)(2) / camera.PixelsPerUnitAt(position.Z);

            mDarkPolygon.SetPoint(0, -scaleX - extraDistance,
                scaleY + extraDistance);
            mDarkPolygon.SetPoint(1, scaleX + extraDistance,
                scaleY + extraDistance);
            mDarkPolygon.SetPoint(2, scaleX + extraDistance,
                -scaleY - extraDistance);
            mDarkPolygon.SetPoint(3, -scaleX - extraDistance,
                -scaleY - extraDistance);
            mDarkPolygon.SetPoint(4, -scaleX - extraDistance,
                scaleY + extraDistance);

            mLightPolygon.SetPoint(0, -scaleX, scaleY);
            mLightPolygon.SetPoint(1, scaleX, scaleY);
            mLightPolygon.SetPoint(2, scaleX, -scaleY);
            mLightPolygon.SetPoint(3, -scaleX, -scaleY);
            mLightPolygon.SetPoint(4, -scaleX, scaleY);


        }

        #endregion
    }
}
