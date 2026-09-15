using System.Collections.Generic;
using System.Linq;
using GlueSaveClasses;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Split out of <c>CustomVariableExtensionMethods</c> (in <c>Glue.csproj</c>, net8.0-windows):
    /// <see cref="FixValue"/> is a pure type-conversion function with no singleton coupling. Lives here
    /// (net8.0, no WPF) so it and its tests can build and run on Linux/macOS. See #2276. Not an
    /// extension method, so (unlike most #2276 moves) its call sites needed updating - extension method
    /// resolution doesn't care which class declares a method, but a plain static call does.
    /// </summary>
    public static class CustomVariableCommonExtensions
    {
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
            else if (type == "decimal")
            {
                if (variableValue is int asInt)
                {
                    variableValue = (decimal)asInt;
                }
                else if (variableValue is double asDouble)
                {
                    variableValue = (decimal)asDouble;
                }
            }
            else if (type == "decimal?")
            {
                if (variableValue is int asInt)
                {
                    variableValue = (decimal?)asInt;
                }
                else if (variableValue is double asDouble)
                {
                    variableValue = (decimal?)asDouble;
                }
            }
            else if (type == "List<Vector2>")
            {
                if (variableValue is JArray jArray)
                {
                    List<Vector2> newList = new List<Vector2>();
                    foreach (string innerValue in jArray)
                    {
                        var split = innerValue.Split(",").Select(item => item.Trim()).ToArray();

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
            else if (type == "List<float>")
            {
                if (variableValue is JArray jArray)
                {
                    variableValue = jArray.Select(item => item.ToObject<float>()).ToList();
                }
            }
            else if (type == "List<int>")
            {
                if (variableValue is JArray jArray)
                {
                    variableValue = jArray.Select(item => item.ToObject<int>()).ToList();
                }
            }
            else if (type == "List<string>")
            {
                if (variableValue is JArray jArray)
                {
                    List<string> newList = new List<string>();
                    foreach (string innerValue in jArray)
                    {
                        newList.Add(innerValue.ToString());
                    }
                    variableValue = newList;
                }
            }
            else if (type == "FloatRectangle?")
            {
                var wasAssigned = false;
                if (variableValue is string asString)
                {
                    if (asString.StartsWith("(") & asString.EndsWith(")"))
                    {
                        asString = asString.Substring(1, asString.Length - 2);
                    }
                    var values = asString.Split(",");

                    if (values.Length == 4)
                    {
                        if (float.TryParse(values[0], out float x) &&
                            float.TryParse(values[1], out float y) &&
                            float.TryParse(values[2], out float width) &&
                            float.TryParse(values[3], out float height))
                        {
                            variableValue = new FloatRectangle(x, y, width, height);
                            wasAssigned = true;
                        }
                    }
                }
                if (!wasAssigned)
                {
                    variableValue = null;
                }
            }
            return variableValue;
        }
    }
}
