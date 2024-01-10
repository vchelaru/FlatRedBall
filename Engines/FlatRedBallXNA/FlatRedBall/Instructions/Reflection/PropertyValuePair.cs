using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using FlatRedBall.Utilities;
using System.Globalization;
using FlatRedBall.IO;

#if !FRB_RAW
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
#endif

namespace FlatRedBall.Instructions.Reflection
{
    public struct PropertyValuePair
    {
        static Type stringType = typeof(string);

        public string Property;
        public object Value;

        static Dictionary<string, Type> mUnqualifiedTypeDictionary = new Dictionary<string, Type>();

#if UWP

        /// <summary>
        /// Stores a reference to the current assembly. This is the
        /// assembly of your game.  This must be explicitly set.
        /// </summary>
        public static Assembly TopLevelAssembly
        {
            get;
            set;
        }

#endif

        public static List<Assembly> AdditionalAssemblies
        {
            get;
            private set;
        }

        static PropertyValuePair()
        {
            AdditionalAssemblies = new List<Assembly>();
        }

        public PropertyValuePair(string property, object value)
        {
            Property = property;
            Value = value;
        }


        public static string ConvertTypeToString(object value)
        {
            if (value == null) return string.Empty;

            // Get the type
            Type typeToConvertTo = value.GetType();

            // Do the conversion
            #region Convert To String

            if (typeToConvertTo == typeof(bool))
            {
                return ((bool)value).ToString();
            }

            if (typeToConvertTo == typeof(int) || typeToConvertTo == typeof(Int32) || typeToConvertTo == typeof(Int16))
            {
                return ((int)value).ToString();
            }

            if (typeToConvertTo == typeof(float) || typeToConvertTo == typeof(Single))
            {
                return ((float)value).ToString(CultureInfo.InvariantCulture);
            }

            if (typeToConvertTo == typeof(double))
            {
                return ((double)value).ToString(CultureInfo.InvariantCulture);
            }

            if(typeToConvertTo == typeof(decimal))
            {
                return ((decimal)value).ToString(CultureInfo.InvariantCulture);
            }

            if (typeToConvertTo == typeof(string))
            {
                return (string)value;
            }

#if !FRB_RAW
            if (typeToConvertTo == typeof(Texture2D))
            {
                return ((Texture2D)value).Name;
            }

            if (typeToConvertTo == typeof(Matrix))
            {
                Matrix m = (Matrix)value;

                float[] values = new float[16];

                values[0] = m.M11;
                values[1] = m.M12;
                values[2] = m.M13;
                values[3] = m.M14;

                values[4] = m.M21;
                values[5] = m.M22;
                values[6] = m.M23;
                values[7] = m.M24;

                values[8] = m.M31;
                values[9] = m.M32;
                values[10] = m.M33;
                values[11] = m.M34;

                values[12] = m.M41;
                values[13] = m.M42;
                values[14] = m.M43;
                values[15] = m.M44;

                string outputString = string.Empty;

                // output values in comma-delimited form
                for (int i = 0; i < values.Length; i++)
                {
                    outputString += ((i == 0) ? string.Empty : ",") +
                        ConvertTypeToString(values[i]);
                }

                return outputString;
            }

            if (typeToConvertTo == typeof(Vector2))
            {
                Vector2 v = (Vector2)value;

                return ConvertTypeToString(v.X) + "," +
                    ConvertTypeToString(v.Y);
            }

            if (typeToConvertTo == typeof(Vector3))
            {
                Vector3 v = (Vector3)value;

                return ConvertTypeToString(v.X) + "," +
                    ConvertTypeToString(v.Y) + "," +
                    ConvertTypeToString(v.Z);
            }

            if (typeToConvertTo == typeof(Vector4))
            {
                Vector4 v = (Vector4)value;

                return ConvertTypeToString(v.X) + "," +
                    ConvertTypeToString(v.Y) + "," +
                    ConvertTypeToString(v.Z) + "," +
                    ConvertTypeToString(v.W);
            }
#endif
#if UWP
            if (typeToConvertTo.IsEnum())
#else
            if (typeToConvertTo.IsEnum)
#endif
            {
                return value.ToString();
            }

#endregion

            

            // No cases matched, return empty string
            return String.Empty;
        }

