using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.Parsing;
using Newtonsoft.Json;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Split out of <c>NamedObjectSaveExtensionMethods</c> (in <c>Glue.csproj</c>, net8.0-windows): the
    /// type fix-ups over a <see cref="NamedObjectSave"/>'s instructions and properties, run after a
    /// Json load/clone turns ints into longs and enums into ints. They resolve enum types through
    /// <see cref="ITypeResolutionCore"/> (a narrow seam over <c>TypeManager.GetTypeFromString</c>) and
    /// variable definitions through <see cref="IAvailableAssetTypesCore"/> (via
    /// <see cref="NamedObjectSaveAssetTypeExtensions.GetAssetTypeInfo"/>) instead of reaching for the
    /// Glue singletons directly. Lives here (net8.0, no WPF) so it and its tests can build and run on
    /// Linux/macOS. See #2276. Named differently from the original class (not a forwarding stub) to
    /// avoid a duplicate-type clash now that both assemblies are visible together via
    /// <c>Glue.csproj</c>'s <c>ProjectReference</c> to <c>GlueCommon</c>; extension method resolution
    /// doesn't care which class declares it, so existing call sites are unaffected.
    /// </summary>
    public static class NamedObjectSaveTypeExtensions
    {
        public static NamedObjectSave Clone(this NamedObjectSave instance)
        {
            // This doesn't work as well in XML due to enum values, so let's use Json instead
            //NamedObjectSave newNamedObjectSave = FileManager.CloneObject(instance);
            var serialized = JsonConvert.SerializeObject(instance);
            var newNamedObjectSave = JsonConvert.DeserializeObject<NamedObjectSave>(serialized);

            newNamedObjectSave.UpdateCustomProperties();
            // March 6, 2012
            // UpdateCustomProperties
            // creates the InstructionSaves
            // for the NamedObjectSave according
            // to the variables for this object; however,
            // an object may have InstructionSaves for variables
            // that aren't part of its type - they may exist because
            // the user has switched from an old type and Glue is holding
            // on to those old values in case the user wants to switch back.
            // Therefore, we shouldn't fill the instruction saves this way, instead
            // let's just have the instruction saves be Added.
            newNamedObjectSave.InstructionSaves = new List<CustomVariableInNamedObject>();

            newNamedObjectSave.ContainedObjects = new List<NamedObjectSave>(instance.ContainedObjects.Count);

            for (int i = 0; i < instance.InstructionSaves.Count; i++)
            {
                // See above on why we use json
                var instructionSerialized = JsonConvert.SerializeObject(instance.InstructionSaves[i]);

                var duplicateInstruction = JsonConvert.DeserializeObject<CustomVariableInNamedObject>(instructionSerialized);
                    //FileManager.CloneObject(instance.InstructionSaves[i]);

                // Events are instance-specific so we prob don't want to copy those
                duplicateInstruction.EventOnSet = null;

                newNamedObjectSave.InstructionSaves.Add(duplicateInstruction);
            }
            newNamedObjectSave.FixAllTypes();

            foreach (NamedObjectSave containedNamedObject in instance.ContainedObjects)
            {
                newNamedObjectSave.ContainedObjects.Add(containedNamedObject.Clone());
            }

            return newNamedObjectSave;
        }

        public static void FixAllTypes(this NamedObjectSave instance)
        {
            var ati = instance.GetAssetTypeInfo();
            foreach (CustomVariableInNamedObject instruction in instance.InstructionSaves)
            {
                if(instruction.Type == null)
                {
                    var existingVariableDefinition = ati?.VariableDefinitions.FirstOrDefault(item => item.Name == instruction.Member);

                    instruction.Type = existingVariableDefinition?.Type;
                }
                FixAllTypes(instruction);
            }

            foreach(var property in instance.Properties)
            {
                // special case it:
                if(property.Name == "DestinationRectangle" && property.Value is string asString)
                {
                    property.Value = CustomVariableCommonExtensions.FixValue(asString, "FloatRectangle?");

                }
                else
                {
                    FixAllTypes(property);
                }
            }

            foreach (NamedObjectSave contained in instance.ContainedObjects)
            {
                contained.FixAllTypes();
            }
        }

        public static void FixEnumerationTypes(this NamedObjectSave instance)
        {
            foreach (CustomVariableInNamedObject instruction in instance.InstructionSaves)
            {
                FixEnumerationType(instruction);
            }

            foreach (NamedObjectSave contained in instance.ContainedObjects)
            {
                contained.FixEnumerationTypes();
            }
        }

        private static void FixAllTypes(CustomVariableInNamedObject instruction)
        {
            FixEnumerationType(instruction);

            if (!string.IsNullOrEmpty(instruction.Type) && instruction.Value != null)
            {
                object variableValue = instruction.Value;
                var type = instruction.Type;
                variableValue = CustomVariableCommonExtensions.FixValue(variableValue, type);
                instruction.Value = variableValue;
            }
        }


        private static void FixAllTypes(PropertySave property)
        {
            if (!string.IsNullOrEmpty(property.Type) && property.Value != null)
            {
                object variableValue = property.Value;
                var type = property.Type;

                variableValue = CustomVariableCommonExtensions.FixValue(variableValue, type);

                property.Value = variableValue;
            }
        }

        private static void FixEnumerationType(CustomVariableInNamedObject instruction)
        {
            if (!string.IsNullOrEmpty(instruction.Type))
            {
                Type type = TypeResolutionCore.Self.GetTypeFromString(instruction.Type);

                if (type != null && instruction.Value != null && type.IsEnum
                    // it may already be an enum:
                    && instruction.Value.GetType() != type)
                {
                    int valueAsInt = 0;
                    if (instruction.Value is int asInt)
                    {
                        valueAsInt = asInt;
                    }
                    else if (instruction.Value is long asLong)
                    {
                        valueAsInt = (int)asLong;
                    }
                    Array array = Enum.GetValues(type);

                    // The enumerations may not necessarily be
                    // 0,1,2,3,4
                    // They may skip values or start at non-zero values.
                    // Therefore, we need to compare the int values
                    for (int i = 0; i < array.Length; i++)
                    {
                        if ((int)(array.GetValue(i)) == valueAsInt)
                        {
                            instruction.Value = array.GetValue(i);
                            break;
                        }
                    }
                }
            }
        }

        public static void ResetVariablesReferencing(this NamedObjectSave namedObject, ReferencedFileSave rfs)
        {
            for(int i = namedObject.InstructionSaves.Count - 1; i > -1 ; i--)
            {
                var variable = namedObject.InstructionSaves[i];

                if (CustomVariableTypeExtensions.GetIsFile(variable.Type) && (string)(variable.Value) == rfs.GetInstanceName())
                {
                    // We're going to make it null, but
                    // we don't save null instructions in
                    // NOS's so that our .glux stays small
                    // and so there's less chances of conflicts
                    // occurring because of undefined sorting behavior.
                    namedObject.InstructionSaves.RemoveAt(i);
                }
            }

        }
    }
}
