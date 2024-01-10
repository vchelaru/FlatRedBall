using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.SaveClasses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Serialization;

namespace FlatRedBall.Glue.Elements
{
     public static class VariableDefinitionExtensionMethods
    {
        public static object GetCastedDefaultValue(this VariableDefinition variable)
        {
            var toReturn = variable.DefaultValue;

            return TypeManager.Parse(variable.Type, variable.DefaultValue);
        }

    }
}