        public static T ConvertStringToType<T>(string value)
        {
            return (T)ConvertStringToType(value, typeof(T));
        }

        public static object ConvertStringToType(string value, string qualifiedTypeName)
        {
            // use "Global" so this file can be used outside of FRB proper
            return ConvertStringValueToValueOfType(value, qualifiedTypeName, null, "Global", trimQuotes:false);
        }

        public static object ConvertStringToType(string value, Type typeToConvertTo)
        {
#if FRB_RAW
            return ConvertStringToType(value, typeToConvertTo, "Global");
#else
            return ConvertStringToType(value, typeToConvertTo, FlatRedBallServices.GlobalContentManager);
#endif
        }

        public static object ConvertStringToType(string value, Type typeToConvertTo, string contentManagerName)
        {
            return ConvertStringToType(value, typeToConvertTo, contentManagerName, false);

        }

        public static object ConvertStringToType(string value, Type typeToConvertTo, string contentManagerName, bool trimQuotes)
        {
            if (IsGenericList(typeToConvertTo))
            {
                return CreateGenericListFrom(value, typeToConvertTo, contentManagerName);
            }
            else
            {
                return ConvertStringValueToValueOfType(value, typeToConvertTo.FullName, typeToConvertTo, contentManagerName, trimQuotes);
            }
        }

        public static object ConvertStringValueToValueOfType(string value, string desiredType, Type alreadyKnownType, string contentManagerName, bool trimQuotes)
        {
            value = value.Trim(); // this is in case there is a space in front - I don't think we need it.

            //Fix any exported CSV bugs (such as quotes around a boolean)
            if (trimQuotes ||
                (value != null && (desiredType != typeof(string).FullName && desiredType != typeof(char).FullName)
                && value.Contains("\"") == false) // If it has a quote, then we don't want to trim.
                )
            {
                if (!value.StartsWith("new ") && desiredType != typeof(string).FullName)
                {
                    value = FlatRedBall.Utilities.StringFunctions.RemoveWhitespace(value);
                }
                value = value.Replace("\"", "");
            }

            // Do the conversion
            #region Convert To Object

            // String needs to be first because it could contain equals and
            // we don't want to cause problems 
            bool handled = false;
            object toReturn = null;

            if (desiredType == typeof(string).FullName)
            {
                toReturn = value;
                handled = true;
            }

            if (!handled)
            {
                TryHandleComplexType(value, desiredType, alreadyKnownType, out handled, out toReturn);
            }

            if (!handled)
            {


                #region bool

                if (desiredType == typeof(bool).FullName || desiredType == "bool")
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        return false;
                    }

                    // value could be upper case like "TRUE" or "True".  Make it lower
                    value = value.ToLowerInvariant();

                    toReturn = bool.Parse(value);
                    handled = true;
                }
                else if(desiredType == typeof(bool?).FullName)
                {
                    if(string.IsNullOrEmpty(value))
                    {
                        toReturn = (bool?)null;
                        handled = true;

                    }
                    else
                    {
                        value = value.ToLowerInvariant();

                        toReturn = bool.Parse(value);
                        handled = true;

                    }
                }

#endregion

                #region int, Int32, Int16, uint, long

                else if (desiredType == typeof(int).FullName || desiredType == typeof(Int32).FullName || desiredType == typeof(Int16).FullName ||
                    desiredType == typeof(uint).FullName || desiredType == typeof(long).FullName || desiredType == typeof(byte).FullName)
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        return 0;
                    }


                    int indexOfDecimal = value.IndexOf('.');

