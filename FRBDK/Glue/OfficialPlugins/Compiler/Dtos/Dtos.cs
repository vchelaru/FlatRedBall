using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.Compiler.Dtos
{
    class RemoveObjectDto
    {
        public string ElementName { get; set; }
        public string ObjectName { get; set; }
    }

    public class SetVariableDto
    {
        public string InstanceOwner { get; set; }

        public string ObjectName { get; set; }
        public string VariableName { get; set; }
        public object VariableValue { get; set; }
        public string Type { get; set; }
    }

    class SelectObjectDto
    {
        public string ObjectName { get; set; }
        public string ElementName { get; set; }
    }

    public class GlueVariableSetData
    {
        public string InstanceOwner { get; set; }
        public string VariableName { get; set; }
        public string VariableValue { get; set; }
        public string Type { get; set; }
    }
}
