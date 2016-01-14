using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Plugins.Interfaces
{
    public interface INamedObject : IPlugin
    {
        void ReactToNamedObjectChangedValue(string changedMember, object oldValue);
    }
}