                    if (value.IndexOf(",") != -1)
                    {
                        value = value.Replace(",", "");
                    }

                    #region uint
#if FRB_XNA
                    if (desiredType == typeof(uint).FullName)
                    {
                        if (indexOfDecimal == -1)
                        {
                            return uint.Parse(value);

                        }
                        else
                        {
                            return (uint)(Math.MathFunctions.RoundToInt(float.Parse(value, CultureInfo.InvariantCulture)));
                        }
                    }
#endif
#endregion

                    #region byte
#if FRB_XNA

                    if (desiredType == typeof(byte).FullName)
                    {
                        if (indexOfDecimal == -1)
                        {
                            return byte.Parse(value);

                        }
                        else
                        {
                            return (byte)(Math.MathFunctions.RoundToInt(float.Parse(value, CultureInfo.InvariantCulture)));
                        }
                    }
#endif
#endregion

                    #region long
                    if (desiredType == typeof(long).FullName)
                    {
                        if (indexOfDecimal == -1)
                        {
                            return long.Parse(value);

                        }
#if FRB_XNA

                        else
                        {
                            return (long)(Math.MathFunctions.RoundToInt(float.Parse(value, CultureInfo.InvariantCulture)));
                        }
#endif
                    }

#endregion

                    #region regular int
                    else
                    {

                        if (indexOfDecimal == -1)
                        {
                            return int.Parse(value);

                        }
#if FRB_XNA

                        else
                        {
                            return (int)(Math.MathFunctions.RoundToInt(float.Parse(value, CultureInfo.InvariantCulture)));
                        }
#endif
                    }
#endregion
                }

                else if(desiredType == typeof(int?).FullName)
                {
                    if(string.IsNullOrWhiteSpace(value))
                    {
                        toReturn = (int?)null;
                        handled = true;
                    }
                    else
                    {
                        handled = true;
                        toReturn = int.Parse(value);
                    }
                }

                #endregion

                #region float, Single

                else if (desiredType == typeof(float).FullName || desiredType == typeof(Single).FullName)
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        return 0f;
                    }

                    return float.Parse(value, CultureInfo.InvariantCulture);
                }

                else if (desiredType == typeof(float?).FullName)
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        toReturn = (float?)null;
                        handled = true;
                    }
                    else
                    {
                        handled = true;
                        toReturn = float.Parse(value, CultureInfo.InvariantCulture);
                    }
                }

                #endregion

                #region double

                else if (desiredType == typeof(double).FullName)
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        return 0.0;
                    }

                    return double.Parse(value, CultureInfo.InvariantCulture);
                }

#endregion

                else if(desiredType == typeof(double?).FullName)
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        toReturn = (double?)null;
                        handled = true;
                    }
                    else
                    {
                        handled = true;
                        toReturn = double.Parse(value);
                    }
                }

                #region Decimal

                else if(desiredType == typeof(decimal).FullName)
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        return 0.0m;
                    }

                    return decimal.Parse(value, CultureInfo.InvariantCulture);
                }

                #endregion

                else if(desiredType == typeof(DateTime).FullName)
                {
                    //return DateTime.Parse(value);
                    var parsedDateTime = DateTime.Parse(value, null, DateTimeStyles.RoundtripKind);

                    return parsedDateTime;
                }

