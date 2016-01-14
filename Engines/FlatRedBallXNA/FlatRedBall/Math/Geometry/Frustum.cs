using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Math.Geometry
{
    #region XML Docs
    /// <summary>
    /// A drawable, 3D volume defining the visible area of a camera.  This can be used to 
    /// display the visible area of a Camera. 
    /// </summary>
    #endregion
    public class Frustum
    {
        #region Fields

        const int NumberOfLines = 12;

        PositionedObjectList<Line> mLines = new PositionedObjectList<Line>(NumberOfLines);

        #endregion

        #region Methods

        #region Constructor

        public Frustum(BoundingFrustum sourceBoundingFrustum)
        {
            UpdateFromBoundingFrustum(sourceBoundingFrustum);
        }

        #endregion

        #region Public Methods

        public void UpdateFromBoundingFrustum(BoundingFrustum sourceBoundingFrustum)
        {
            #region Create the lines if necessary

            if (mLines.Count == 0)
            {
                for (int i = 0; i < NumberOfLines; i++)
                {
                    Line line = ShapeManager.AddLine();
                    mLines.Add(line);
                }
            }

            #endregion

            Vector3[] corners = sourceBoundingFrustum.GetCorners();

            mLines[0].SetFromAbsoluteEndpoints(corners[0], corners[1]);
            mLines[1].SetFromAbsoluteEndpoints(corners[1], corners[2]);
            mLines[2].SetFromAbsoluteEndpoints(corners[2], corners[3]);
            mLines[3].SetFromAbsoluteEndpoints(corners[3], corners[0]);

            mLines[4].SetFromAbsoluteEndpoints(corners[4], corners[5]);
            mLines[5].SetFromAbsoluteEndpoints(corners[5], corners[6]);
            mLines[6].SetFromAbsoluteEndpoints(corners[6], corners[7]);
            mLines[7].SetFromAbsoluteEndpoints(corners[7], corners[4]);

            mLines[8].SetFromAbsoluteEndpoints(corners[0], corners[4]);
            mLines[9].SetFromAbsoluteEndpoints(corners[1], corners[5]);
            mLines[10].SetFromAbsoluteEndpoints(corners[2], corners[6]);
            mLines[11].SetFromAbsoluteEndpoints(corners[3], corners[7]);


            sourceBoundingFrustum.Near.ToString();
        }

        #endregion

        #endregion
    }
}
