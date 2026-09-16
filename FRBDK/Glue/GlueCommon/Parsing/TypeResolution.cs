using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;

namespace FlatRedBall.Glue.Parsing
{
    /// <summary>
    /// The resolution half of <c>FlatRedBall.Glue.Parsing.TypeManager.GetTypeFromString</c> (in
    /// <c>Glue.csproj</c>): the string-to-<see cref="Type"/> matching algorithm itself, with the
    /// reflected/loaded type lists passed in as parameters instead of read from static fields.
    /// <c>TypeManager</c> still owns loading those lists (<c>LoadAssembliesIfNecessary</c>, including
    /// the one genuinely coupled step - scanning installed plugins via <c>PluginManager</c>, which
    /// can't move here) and just forwards to this method. See issue #2276.
    /// </summary>
    public static class TypeResolution
    {
        public static Type GetTypeFromString(
            string typeString,
            IReadOnlyDictionary<string, Type> commonTypes,
            IReadOnlyList<Type> additionalTypes,
            IReadOnlyList<Type> flatRedBallTypes,
            IReadOnlyList<Type> xnaFrameworkTypes,
            IReadOnlyList<Type> xnaFrameworkGameTypes,
            IReadOnlyList<Type> pluginTypes,
            IEnumerable<AssetTypeInfo> allAssetTypes)
        {
            if (typeString == null)
            {
                return null;
            }

            bool isArray = false;

            Type typeToReturn = null;

            if (typeString.EndsWith("[]"))
            {
                isArray = true;
                typeString = typeString.Substring(0, typeString.Length - 2);
            }

            if (typeString == "bool" || typeString == "Boolean" || typeString == "System.Boolean")
            {
                typeToReturn = typeof(bool);
            }
            else if (typeString == "bool?")
            {
                typeToReturn = typeof(bool?);
            }
            else if (typeString == "float" || typeString == "Single")
            {
                typeToReturn = typeof(float);
            }
            else if (typeString == "float?")
            {
                typeToReturn = typeof(float?);
            }
            else if (typeString == "string" || typeString == "String")
            {
                typeToReturn = typeof(string);
            }
            else if (typeString == "char")
            {
                typeToReturn = typeof(char);
            }
            else if (typeString == "long")
            {
                typeToReturn = typeof(long);
            }
            else if (typeString == "long?")
            {
                typeToReturn = typeof(long?);
            }
            else if (typeString == "int" || typeString == "Int32")
            {
                typeToReturn = typeof(int);
            }
            else if (typeString == "int?")
            {
                typeToReturn = typeof(int?);
            }
            else if (typeString == "uint")
            {
                typeToReturn = typeof(uint);
            }
            else if (typeString == "double" || typeString == "Double")
            {
                typeToReturn = typeof(double);
            }
            else if (typeString == "double?")
            {
                typeToReturn = typeof(double?);
            }
            else if (typeString == "decimal" || typeString == "Decimal")
            {
                typeToReturn = typeof(decimal);
            }
            else if (typeString == "decimal?" || typeString == "Decimal")
            {
                typeToReturn = typeof(decimal?);
            }
            else if (typeString == "byte")
            {
                typeToReturn = typeof(byte);
            }
            else if (typeString == "byte?")
            {
                typeToReturn = typeof(byte?);
            }
            else
            {
                if (typeString.Contains("<") && typeString.Contains(">") && !typeString.EndsWith("<>"))
                {
                    string typeToMakeGenericName = typeString.Substring(0, typeString.IndexOf('<'));

                    int afterOpenBracket = typeString.IndexOf('<') + 1;
                    int closingBracket = typeString.LastIndexOf('>');

                    string internalTypeName = typeString.Substring(afterOpenBracket, closingBracket - afterOpenBracket);

                    Type typeToMakeGeneric = GetTypeFromString(typeToMakeGenericName + "<>", commonTypes, additionalTypes,
                        flatRedBallTypes, xnaFrameworkTypes, xnaFrameworkGameTypes, pluginTypes, allAssetTypes);
                    if (typeToMakeGeneric != null)
                    {
                        Type internalType = GetTypeFromString(internalTypeName, commonTypes, additionalTypes,
                            flatRedBallTypes, xnaFrameworkTypes, xnaFrameworkGameTypes, pluginTypes, allAssetTypes);

                        typeToReturn = typeToMakeGeneric.MakeGenericType(internalType);
                    }
                }

                // If we got here then maybe we have a type that's understood by our AssetTypeInfos
                if (typeToReturn == null && commonTypes.ContainsKey(typeString))
                {
                    typeToReturn = commonTypes[typeString];
                }

                bool isFullyQualified = typeString.Contains('.');

                if (isFullyQualified)
                {
                    foreach (Type type in additionalTypes)
                    {
                        string fullName = type.FullName.Replace('+', '.');

                        if (fullName.EndsWith(typeString))
                        {
                            typeToReturn = type;
                            break;
                        }
                    }
                }
                else
                {
                    foreach (Type type in additionalTypes)
                    {
                        if (type.Name == typeString)
                        {
                            typeToReturn = type;
                            break;
                        }
                    }
                }

                string unqualifiedType = typeString;

                if (isFullyQualified)
                {
                    int lastIndex = typeString.LastIndexOf('.') + 1;
                    unqualifiedType = typeString.Substring(lastIndex,
                        typeString.Length - lastIndex);
                }

                if (typeToReturn == null)
                {
                    foreach (Type type in flatRedBallTypes)
                    {
                        if (isFullyQualified && type.FullName == typeString)
                        {
                            typeToReturn = type;
                            break;
                        }
                        // If it's fully qualified, then we want to prevent false matches
                        else if (isFullyQualified == false && type.Name == unqualifiedType)
                        {
                            typeToReturn = type;
                            break;
                        }
                    }
                }

                if (typeToReturn == null && isFullyQualified)
                {
                    var foundPluginType = pluginTypes.FirstOrDefault(item => item.FullName == typeString);
                    if (foundPluginType != null)
                    {
                        typeToReturn = foundPluginType;
                    }
                }

                foreach (AssetTypeInfo ati in allAssetTypes)
                {
                    if (ati.RuntimeTypeName == typeString ||
                        ati.QualifiedRuntimeTypeName.QualifiedType == typeString
                        )
                    {
                        foreach (Type type in flatRedBallTypes)
                        {
                            if (type.FullName == ati.QualifiedRuntimeTypeName.QualifiedType)
                            {
                                return type;
                            }
                        }
                    }
                }

                if (typeToReturn == null)
                {
                    foreach (Type type in xnaFrameworkTypes)
                    {
                        if (type.Name == typeString || type.FullName == typeString)
                        {
                            typeToReturn = type;
                            break;
                        }
                    }
                }

                if (typeToReturn == null)
                {
                    foreach (Type type in xnaFrameworkGameTypes)
                    {
                        if (type.Name == typeString || type.FullName == typeString)
                        {
                            typeToReturn = type;
                            break;
                        }
                    }
                }

                if (typeToReturn == null)
                {
                    // If we got here, then we really don't know what's up, so just return the name
                    typeToReturn = Type.GetType(typeString);
                }
            }

            if (isArray && typeToReturn != null)
            {
                return typeToReturn.MakeArrayType();
            }
            else
            {
                return typeToReturn;
            }
        }

