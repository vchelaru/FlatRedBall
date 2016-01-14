using System;
using System.Reflection;
using System.Collections.Generic;
using FlatRedBall.Attributes;
using Microsoft.Xna.Framework.Content;
using System.Collections;
using FlatRedBall.Utilities;
using FlatRedBall.Content.Scene;

namespace FlatRedBall.Content
{



    #region XML Docs
    /// <summary>
    /// Class used by the FlatRedBall pipeline to read in XNBs.
    /// </summary>
    #endregion
    public static class ObjectReader
    {
        public static bool UseReflection = false;


        public static T ReadObject<T>(ContentReader input)
        {
            Type type = typeof(T);
            object objectToReturn = default(T);

            if (type.Name == "SpriteSave")
            {
                objectToReturn = SpriteReader.ReadUsingGeneratedCode(input);
            }
            else if (type.Name == "TextSave")
            {
                objectToReturn = TextReader.ReadTextSave(input);
            }
            else if (type.Name == "AnimationChainListSave")
            {
                objectToReturn = AnimationChainListReader.ReadAnimationChainListSave(input);
            }
            else if (type.Name == "AnimationChainSave")
            {
                objectToReturn = AnimationChainListReader.ReadAnimationChainSave(input);
            }
            else if (type.Name == "AnimationFrameSave")
            {
                objectToReturn = AnimationChainListReader.ReadAnimationFrameSave(input);
            }
            else
            {



                //If T is primitive
                #region Read primative values
                //Immediately Read if this is a primative type
                if ((type.IsPrimitive) || type == typeof(string) || type == typeof(decimal))
                {
                    objectToReturn = input.ReadObject<T>();

                }

                //Read an Int for the Enum value
                else if (type.BaseType == typeof(System.Enum))
                {
                    objectToReturn = (T)Enum.ToObject(typeof(T), (byte)input.ReadInt32());
                }
                #endregion

                //If T is a Collection
                #region Read Collections
                //Check if this type is a collection

                else if (GetInterface(type, "IEnumerable") != null)
                {
                    /*
    #if XBOX360
                    MethodInfo[] methods = typeof(ObjectReader).GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
           
                    MethodInfo readCollection = methods[9];
                    readCollection = readCollection.MakeGenericMethod(new Type[1] { typeof(T) });
                    objectToReturn = readCollection.Invoke(null, new object[2] { input, false });
    #else
                    objectToReturn = ReadCollection<T>(input, false);
    #endif
                    */

                    objectToReturn = ReadCollection<T>(input, false);
                }
                #endregion

                //If T is an object
                #region Read Objects
                else
                {

                    List<MemberInfo> members = GetMembersToRead(type);

                    //obtain the location of null fields for this object
                    bool[] isNullArray = ReadBoolArray(input);

                    if (members.Count != isNullArray.Length)
                    {
                        // Used to be FileLoadException, but that's not
                        // supported on the 360.
                        throw new Exception("The object of type " +
                            type.ToString() + " has " + members.Count + " members, but the ObjectReader " +
                            "read " + isNullArray.Length + " bools for null members.");
                    }

                    if (!type.IsValueType && !HasParameterlessConstructor(type))
                    {
                        // Reference types must have no-argument constructors or else they can't be created.
                        throw new ArgumentException("The type " + type.Name + " does not have a parameterless constructor. " +
                            "This is needed to properly load the object through the Content Pipeline.");
                    }

                    //Build the instance
                    objectToReturn = System.Activator.CreateInstance<T>();



                    #region Read each Field and Property
                    int i = 0;
                    foreach (MemberInfo member in members)
                    {
                        //If this member is a property
                        if (member.MemberType == MemberTypes.Property)
                        {
                            #region Read Property
                            PropertyInfo field = (member as PropertyInfo);

                            if (!isNullArray[i++])
                            {
                                object memberValue = RecursiveRead(field.PropertyType, input);

                                //check if it's tagged with XmlIgnore
                                if (IsIgnored(field)) continue;

                                if (type.IsValueType) objectToReturn = BoxAndSetValue((object)objectToReturn, field, memberValue);
                                else field.SetValue(objectToReturn, memberValue, null);
                            }
                            else
                            {
                                if (field.PropertyType == typeof(string))
                                {
                                    if (type.IsValueType) objectToReturn = BoxAndSetValue((object)objectToReturn, field, String.Empty);
                                    else field.SetValue(objectToReturn, String.Empty, null);
                                }
                            }
                            #endregion
                        }

                        else
                        {
                            #region Read Field
                            FieldInfo field = (member as FieldInfo);

                            //if this value isn't meant to be null, read it
                            if (!isNullArray[i++])
                            {

                                if (IsExternalInstance(field))
                                {
                                    ReadExternalInstance(field, ref objectToReturn, input);
                                }

                                else
                                {
                                    object memberValue;
                                    if (typeof(T) == typeof(FlatRedBall.Content.Instructions.InstructionSave) && field.Name.Equals("Value"))
                                    {
                                        string typeName = (objectToReturn as FlatRedBall.Content.Instructions.InstructionSave).Type;
                                        memberValue = RecursiveRead(System.Type.GetType(typeName), input);
                                    }
                                    else memberValue = RecursiveRead(field.FieldType, input);


                                    //check if it's tagged with XmlIgnore
                                    if (IsIgnored(field)) continue;

                                    if (type.IsValueType)
                                        objectToReturn = BoxAndSetValue((object)objectToReturn, field, memberValue);
                                    else
                                        field.SetValue(objectToReturn, memberValue);
                                }
                            }

                            //if it is meant to be null, leave it as null or set it to String.Empty(for strings)
                            else
                            {
                                if (field.FieldType == typeof(string))
                                {
                                    if (type.IsValueType)
                                        objectToReturn = BoxAndSetValue((object)objectToReturn, field, String.Empty);
                                    else
                                        field.SetValue(objectToReturn, String.Empty);
                                }
                            }
                            #endregion
                        }
                    }
                    #endregion
                }
                #endregion

            }
            return (T)objectToReturn;

        }


