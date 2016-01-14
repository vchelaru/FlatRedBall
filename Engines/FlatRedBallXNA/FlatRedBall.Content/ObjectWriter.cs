using System;
using System.Reflection;
using FlatRedBall.Instructions.Reflection;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using FlatRedBall.Attributes;
using FlatRedBall.Content.SpriteGrid;
using System.Diagnostics;
using FlatRedBall.Content.Scene;
using System.Windows.Forms;
using FlatRedBall.Content.AnimationChain;

namespace FlatRedBall.Content
{
    public static class ObjectWriter
    {

        #region Methods

        public static void WriteObject<T>(ContentWriter output, T value)
        {          
            Type valueType = typeof(T);

            if (!ObjectReader.UseReflection && valueType.Name == "SpriteSaveContent")
            {
                SpriteSaveWriter.WriteUsingGeneratedCode(output, (SpriteSaveContent)((object)value));
            }
            else if (!ObjectReader.UseReflection && valueType.Name == "TextSaveContent")
            {
                TextSaveWriter.WriteTextObject(output, (TextSaveContent)((object)value));
            }
            else if (!ObjectReader.UseReflection && valueType.Name == "AnimationChainListSaveContent")
            {
                AnimationChainArrayWriter.WriteAnimationChainListSave(output, (AnimationChainListSaveContent)((object)value));
            }
            else if (!ObjectReader.UseReflection && valueType.Name == "AnimationChainSaveContent")
            {
                AnimationChainArrayWriter.WriteAnimationChainSave(output, (AnimationChainSaveContent)((object)value));
            }
            else if (!ObjectReader.UseReflection && valueType.Name == "AnimationFrameSaveContent")
            {
                AnimationChainArrayWriter.WriteAnimationFrameSave(output, (AnimationFrameSaveContent)((object)value));
            }
            else
            {
                #region Declare initial values


                Type[] valueInterfaces = valueType.GetInterfaces();
                object fieldValue;
                Type fieldType;
                #endregion



                //If T is primitive
                #region Write Primitive
                if ((valueType.IsPrimitive) || valueType == typeof(string) || valueType == typeof(decimal))
                {
                    output.WriteObject<T>(value);
                }

                else if (valueType.BaseType == typeof(System.Enum))
                {
                    output.Write(System.Convert.ToInt32(value));
                }
                #endregion

                #region If this is an Enumeration(or collection)
                else if (valueType.GetInterface("IEnumerable") != null)
                {
                    //if the value is a type that implements IList, such as Array
                    if (valueType.GetInterface("IList") != null)
                    {



                        //write the count
                        int count = (value as System.Collections.ICollection).Count;

                        output.Write(count);

                        //cast it to an IList
                        System.Collections.IList list = (value as System.Collections.IList);

                        #region Create isNullArray
                        bool[] isNullArray = new bool[count];
                        int i = 0;

                        for (; i < count; ++i)
                        {
                            isNullArray[i] = ((value as System.Collections.IList)[i] == null);
                        }
                        #endregion

                        WriteBoolArray(output, isNullArray);
                        i = 0;

                        //Recursively call WriteObject for each element
                        foreach (object o in list)
                        {
                            if (value.GetType().IsArray)
                            {
                                if (!isNullArray[i++])
                                {

                                    if (IsExternalReference(valueType.GetElementType()))
                                    {

                                        if (valueType.GetElementType().IsArray)
                                        {
                                            MethodInfo multiListRecur = typeof(ObjectWriter).GetMethod("WriteObject").MakeGenericMethod(new Type[1] { valueType.GetElementType() });

                                            multiListRecur.Invoke(null, new object[2] { output, o });

                                        }
                                        else
                                        {
                                            WriteExternalReference(valueType.GetElementType(), o, output);

                                        }
                                    }
                                    else
                                    {
                                        MethodInfo listRecur = typeof(ObjectWriter).GetMethod("WriteObject").MakeGenericMethod(new Type[1] { valueType.GetElementType() });
                                        listRecur.Invoke(null, new object[2] { output, o });
                                    }
                                }
                            }
                            else
                            {
                                if (!isNullArray[i++])
                                {

                                    if (IsExternalReference(valueType.GetGenericArguments()[0]))
                                    {
                                        WriteExternalReference(valueType.GetGenericArguments()[0], o, output);
                                    }
                                    else
                                    {

                                        MethodInfo listRecur = typeof(ObjectWriter).GetMethod("WriteObject").MakeGenericMethod(new Type[1] { valueType.GetGenericArguments()[0] });
                                        listRecur.Invoke(null, new object[2] { output, o });
                                    }
                                }
                            }
                        }

                    }

                    //If value implements IDictionary
                    else if (ArrayContains<Type>(valueInterfaces, ImplementsInterface<System.Collections.IDictionary>))
                    {
                        //Write the count and cast it.
                        int count = (value as System.Collections.ICollection).Count;
                        output.Write(count);
                        System.Collections.IDictionary dict = (value as System.Collections.IDictionary);

                        bool[] isNullArray = new bool[count];
                        int i = 0;

                        foreach (object o in dict.Keys)
                        {
                            isNullArray[i++] = (dict[o] == null);
                        }

                        WriteBoolArray(output, isNullArray);
                        i = 0;


                        //Recursively call WriteObject for each key and value
                        foreach (object o in dict.Keys)
                        {
                            MethodInfo dictRecur = typeof(ObjectWriter).GetMethod("WriteObject").MakeGenericMethod(new Type[1] { o.GetType() });
                            dictRecur.Invoke(null, new object[2] { output, o });

                            if (!isNullArray[i++])
                            {
                                MethodInfo dictRecur2 = typeof(ObjectWriter).GetMethod("WriteObject").MakeGenericMethod(new Type[1] { dict[o].GetType() });
                                dictRecur2.Invoke(null, new object[2] { output, o });
                            }
                        }
                    }

                    //If value implements ICollection (Stack, Queue)
                    else if (ArrayContains<Type>(valueInterfaces, ImplementsInterface<System.Collections.ICollection>))
                    {
                        int count = (value as System.Collections.ICollection).Count;

                        //write count and cast
                        output.Write(count);
                        System.Collections.ICollection coll = (value as System.Collections.ICollection);

                        bool[] isNullArray = new bool[count];
                        int i = 0;

                        foreach (object o in coll)
                        {
                            isNullArray[i++] = (o == null);
                        }

                        WriteBoolArray(output, isNullArray);
                        i = 0;


                        //recursively write each object
                        foreach (object o in coll)
                        {
                            if (!isNullArray[i++])
                            {
                                MethodInfo collRecur = typeof(ObjectWriter).GetMethod("WriteObject").MakeGenericMethod(new Type[1] { o.GetType() });
                                collRecur.Invoke(null, new object[2] { output, o });
                            }
                        }

                    }

                }
                #endregion

                //Otherwise, if value is a non-Collection, non-Primitive class
                else
                {

                    List<MemberInfo> fields = GetMembersToWrite(valueType);
                    /*

                    if (typeof(T) == typeof(FlatRedBall.Content.Model.PositionedModelSave) ||
                        typeof(T) == typeof(FlatRedBall.Content.Model.PositionedModelSaveContent))
                    {
                        string str = "";
                        foreach (MemberInfo field in fields)
                        {
                          str += field.Name + "\n";
                        }

                        throw new Exception(str);
                    }
                    */

                    int i = 0;

                    bool[] isNullArray = new bool[fields.Count];


                    //locate null fields
                    foreach (MemberInfo member in fields)
                    {
                        if (member.MemberType == MemberTypes.Field)
                        {
                            FieldInfo field = (member as FieldInfo);
                            if (!field.IsStatic)
                            {
                                isNullArray[i] = (field.GetValue(value) == null);
                                ++i;
                            }
                        }
                        else
                        {
                            PropertyInfo prop = (member as PropertyInfo);
                            if (prop.CanWrite)
                            {

                                isNullArray[i] = (prop.GetValue(value, null) == null);
                                ++i;
                            }
                        }
                    }

                    //If isNullArray wasn't filled completely
                    if (i < isNullArray.Length)
                    {
                        bool[] temp = new bool[i];
                        Array.Copy(isNullArray, 0, temp, 0, i);
                        isNullArray = temp;
                    }


                    //Write isNullArray to the file
                    WriteBoolArray(output, isNullArray);

                    //reset counter
                    i = 0;

                    //Iterate through its fields
                    foreach (MemberInfo member in fields)
                    {
                        //  if (member.Name.Equals("GridTextureReferences"))
                        //{
                        //   throw new Exception(member.Name + " " + isNullArray[i]);
                        //   }
                        #region If the member is a Property
                        if (member.MemberType == MemberTypes.Property)
                        {
                            PropertyInfo property = (member as PropertyInfo);


                            //check if it's tagged with XmlIgnore
                            // if (IsIgnored(property) && !IsExternalReference(property.PropertyType)) continue;

                            fieldValue = property.GetValue(value, null);
                            fieldType = property.PropertyType;
                            //Ignore static fields
                            if ((property.CanWrite) && (!isNullArray[i++]))
                            {
                                //Take care of ExternalReferences
                                if (IsExternalReference(property.PropertyType) && (property.PropertyType.GetInterface("IEnumerable") == null))
                                {
                                    MethodInfo writeRef = typeof(ContentWriter).GetMethod("WriteExternalReference").MakeGenericMethod(new Type[1] { fieldType.GetGenericArguments()[0] });
                                    writeRef.Invoke(output, new object[1] { fieldValue });
                                    continue;
                                }

                                //Recursively call WriteObject with each field's value.
                                MethodInfo recur = typeof(ObjectWriter).GetMethod("WriteObject").MakeGenericMethod(new Type[1] { fieldType });
                                object[] args = new object[2] { output, fieldValue };
                                recur.Invoke(null, args);
                            }
                        }
                        #endregion

                        #region else, it's a Field
                        else
                        {
                            FieldInfo field = (member as FieldInfo);


                            if (typeof(T) == typeof(FlatRedBall.Content.Instructions.InstructionSave) && field.Name.Equals("Value"))
                            {
                                fieldType = System.Type.GetType((value as FlatRedBall.Content.Instructions.InstructionSave).Type);
                            }
                            else
                                fieldType = field.FieldType;

                            fieldValue = field.GetValue(value);

                            //Ignore static fields
                            if ((!field.IsStatic) && (!isNullArray[i++]))
                            {
                                //Take care of ExternalReferences
                                if (IsExternalReference(field.FieldType) && (field.FieldType.GetInterface("IEnumerable") == null))
                                {
                                    WriteExternalReference(fieldType, fieldValue, output);
                                    continue;
                                }

                                //Recursively call WriteObject with each field's value.
                                MethodInfo recur = typeof(ObjectWriter).GetMethod("WriteObject").MakeGenericMethod(new Type[1] { fieldType });
                                object[] args = new object[2] { output, fieldValue };
                                recur.Invoke(null, args);
                            }
                        }

                        #endregion
                    }
                }
            }
        }



