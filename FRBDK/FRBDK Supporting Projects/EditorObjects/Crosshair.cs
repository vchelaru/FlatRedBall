using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Math.Geometry;

#if FRB_MDX
using Microsoft.DirectX;
using System.Drawing;
#else
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Color = Microsoft.Xna.Framework.Graphics.Color;
#endif

namespace EditorObjects
{
    public class Crosshair : PositionedObject
    {
        #region Fields
        public Color mColor;
        private Line[] mCrossHair;

        #endregion

        #region Properties
        public Color Color
        {
            get { return mColor; }
            set { 
                    mColor = value;
                    mCrossHair[0].Color = mColor;
                    mCrossHair[1].Color = mColor;
                }
        }

        public Boolean Visible
        {
            get { return mCrossHair[0].Visible && mCrossHair[1].Visible; }
            set {
                    mCrossHair[0].Visible = value;
                    mCrossHair[1].Visible = value;
                }
        }
        #endregion

        #region Methods

        #region Constructor
        public Crosshair()
        {
            mCrossHair = new Line[2];

            mCrossHair[0] = ShapeManager.AddLine();
            mCrossHair[1] = ShapeManager.AddLine();
            mCrossHair[1].RotationZ = (float)Math.PI / 2.0f;

            mCrossHair[0].ScaleBy(0.25F);
            mCrossHair[1].ScaleBy(0.25F);

            mCrossHair[0].AttachTo(this, true);
            mCrossHair[1].AttachTo(this, true);
            SpriteManager.AddPositionedObject(this);

            mColor = Color.White;
            mCrossHair[0].Color = mColor;
            mCrossHair[1].Color = mColor;
        }
        #endregion

        public void Destroy()
        {
            ShapeManager.Remove(mCrossHair[0]);
            ShapeManager.Remove(mCrossHair[1]);
            SpriteManager.RemovePositionedObject(this);
        }

        public void UpdateScale()
        {
            UpdateScale(SpriteManager.Camera);
        }

        public void UpdateScale(Camera camera)
        {
            float scale = (float)mCrossHair[0].RelativePoint1.X;

            float desiredScale = 5 / camera.PixelsPerUnitAt(this.Z);

            mCrossHair[0].ScaleBy(desiredScale / scale);
            mCrossHair[1].ScaleBy(desiredScale / scale);

        }

        #endregion
    }

}
