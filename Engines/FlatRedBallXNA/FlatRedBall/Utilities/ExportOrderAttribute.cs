using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Utilities
{
    /// <summary>
    /// Represents the order that variables appear in Glue.
    /// </summary>
    /// <remarks>
    /// This should go away and should be handled by Glue's VariableDefinition which can decorate variables.
    /// </remarks>
    public class ExportOrderAttribute : Attribute
    {
        public int OrderValue
        {
            get;
            set;
        }

        public ExportOrderAttribute(int orderValue)
        {
            OrderValue = orderValue;
        }
    }
}
