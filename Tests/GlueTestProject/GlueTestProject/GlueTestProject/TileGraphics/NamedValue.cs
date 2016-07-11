using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TMXGlueLib.DataTypes
{
    public struct NamedValue
    {
        public string Name;
        public object Value;

        public override string ToString()
        {
            return $"{Name}={Value}";
        }
    }
}
