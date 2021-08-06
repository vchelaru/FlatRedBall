using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueCsvEditor.Data
{
    public class ComplexTypeProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public List<string> Attributes { get; set; } = new List<string>();

        public override string ToString()
        {
            return $"{Type} {Name} = {Value}";
        }
    }
}
