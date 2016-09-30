using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TMXGlueLib.DataTypes
{
    public struct NamedValue
    {
        public string Name;
        public string Type;
        public object Value;

        public override string ToString()
        {
            if(string.IsNullOrEmpty(Type))
            {
                return $"{Name}={Value}";
            }
            else
            {
                return $"{Type} {Name}={Value}";
            }

        }
    }
}
