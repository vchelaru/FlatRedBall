using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Graphics
{
    #region XML Docs
    /// <summary>
    /// Interface for an object which has visibility control.
    /// </summary>
    #endregion
    public interface IVisible
    {
        bool Visible
        {
            get;
            set;
        }

        IVisible Parent
        {
            get;
        }

        bool AbsoluteVisible
        {
            get;
        }

        bool IgnoresParentVisibility
        {
            get;
            set;
        }
    }
}
