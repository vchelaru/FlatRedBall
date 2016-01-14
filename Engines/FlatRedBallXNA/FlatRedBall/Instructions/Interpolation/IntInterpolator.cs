using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Instructions.Interpolation
{
    #region XML Docs
    /// <summary>
    /// An IInterpolator which can interpolate integer values.
    /// </summary>
    #endregion
    public class IntInterpolator : IInterpolator<int>
    {
        public int Interpolate(int start, int end, double interpolationValue)
        {
            return end - (int)((end - start) * interpolationValue);
        }
    }
}
