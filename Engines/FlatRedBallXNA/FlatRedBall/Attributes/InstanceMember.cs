using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Attributes
{
    #region XML Docs
    /// <summary>
    /// Matches the marked member with the member to set when this instance is loaded through
    /// the content pipeline.  This attribute is usually applied to ExternalReference objects.
    /// </summary>
    #endregion
    public class InstanceMember : System.Attribute
    {   

        public string memberName;

        public InstanceMember(string memberName)
        {
            this.memberName = memberName;
        }   

    }
}