        private static bool ImplementsInterface<TType>(Type t1)
        {
            if (t1 == typeof(TType))
            {
                return true;
            }
            else
                return false;
        }

        private static bool ArrayContains<K>(K[] inputArray, Predicate<K> pred)
        {
            bool returnValue = false;
            foreach (K value in inputArray)
            {
                if (pred.Invoke(value))
                {
                    returnValue = true;
                    break;
                }
            }
            return returnValue;

        }

        private static void WriteBoolArray(ContentWriter output, bool[] arrayToWrite)
        {
            output.Write(arrayToWrite.Length);
            foreach (bool b in arrayToWrite)
            {
                output.Write(b);
            }
        }

        private static InstanceMember GetInstanceMember(FieldInfo field)
        {
            object[] attributes = field.GetCustomAttributes(true);
            InstanceMember instance = null;
            InstanceListMember instanceList = null;
            foreach (object attr in attributes)
            {
                instance = (attr as InstanceMember);
                instanceList = (attr as InstanceListMember);
                if (instance != null || instanceList != null) break;
            }
            if (instance == null && instanceList == null) throw new Exception("Field " + field + " is an ExternalReference, but does not contain a valid FlatRedBall.Attributes.InstanceMember Attribute.");
            else if (instanceList == null) return instance;
            else return new InstanceMember(instanceList.memberName);
        }