        /// <summary>
        /// Moved out of <c>TypeManager.GetTypeFromParsedType</c> (#2276) - the only coupling was to
        /// <see cref="MakeGenericType"/>'s own <c>DialogService</c> call, now routed through
        /// <paramref name="errorReporting"/>.
        /// </summary>
        public static Type GetTypeFromParsedType(
            ParsedType parsedType,
            IReadOnlyDictionary<string, Type> commonTypes,
            IReadOnlyList<Type> additionalTypes,
            IReadOnlyList<Type> flatRedBallTypes,
            IReadOnlyList<Type> xnaFrameworkTypes,
            IReadOnlyList<Type> xnaFrameworkGameTypes,
            IReadOnlyList<Type> pluginTypes,
            IEnumerable<AssetTypeInfo> allAssetTypes,
            IErrorReportingCore errorReporting)
        {
            if (parsedType.GenericType != null)
            {
                Type baseType = GetTypeFromString(parsedType.Name + "<>", commonTypes, additionalTypes,
                    flatRedBallTypes, xnaFrameworkTypes, xnaFrameworkGameTypes, pluginTypes, allAssetTypes);

                if (baseType == null)
                {
                    baseType = GetTypeFromString(parsedType.Name, commonTypes, additionalTypes,
                        flatRedBallTypes, xnaFrameworkTypes, xnaFrameworkGameTypes, pluginTypes, allAssetTypes);
                }
                if (baseType == null)
                {
                    baseType = GetTypeFromString(parsedType.NameWithGenericNotation, commonTypes, additionalTypes,
                        flatRedBallTypes, xnaFrameworkTypes, xnaFrameworkGameTypes, pluginTypes, allAssetTypes);
                }

                if (baseType == null)
                {
                    return null;
                }

                if (baseType.IsGenericTypeDefinition)
                {
                    return MakeGenericType(parsedType, baseType, commonTypes, additionalTypes,
                        flatRedBallTypes, xnaFrameworkTypes, xnaFrameworkGameTypes, pluginTypes, allAssetTypes,
                        errorReporting);
                }
                else
                {
                    return baseType;
                }
            }
            else if (parsedType.GenericRestrictions.Count != 0)
            {
                return GetTypeFromString(parsedType.GenericRestrictions[0], commonTypes, additionalTypes,
                    flatRedBallTypes, xnaFrameworkTypes, xnaFrameworkGameTypes, pluginTypes, allAssetTypes);
            }
            else
            {
                string typeAsString = parsedType.NameWithGenericNotation;

                return GetTypeFromString(typeAsString, commonTypes, additionalTypes,
                    flatRedBallTypes, xnaFrameworkTypes, xnaFrameworkGameTypes, pluginTypes, allAssetTypes);
            }
        }

