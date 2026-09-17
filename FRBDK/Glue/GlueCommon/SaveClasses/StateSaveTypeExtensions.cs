using System;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.Parsing;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Split out of <c>StateSaveExtensionMethods</c> (in <c>Glue.csproj</c>, net8.0-windows): the type
    /// fix-ups over a <see cref="StateSave"/>'s instructions, run after a Json load turns ints into
    /// longs and enums into ints. They resolve enum types through <see cref="ITypeResolutionCore"/> (a
    /// narrow seam over <c>TypeManager.GetTypeFromString</c>) instead of reaching for the Glue
    /// singleton directly. Lives here (net8.0, no WPF) so it and its tests can build and run on
    /// Linux/macOS. See #2276. Named differently from the original class (not a forwarding stub) to
    /// avoid a duplicate-type clash now that both assemblies are visible together via
    /// <c>Glue.csproj</c>'s <c>ProjectReference</c> to <c>GlueCommon</c>; extension method resolution
    /// doesn't care which class declares it, so existing call sites are unaffected.
    /// </summary>
    public static class StateSaveTypeExtensions
    {
        public static void FixAllTypes(this StateSave instance, GlueElement owner)
        {
            instance.FixEnumerationTypes();

            foreach (InstructionSave instruction in instance.InstructionSaves)
            {
                if (!string.IsNullOrEmpty(instruction.Type) && instruction.Value != null)
                {
                    object variableValue = instruction.Value;

                    var matchingVariable = owner.GetCustomVariable(instruction.Member);

                    var variableType = matchingVariable?.Type;

                    if( !string.IsNullOrWhiteSpace(variableType) && variableType != instruction.Type)
                    {
                        // The variable type has been changed, so let's update the state type:
                        instruction.Type = variableType;
                    }

                    var type = instruction.Type;
                    variableValue = CustomVariableCommonExtensions.FixValue(variableValue, type);
                    instruction.Value = variableValue;
                }
            }
        }

        public static void FixEnumerationTypes(this StateSave instance)
        {
            foreach (InstructionSave instructionSave in instance.InstructionSaves)
            {
                Type type = TypeResolutionCore.Self.GetTypeFromString(instructionSave.Type);

                if (type != null && type.IsEnum && instructionSave.Value?.GetType() != type)
                {
                    int valueAsInt = 0;
                    if(instructionSave.Value is int asInt)
                    {
                        valueAsInt = asInt;
                    }
                    else if(instructionSave.Value is long asLong)
                    {
                        valueAsInt = (int)asLong;
                    }
                    Array array = Enum.GetValues(type);

                    instructionSave.Value = array.GetValue(valueAsInt);
                }
            }
        }

        public static void ConvertEnumerationValuesToInts(this StateSave instance)
        {
            foreach (InstructionSave instructionSave in instance.InstructionSaves)
            {
                if(instructionSave.Value?.GetType()?.IsEnum == true)
                {
                    instructionSave.Value = (int)instructionSave.Value;
                }
            }
        }
    }
}
