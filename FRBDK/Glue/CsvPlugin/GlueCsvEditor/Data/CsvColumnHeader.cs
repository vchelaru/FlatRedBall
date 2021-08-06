using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueCsvEditor.Data
{
    public class CsvColumnHeader
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsRequired { get; set; }
        public bool IsList { get; set; }
    }
}