#if !FRB_RAW

                #region Texture2D

                                else if (desiredType == typeof(Texture2D).FullName)
                                {
                                    if (string.IsNullOrEmpty(value))
                                    {
                                        return null;
                                    }
                #if !SILVERLIGHT && !ZUNE
                                    if (FileManager.IsRelative(value))
                                    {
                                        // Vic says:  This used to throw an exception on relative values.  I'm not quite
                                        // sure why this is the case...why don't we just make it relative to the relative
                                        // directory?  Maybe there's a reason to have this exception, but I can't think of
                                        // what it is, and I'm writing a tutorial on how to load Texture2Ds from CSVs right
                                        // now and it totally makes sense that the user would want to use a relative directory.
                                        // In fact, the user will want to always use a relative directory so that their project is
                                        // portable.
                                        //throw new ArgumentException("Texture path must be absolute to load texture.  Path: " + value);
                                        value = FileManager.RelativeDirectory + value;
                                    }

                                    // Try to load a compiled asset first
                                    if (FileManager.FileExists(FileManager.RemoveExtension(value) + @".xnb"))
                                    {
                                        Texture2D texture =
                                            FlatRedBallServices.Load<Texture2D>(FileManager.RemoveExtension(value), contentManagerName);


                                        // Vic asks:  Why did we have to set the name?  This is redundant and gets
                                        // rid of the standardized file name which causes caching to not work properly.
                                        // texture.Name = FileManager.RemoveExtension(value);
                                        return texture;
                                    }
                                    else
                                    {
                                        Texture2D texture =
                                            FlatRedBallServices.Load<Texture2D>(value, contentManagerName);

                                        // Vic asks:  Why did we have to set the name?  This is redundant and gets
                                        // rid of the standardized file name which causes caching to not work properly.                        
                                        // texture.Name = value;
                                        return texture;
                                    }
                #else
                                return null;
                #endif
                                }

                #endregion

                #region Matrix

                                else if (desiredType == typeof(Matrix).FullName)
                                {
                                    if (string.IsNullOrEmpty(value))
                                    {
                                        return Matrix.Identity;
                                    }

                                    value = StripParenthesis(value);

                                    // Split the string
                                    string[] stringvalues = value.Split(new char[] { ',' });

                                    if (stringvalues.Length != 16)
                                    {
                                        throw new ArgumentException("String to Matrix conversion requires 16 values, " +
                                            "supplied string contains " + stringvalues.Length + " values", "value");
                                    }

                                    // Convert to floats
                                    float[] values = new float[16];
                                    for (int i = 0; i < values.Length; i++)
                                    {
                                        values[i] = float.Parse(stringvalues[i], CultureInfo.InvariantCulture);
                                    }

                                    // Parse to matrix
                                    Matrix m = new Matrix(
                                        values[0], values[1], values[2], values[3],
                                        values[4], values[5], values[6], values[7],
                                        values[8], values[9], values[10], values[11],
                                        values[12], values[13], values[14], values[15]
                                        );

                                    return m;
                                }

                #endregion

                #region Vector2

                                else if (desiredType == typeof(Vector2).FullName)
                                {
                                    if (string.IsNullOrEmpty(value))
                                    {
                                        return new Vector2(0, 0);
                                    }

                                    value = StripParenthesis(value);

                                    // Split the string
                                    string[] stringvalues = value.Split(new char[] { ',' });

                                    if (stringvalues.Length != 2)
                                    {
                                        throw new ArgumentException("String to Vector2 conversion requires 2 values, " +
                                            "supplied string contains " + stringvalues.Length + " values", "value");
                                    }

                                    // Convert to floats
                                    float[] values = new float[2];
                                    for (int i = 0; i < values.Length; i++)
                                    {
                                        values[i] = float.Parse(stringvalues[i], CultureInfo.InvariantCulture);
                                    }

                                    return new Vector2(values[0], values[1]);
                                }

                #endregion

                #region Vector3

                                else if (desiredType == typeof(Vector3).FullName)
                                {
                                    if (string.IsNullOrEmpty(value))
                                    {
                                        return new Vector3(0, 0, 0);
                                    }

                                    value = StripParenthesis(value);


                                    // Split the string
                                    string[] stringvalues = value.Split(new char[] { ',' });

                                    if (stringvalues.Length != 3)
                                    {
                                        throw new ArgumentException("String to Vector3 conversion requires 3 values, " +
                                            "supplied string contains " + stringvalues.Length + " values", "value");
                                    }

                                    // Convert to floats
                                    float[] values = new float[3];
                                    for (int i = 0; i < values.Length; i++)
                                    {
                                        values[i] = float.Parse(stringvalues[i], CultureInfo.InvariantCulture);
                                    }

                                    return new Vector3(values[0], values[1], values[2]);
                                }

                #endregion

                #region Vector4

                                else if (desiredType == typeof(Vector4).FullName)
                                {
                                    if (string.IsNullOrEmpty(value))
                                    {
                                        return new Vector4(0, 0, 0, 0);
                                    }

                                    value = StripParenthesis(value);

                                    // Split the string
                                    string[] stringvalues = value.Split(new char[] { ',' });

                                    if (stringvalues.Length != 4)
                                    {
                                        throw new ArgumentException("String to Vector4 conversion requires 4 values, " +
                                            "supplied string contains " + stringvalues.Length + " values", "value");
                                    }

                                    // Convert to floats
                                    float[] values = new float[4];
                                    for (int i = 0; i < values.Length; i++)
                                    {
                                        values[i] = float.Parse(stringvalues[i], CultureInfo.InvariantCulture);
                                    }

                                    return new Vector4(values[0], values[1], values[2], values[3]);
                                }

                #endregion
