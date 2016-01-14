using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Attributes
{
    #region XML Docs
    /// <summary>
    /// Attribute marking a member in a save class as an external instance.  That is, it should
    /// be treated as external content by the ObjectReaders and ObjectWriters in the FlatRedBall
    /// content pipeline.
    /// </summary>
    #endregion
    public class ExternalInstanceList : System.Attribute
    {
    }
}