using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Math.Geometry;

namespace FlatRedBall.Math.Statistics
{
    #region XML Docs
    /// <summary>
    /// A class which can be used to calculate a best-fit line given a set of points.
    /// </summary>
    #endregion
    public class LinearRegression
    {
        #region Fields

        List<Point> mPoints = new List<Point>();

        #endregion

        #region Properties

        public List<Point> Points
        {
            get { return mPoints; }
        }

        #endregion

        #region Methods

        public void CalculateSlopeAndIntercept(out double slope, out double intercept)
        {
            if (mPoints.Count == 0)
            {
                slope = 0;
                intercept = 0;
                return;
            }

            double xAvg = 0;
            double yAvg = 0;

            for (int i = 0; i < mPoints.Count; i++)
            {
                xAvg += mPoints[i].X;
                yAvg += mPoints[i].Y;
            }

            xAvg = xAvg / (double)mPoints.Count;
            yAvg = yAvg / (double)mPoints.Count;
  
            double v1 = 0;
            double v2 = 0;

            for (int i = 0; i < mPoints.Count; i++)
            {

                v1 += (mPoints[i].X - xAvg) * (mPoints[i].Y - yAvg);
                v2 += System.Math.Pow(mPoints[i].X - xAvg, 2);
            }

            slope = v1 / v2;
            intercept = yAvg - slope * xAvg;
        }

        #endregion
    }
}
