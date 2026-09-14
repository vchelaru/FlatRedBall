using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.IO;
using System.Collections.ObjectModel;
using FlatRedBall.Math;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Graphics;
using System.Security.Policy;
using System.Reflection.Metadata.Ecma335;
using Gum.DataTypes;

namespace FlatRedBall.Glue.Parsing
{
    public static class TypeManager
    {
        #region Fields

        static Dictionary<string, Type> mCommonTypes;

        static Type[] FlatRedBallTypes;
        static Type[] mTypesInMicrosoftXnaFramework;
        static Type[] mTypesInMicrosoftXnaFrameworkGame;
#if XNA4
        static Type[] mTypesInMicrosoftXnaFrameworkGraphics;
#endif
        static Type[] pluginTypes;

        static List<Type> mAdditionalTypes = new List<Type>();

#endregion

        // GlueCommon can't reference this assembly (wrong direction), so it can't set
        // TypeResolutionCore.Self itself - wire it here instead, the first time this class is
        // touched, which every real caller of TypeResolutionCore.Self does transitively via
        // TypeManager.GetTypeFromString. Same trick as ObjectFinder's static constructor.
        static TypeManager()
        {
            TypeResolutionCore.Self = new TypeManagerTypeResolutionCore();
        }

        class TypeManagerTypeResolutionCore : ITypeResolutionCore
        {
            public Type GetTypeFromString(string typeString) => TypeManager.GetTypeFromString(typeString);
        }

        public static Type GetTypeFromParsedType(ParsedType parsedType)
        {
            if (parsedType.GenericType != null)
            {
                Type baseType = GetTypeFromString(parsedType.Name + "<>");

                if (baseType == null)
                {
                    baseType = GetTypeFromString(parsedType.Name);
                }
                if (baseType == null)
                {
                    baseType = GetTypeFromString(parsedType.NameWithGenericNotation);
                }

                if (baseType == null)
                {
                    int m = 3;
                    return null;
                }

                if (baseType.IsGenericTypeDefinition)
                {

                    return MakeGenericType(parsedType, baseType);
                }
                else
                {
                    return baseType;
                }

            }
            else if (parsedType.GenericRestrictions.Count != 0)
            {
                return GetTypeFromString(parsedType.GenericRestrictions[0]);
            }
            else
            {

                string typeAsString = parsedType.NameWithGenericNotation;

                return GetTypeFromString(typeAsString);
            }
        }

