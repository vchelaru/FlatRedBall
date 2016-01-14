using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueView.Scripting
{
    public interface IAssignableReference
    {
        //void Assign(Object value);

        Type TypeOfReference
        {
            get;
        }

        object CurrentValue
        {
            get;
            set;
        }
    }
}
