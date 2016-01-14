using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Instructions
{
    #region XML Docs
    /// <summary>
    /// Provides an interface for objects which can store Instructions.
    /// </summary>
    #endregion
    public interface IInstructable
    {
        #region XML Docs
        /// <summary>
        /// The list of Instructions that this instance owns.  These instructions usually
        /// will execute on this instance; however, this is not a requirement.
        /// </summary>
        #endregion
        InstructionList Instructions
        {
            get;
        }
    }
}
