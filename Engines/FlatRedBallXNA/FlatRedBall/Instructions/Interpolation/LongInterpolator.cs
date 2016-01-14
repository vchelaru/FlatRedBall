using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Instructions.Interpolation
{
    #region XML Docs
    /// <summary>
    /// Provides interplation methods for longs.
    /// </summary>
    #endregion
    public class LongInterpolator : IInterpolator<long>
    {

        public long Interpolate(long start, long end, double interpolationValue)
        {
            return end - (long)((end - start) * interpolationValue);
        }

    }
}
