using System;

namespace FlatRedBall.Glue.Parsing
{
    /// <summary>
    /// The subset of <c>FlatRedBall.Glue.Parsing.TypeManager</c>'s logic (in <c>Glue.csproj</c>) that has no
    /// dependency on Glue's UI, plugin system, or asset-type registry - string/value conversion only, no
    /// reflection-based type resolution. Lives here (net8.0, no WPF) rather than in <c>Glue.csproj</c>
    /// (net8.0-windows, WPF) so it and its tests can build and run on Linux/macOS. See issue #2276.
    /// <c>TypeManager</c>'s own methods of the same name just forward here, so existing call sites are
    /// unaffected.
    /// </summary>
    public static class TypeConversion
    {
        public static string GetCommonTypeName(string qualifiedName)
        {
            switch (qualifiedName)
            {
                case "System.Single":
                case "Single":
                    return "float";
                case "System.Boolean":
                case "Boolean":
                    return "bool";
                case "System.Int32":
                case "Int32":
                    return "int";
                case "System.String":
                case "String":
                    return "string";
                case "Double":
                    return "double";
                case "Decimal":
                case "System.Decimal":
                    return "decimal";
            }

            if (qualifiedName.StartsWith("System.Nullable`1[[System.Int32,"))
            {
                return "int?";
            }

            if (qualifiedName.StartsWith("System.Nullable`1[[System.Boolean,"))
            {
                return "bool?";
            }
            else if (qualifiedName.StartsWith("System.Nullable`1[[System.Single,"))
            {
                return "float?";
            }
            else if (qualifiedName.StartsWith("System.Nullable`1[[System.Decimal,"))
            {
                return "decimal?";
            }

            return qualifiedName;
        }

        public static string GetFriendlyGenericName(Type type)
        {
            string friendlyName = type?.Name;
            if (type?.IsGenericType == true)
            {
                int iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0)
                {
                    friendlyName = friendlyName.Remove(iBacktick);
                }
                friendlyName += "<";
                Type[] typeParameters = type.GetGenericArguments();
                for (int i = 0; i < typeParameters.Length; ++i)
                {
                    string typeParamName = GetFriendlyGenericName(typeParameters[i]);
                    friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
                }
                friendlyName += ">";
            }

            return friendlyName;
        }

        public static Type GetElementType(Type listType)
        {
            if (listType == null)
            {
                return null;
            }
            if (listType.FullName == "FlatRedBall.SpriteList")
            {
                return typeof(FlatRedBall.Sprite);
            }
            else
            {
                return listType.GetElementType();
            }
        }

        public static string GetDefaultForType(string type)
        {
            if (TryGetDefaultForType(type, out string defaultValue))
            {
                return defaultValue;
            }
            throw new ArgumentException("Could not find the value for type " + type);
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> is one of the primitives this class knows the code-string
        /// default for. Callers that must not fail on an arbitrary type (a BitmapFont, a Layer, any engine
        /// reference type) use this and decide their own fallback.
        /// </summary>
        public static bool TryGetDefaultForType(string type, out string defaultValue)
        {
            switch (type)
            {
                case "string":
                case "String":
                case "System.String":
                case "string?":
                case "String?":
                case "Nullable<string>":
                case "Nullable<String>":
                    defaultValue = "null";
                    return true;

                case "Boolean":
                case "bool":
                case "System.Boolean":
                    defaultValue = "false";
                    return true;

                case "Single":
                case "float":
                case "System.Single":

                case "Double":
                case "double":
                case "System.Double":

                case "decimal":
                case "Decimal":
                case "System.Decimal":

                    defaultValue = "0";
                    return true;
                case "Int16":

                case "Int32":
                case "int":
                case "System.Int32":

                case "long":
                case "Int64":
                case "System.Int64":

                case "byte":
                case "Byte":

                case "ColorOperation":

                    defaultValue = "0";
                    return true;
                case "float?":
                case "int?":
                case "long?":
                case "byte?":
                case "double?":
                case "bool?":
                case "Nullable<Boolean>":
                case "Nullable<Int32>":
                    defaultValue = "null";
                    return true;
                default:
                    defaultValue = null;
                    return false;
            }
        }

        public static object Parse(string typeName, string value)
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
            else if (typeName == "decimal")
            {
                decimal decimalToReturn = 0.0m;

                decimal.TryParse(value, out decimalToReturn);

                return decimalToReturn;
            }
            else
            {
                return toReturn;
            }
        }

