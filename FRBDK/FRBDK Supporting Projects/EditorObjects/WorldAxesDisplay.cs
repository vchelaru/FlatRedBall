using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;

using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;

#if XNA4
using Color = Microsoft.Xna.Framework.Color;

#elif FRB_XNA
using Color = Microsoft.Xna.Framework.Graphics.Color;
#else

using Color = System.Drawing.Color;
#endif

namespace EditorObjects
{
    public class WorldAxesDisplay
    {
        #region Fields

        Line mXAxis;
        Line mYAxis;

        #endregion

        #region Properties

        public Color Color
        {
            get { return mXAxis.Color; }
            set
            {
                mXAxis.Color = value;
                mYAxis.Color = value;
            }
        }

        public bool Visible
        {
            get { return mXAxis.Visible; }
            set
            {
                mXAxis.Visible = value;
                mYAxis.Visible = value;
            }
        }

        #endregion 

        #region Methods

        public WorldAxesDisplay()
        {
            mXAxis = ShapeManager.AddLine();
            mXAxis.RelativePoint1 = new Point3D(0, 1000000);
            mXAxis.RelativePoint2 = new Point3D(0, -1000000);

            mYAxis = ShapeManager.AddLine();
            mYAxis.RelativePoint1 = new Point3D(1000000, 0);
            mYAxis.RelativePoint2 = new Point3D(-1000000, 0);

#if FRB_XNA
            mXAxis.Color = new Color(40, 40, 40, 255);
            mYAxis.Color = new Color(40, 40, 40, 255);
#else
            mXAxis.Color = Color.FromArgb(255, 40, 40, 40);            
            mYAxis.Color = Color.FromArgb(255, 40, 40, 40);
#endif
        }

        #endregion
    }
}
