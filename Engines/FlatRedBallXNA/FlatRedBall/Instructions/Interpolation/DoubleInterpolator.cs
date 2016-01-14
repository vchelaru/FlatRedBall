using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Instructions.Interpolation
{
    #region XML Docs
    /// <summary>
    /// Provides interpolation for doubles - used for interpolating between
    /// Keyframes in InstructionSets.
    /// </summary>
    #endregion
    public class DoubleInterpolator : IInterpolator<double>
    {
        #region XML Docs
        /// <summary>
        /// Interolates between the two argument doubles using the interpolation value.
        /// </summary>
        /// <param name="start">The first or starting value.</param>
        /// <param name="end">The end value.</param>
        /// <param name="interpolationValue">A value between 0 and 1 that determines how
        /// the values are interpolated.</param>
        /// <returns>The interpolated value.</returns>
        #endregion
        public double Interpolate(double start, double end, double interpolationValue)
        {
            return end - (end - start) * interpolationValue;
        }
    }
}
