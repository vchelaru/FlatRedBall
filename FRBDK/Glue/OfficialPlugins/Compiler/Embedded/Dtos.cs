using System;
using System.Collections.Generic;
using System.Text;

namespace {ProjectNamespace}.GlueControl.Dtos
{
    class RemoveObjectDto
    {
        public string ElementName { get; set; }
        public string ObjectName { get; set; }
    }

    class SetVariableDto
    {
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
        public string VariableName { get; set; }
        public string VariableValue { get; set; }
        public string Type { get; set; }
    }
}