        #region Private Methods

        private static object BoxAndSetValue(object valueType, MemberInfo member, object value)
        {
            if (member.MemberType == MemberTypes.Field)
                (member as FieldInfo).SetValue(valueType, value);
            else
                (member as PropertyInfo).SetValue(valueType, value, null);

            return valueType;
        }

        private static Type GetInterface(Type type, string interfaceName)
        {
            Type[] interfaces = type.GetInterfaces();

            foreach (Type interfaceType in interfaces)
            {
                if (interfaceType.Name == interfaceName)
                    return interfaceType;
            }
            return null;
        }

        private static bool HasParameterlessConstructor(Type type)
        {
            return type.GetConstructor(new Type[0]) != null;
        }



        private static bool IsExternalInstance(FieldInfo field)
        {
            ExternalInstance instance = null;
            ExternalInstanceList instanceList = null;

            foreach (object attribute in field.GetCustomAttributes(true))
            {
                instance = (attribute as ExternalInstance);
                instanceList = (attribute as ExternalInstanceList);
                if (instance != null || instanceList != null)
                    break;

            }
            if (instance == null && instanceList == null) return false;
            else return true;
        }

        private static bool IsIgnored(FieldInfo field)
        {
            System.Xml.Serialization.XmlIgnoreAttribute ignoreAttribute = null;

            foreach (object attribute in field.GetCustomAttributes(true))
            {
                ignoreAttribute = (attribute as System.Xml.Serialization.XmlIgnoreAttribute);
                if (ignoreAttribute != null) break;
            }
            if (ignoreAttribute == null) return false;
            else return true;
        }

        private static bool IsExternalInstanceList(FieldInfo field)
        {
            ExternalInstanceList instanceList = null;

            foreach (object attribute in field.GetCustomAttributes(true))
            {
                instanceList = (attribute as ExternalInstanceList);
                if (instanceList != null) break;
            }
            if (instanceList == null) return false;
            else return true;
        }

        private static bool IsExternalInstance(PropertyInfo property)
        {
            ExternalInstance instance = null;
            ExternalInstanceList instanceList = null;

            foreach (object attribute in property.GetCustomAttributes(true))
            {
                instance = (attribute as ExternalInstance);
                instanceList = (attribute as ExternalInstanceList);
                if (instance != null || instanceList != null) break;
            }
            if (instance == null && instanceList == null) return false;
            else return true;
        }