        /// <summary>
        /// Moved out of <c>TypeManager.MakeGenericType</c> (#2276) - its only coupling was the
        /// <c>DialogService.ShowMessage</c> call on the <c>MakeGenericType</c> failure branch, now routed
        /// through <paramref name="errorReporting"/> (<see cref="IErrorReportingCore"/>).
        /// </summary>
        public static Type MakeGenericType(
            ParsedType parsedType,
            Type baseType,
            IReadOnlyDictionary<string, Type> commonTypes,
            IReadOnlyList<Type> additionalTypes,
            IReadOnlyList<Type> flatRedBallTypes,
            IReadOnlyList<Type> xnaFrameworkTypes,
            IReadOnlyList<Type> xnaFrameworkGameTypes,
            IReadOnlyList<Type> pluginTypes,
            IEnumerable<AssetTypeInfo> allAssetTypes,
            IErrorReportingCore errorReporting)
        {
            string genericString = parsedType.GenericType.Name;

            if (genericString.Contains(','))
            {
                string[] strings = genericString.Split(',');

                Type[] types = new Type[strings.Length];

                for (int i = 0; i < strings.Length; i++)
                {
                    types[i] = GetTypeFromString(strings[i], commonTypes, additionalTypes,
                        flatRedBallTypes, xnaFrameworkTypes, xnaFrameworkGameTypes, pluginTypes, allAssetTypes);
                }

                return baseType.MakeGenericType(types);
            }
            else
            {
                if (genericString.Contains('.'))
                {
                    int lastDot = genericString.LastIndexOf('.');

                    genericString = genericString.Substring(lastDot + 1, genericString.Length - (lastDot + 1));
                }
                Type genericType = GetTypeFromString(genericString, commonTypes, additionalTypes,
                    flatRedBallTypes, xnaFrameworkTypes, xnaFrameworkGameTypes, pluginTypes, allAssetTypes);

                if (genericType == null && parsedType.GenericType.Name == "T")
                {
                    if (parsedType.GenericRestrictions.Count != 0)
                    {
                        genericType = GetTypeFromString(parsedType.GenericRestrictions[0], commonTypes, additionalTypes,
                            flatRedBallTypes, xnaFrameworkTypes, xnaFrameworkGameTypes, pluginTypes, allAssetTypes);
                    }
                    else
                    {
                        genericType = typeof(object);
                    }
                }
                if (genericType == null)
                {
                    return null;
                }
                else
                {
                    try
                    {
                        return baseType.MakeGenericType(genericType);
                    }
                    catch (Exception)
                    {
                        errorReporting.ShowMessage("Error making a generic type out of " + baseType.Name + "<" + genericType.Name + ">" +
                            "\n This is probably because your game hasn't been rebuilt since you've made a critical change");
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Moved out of <c>TypeManager.GetTypeInListFromParsedType</c> (#2276) - zero coupling, only
        /// needed the existing <see cref="GetTypeFromString"/> seam.
        /// </summary>
        public static Type GetTypeInListFromParsedType(
            ParsedType parsedType,
            IReadOnlyDictionary<string, Type> commonTypes,
            IReadOnlyList<Type> additionalTypes,
            IReadOnlyList<Type> flatRedBallTypes,
            IReadOnlyList<Type> xnaFrameworkTypes,
            IReadOnlyList<Type> xnaFrameworkGameTypes,
            IReadOnlyList<Type> pluginTypes,
            IEnumerable<AssetTypeInfo> allAssetTypes)
        {
            string typeAsString;

            if (parsedType.GenericType != null)
            {
                typeAsString = parsedType.GenericType.Name;
            }
            else
            {
                // it's probably a [], so just use the type itself
                typeAsString = parsedType.Name;
            }

            return GetTypeFromString(typeAsString, commonTypes, additionalTypes,
                flatRedBallTypes, xnaFrameworkTypes, xnaFrameworkGameTypes, pluginTypes, allAssetTypes);
        }
    }
}
