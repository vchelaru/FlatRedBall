using System;
using System.Collections.Generic;
using FlatRedBall.Glue.Parsing;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Split out of <c>NamedObjectSaveExtensionMethods</c> (in <c>Glue.csproj</c>, net8.0-windows): these
    /// methods have no dependency on Glue's UI, plugin system, or <c>ObjectFinder.Self</c>/<c>AvailableAssetTypes.Self</c>
    /// project lookups - pure logic over a <see cref="NamedObjectSave"/>'s own fields. Lives here (net8.0,
    /// no WPF) so it and its tests can build and run on Linux/macOS. See issue #2276. Named differently
    /// from the original class (not a forwarding stub) to avoid a duplicate-type clash now that both
    /// assemblies are visible together via <c>Glue.csproj</c>'s <c>ProjectReference</c> to
    /// <c>GlueCommon</c>; extension method resolution doesn't care which class declares it, so existing
    /// call sites are unaffected.
    /// </summary>
    public static class NamedObjectSaveCommonExtensions
    {
        /// <summary>
        /// Updates the InstructionSaves in the argument NamedObject
        /// according to the source type.  This method will never remove
        /// instructions, but will add them if the NOS comes from a type that
        /// has properties that are currently not represented in the NOS's Instructions.
        /// </summary>
        /// <param name="instance">The NamedObject to update properties on.</param>
        public static void UpdateCustomProperties(this NamedObjectSave instance)
        {
            instance.InstructionSaves.Sort((first, second) => first.Member?.CompareTo(second.Member) ?? 0);
        }

        /// <summary>
        /// Calls UpdateCustomProperties on every NamedObjectSave in the container, recursively through
        /// ContainedObjects. Moved from Glue.csproj's INamedObjectContainerExtensionMethods (#2276).
        /// </summary>
        public static void UpdateCustomProperties(this INamedObjectContainer container)
        {
            if (container == null)
            {
                throw new ArgumentException("Argument container is null", "container");
            }

            UpdateCustomProperties(container.NamedObjects);
        }

        private static void UpdateCustomProperties(List<NamedObjectSave> namedObjectList)
        {
            for (int i = 0; i < namedObjectList.Count; i++)
            {
                namedObjectList[i].UpdateCustomProperties();

                UpdateCustomProperties(namedObjectList[i].ContainedObjects);
            }
        }

        public static void ConvertEnumerationValuesToInts(this NamedObjectSave instance)
        {
            foreach (CustomVariableInNamedObject instruction in instance.InstructionSaves)
            {
                if (instruction.Value != null && instruction.Value.GetType().IsEnum)
                {
                    instruction.Value = (int)instruction.Value;
                }
            }
            // to prevent some threading issues:
            foreach (var property in instance.Properties.ToArray())
            {
                if (property.Value != null && property.Value.GetType().IsEnum)
                {
                    property.Value = (int)property.Value;
                }
            }

            foreach (NamedObjectSave contained in instance.ContainedObjects)
            {
                contained.ConvertEnumerationValuesToInts();
            }
        }

        public static void PostLoadLogic(this NamedObjectSave instance)
        {
            for (int i = instance.InstructionSaves.Count - 1; i > -1; i--)
            {
                if (instance.InstructionSaves[i].Value == null)
                {
                    instance.InstructionSaves.RemoveAt(i);
                }
            }
        }

        public static void SetProperty(this NamedObjectSave instance, string propertyName, object value)
        {
            instance.Properties.SetValue(propertyName, value);
        }

        public static CustomVariableInNamedObject AddInstruction(this NamedObjectSave instance, string member, string type)
        {
            CustomVariableInNamedObject instructionSave = new CustomVariableInNamedObject();
            instructionSave.Value = null; // make it the default
            instructionSave.Type = TypeConversion.GetCommonTypeName(type);
            instructionSave.Member = member;
            instance.InstructionSaves.Add(instructionSave);
            return instructionSave;
        }

        public static CustomVariableInNamedObject AddNewGenericInstructionFor(this NamedObjectSave instance, string member, Type type)
        {
            CustomVariableInNamedObject instructionSave = new CustomVariableInNamedObject();
            instructionSave.Value = null; // make it the default

            // April 2, 2018
            // This used to just assign type.Name, but that can cause ambiguity between
            // different systems like FRB's HorizontalAlignment and Gum's HorizontalAlignment,
            // so we need to have the values be fully qualified.
            //instructionSave.Type = type.Name;

            // List<string> could maybe use the GetFriendlyGenericName
            // method, but it seems to rely on lower-case string, so let's leave it at that...
            if (type == typeof(List<string>))
            {
                instructionSave.Type = "List<string>";
            }
            else if (type.IsGenericType)
            {
                instructionSave.Type = TypeConversion.GetFriendlyGenericName(type);
            }
            else
            {
                instructionSave.Type = type.FullName;
            }

            instructionSave.Type = TypeConversion.GetCommonTypeName(instructionSave.Type);
            instructionSave.Member = member;
            // Create a new instruction

            instance.InstructionSaves.Add(instructionSave);
            return instructionSave;
        }

        public static NamedObjectSave GetNamedObject(this INamedObjectContainer namedObjectContainer, string namedObjectName)
        {
            return GetNamedObjectInList(namedObjectContainer.NamedObjects, namedObjectName);
        }

        public static NamedObjectSave GetNamedObjectInList(List<NamedObjectSave> namedObjectList, string namedObjectName)
        {
            for (int i = 0; i < namedObjectList.Count; i++)
            {
                NamedObjectSave nos = namedObjectList[i];

                if (nos.InstanceName == namedObjectName)
                {
                    return nos;
                }

                if (nos.ContainedObjects != null && nos.ContainedObjects.Count != 0)
                {
                    NamedObjectSave foundNos = GetNamedObjectInList(nos.ContainedObjects, namedObjectName);

                    if (foundNos != null)
                    {
                        return foundNos;
                    }
                }
            }

            return null;
        }
    }
}
