using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using FlatRedBall.Utilities;

namespace FlatRedBall.Math
{
    #region XML Docs
    /// <summary>
    /// Interface defining that an object has a RemoveObject method.  This standardizes the way that objects remove themselves from
    /// two-way lists.
    /// </summary>
    /// <remarks>
    /// This should not be implemented outside of the FlatRedBall Engine, but is public so that PositionedOjbect-inheriting
    /// objects can access the lists of objects that they belong to.
    /// </remarks>
    #endregion
    public interface IAttachableRemovable : INameable, IList
    {
        void RemoveGuaranteedContain(IAttachable attachable);
    }
}