        private static bool IsIgnored(PropertyInfo property)
        {
            System.Xml.Serialization.XmlIgnoreAttribute ignoreAttribute = null;

            foreach (object attribute in property.GetCustomAttributes(true))
            {
                ignoreAttribute = (attribute as System.Xml.Serialization.XmlIgnoreAttribute);
                if (ignoreAttribute != null) break;
            }
            if (ignoreAttribute == null) return false;
            else return true;
        }

        private static List<MemberInfo> GetMembersToRead(Type type)
        {
            List<MemberInfo> returnList = new List<MemberInfo>();
            returnList.AddRange(type.GetProperties(BindingFlags.Public | BindingFlags.Instance));
            returnList.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic));
            returnList.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.Public));

            returnList.Sort(SortMembersForReading);

            for (int i = returnList.Count - 1; i > -1; i--)
            {
                if (IsPrivateField(returnList[i]))
                {
                    returnList.RemoveAt(i);
                }
            }
            // This doesn't seem to work in XNA4 on the 360:
            //returnList.RemoveAll(IsPrivateField);
            return returnList;

        }

        #region Reading Helpers

        private static T ReadCollection<T>(ContentReader input, bool isExternal)
        {
            if (!isExternal)
                return ReadCollection<T>(input, false, null);
            else
                throw new ArgumentException("Must provide the expected Type to be read when Reading a collection of External References");
        }

        private static T ReadCollection<T>(ContentReader input, bool isExternal, Type readType)
        {
            object objectToReturn = null;
            Type type = typeof(T);
            int count = input.ReadInt32();
            bool[] isNullArray = ReadBoolArray(input);

            if (count != isNullArray.Length)
            {
                // Used to be FileLoadException, but that's not supported
                // on the Xbox 360
                throw new Exception("The collection of type " + 
                    type.ToString() + " has " + count + " entries, but the ObjectReader " + 
                    "read " + isNullArray.Length + " bools for null entries.");
            }

            //If type is an Array or List
            if (GetInterface(type, "IList") != null)
            {
                MethodInfo listRecur = default(MethodInfo);

                //If this is an array
                if (type.IsArray)
                {
                    #region Read Array
                    Type elementType = type.GetElementType();

                    //Build the instance to return and the recursive call.
                    objectToReturn = Array.CreateInstance(type.GetElementType(), count);

                    //If this field is marked with the ExternalInstance attribute.
                    if (isExternal)
                    {
                        //If the elements of this array are also Collections
                        if (GetInterface(elementType, "IEnumerable") != null)
                        {
                            listRecur = typeof(ObjectReader).GetMethod("ReadCollection", BindingFlags.NonPublic | BindingFlags.Static,
                                null, new Type[] {input.GetType(), typeof(bool), typeof(Type)},
                                null);
                            
                            //Check if null, which will be the case if running on the 360
                            if (listRecur == null)
                            {
                                listRecur = GetPrivateObjectReaderMethod("ReadCollection", new Type[] { input.GetType(), typeof(bool), typeof(Type) });
                            }
                            
                            listRecur = listRecur.MakeGenericMethod(new Type[1] { elementType });
                            for (int i = 0; i < count; ++i)
                            {
                                if (!isNullArray[i])
                                    (objectToReturn as System.Collections.IList)[i] = listRecur.Invoke(null, new object[3] { input, true, elementType });
                            }
                        }

                        else
                        {
                            listRecur = typeof(ContentReader).GetMethod("ReadExternalReference").MakeGenericMethod(new Type[1] { elementType });
                            for (int i = 0; i < count; ++i)
                            {
                                if (!isNullArray[i])
                                    (objectToReturn as System.Collections.IList)[i] = listRecur.Invoke(input, new object[0] { });
                            }
                        }
                    }

                    else
                    {
                        listRecur = typeof(ObjectReader).GetMethod("ReadObject").MakeGenericMethod(new Type[1] { elementType });

                        for (int i = 0; i < count; ++i)
                        {
                            //invoke the recursive call for each element.
                            if (!isNullArray[i])

                                (objectToReturn as System.Collections.IList)[i] = listRecur.Invoke(null, new object[1] { input });
                        }
                    }
                    #endregion
                }

                //Similar process, but for Lists
                else
                {
                    #region Read List
                    Type tempType = type;
                    objectToReturn = System.Activator.CreateInstance<T>();

                    //Move up the inheritence tree until you get a Generic definition
                    while (!tempType.IsGenericType)
                    {
                        if (tempType.BaseType != null)
                            tempType = tempType.BaseType;
                    }

                    if (count != 0)
                    {
                        //Build the recursive call with the Generic type
                        Type[] genericParam = tempType.GetGenericArguments();

                        if (genericParam[0] == typeof(FlatRedBall.Content.Scene.SpriteSave))
                        {
                            for (int i = 0; i < count; ++i)
                            {
                                if (!isNullArray[i])
                                {
                                    FlatRedBall.Content.Scene.SpriteSave ss =
                                        ObjectReader.ReadObject<FlatRedBall.Content.Scene.SpriteSave>(input);


                                    (objectToReturn as System.Collections.IList).Add(ss);

                                }
                            }                            

                        }
                        else
                        {
                            Type objectReaderType = typeof(ObjectReader);
                            
                            MethodInfo nonGenericReadObject = objectReaderType.GetMethod("ReadObject");

                            listRecur = nonGenericReadObject.MakeGenericMethod(genericParam);


                            for (int i = 0; i < count; ++i)
                            {
                                if (!isNullArray[i])
                                    (objectToReturn as System.Collections.IList).Add(listRecur.Invoke(null, new object[1] { input }));
                            }
                        }


                    }
                    #endregion
                }

            }

            //For Dictionaries 
            //ToDo: Test
            else if (GetInterface(type, "IDictionary") != null)
            {
                #region Read Dictionary
                objectToReturn = System.Activator.CreateInstance<T>();
                Type[] genericParam = type.GetGenericArguments();
                MethodInfo dictRecur1 = typeof(ObjectReader).GetMethod("ReadObject").MakeGenericMethod(new Type[1] { genericParam[0] });
                MethodInfo dictRecur2 = typeof(ObjectReader).GetMethod("ReadObject").MakeGenericMethod(new Type[1] { genericParam[1] });

                for (int i = 0; i < count; ++i)
                {
                    //Dictionary values are allowed to be null, but their keys are not.
                    if (!isNullArray[i])
                    {
                        (objectToReturn as System.Collections.IDictionary).Add(dictRecur1.Invoke(null, new object[1] { input }),
                                                                           dictRecur2.Invoke(null, new object[1] { input }));
                    }
                    else
                    {
                        (objectToReturn as System.Collections.IDictionary).Add(dictRecur1.Invoke(null, new object[1] { input }),
                                                                           null);
                    }
                }
                #endregion
            }

            //For Stacks
            else if (GetInterface(type, "Stack") != null)
            {
#if WINDOWS_PHONE || XBOX360
                throw new NotImplementedException();
#else
                #region Read Stack
                objectToReturn = System.Activator.CreateInstance<T>();
                Type[] genericParam = type.GetGenericArguments();
                MethodInfo stackRecur = typeof(ObjectReader).GetMethod("ReadObject").MakeGenericMethod(new Type[1] { genericParam[0] });

                for (int i = 0; i < count; ++i)
                {

                    if (!isNullArray[i])
                        (objectToReturn as System.Collections.Stack).Push(stackRecur.Invoke(null, new object[1] { input }));
                }
                #endregion
#endif
            }

            //And for Queues
            else if (GetInterface(type, "Queue") != null)
            {
#if XNA4
                throw new NotImplementedException();
#else
                #region Read Queue
                objectToReturn = System.Activator.CreateInstance<T>();
                Type[] genericParam = type.GetGenericArguments();
                MethodInfo queueRecur = typeof(ObjectReader).GetMethod("ReadObject").MakeGenericMethod(new Type[1] { genericParam[0] });

                for (int i = 0; i < count; ++i)
                {
                    if (!isNullArray[i])
                        (objectToReturn as System.Collections.Queue).Enqueue(queueRecur.Invoke(null, new object[1] { input }));
                }
                #endregion
#endif
            }

            return (T)objectToReturn;
        }

        private static object RecursiveRead(Type type, ContentReader input)
        {
            MethodInfo call = typeof(ObjectReader).GetMethod("ReadObject").MakeGenericMethod(new Type[1] { type });
            //try
            //{
                return call.Invoke(null, new object[1] { input });
            //}
            //catch (Exception e)
            //{
            //    System.Diagnostics.Trace.Write(e.Message);
            //}
            //return null;
        }

        private static void ReadExternalInstance(FieldInfo field, ref object container, ContentReader input)
        {

            if (GetInterface(field.FieldType, "IEnumerable") == null)
            {

                Type refType = field.FieldType;
                MethodInfo readRef = typeof(ContentReader).GetMethod("ReadExternalReference").MakeGenericMethod(new Type[1] { refType });
                field.SetValue(container, readRef.Invoke(input, null));
            }
            else if (IsExternalInstanceList(field))
            {
                Type refType = field.FieldType;
                MethodInfo readCollectionMethod =
                    typeof(ObjectReader).GetMethod("ReadCollection",
                        BindingFlags.NonPublic | BindingFlags.Static,
                        null,
                        new Type[] { input.GetType(), typeof(bool), typeof(Type) },
                        null);

                if (readCollectionMethod == null)
                {
                    //The 360 doesn't seem to like methods with generic retern types, so GetMethod() ignores them. However
                    //GetMethods() still includes them...So to prevent errors such as the one TrashMan360 experienced, we loop
                    //through all methods and check names before throwing the exception(if it's not found)

                    readCollectionMethod = GetPrivateObjectReaderMethod("ReadCollection", new Type[3] { input.GetType(),
                                                                                                        typeof(bool),
                                                                                                        typeof(Type)});

                    if(readCollectionMethod == null)
                        throw new NullReferenceException("No ReadCollection method");
                }

                MethodInfo readRef =
                    readCollectionMethod.MakeGenericMethod(new Type[1] { refType });


                if (readRef == null)
                {
                    throw new NullReferenceException("No generic ReadCollection method");
                }
                try
                {
                    field.SetValue(container, readRef.Invoke(input, new object[3] { input, true, refType }));
                }
                catch (Exception)
                {
                    //
                }
            }
            else
            {
                Type refType = field.FieldType;
                MethodInfo readRef = typeof(ContentReader).GetMethod("ReadExternalReference").MakeGenericMethod(new Type[1] { refType });
                field.SetValue(container, readRef.Invoke(input, null));

            }

        }

        static V Cast<V>(object val)
        {
            return (V)val;
        }

        private static MethodInfo GetPrivateObjectReaderMethod(string methodName, Type[] methodArgs)
        {
            MethodInfo toReturn = null;

            MethodInfo[] methods = typeof(ObjectReader).GetMethods(BindingFlags.NonPublic | BindingFlags.Static);

            foreach (MethodInfo method in methods)
            {
                if (method.Name.Equals(methodName))
                {
                    bool passing = true;

                    MethodInfo genMethod = method.MakeGenericMethod(new Type[1] { typeof(bool) });
                    ParameterInfo[] parameters = genMethod.GetParameters();

                    if (parameters.Length == methodArgs.Length)
                    {
                        for (int i = 0; i < methodArgs.Length && passing; ++i)
                        {
                            if (parameters[i].ParameterType != methodArgs[i])
                                passing = false;
                        }
                    }
                    else passing = false;

                    if (passing)
                    {
                        toReturn = method;
                        break;
                    }
                }
            }

            return toReturn;
        }

        private static bool[] ReadBoolArray(ContentReader input)
        {
            int count = input.ReadInt32();
            bool[] returnArray = new bool[count];
            for (int i = 0; i < count; ++i)
            {
                returnArray[i] = input.ReadBoolean();
            }
            return returnArray;
        }
        #endregion

        #region Sorting Helpers


        public static bool DoesMemberSortFirst(MemberInfo memberInfo)
        {
            object[] attributes = memberInfo.GetCustomAttributes(true);

            foreach (object attribute in attributes)
            {
                if (attribute is ContentSorting)
                {
                    return ((ContentSorting)attribute).SortingStyle == SortingStyle.First;
                }
            }
            return false;
        }

        private static int SortMembersForReading(MemberInfo m1, MemberInfo m2)
        {

            if (ObjectReader.DoesMemberSortFirst(m1))
            {
                return -1;
            }
            else if (ObjectReader.DoesMemberSortFirst(m2))
            {
                return 1;
            }
            else if (m1.MemberType == m2.MemberType)
            {
                if (m1.MemberType == MemberTypes.Field)
                    return SortFieldsByType((m1 as FieldInfo), (m2 as FieldInfo));
                else
                    return SortPropertiesByType((m1 as PropertyInfo), (m2 as PropertyInfo));
            }
            else
            {
                if (m1.MemberType == MemberTypes.Field)
                    return -1;
                else
                    return 1;
            }
        }

        private static int SortPropertiesByType(PropertyInfo p1, PropertyInfo p2)
        {
            if (p1.PropertyType == p2.PropertyType)
            {
                if (IsExternalInstance(p1)) throw new Exception("Objects read through the ObjectReader may not have properties tagged with ExternalInstance. Please load ExternalReference content into fields.");
                else return SortMembersByName((p1 as MemberInfo), (p2 as MemberInfo));
            }
            else
            {
                if (IsExternalInstance(p1))
                {
                    throw new Exception("Objects read through the ObjectReader may not have properties tagged with ExternalInstance. Please load ExternalReference content into fields.");
                }
                else if (IsExternalInstance(p2))
                {
                    throw new Exception("Objects read through the ObjectReader may not have properties tagged with ExternalInstance. Please load ExternalReference content into fields.");
                }
                else
                {
                    return SortMembersByName((p1 as MemberInfo), (p2 as MemberInfo));
                   // return String.Compare(p1.PropertyType.FullName, p2.PropertyType.FullName, false);
                }
            }
        }

        public static int SortFieldsByType(FieldInfo f1, FieldInfo f2)
        {
            
            if (f1.FieldType == f2.FieldType)
            {
#if WINDOWS_PHONE || XBOX360
                if (IsExternalInstance(f1)) return String.Compare(f1.Name, f2.Name,StringComparison.InvariantCultureIgnoreCase);
#else
                if (IsExternalInstance(f1)) return String.Compare(f1.Name, f2.Name, false);
#endif
                return SortMembersByName((f1 as MemberInfo), (f2 as MemberInfo));
            }
            else
            {
                if (IsExternalInstance(f1))
                {
                    if (IsExternalInstance(f2))
                    {
#if WINDOWS_PHONE || XBOX360
                        return String.Compare(f1.Name, f2.Name, StringComparison.InvariantCultureIgnoreCase);
#else
                        return String.Compare(f1.Name, f2.Name, false);
#endif
                    }
                    else
                    {
                        return 1;
                    }
                }
                else if (IsExternalInstance(f2))
                {
                    return -1;
                }
                else
                {
                    /*int result = String.Compare(f1.FieldType.FullName, f2.FieldType.FullName, false);

                    if (result < 0)
                        return -1;
                    else
                        return 1;*/
                    return SortMembersByName((f1 as MemberInfo), (f2 as MemberInfo));
                }
            }
        }

        private static int SortMembersByName(MemberInfo m1, MemberInfo m2)
        {
#if WINDOWS_PHONE || XBOX360
            int retValue = String.Compare(m1.Name, m2.Name, StringComparison.InvariantCultureIgnoreCase);
#else
            int retValue = String.Compare(m1.Name, m2.Name, false);
#endif
            if (retValue == 0)
            {
                if (!m1.DeclaringType.Equals(m2.DeclaringType))
                {
                    string errorMessage = "ObjectReader cannot deserialize using this object as a container. Type " + m1.DeclaringType + " and type " + m2.DeclaringType +
                                            " both declare a member of the same type and name, " + m1.Name + ".";
                    throw new AmbiguousMatchException(errorMessage);
                }
                else return retValue;
            }
            else return retValue;
        }

        private static bool IsPrivateField(MemberInfo member)
        {
            if (member.MemberType == MemberTypes.Field)
            {
                FieldInfo field = (member as FieldInfo);

                if (field.IsPrivate && !IsExternalInstance(field)) return true;
                else return false;
            }
            else if (member.MemberType == MemberTypes.Property)
            {
                PropertyInfo property = (member as PropertyInfo);

                if (!property.CanWrite) return true;
                else if (property.GetIndexParameters().Length > 0) return true;
                else return false;
            }
            else return false;
        }
        #endregion
        #endregion
    }
}
