using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Instructions.Interpolation
{
    #region XML Docs
    /// <summary>
    /// An IInterpolator which can interpolate float values.
    /// </summary>
    #endregion
    public class FloatInterpolator : IInterpolator<float>
    {
        #region IInterpolator<float> Members

        public float Interpolate(float start, float end, double interpolationValue)
        {
            return end - (float)((end - start) * interpolationValue);
        }

        #endregion
    }
}