#endif

                else if(desiredType.StartsWith("System.Nullable`1"))
                {
                    if(string.IsNullOrEmpty(value))
                    {
                        return null;
                    }
                    else if(alreadyKnownType != null)
                    {
                        var genericType = alreadyKnownType.GenericTypeArguments[0];

                        // now just return the type using the generic:
                        return ConvertStringValueToValueOfType(value, genericType.FullName, genericType, contentManagerName, trimQuotes);
                    }
                }

                #region enum
                else if (IsEnum(desiredType))
                {
#if DEBUG
                    if (string.IsNullOrEmpty(value))
                    {
                        throw new InvalidOperationException("Error trying to create enum value for empty string.  Enum type: " + desiredType);
                    }
#endif


                    bool ignoreCase = true; // 3rd arugment needed for 360

                    Type foundType;

                    if (mUnqualifiedTypeDictionary.ContainsKey(desiredType))
                    {
                        foundType = mUnqualifiedTypeDictionary[desiredType];
                    }
                    else
                    {
                        foundType = TryToGetTypeFromAssemblies(desiredType);
                    }

                    return Enum.Parse(foundType, value, ignoreCase); // built-in .NET version
                    //return StringEnum.Parse(typeToConvertTo, value);
                }

#endregion

                #region Color
#if FRB_XNA

                else if (desiredType == typeof(Color).FullName)
                {
#if WINDOWS_8 || UWP
                    PropertyInfo info = typeof(Color).GetProperty(value);
#else
                    PropertyInfo info = typeof(Color).GetProperty(value, BindingFlags.GetField | BindingFlags.Public | BindingFlags.Static);
#endif

                    if (info == null)
                    {
                        if (value.StartsWith("Color."))
                        {
                            throw new Exception("Could not parse the value " + value + ".  Remove \"Color.\" and instead " +
                                "use " + value.Substring("Color.".Length));
                        }
                        else
                        {
                            throw new Exception("Could not parse " + value + " as a Color");
                        }
                    }

                    toReturn = info.GetValue(null, null);
                    handled = true;
                }

