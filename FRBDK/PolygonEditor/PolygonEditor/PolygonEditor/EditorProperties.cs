using System;

using FlatRedBall;
using FlatRedBall.ManagedSpriteGroups;

#if FRB_XNA
using Color = Microsoft.Xna.Framework.Graphics.Color;
#else
using System.Drawing;
#endif

namespace PolygonEditor
{
    public class EditorProperties
    {
        #region Fields
        Color mBackgrounColor;


        static Color mPolygonColor = Color.Red;
        static Color mAxisAlignedRectangleColor = Color.Yellow;
        static Color mCircleColor = Color.Orange;

        static Color mCubeColor = Color.White;

        static Color mSelectedCornerColor = Color.Green;
        static Color mNewPointPolygonColor = Color.LightBlue;

        static Color mSphereColor = Color.Cyan;

        static Color mCapsule2DColor = Color.Gray;

        #endregion

        #region Properties
        public Color BackgroundColor
        {
            get { return SpriteManager.Camera.BackgroundColor; }
            set { SpriteManager.Camera.BackgroundColor = value; }
        }

        public static Color AxisAlignedCubeColor
        {
            get { return mCubeColor; }
            set { mCubeColor = value; }
        }

        public static Color AxisAlignedRectangleColor
        {
            get { return mAxisAlignedRectangleColor; }
            set { mAxisAlignedRectangleColor = value; }
        }

        public static Color Capsule2DColor
        {
            get { return mCapsule2DColor; }
            set { mCapsule2DColor = value; }
        }

        public static Color CircleColor
        {
            get { return mCircleColor; }
            set { mCircleColor = value; }
        }

        public static Color SphereColor
        {
            get { return mSphereColor; }
            set { mSphereColor = value; }
        }

        public static Color PolygonColor
        {
            get { return mPolygonColor; }
            set { mPolygonColor = value; }
        }

        public static Color SelectedCornerColor
        {
            get { return mSelectedCornerColor; }
            set { mSelectedCornerColor = value; }
        }

        public static Color NewPointPolygonColor
        {
            get { return mNewPointPolygonColor; }
            set { mNewPointPolygonColor = value; }
        }

        #endregion

        #region Methods
        public EditorProperties()
        {
        }
        #endregion
    }
}