        public static bool TryConvertStringValue(string type, string variableValue, out object convertedValue)
        {
            convertedValue = null;
            var handled = false;
            switch (type)
            {
                case "float":
                case nameof(Single):
                case "System.Single":

                    if (!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = float.Parse(variableValue);
                    }
                    else
                    {
                        convertedValue = 0f;
                    }
                    handled = true;
                    break;
                case "float?":
                    if (!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = float.Parse(variableValue);
                    }
                    else
                    {
                        convertedValue = (float?)null;
                    }
                    handled = true;
                    break;

                case "int":
                case nameof(Int32):
                case "System.Int32":

                    if (!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = int.Parse(variableValue);
                    }
                    else
                    {
                        convertedValue = 0;
                    }
                    handled = true;
                    break;

                case "int?":

                    if (!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = int.Parse(variableValue);
                    }
                    else
                    {
                        convertedValue = (int?)null;
                    }

                    handled = true;
                    break;
                case "long":

                    if (!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = long.Parse(variableValue);
                    }
                    else
                    {
                        convertedValue = 0;
                    }
                    handled = true;
                    break;
                case "long?":

                    if (!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = long.Parse(variableValue);
                    }
                    else
                    {
                        convertedValue = (long?)null;
                    }
                    handled = true;
                    break;
                case "bool":
                case nameof(Boolean):
                case "System.Boolean":

                    if (!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = bool.Parse(variableValue.ToLowerInvariant());
                    }
                    else
                    {
                        convertedValue = false;
                    }
                    handled = true;
                    break;
                case "bool?":

                    if (!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = bool.Parse(variableValue.ToLowerInvariant());
                    }
                    else
                    {
                        convertedValue = (bool?)null;
                    }

                    handled = true;
                    break;
                case "double":
                case nameof(Double):
                case "System.Double":

                    if (!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = double.Parse(variableValue);
                    }
                    else
                    {
                        convertedValue = 0.0;
                    }
                    handled = true;
                    break;
                case "double?":
                    if (!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = double.Parse(variableValue);
                    }
                    else
                    {
                        convertedValue = null;
                    }
                    handled = true;
                    break;

                case "decimal":
                case nameof(Decimal):
                case "System.Decimal":

                    if (!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = decimal.Parse(variableValue);
                    }
                    else
                    {
                        convertedValue = 0.0m;
                    }
                    handled = true;
                    break;
                case "decimal?":

                    if (!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = decimal.Parse(variableValue);
                    }
                    else
                    {
                        convertedValue = (decimal?)null;
                    }
                    handled = true;
                    break;

                case "byte":

                    if (!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = byte.Parse(variableValue);
                    }
                    else
                    {
                        convertedValue = (byte)0;
                    }
                    handled = true;
                    break;

                case "byte?":

                    if (!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = byte.Parse(variableValue);
                    }
                    else
                    {
                        convertedValue = (byte?)null;
                    }
                    handled = true;
                    break;
                case "Microsoft.Xna.Framework.Color":
                case nameof(Microsoft.Xna.Framework.Color):
                    if (!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = typeof(Microsoft.Xna.Framework.Color).GetProperty(variableValue).GetValue(null);
                    }
                    else
                    {
                        // do we default to white? that's default for shapes
                        convertedValue = Microsoft.Xna.Framework.Color.White;
                    }
                    handled = true;
                    break;
                case nameof(Microsoft.Xna.Framework.Graphics.TextureAddressMode):
                case "Microsoft.Xna.Framework.Graphics.TextureAddressMode":
                    convertedValue = ToEnum<Microsoft.Xna.Framework.Graphics.TextureAddressMode>(variableValue);
                    handled = true;
                    break;
                case nameof(FlatRedBall.Graphics.ColorOperation):
                case "FlatRedBall.Graphics.ColorOperation":
                    convertedValue = ToEnum<FlatRedBall.Graphics.ColorOperation>(variableValue);

                    handled = true;
                    break;
                case nameof(FlatRedBall.Graphics.BlendOperation):
                case "FlatRedBall.Graphics.BlendOperation":
                    convertedValue = ToEnum<FlatRedBall.Graphics.BlendOperation>(variableValue);

                    handled = true;
                    break;

            }

            T ToEnum<T>(string asString)
            {
                if (int.TryParse(asString, out int parsedInt))
                {
                    return (T)(object)parsedInt;
                }
                return default(T);
            }
            return handled;
        }

        public static bool TryCastValue(string newType, object variableValue, out object convertedValue)
        {
            var handled = false;
            convertedValue = variableValue;
            if (newType == "int")
            {
                if (variableValue is long asLong)
                {
                    convertedValue = (int)asLong;
                    handled = true;
                }
            }
            else if (newType == "int?")
            {
                if (variableValue is long asLong)
                {
                    convertedValue = (int?)asLong;
                    handled = true;
                }
            }
            else if (newType == "float" || newType == "Single")
            {
                if (variableValue is int asInt)
                {
                    convertedValue = (float)asInt;
                    handled = true;
                }
                else if (variableValue is double asDouble)
                {
                    convertedValue = (float)asDouble;
                    handled = true;
                }
                else if (variableValue is decimal asDecimal)
                {
                    convertedValue = (float)asDecimal;
                    handled = true;
                }
            }
            else if (newType == "decimal" || newType == "Decimal")
            {
                if (variableValue is int asInt)
                {
                    convertedValue = (decimal)asInt;
                    handled = true;
                }
                else if (variableValue is double asDouble)
                {
                    convertedValue = (decimal)asDouble;
                    handled = true;
                }
            }
            else if (newType == "float?")
            {
                if (variableValue is int asInt)
                {
                    convertedValue = (float?)asInt;
                    handled = true;
                }
                else if (variableValue is double asDouble)
                {
                    convertedValue = (float?)asDouble;
                    handled = true;
                }
            }
            else if (newType == "decimal?")
            {
                if (variableValue is int asInt)
                {
                    convertedValue = (decimal)asInt;
                    handled = true;
                }
                else if (variableValue is double asDouble)
                {
                    convertedValue = (decimal)asDouble;
                    handled = true;
                }
            }
            else if (newType == "double")
            {
                if (variableValue is int asInt)
                {
                    convertedValue = (decimal)asInt;
                    handled = true;
                }
                else if (variableValue is double asDouble)
                {
                    convertedValue = asDouble;
                    handled = true;
                }
            }
            else if (newType == "string")
            {
                if (variableValue is int asInt)
                {
                    convertedValue = asInt.ToString();
                    handled = true;
                }
            }

            return handled;
        }
    }
}
