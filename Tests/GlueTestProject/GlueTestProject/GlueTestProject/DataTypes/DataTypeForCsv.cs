using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueTestProject.DataTypes
{
    public class DataTypeForCsv
    {
        public string Name { get; set; }
        public bool IsMagical { get; set; }
        public int Value { get; set; }
        public List<string> WhereCanBeBought { get; set; }
    }
}
