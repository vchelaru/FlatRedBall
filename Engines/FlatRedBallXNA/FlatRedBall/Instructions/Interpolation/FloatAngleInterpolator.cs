using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math;

namespace FlatRedBall.Instructions.Interpolation
{
    #region XML Docs
    /// <summary>
    /// An interpolator which interpolates between angle floats.
    /// </summary>
    #endregion
    public class FloatAngleInterpolator : IInterpolator<float>
    {
        public float Interpolate(float start, float end, double interpolationValue)
        {
            return end -
                (float)(MathFunctions.AngleToAngle(start, end) * interpolationValue);
        }
    }
}