        private static bool IsExternalReference(Type fieldType)
        {

            bool value = false;
            if (fieldType.IsGenericType)
            {
                if (fieldType.GetGenericTypeDefinition() == typeof(Microsoft.Xna.Framework.Content.Pipeline.ExternalReference<Texture2D>).GetGenericTypeDefinition())
                {
                    value = true;
                }
            }
            else if (fieldType.GetInterface("IEnumerable") != null)
            {
                if (fieldType.ToString().Contains("ExternalReference`1[")) value = true;
            }
            return value;
        }

        private static int SortMembersByName(MemberInfo m1, MemberInfo m2)
        {
            int retValue = String.Compare(m1.Name, m2.Name, false);
            if (retValue == 0)
            {
                if (!m1.DeclaringType.Equals(m2.DeclaringType))
                {
                    string errorMessage = "ObjectWriter cannot serialize this object. Type " + m1.DeclaringType + " and type " + m2.DeclaringType +
                                            " both declare a member of the same type and name, " + m1.Name + ".";
                    throw new AmbiguousMatchException(errorMessage);
                }
                else return retValue;
            }
            else return retValue;
        }

        private static int SortPropertiesByType(PropertyInfo p1, PropertyInfo p2)
        {
            if (p1.PropertyType == p2.PropertyType)
            {
                if (IsExternalReference(p1.PropertyType)) throw new Exception("Objects serialized through the ObjectWriter may not have properties of type ExternalReference<>, only fields tagged with the InstanceMember attribute.");
                else return SortMembersByName((p1 as MemberInfo), (p2 as MemberInfo));
            }
            else
            {
                if (IsExternalReference(p1.PropertyType))
                {
                    throw new Exception("Objects serialized through the ObjectWriter may not have properties of type ExternalReference<>, only fields tagged with the InstanceMember attribute.");
                }
                else if (IsExternalReference(p2.PropertyType))
                {
                    throw new Exception("Objects serialized through the ObjectWriter may not have properties of type ExternalReference<>, only fields tagged with the InstanceMember attribute.");
                }
                else
                {
                    return SortMembersByName((p1 as MemberInfo), (p2 as MemberInfo));
                  //  return String.Compare(p1.PropertyType.FullName, p2.PropertyType.FullName, false);
                }
            }
        }

