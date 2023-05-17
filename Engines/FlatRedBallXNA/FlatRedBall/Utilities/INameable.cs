using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Utilities
{
    #region XML Docs
    /// <summary>
    /// Defines that an object has a name.
    /// </summary>
    /// <remarks>
    /// Objects which are referenced by other objects in serializable classes
    /// should be INameable so that the in-memory reference can be coverted to
    /// a string and then re-created when the object is deserialized.
    /// </remarks>
    #endregion
    public interface INameable
    {
        /// <summary>
        /// The name of the object.
        /// </summary>
        string Name { get; set;}
    }
}
