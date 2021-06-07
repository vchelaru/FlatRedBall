using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.Compiler.Dtos
{
    public class SetVariableDto
    {
        public string ObjectName { get; set; }
        public string VariableName { get; set; }
        public object VariableValue { get; set; }
        public string Type { get; set; }

    }
}
