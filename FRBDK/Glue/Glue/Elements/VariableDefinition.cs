using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Elements
{
    /// <summary>
    /// Defines characteristics of a variable on an object for use by Glue when displaying the
    /// variable.
    /// </summary>
    /// <remarks>
    /// VariableDefinitions are used in the AssetTypeInfo class to either define new variables or
    /// provide information about existing variables. This information can be used to improve how variables
    /// are to be displayed by Glue. VariableDefinitions are a more-informative replacement of AssetTypeInfo's
    /// ExtraVariablesPattern
    /// </remarks>.
    public class VariableDefinition
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string DefaultValue { get; set; }
        public string Category { get; set; }
        /// <summary>
        /// If true, Glue will not do the standard variable assignment. This should be true
        /// for plugins that want to fully handle their own code generation for certain variables
        /// </summary>
        public bool UsesCustomCodeGeneration { get; set; }

        public override string ToString()
        {
            return Name + " (" + Type + ")";
        }

    }
}