        private static Type MakeGenericType(ParsedType parsedType, Type baseType)
        {
            string genericString = parsedType.GenericType.Name;

            if (genericString.Contains(','))
            {
                string[] strings = genericString.Split(',');

                Type[] types = new Type[strings.Length];

                for (int i = 0; i < strings.Length; i++)
                {
                    types[i] = GetTypeFromString(strings[i]);
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
                Type genericType = GetTypeFromString(genericString);

                if (genericType == null && parsedType.GenericType.Name == "T")
                {
                    if (parsedType.GenericRestrictions.Count != 0)
                    {
                        genericType = GetTypeFromString(parsedType.GenericRestrictions[0]);
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
                    catch(Exception exception)
                    {
                        DialogService.ShowMessage("Error making a generic type out of " + baseType.Name + "<" + genericType.Name + ">" +
                            "\n This is probably because your game hasn't been rebuilt since you've made a critical change");
                        return null;
                    }
                }
            }
        }

        public static Type GetTypeInListFromParsedType(ParsedType parsedType)
        {
            string typeAsString = "";

            if (parsedType.GenericType != null)
            {
                typeAsString = parsedType.GenericType.Name;
            }
            else
            {
                // it's probably a [], so just use the type itself
                typeAsString = parsedType.Name;
            }

            return GetTypeFromString(typeAsString);
        }

        public static Type GetTypeFromString(string typeString)
        {
            if (typeString == null)
            {
                return null;
            }

            LoadAssembliesIfNecessary();

            return TypeResolution.GetTypeFromString(
                typeString,
                mCommonTypes,
                mAdditionalTypes,
                FlatRedBallTypes,
                mTypesInMicrosoftXnaFramework,
                mTypesInMicrosoftXnaFrameworkGame,
                pluginTypes,
                AvailableAssetTypes.Self.AllAssetTypes);
        }

        static byte[] LoadFileToBytes(string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Open);
            byte[] buffer = new byte[(int)fs.Length];
            fs.Read(buffer, 0, buffer.Length);
            fs.Close();

            return buffer;
        }

        public static void LoadAdditionalTypes(string assemblyFileName, string namespaceFilter = null)
        {
            Assembly assembly = Assembly.Load(LoadFileToBytes(assemblyFileName) );
            LoadAdditionalTypes(assembly, namespaceFilter);
        }

        public static void LoadAdditionalTypes(Assembly assembly, string namespaceFilter = null)
        {
            try
            {
                Type[] types = assembly.GetTypes();
                if (string.IsNullOrEmpty(namespaceFilter))
                {
                    mAdditionalTypes.AddRange(types);
                }
                else
                {
                    mAdditionalTypes.AddRange(types.Where(type => type.FullName.StartsWith(namespaceFilter)));
                }
            }
            catch (ReflectionTypeLoadException)
            {
                DialogService.ShowMessage("Encountered exception while trying to load " + assembly.FullName +
                    "\nThis is likely because the assembly is using a different version of the .NET framework.");
            }
            catch (TypeLoadException)
            {

            }
            catch (Exception)
            {

            }
        }

        static object mLockObject = new object();

        private static void LoadAssembliesIfNecessary()
        {
            lock (mLockObject)
            {
                if (FlatRedBallTypes == null)
                {
                    Assembly assembly = null;
                    string location = "";

                    #region FlatRedBall

                    string programFilesLocation = Environment.GetEnvironmentVariable("ProgramFiles(x86)") + "\\"; ;
                    string alternativeProgramFilesLocation = @"C:\Program Files\";//Environment.GetEnvironmentVariable("ProgramFiles");

                    Assembly frbAssembly = Assembly.GetAssembly(typeof(Sprite));

                    FlatRedBallTypes = frbAssembly.GetTypes();
                    #endregion



#region Microsoft.Xna.Framework

                    assembly = Assembly.GetAssembly(typeof(Microsoft.Xna.Framework.Matrix));
                    mTypesInMicrosoftXnaFramework = assembly.GetTypes();

#endregion

#region Microsoft.Xna.Framework.Game

                    assembly = Assembly.GetAssembly(typeof(Microsoft.Xna.Framework.Game));// Assembly.LoadFile(location);
                    mTypesInMicrosoftXnaFrameworkGame = assembly.GetTypes();

#endregion

#if XNA4

                    string appended = @"Microsoft XNA\XNA Game Studio\v4.0\References\Windows\x86\Microsoft.Xna.Framework.Graphics.dll";

                    if (File.Exists(programFilesLocation + appended))
                    {
                        location = programFilesLocation + appended;
                    }
                    else
                    {
                        location = alternativeProgramFilesLocation + appended;
                    }

                    assembly = null;

                    if (File.Exists(location))
                    {
                        assembly = Assembly.LoadFile(location);
                    }
                    else
                    {
                        // Can't find the .dll, so let's just <GULP> hope they have it installed 
                        assembly = Assembly.GetAssembly(typeof(Microsoft.Xna.Framework.Graphics.Texture2D));
                    }
                    mTypesInMicrosoftXnaFrameworkGraphics = assembly.GetTypes();

#endif


                #region Common Types

                    mCommonTypes = new Dictionary<string, Type>();

                    mCommonTypes.Add(typeof(ArrayList).Name, typeof(ArrayList));
                    mCommonTypes.Add("List`1", typeof(List<>));
                    mCommonTypes.Add("List<>", typeof(List<>));
                    mCommonTypes.Add("Activator", typeof(Activator));
                    mCommonTypes.Add("IEquatable`1", typeof(IEquatable<object>));
                    mCommonTypes.Add("ReadOnlyCollection", typeof(ReadOnlyCollection<>));
                    mCommonTypes.Add("ReadOnlyCollection`1", typeof(ReadOnlyCollection<>));
                    mCommonTypes.Add("AttachableList<T>", typeof(AttachableList<IAttachable>));
                    mCommonTypes.Add("AttachableList<>", typeof(AttachableList<IAttachable>));
                    mCommonTypes.Add("Dictionary<>", typeof(Dictionary<,>));
                    mCommonTypes.Add("Type", typeof(Type));
                    mCommonTypes.Add("IList", typeof(IList));
                    mCommonTypes.Add("IList`1", typeof(IList<object>));
                    mCommonTypes.Add("IList<>", typeof(IList<>));



#endregion

                }

                if(pluginTypes == null)
                {
                    var listOfTypesInAllPlugins = new List<Type>();
                    foreach(var pluginManager in Plugins.PluginManager.GetInstances())
                    {
                        // Ignore embedded plugin, it's this assembly
                        foreach(var pluginContainer in pluginManager.PluginContainers
                            .Where(container => container.Value.Plugin is Plugins.EmbeddedPlugins.EmbeddedPlugin == false))
                        {
                            var plugin = pluginContainer.Value.Plugin;

                            var typesInThisPlugin = plugin.GetType().Assembly.GetTypes()
                                // for now just consider enums, but we may want to expand this later
                                .Where(item =>item.IsEnum)
                                .ToList() ;

                            if(plugin is Plugins.PluginBase)
                            {
                                var additionalTypesForThisPlugin = (plugin as Plugins.PluginBase)?.GetUsedTypes?.Invoke();

                                if (additionalTypesForThisPlugin != null)
                                {
                                    typesInThisPlugin.AddRange(additionalTypesForThisPlugin);
                                }

                            }



                            foreach(var typeInThisPlugin in typesInThisPlugin)
                            {
                                if(listOfTypesInAllPlugins.Contains(typeInThisPlugin) == false)
                                {
                                    listOfTypesInAllPlugins.Add(typeInThisPlugin);
                                }
                            }
                        }
                    }

                    pluginTypes = listOfTypesInAllPlugins.ToArray();
                }
            }
        }


        public static string GetCommonTypeName(string qualifiedName) =>
            TypeConversion.GetCommonTypeName(qualifiedName);

        public static string GetFriendlyGenericName(Type type) =>
            TypeConversion.GetFriendlyGenericName(type);


        public static Type GetElementType(Type listType) =>
            TypeConversion.GetElementType(listType);


        public static Type GetFlatRedBallType(string typeString)
        {
            if (!string.IsNullOrEmpty(typeString))
            {
                typeString = typeString.Replace("<T>", "`1");

                LoadAssembliesIfNecessary();

                foreach (Type type in FlatRedBallTypes)
                {
                    if (type.Name == typeString)
                    {
                        return type;
                    }
                }

            }
            return null;
        }

        public static object GetDefaultForTypeAsType(string type)
        {



            switch (type)
            {
                case "String":
                case "string":
                    return null;
                case "Boolean":
                case "bool":
                    return false;
                case "Single":
                case "float":
                    return 0.0f;
                case "double":
                    return 0.0;
                case "decimal":
                    return 0.0m;
                case "Int16":
                    return (Int16)0;
                case "Int32":
                    return (Int32)0;
                case "Int64":
                    return (Int64)0;
                case "int":
                    return (int)0;
                case "long":
                    return (long)0;
                case "byte":
                case "Byte":
                    return (byte)0;
                case "short":
                    return (short)0;

                default:
                    {

                        // Try to get it from parsed type...
                        var systemType = GetTypeFromString(type);

                        if(systemType != null)
                        {
                            if (systemType.IsValueType)
                            {
                                return Activator.CreateInstance(systemType);
                            }
                            else
                            {
                                return null;
                            }
                        }

                    }
                    throw new ArgumentException("Could not find the value for type " + type);
            }

        }

        public static string GetDefaultForType(string type) =>
            TypeConversion.GetDefaultForType(type);

        /// <summary>
        /// Returns whether <paramref name="type"/> is one of the primitives this class knows the code-string
        /// default for. Callers that must not fail on an arbitrary type (a BitmapFont, a Layer, any engine
        /// reference type) use this and decide their own fallback.
        /// </summary>
        public static bool TryGetDefaultForType(string type, out string defaultValue) =>
            TypeConversion.TryGetDefaultForType(type, out defaultValue);

        /// <summary>
        /// Resolves <paramref name="type"/> as an enum and <paramref name="memberName"/> as one of its
        /// members, handing back that member's underlying value as a string - "2" for
        /// FlatRedBall.Graphics.VerticalAlignment.Center, for example. Returns false for anything that is
        /// not an enum, or a name that enum does not define.
        /// </summary>
        /// <remarks>
        /// The number rather than the name, because that is what the game can always read back: the
        /// embedded VariableAssignmentLogic.ConvertStringToType parses an integer for every enum it
        /// handles, while parsing a member *name* is either gated behind the MONOGAME_381/FNA define (the
        /// enums it special-cases, such as HorizontalAlignment) or relies on an assembly scan finding the
        /// type.
        /// </remarks>
        public static bool TryGetEnumValueAsNumber(string type, string memberName, out string valueAsNumber)
        {
            valueAsNumber = null;

            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(memberName))
            {
                return false;
            }

            var resolvedType = GetTypeFromString(type.Trim());

            if (resolvedType?.IsEnum != true)
            {
                return false;
            }

            // AssetTypeInfo values come from ContentTypes.csv, which is written with loose spacing
            // ("Name=MaxWidthBehavior, Category = Text, DefaultValue=Chop").
            var trimmedMemberName = memberName.Trim();

            if (!Enum.IsDefined(resolvedType, trimmedMemberName))
            {
                return false;
            }

            var asEnum = Enum.Parse(resolvedType, trimmedMemberName);
            valueAsNumber = Convert.ToInt64(asEnum).ToString(System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }

        /// <summary>
        /// The one place callers that need a variable's real default (not the CLR zero for its type) should
        /// go through: for an enum, prefers <paramref name="declaredDefault"/> (the AssetTypeInfo's
        /// DefaultValue) resolved to its underlying number, falls back to the enum's own CLR zero if that
        /// doesn't resolve (issue #2283 - a missing/stale declared default must not fall through to null,
        /// which is what made #2272's crash possible). For a non-enum primitive, prefers
        /// <paramref name="declaredDefault"/> when it actually parses as that type (issue #2298 - Sprite's
        /// Alpha declares DefaultValue=1, but the CLR zero for float is 0, so "Set to Default" was pushing
        /// an invisible sprite instead of an opaque one), and otherwise defers to
        /// <see cref="TryGetDefaultForType"/>.
        /// </summary>
        public static bool TryGetVariableDefaultValue(string type, string declaredDefault, out string defaultValue)
        {
            if (TryGetEnumValueAsNumber(type, declaredDefault, out defaultValue))
            {
                return true;
            }

            if (GetTypeFromString(type?.Trim())?.IsEnum == true)
            {
                defaultValue = "0";
                return true;
            }

            if (TryGetDeclaredPrimitiveDefault(type, declaredDefault, out defaultValue))
            {
                return true;
            }

            return TryGetDefaultForType(type, out defaultValue);
        }

        /// <summary>
        /// Validates <paramref name="declaredDefault"/> actually parses as <paramref name="type"/> before
        /// trusting it - a stale/typo'd ContentTypes.csv entry must fall back to the CLR default rather
        /// than propagate garbage (see the enum equivalent, "NotAMember", above).
        /// </summary>
        private static bool TryGetDeclaredPrimitiveDefault(string type, string declaredDefault, out string defaultValue)
        {
            defaultValue = null;
            var trimmedDefault = declaredDefault?.Trim();
            if (string.IsNullOrEmpty(trimmedDefault))
            {
                return false;
            }

            switch (type?.Trim())
            {
                case "bool":
                case "Boolean":
                case "System.Boolean":
                    if (bool.TryParse(trimmedDefault, out var boolValue))
                    {
                        defaultValue = boolValue ? "true" : "false";
                        return true;
                    }
                    return false;

                case "float":
                case "Single":
                case "System.Single":
                case "double":
                case "Double":
                case "System.Double":
                case "decimal":
                case "Decimal":
                case "System.Decimal":
                    if (double.TryParse(trimmedDefault, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out _))
                    {
                        defaultValue = trimmedDefault;
                        return true;
                    }
                    return false;

                case "Int16":
                case "int":
                case "Int32":
                case "System.Int32":
                case "long":
                case "Int64":
                case "System.Int64":
                case "byte":
                case "Byte":
                    if (long.TryParse(trimmedDefault, System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out _))
                    {
                        defaultValue = trimmedDefault;
                        return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Like <see cref="TryGetVariableDefaultValue"/>, but for code-generation contexts where the default
        /// becomes a C# source literal rather than a runtime-parsed string: for an enum this returns a
        /// qualified member-name expression ("FlatRedBall.Graphics.MaxWidthBehavior.Chop") instead of the
        /// numeric string the runtime path hands <c>VariableAssignmentLogic.ConvertStringToType</c>, since
        /// assigning a bare int to an enum-typed local doesn't compile. Everything else forwards unchanged
        /// to <see cref="TryGetVariableDefaultValue"/> (see issue #2283).
        /// </summary>
        public static bool TryGetVariableDefaultValueExpression(string type, string declaredDefault, out string expression)
        {
            var resolvedType = GetTypeFromString(type?.Trim());

            if (resolvedType?.IsEnum == true)
            {
                var trimmedDeclaredDefault = declaredDefault?.Trim();

                var memberName = !string.IsNullOrEmpty(trimmedDeclaredDefault) && Enum.IsDefined(resolvedType, trimmedDeclaredDefault)
                    ? trimmedDeclaredDefault
                    : Enum.GetName(resolvedType, Activator.CreateInstance(resolvedType));

                if (memberName == null)
                {
                    expression = null;
                    return false;
                }

                expression = resolvedType.FullName.Replace('+', '.') + "." + memberName;
                return true;
            }

            return TryGetVariableDefaultValue(type, declaredDefault, out expression);
        }

        public static object Parse(string typeName, string value) =>
            TypeConversion.Parse(typeName, value);

        public static bool TryConvertStringValue(string type, string variableValue, out object convertedValue) =>
            TypeConversion.TryConvertStringValue(type, variableValue, out convertedValue);

        public static bool TryCastValue(string newType, object variableValue, out object convertedValue) =>
            TypeConversion.TryCastValue(newType, variableValue, out convertedValue);
    }
}
