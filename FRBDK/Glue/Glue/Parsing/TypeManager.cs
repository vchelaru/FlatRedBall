using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.IO;
using System.Collections.ObjectModel;
using FlatRedBall.Math;
using FlatRedBall.Glue.Elements;


using FlatRedBall.Math;
using System.Security.Policy;


namespace FlatRedBall.Glue.Parsing
{
    public static class TypeManager
    {
        #region Fields

        static Dictionary<string, Type> mCommonTypes;

        static Type[] FlatRedBallTypes;
        static Type[] mTypesInMicrosoftXnaFramework;
        static Type[] mTypesInMicrosoftXnaFrameworkGame;
        static Type[] mTypesInMicrosoftXnaFrameworkGraphics;
        static Type[] pluginTypes;

        static List<Type> mAdditionalTypes = new List<Type>();

        #endregion

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
                        System.Windows.Forms.MessageBox.Show("Error making a generic type out of " + baseType.Name + "<" + genericType.Name + ">" +
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
            ////////////////////EARLY OUT////////////////////
            if (typeString == null)
            {
                return null;
            }
            //////////////////END EARLY OUT//////////////////


            LoadAssembliesIfNecessary();

            #region Identify if the type is an array and change the typeString if so

            bool isArray = false;

            Type typeToReturn = null;

            if (typeString != null && typeString.EndsWith("[]"))
            {
                isArray = true;
                typeString = typeString.Substring(0, typeString.Length - 2);
            }

            #endregion

            #region Check primitive types

            if (typeString == "bool" || typeString == "Boolean" || typeString == "System.Boolean")
            {
                typeToReturn = typeof(bool);
            }
            else if (typeString == "float" || typeString == "Single")
            {
                typeToReturn = typeof(float);
            }
            else if(typeString == "float?")
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
            else if(typeString == "long?")
            {
                typeToReturn = typeof(long?);
            }
            else if (typeString == "int" || typeString == "Int32")
            {
                typeToReturn = typeof(int);
            }
            else if(typeString == "int?")
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
            else if(typeString == "double?")
            {
                typeToReturn = typeof(double?);
            }
            else if (typeString == "byte")
            {
                typeToReturn = typeof(byte);
            }

            #endregion

            else
            {
                if (typeString != null && typeString.Contains("<") && typeString.Contains(">") && !typeString.EndsWith("<>"))
                {
                    string typeToMakeGenericName = typeString.Substring(0, typeString.IndexOf('<'));

                    int afterOpenBracket = typeString.IndexOf('<') + 1;
                    int closingBracket = typeString.LastIndexOf('>');

                    string internalTypeName = typeString.Substring(afterOpenBracket, closingBracket - afterOpenBracket);

                    Type typeToMakeGeneric = GetTypeFromString(typeToMakeGenericName + "<>");
                    if (typeToMakeGeneric != null)
                    {
                        Type internalType = GetTypeFromString(internalTypeName);

                        typeToReturn = typeToMakeGeneric.MakeGenericType(internalType);
                    }
                }

                // If we got here then maybe we have a type that's understood by our AssetTypeInfos
                #region Check common types

                if (typeToReturn == null && typeString != null && mCommonTypes.ContainsKey(typeString))
                {
                    typeToReturn = mCommonTypes[typeString];
                }


                #endregion
                bool isFullyQualified = typeString.Contains('.');
                #region Check Additional (custom) types

                if (isFullyQualified)
                {
                    foreach (Type type in mAdditionalTypes)
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

                    foreach (Type type in mAdditionalTypes)
                    {

                        if (type.Name == typeString)
                        {
                            typeToReturn = type;
                            break;
                        }
                    }
                }
                #endregion


                string unqualifiedType = typeString;

                if (isFullyQualified)
                {
                    int lastIndex = typeString.LastIndexOf('.') + 1;
                    unqualifiedType = typeString.Substring(lastIndex,
                        typeString.Length - lastIndex);
                }

                if (typeToReturn == null)
                {
                    foreach (Type type in FlatRedBallTypes)
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


                if(typeToReturn == null && isFullyQualified)
                {
                    var foundPluginType = pluginTypes.FirstOrDefault(item => item.FullName == typeString);
                    if(foundPluginType != null)
                    {
                        typeToReturn = foundPluginType;
                    }
                }
                
                foreach (AssetTypeInfo ati in AvailableAssetTypes.Self.AllAssetTypes)
                {
                    if (ati.RuntimeTypeName == typeString ||
                        ati.QualifiedRuntimeTypeName.QualifiedType == typeString
                        )
                    {
                        foreach (Type type in FlatRedBallTypes)
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
                    Type[] types = mTypesInMicrosoftXnaFramework;

                    foreach (Type type in types)
                    {
                        if (type.Name == typeString || type.FullName == typeString)
                        {
                            typeToReturn = type;
                            break;
                        }
                    }
                }


                if (typeToReturn == null && mTypesInMicrosoftXnaFrameworkGraphics != null)
                {
                    Type[] types = mTypesInMicrosoftXnaFrameworkGraphics;

                    foreach (Type type in types)
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
                    Type[] types = mTypesInMicrosoftXnaFrameworkGame;

                    foreach (Type type in types)
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
            catch (ReflectionTypeLoadException reflectionException)
            {
                System.Windows.Forms.MessageBox.Show("Encountered exception while trying to load " + assembly.FullName +
                    "\nThis is likely because the assembly is using a different version of the .NET framework.");
            }
            catch (TypeLoadException ex)
            {

            }
            catch (Exception e)
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


        public static string ConvertToCommonType(string qualifiedName)
        {
            switch (qualifiedName)
            {
                case "System.Single":
                case "Single":
                    return "float";
                //break;
                case "System.Boolean":
                case "Boolean":
                    return "bool";
                //break;
                case "System.Int32":
                case "Int32":
                    return "int";
                //break;
                case "System.String":
                case "String":
                    return "string";
                //break;
                case "Double":
                    return "double";
            }

            if(qualifiedName.StartsWith("System.Nullable`1[[System.Int32,"))
            {
                return "int?";
            }
            else if(qualifiedName.StartsWith("System.Nullable`1[[System.Single,"))
            {
                return "float?";
            }

            return qualifiedName;
        }


        public static Type GetElementType(Type listType)
        {
            if (listType == null)
            {
                return null;
            }
            if (listType.FullName == "FlatRedBall.SpriteList")
            {
                return typeof(Sprite);
            }

            else
            {
                return listType.GetElementType();
            }

        }


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

        public static string GetDefaultForType(string type)
        {
            switch (type)
            {
                case "String":
                case "string":
                    return "null";
                case "Boolean":
                case "bool":
                    return "false";
                case "Single":
                case "float":
                case "Double":
                case "double":
                    return "0";
                case "Int16":
                case "Int32":
                case "Int64":
                case "int":
                case "long":
                case "byte":
                case "Byte":
                    return "0";
                case "float?":
                case "int?":
                case "long?":
                case "byte?":
                case "double?":
                    return "null";
                default:
                    throw new ArgumentException("Could not find the value for type " + type);
            }
        }
    }
}