#endif
                #endregion

                #region Check Unqualified types which are not enums
                else if(mUnqualifiedTypeDictionary.ContainsKey(desiredType))
                {
                    var foundType = mUnqualifiedTypeDictionary[desiredType];

                    // See if this has a static here:
                    var foundStaticField = foundType.GetField(value);

                    var fieldValue = foundStaticField?.GetValue(null);
                    if (fieldValue != null)
                    {
                        handled = true;
                        return fieldValue;
                    }
                }
                #endregion
                #endregion

                // Why do we catch exceptions here?  That seems baaaad
                //catch (Exception)
                //{
                //    //int m = 3;
                //}


                if (!handled)
                {
                    throw new NotImplementedException("Cannot convert the value " + value + $" ({value?.GetType()}) to the type " +
                        desiredType.ToString());
                }
            }

            return toReturn;
        }

        private static bool IsEnum(string typeAsString)
        {
            if(typeAsString == null)
            {
                throw new ArgumentNullException(nameof(typeAsString));
            }
            Type foundType = null;

            if (mUnqualifiedTypeDictionary.ContainsKey(typeAsString))
            {
                foundType = mUnqualifiedTypeDictionary[typeAsString];
            }
            else
            {
                foundType = TryToGetTypeFromAssemblies(typeAsString);
            }

            return foundType != null &&
#if WINDOWS_8 || UWP
                foundType.IsEnum();
#else
                foundType.IsEnum;
#endif
        }

        private static void TryHandleComplexType(string value, string typeName, Type alreadyKnownType, out bool handled, out object toReturn)
        {
            handled = false;
            toReturn = null;




            if (value.StartsWith("new "))
            {
                string typeAfterNewString = value.Substring("new ".Length, value.IndexOf('(') - "new ".Length);

                Type foundType = alreadyKnownType;

                if(foundType == null)
                {
                    if (mUnqualifiedTypeDictionary.ContainsKey(typeAfterNewString))
                    {
                        foundType = mUnqualifiedTypeDictionary[typeAfterNewString];
                    }
                    else
                    {
                        foundType = TryToGetTypeFromAssemblies(typeAfterNewString);
                    }
                }


                if (foundType != null)
                {
                    int openingParen = value.IndexOf('(');

                    // Make sure to get the last parenthesis, in case one of the inner properties is a complex type
                    int closingParen = value.LastIndexOf(')');

                    // Make sure valid parenthesis were found
                    if (openingParen < 0 || closingParen < 0)
                        throw new InvalidOperationException("Type definition did not have a matching pair of opening and closing parenthesis");

                    if (openingParen > closingParen)
                        throw new InvalidOperationException("Type definition has parenthesis in the incorrect order");

                    string valueInsideParens = value.Substring(openingParen + 1, closingParen - (openingParen + 1));
                    toReturn = CreateInstanceFromNamedAssignment(foundType, valueInsideParens);
                }
                else
                {
                    throw new InvalidOperationException("Could not find a type in the assemblies for " + foundType);
                }
                handled = true;
            }



            else if (value.Contains("="))
            {
                // They're using the "x=0,y=0,z=0" syntax
                handled = true;

                Type foundType = alreadyKnownType;
                if(foundType == null)
                {
                    if (mUnqualifiedTypeDictionary.ContainsKey(typeName))
                    {
                        foundType = mUnqualifiedTypeDictionary[typeName];
                    }
                    else
                    {
                        foundType = TryToGetTypeFromAssemblies(typeName);
                    }
                }

                toReturn = CreateInstanceFromNamedAssignment(foundType, value);
            }
        }

        public static List<string> SplitProperties(string value)
        {

            var splitOnComma = value.Split(',');

            List<string> toReturn = new List<string>();

            // We may have a List declaration, or a string with commas in it, so we need to account for that
            // For now I'm going to handle the list declaration because that's what I need for Baron, but will
            // eventually return to this and make it more robust to handle strings with commas too.
            int parenCount = 0;
            int quoteCount = 0;

            foreach (string entryInSplit in splitOnComma)
            {
                bool shouldCombine = parenCount != 0 || quoteCount != 0;

                parenCount += entryInSplit.CountOf("(");
                parenCount -= entryInSplit.CountOf(")");
                                
                quoteCount += entryInSplit.CountOf("\"");
                quoteCount = (quoteCount % 2);

                if (shouldCombine)
                {
                    toReturn[toReturn.Count - 1] = toReturn[toReturn.Count - 1] + ',' + entryInSplit;
                }
                else
                {
                    toReturn.Add(entryInSplit);
                }

            }

            return toReturn;
        }

        private static object CreateGenericListFrom(string value, Type listType, string contentManagerName)
        {
            object newObject = Activator.CreateInstance(listType);
            Type genericType = listType.GetGenericArguments()[0];
            MethodInfo add = listType.GetMethod("Add");
            
            int start = value.IndexOf("(") + 1;
            int end = value.IndexOf(")");

            if (end > 0)
            {
                string insideOfParens = value.Substring(start, end - start);

                // Cheat for now, make it more robust later
                var values = SplitProperties(insideOfParens);

                object[] arguments = new object[1];

                foreach (var itemInList in values)
                {
                    object converted = ConvertStringToType(itemInList, genericType, contentManagerName, true);
                    arguments[0] = converted;
                    add.Invoke(newObject, arguments);

                }
            }

            return newObject;
        }

        private static Type TryToGetTypeFromAssemblies(string typeAfterNewString)
        {
            Type foundType = null;


#if WINDOWS_8 || UWP
            foundType = TryToGetTypeFromAssembly(typeAfterNewString, FlatRedBallServices.Game.GetType().GetTypeInfo().Assembly);

            if (foundType == null)
            {
#if DEBUG
                if (TopLevelAssembly == null)
                {
                    throw new Exception("The TopLevelAssembly member must be set before it is used.  It is currently null");
                }
#endif
                foundType = TryToGetTypeFromAssembly(typeAfterNewString, TopLevelAssembly);
            }
            if (foundType == null)
            {
                foundType = TryToGetTypeFromAssembly(typeAfterNewString, typeof(Vector3).GetTypeInfo().Assembly);
            }
            if(foundType == null)
            {
                foundType = TryToGetTypeFromAssembly(typeAfterNewString, typeof(FlatRedBall.Sprite).GetTypeInfo().Assembly);
            }
#else

            // This may be run from a tool.  If so
            // then there is no Game class, so we shouldn't
            // try to use it.
#if FRB_XNA

            if (FlatRedBallServices.Game != null)
            {
                foundType = TryToGetTypeFromAssembly(typeAfterNewString, FlatRedBallServices.Game.GetType().Assembly);
            }
#endif
            if (foundType == null)
            {
                foreach (var assembly in AdditionalAssemblies)
                {
                    foundType = TryToGetTypeFromAssembly(typeAfterNewString, assembly);
                    if (foundType != null)
                    {
                        break;
                    }
                }
            }

            if(foundType == null)
            {
                foundType = TryToGetTypeFromAssembly(typeAfterNewString, Assembly.GetExecutingAssembly());
            }
            if (foundType == null)
            {
#if WINDOWS
                foundType = TryToGetTypeFromAssembly(typeAfterNewString, Assembly.GetEntryAssembly());
#endif
            }
#if FRB_XNA

            if(foundType == null)
            {
                foundType = TryToGetTypeFromAssembly(typeAfterNewString, typeof(Vector3).Assembly);
            }
#endif
#endif

            if (foundType == null)
            {
                throw new ArgumentException
                    ("Could not find the type for " + typeAfterNewString + 
                    "\nIf this is a type in your project, you may need to add the assembly to the PropertyValuePair.AdditionalAssemblies");
            }
            else
            {
                mUnqualifiedTypeDictionary.Add(typeAfterNewString, foundType);
            }

            return foundType;
        }

        private static Type TryToGetTypeFromAssembly(string typeAfterNewString, Assembly assembly)
        {
            Type foundType = null;

            // Make sure the type isn't null, and the type string is trimmed to make the compare valid
            if (typeAfterNewString == null)
                return null;

            typeAfterNewString = typeAfterNewString.Trim();

            // Is this slow?  Do we want to cache off the Type[]?

#if WINDOWS_8
            IEnumerable<Type> types = assembly.ExportedTypes;
#else
            IEnumerable<Type> types = assembly.GetTypes();
#endif
            foreach (Type type in types)
            {
                if (type.Name == typeAfterNewString || type.FullName == typeAfterNewString)
                {
                    foundType = type;
                    break;
                }
            }

            return foundType;
        }

        private static object CreateInstanceFromNamedAssignment(Type type, string value)
        {
            object returnObject = null;

            value = value.Trim();

            returnObject = Activator.CreateInstance(type);

            if (!string.IsNullOrEmpty(value))
            {
                var split = SplitProperties(value);


                foreach (string assignment in split)
                {
                    int indexOfEqual = assignment.IndexOf('=');

                    // If the assignment is not in the proper Name=Value format, ignore it
                    // Update - November 6, 2015
                    // This can hide syntax errors
                    // in the CSV. It allows cells to
                    // do things like: new Vectore(1,2,3)
                    // We want an error to be thrown so the
                    // user knows what to do to fix the CSV:
                    if (indexOfEqual < 0)
                    {
						string message = 
						 "Invalid value " + assignment  + " in " + value + $". Expected a variable assignment like \"X={assignment}\" when creating an instance of {type.Name}";
					
                        throw new Exception(message);
                    }

                    // Make sure the = sign isn't the last character in the assignment
                    if (indexOfEqual >= assignment.Length - 1)
                        continue;

                    string variableName = assignment.Substring(0, indexOfEqual).Trim();
                    string whatToassignTo = assignment.Substring(indexOfEqual + 1, assignment.Length - (indexOfEqual + 1));

                    FieldInfo fieldInfo = type.GetField(variableName);

                    if (fieldInfo != null)
                    {
                        Type fieldType = fieldInfo.FieldType;
                        StripQuotesIfNecessary(ref whatToassignTo);
                        object assignValue = ConvertStringToType(whatToassignTo, fieldType);

                        fieldInfo.SetValue(returnObject, assignValue);
                    }
                    else
                    {
                        PropertyInfo propertyInfo = type.GetProperty(variableName);

#if DEBUG
                        if (propertyInfo == null)
                        {
                            throw new ArgumentException("Could not find the field/property " + variableName + " in the type " + type.Name);
                        }
#endif

                        Type propertyType = propertyInfo.PropertyType;

                        StripQuotesIfNecessary(ref whatToassignTo);
                        object assignValue = ConvertStringToType(whatToassignTo, propertyType);

                        propertyInfo.SetValue(returnObject, assignValue, null);
                    }


                }
            }
            return returnObject;
        }

        public static bool IsGenericList(Type type)
        {
            bool isGenericList = false;
#if WINDOWS_8 || UWP
            // Not sure why we check the declaring type.  I think declaring
            // type is for when a class is inside of another class
            //if (type.DeclaringType.IsGenericParameter && (type.GetGenericTypeDefinition() == typeof(List<>)))
            if (type.GetGenericTypeDefinition() == typeof(List<>))
#else
            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>)))
#endif
            {
                isGenericList = true;
            }

            return isGenericList;
        }

        private static void StripQuotesIfNecessary(ref string whatToassignTo)
        {
            if (whatToassignTo != null)
            {
                string trimmed = whatToassignTo.Trim();

                if (trimmed.StartsWith("\"") &&
                   trimmed.EndsWith("\"") && trimmed.Length > 1)
                {
                    whatToassignTo = trimmed.Substring(1, trimmed.Length - 2);
                }
            }
        }

        //Remove any parenthesis at the start and end of the string.
        private static string StripParenthesis(string value)
        {
            string result = value;

            if (result.StartsWith("("))
            {
                int startIndex = 1;
                int endIndex = result.Length - 1;
                if (result.EndsWith(")"))
                    endIndex -= 1;

                result = result.Substring(startIndex, endIndex);
            }

            return result;
        }

        public override string ToString()
        {
            return Property + " = " + Value;
        }
    }
}
