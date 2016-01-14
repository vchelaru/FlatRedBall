using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Math.Geometry
{
    #region XML Docs
    /// <summary>
    /// An interface for objects which can be scaled on the X, Y, and Z axes.
    /// </summary>
    #endregion
    public interface IScalable3D : IScalable
    {
        // ScaleX and ScaleY come from IScalable
        //new float ScaleX { get; set; }
        //new float ScaleY { get; set; }
        float ScaleZ { get; set; }

        // ScaleXVelocity and ScaleYVelocity are in IScalable
        //float ScaleXVelocity { get; set; }
        //float ScaleYVelocity { get; set; }
        float ScaleZVelocity { get; set; }
    }
}