        private static int SortFieldsByType(FieldInfo f1, FieldInfo f2)
        {
            if (f1.FieldType == f2.FieldType)
            {
                if (IsExternalReference(f1.FieldType)) return String.Compare(GetInstanceMember(f1).memberName, GetInstanceMember(f2).memberName, false);
                return SortMembersByName((f1 as MemberInfo), (f2 as MemberInfo));
            }
            else
            {
                if (IsExternalReference(f1.FieldType))
                {
                    if (IsExternalReference(f2.FieldType))
                    {
                        return String.Compare(GetInstanceMember(f1).memberName, GetInstanceMember(f2).memberName, false);
                    }
                    else
                    {
                        return 1;
                    }
                }
                else if (IsExternalReference(f2.FieldType))
                {
                    return -1;
                }
                else
                {
                    return SortMembersByName((f1 as MemberInfo), (f2 as MemberInfo));
                    /*int result = String.Compare(f1.FieldType.FullName, f2.FieldType.FullName, false);

                    if (result < 0)
                        return -1;
                    else
                        return 1;*/
                }
            }
        }




        private static int SortMembersForWriting(MemberInfo m1, MemberInfo m2)
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

        private static bool IsPrivateField(MemberInfo member)
        {
            if (member.MemberType == MemberTypes.Field)
            {
                FieldInfo field = (member as FieldInfo);

                if (field.IsPrivate && !IsExternalReference(field.FieldType)) return true;
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

        private static List<MemberInfo> GetMembersToWrite(Type valueType)
        {
            List<MemberInfo> returnList = new List<MemberInfo>();
            returnList.AddRange(valueType.GetProperties(BindingFlags.Public | BindingFlags.Instance));
            returnList.AddRange(valueType.GetFields(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic));
            returnList.AddRange(valueType.GetFields(BindingFlags.Instance | BindingFlags.Public));

            returnList.Sort(SortMembersForWriting);
            returnList.RemoveAll(IsPrivateField);
            return returnList;
        }

        private static void WriteExternalReference(Type fieldType, object value, ContentWriter output)
        {

            MethodInfo writeRef = typeof(ContentWriter).GetMethod("WriteExternalReference").MakeGenericMethod(new Type[1] { fieldType.GetGenericArguments()[0] });
                writeRef.Invoke(output, new object[1] { value });
        }

        #endregion

    }
}
