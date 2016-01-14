using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Math.Splines;

namespace FlatRedBall.Content.Math.Splines
{
    #region XML Docs
    /// <summary>
    /// A savable representation of a Spline.  This is used in SplineSaveLists.
    /// </summary>
    #endregion
    public class SplineSave
    {
        #region Fields

        public string Name;

        public List<SplinePointSave> Points = new List<SplinePointSave>();

        public bool Visible;

        #endregion

        #region Methods

        public SplineSave()
        {
            Visible = true;
        }

        public static SplineSave FromSpline(Spline spline)
        {
            SplineSave toReturn = new SplineSave();

            toReturn.Name = spline.Name;
            
            for (int i = 0; i < spline.Count; i++)
            {
                toReturn.Points.Add(
                    SplinePointSave.FromSplinePoint(spline[i]));
            }

            toReturn.Visible = spline.Visible;

            return toReturn;
        }

        public Spline ToSpline()
        {
            Spline spline = new Spline();
            spline.Name = Name;

            for (int i = 0; i < Points.Count; i++)
            {
                spline.Add(Points[i].ToSplinePoint());
            }
            spline.Visible = this.Visible;
            return spline;
        }

        #endregion
    }
}
