using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Gui
{
    #region XML Docs
    /// <summary>
    /// Interface for an object which provides a visual interface for viewing and editing
    /// an instance.  This is the base type for PropertyGrids and ListDisplayWindows.
    /// </summary>
    #endregion
    public interface IObjectDisplayer
    {
        #region XML Docs
        /// <summary>
        /// Exposes the object that is being displayed (casted as an object).
        /// This exists so that a list of IObjectDisplayers has access to the
        /// object displaying.  This should be explicitly implemented.
        /// </summary>
        #endregion
        object ObjectDisplayingAsObject
        {
            get;
            set;
        }

        void UpdateToObject();
    }


    public interface IObjectDisplayer<T> : IObjectDisplayer
    {
        T ObjectDisplaying
        {
            get;
            set;
        }
    }
}
