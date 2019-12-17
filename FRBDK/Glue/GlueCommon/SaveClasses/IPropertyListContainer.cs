using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Interfaces
{
    public interface IPropertyListContainer
    {
        List<PropertySave> Properties
        {
            get;
            set;
        }
    }
}
