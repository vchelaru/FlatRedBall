using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace FlatRedBall.Glue.Elements
{
    #region Enums

    public enum CsvInclusion
    {
        None = 0,
        InThisElement = 1,
        InBaseElements = 2,
        InDerivedElements = 4,
        InUnrelatedElements = 8,
        InGlobalContent = 16,
        AllInProject = InThisElement | InBaseElements | InDerivedElements | InUnrelatedElements | InGlobalContent,


    }
    #endregion


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
        /// <summary>
        /// The value of this variable on the backing class implementation.
        /// This is not a forced default value. The default value will not be code-generated.
        /// </summary>
        public string DefaultValue { get; set; }
        public string Category { get; set; }

        /// <summary>
        /// If true, Glue will not do the standard variable assignment. This should be true
        /// for plugins that want to fully handle their own code generation for certain variables
        /// </summary>
        public bool UsesCustomCodeGeneration { get; set; }

        /// <summary>
        /// Func returning the assignment for a variable.
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        // The second to last string is the name of the variable, which may be a tunneled variable
        public Func<IElement, NamedObjectSave, ReferencedFileSave, string, string> CustomGenerationFunc;

        /// <summary>
        /// Action for filling the code block with the custom definition for a property.
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public Func<IElement, CustomVariable, string> CustomPropertySetFunc;


        /// <summary>
        /// A list of options which the user must pick from. Filling this list with
        /// options prevents the user form freely entering values. This can be used for
        /// functionality similar to enums.
        /// </summary>
        public List<string> ForcedOptions { get; set; } = new List<string>();

        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }

        // Vic says - maybe we'll need this? Not sure...
        //public CsvInclusion PrimaryCsvInclusion { get; set; } = CsvInclusion.InThisElement;
        //public CsvInclusion FallbackCsvInclusion { get; set; } = CsvInclusion.AllInProject;

        [XmlIgnore]
        [JsonIgnore]
        public Func<IElement, NamedObjectSave, ReferencedFileSave, List<string>> CustomGetForcedOptionFunc;


        public bool HasGetter { get; set; } = true;

        [XmlIgnore]
        [JsonIgnore]
        public Type PreferredDisplayer { get; set; }

        public override string ToString()
        {
            return Name + " (" + Type + ")";
        }

        public object GetCastedDefaultValue()
        {
            var toReturn = this.DefaultValue;

            return GetCastedValueForType(this.Type, this.DefaultValue);
        }

        public static object GetCastedValueForType(string typeName, string value)
        { 
            var toReturn = value;
            if (typeName == "bool")
            {
                bool boolToReturn = false;

                bool.TryParse(value, out boolToReturn);

                return boolToReturn;
            }
            else if (typeName == "float")
            {
                float floatToReturn = 0.0f;

                float.TryParse(value, out floatToReturn);

                return floatToReturn;
            }
            else if (typeName == "int")
            {
                int intToReturn = 0;

                int.TryParse(value, out intToReturn);

                return intToReturn;
            }
            else if (typeName == "long")
            {
                long longToReturn = 0;

                long.TryParse(value, out longToReturn);

                return longToReturn;
            }
            else if (typeName == "double")
            {
                double doubleToReturn = 0.0;

                double.TryParse(value, out doubleToReturn);

                return doubleToReturn;
            }
            else
            {
                return toReturn;
            }
        }
    }
}
