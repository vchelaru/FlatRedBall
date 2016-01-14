using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Attributes
{
    #region XML Docs
    /// <summary>
    /// Used internally by the engine to determine the order of 
    /// reading and writing for instances.
    /// </summary>
    #endregion
    public class InstanceListMember : System.Attribute
    {
        public string memberName;

        public InstanceListMember(string memberName)
        {
            this.memberName = memberName;
        }

    }
}
