using FlatRedBall.Content.Instructions;
using GlueControl.Models;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueControl.Models
{
    public static class IElementExtensionMethods
    {
        public static void FixAllTypes(this GlueElement element)
        {
            foreach (NamedObjectSave nos in element.NamedObjects)
            {
                nos.FixAllTypes();
            }
            //foreach (StateSave state in element.AllStates)
            //{
            //    state.FixAllTypes(element);
            //}
            //foreach (CustomVariable customVariable in element.CustomVariables)
            //{
            //    customVariable.FixAllTypes();
            //}
            //foreach(var file in element.ReferencedFiles)
            //{
            //    file.FixAllTypes();
            //}
        }

        public static void FixAllTypes(this NamedObjectSave instance)
        {
            //var ati = instance.GetAssetTypeInfo();
            foreach (var instruction in instance.InstructionSaves)
            {
                //if (instruction.Type == null)
                //{
                //    var existingVariableDefinition = ati?.VariableDefinitions.FirstOrDefault(item => item.Name == instruction.Member);

                //    instruction.Type = existingVariableDefinition?.Type;
                //}
                FixAllTypes(instruction);
            }

            //foreach (var property in instance.Properties)
            //{
            //    FixAllTypes(property);
            //}

            foreach (NamedObjectSave contained in instance.ContainedObjects)
            {
                contained.FixAllTypes();
            }
        }

        private static void FixAllTypes(InstructionSave instruction)
        {
            //FixEnumerationType(instruction);

            if (!string.IsNullOrEmpty(instruction.Type) && instruction.Value != null)
            {
                object variableValue = instruction.Value;
                var type = instruction.Type;
                variableValue = FixValue(variableValue, type);
                instruction.Value = variableValue;
            }
        }

        public static object FixValue(object variableValue, string type)
        {
            if (type == "int")
            {
                if (variableValue is long asLong)
                {
                    variableValue = (int)asLong;
                }
            }
            else if (type == "int?")
            {
                if (variableValue is long asLong)
                {
                    variableValue = (int?)asLong;
                }
            }
            else if (type == "float" || type == "Single")
            {
                if (variableValue is int asInt)
                {
                    variableValue = (float)asInt;
                }
                else if (variableValue is double asDouble)
                {
                    variableValue = (float)asDouble;
                }
            }
            else if (type == "float?")
            {
                if (variableValue is int asInt)
                {
                    variableValue = (float?)asInt;
                }
                else if (variableValue is double asDouble)
                {
                    variableValue = (float?)asDouble;
                }
            }
            else if (type == "List<Vector2>")
            {
                if (variableValue is Newtonsoft.Json.Linq.JArray jArray)
                {
                    List<Vector2> newList = new List<Vector2>();
                    foreach (string innerValue in jArray)
                    {
                        var split = innerValue.Split(',').Select(item => item.Trim()).ToArray();

                        if (split.Length == 2)
                        {
                            var firstValue = float.Parse(split[0], System.Globalization.CultureInfo.InvariantCulture);
                            var secondValue = float.Parse(split[1], System.Globalization.CultureInfo.InvariantCulture);

                            newList.Add(new Vector2(firstValue, secondValue));
                        }
                    }
                    variableValue = newList;
                }
            }

            return variableValue;
        }


    }
}
