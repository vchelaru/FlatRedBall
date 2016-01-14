using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Attributes
{
    #region XML Docs
    /// <summary>
    /// Defines a member as an external instance.  Members which are external instances
    /// are not read by ContentReaders.  Rather they're set by ExternalInstances of referenced
    /// content such as Texture2Ds.
    /// </summary>
    #endregion
    public class ExternalInstance : System.Attribute
    {
    }
}
