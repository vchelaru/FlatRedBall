using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Instructions.Interpolation
{
    #region XML Docs
    /// <summary>
    /// Base interface for interpolators - to be used in lists.
    /// </summary>
    #endregion
    public interface IInterpolator
    {

    }

    #region XML Docs
    /// <summary>
    /// Generic interface for an interpolator.  These are used to interpolate
    /// Keyframes in InstructionSets.
    /// </summary>
    /// <typeparam name="T">The type that the Interpolator interpolates.</typeparam>
    #endregion
    public interface IInterpolator<T> : IInterpolator
    {
        #region XML Docs
        /// <summary>
        /// Interpolates between the first two arguments using the third as the interpolation value.
        /// </summary>
        /// <param name="start">The first value to use in interpolation.</param>
        /// <param name="end">The second value to use in interpolation.</param>
        /// <param name="interpolationValue">The interpolation value - should be between 0 and 1.</param>
        /// <returns>The result of the interpolation.</returns>
        #endregion
        T Interpolate(T start, T end, double interpolationValue);
    }
}
