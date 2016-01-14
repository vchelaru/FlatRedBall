using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Plugins.Interfaces
{
    public interface IPropertyChange : IPlugin
    {
        void ReactToChangedProperty(string changedMember, object oldValue);
    }
}
