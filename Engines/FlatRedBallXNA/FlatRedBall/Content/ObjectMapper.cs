using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using FlatRedBall.Utilities;

namespace FlatRedBall.Content
{
    #region XML Docs
    /// <summary>
    /// Used to perform methods defined in a MappingMethods class on properties within a class.
    /// </summary>
    /// <remarks>
    /// The ObjectMapper can be used to perform tasks like code generation and serialization of objects.
    /// </remarks>
    #endregion
    public class ObjectMapper
    {
        #region Fields

        // This isn't being used, 
        // so removing it to clear
        // out a warning.
        //MappingMethods mMappingMethods;

        #endregion

        #region Methods

        #region XML Docs
        /// <summary>
        /// Pass an object and a MappingMethods and MapObject will map each member in that object to the corresponding
        /// method from the mappingMethods.
        /// </summary>
        /// <param name="objectToMap">object whose members will be called on.</param>
        /// <param name="mappingMethods">MappingMethods which has dictionaries of members/methods.</param>
        #endregion
        public void MapObject(object objectToMap, MappingMethods mappingMethods)
        {
            mappingMethods.Start();

            if (objectToMap == null)
            {
                throw new ArgumentException("The objectToMap is a null type");
            }
            else
            {
                MapObject(objectToMap, mappingMethods, objectToMap.GetType());
            }

            mappingMethods.End();
        }

        public void MapObject(Type typeOfObjectToMap, MappingMethods mappingMethods)
        {
            mappingMethods.Start();

            MapObject(null, mappingMethods, typeOfObjectToMap);

            mappingMethods.End();
        }

        public void MapObject(object objectToMap, MappingMethods mappingMethods, Type typeOfArgumentObject)
        {
            // get all your properties you want to map
            // put them in the right order
            List<MemberInfo> members = GetMembers(typeOfArgumentObject,
                mappingMethods.AcceptedMemberTypes, mappingMethods.MemberBitMask);

            #region Get rid of same type recursions; REVERSE FOR LOOP YAH!
            for (int i = members.Count - 1; i > -1; i--)
            {
                if (members[i].MemberType == MemberTypes.Field)
                {
                    if ((members[i] as FieldInfo).FieldType == typeOfArgumentObject)
                    {
                        members.Remove(members[i]);
                    }
                }
                else if (members[i].MemberType == MemberTypes.Property)
                {
                    if ((members[i] as PropertyInfo).PropertyType == typeOfArgumentObject)
                    {
                        members.Remove(members[i]);
                    }
                }
            }
            #endregion

            foreach (MemberInfo memberInfo in members)
            {
                object memberValue = null;
                //Type typeOfMember;

                #region Map Property
                if (memberInfo.MemberType == MemberTypes.Property)
                {
                    if (objectToMap != null)
                    {
                        memberValue = (memberInfo as PropertyInfo).GetValue(objectToMap, null);
                    }

                    MapMember(memberInfo, mappingMethods, memberValue);
                }
                #endregion

                #region Map Field
                else if (memberInfo.MemberType == MemberTypes.Field)
                {
                    if (objectToMap != null)
                    {
                        memberValue = (memberInfo as FieldInfo).GetValue(objectToMap);
                    }

                    MapMember(memberInfo, mappingMethods, memberValue);
                }
                #endregion

                else //Not a Property or Field, ERROR! (this should never happen)
                {
                    throw new Exception("MemberType not accepted, MapObject only accepts Fields and Properties.");
                }

            }

        }

        private static int SortMembersByName(MemberInfo m1, MemberInfo m2)
        {
#if XNA4
            int retValue = String.Compare(m1.Name, m2.Name, StringComparison.InvariantCultureIgnoreCase);
#else
            int retValue = String.Compare(m1.Name, m2.Name, false);
#endif
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

        //Primary sort is Size, secondary sort is Name.
        private static int SortMembersBySizeAndName(MemberInfo m1, MemberInfo m2)
        {
            #region Declare local variables
            int m1Size = 0;
            int m2Size = 0;
            bool m1IsGeneric = false;
            Type m1GenericType = null;
            bool m2IsGeneric = false;
            Type m2GenericType = null;
            #endregion


            #region SizeOf m1 (if Field)
            if (m1.MemberType == MemberTypes.Field)
            {
                GetSizeOfMember((m1 as FieldInfo), ref m1Size, ref m1GenericType, ref m1IsGeneric);
            }
            #endregion

            #region SizeOf m1 (if Property)
            else //PropertyInfo
            {
                GetSizeOfMember((m1 as PropertyInfo), ref m1Size, ref m1GenericType, ref m1IsGeneric);
            }
            #endregion

            #region SizeOf m2 (if Field)
            if (m2.MemberType == MemberTypes.Field)
            {
                GetSizeOfMember((m2 as FieldInfo), ref m2Size, ref m2GenericType, ref m2IsGeneric);
            }
            #endregion

            #region SizeOf m2 (if Property)
            else //PropertyInfo
            {
                GetSizeOfMember((m2 as PropertyInfo), ref m2Size, ref m2GenericType, ref m2IsGeneric);
            }
            #endregion

            if (m1Size > m2Size)
            {
                return -1;
            }
            else if (m1Size == m2Size)
            {
                #region Both generic types but not the same type.
                if (m1IsGeneric && m2IsGeneric && m1GenericType != m2GenericType)
                {
                    if (System.Runtime.InteropServices.Marshal.SizeOf(m1GenericType) < System.Runtime.InteropServices.Marshal.SizeOf(m2GenericType) ||
                        m1GenericType == typeof(bool) || m1GenericType == typeof(char)) //Because Marshal.SizeOf(Boolean) is 4 bytes instead of 1 (managed vs. unmanaged .NET)
                    {
                        return 1;
                    }
                    return -1;
                }
                #endregion
                else
                {
                    return SortMembersByName(m1, m2);
                }
            }
            else //m2 is bigger.
            {
                return 1;
            }
        }

        private static List<MemberInfo> GetMembers(Type valueType, List<MemberTypes> acceptedMemberTypes, BindingFlags bitMask)
        {

            List<MemberInfo> returnList = new List<MemberInfo>();
            returnList.AddRange(valueType.GetProperties(bitMask));
            returnList.AddRange(valueType.GetFields(bitMask));
            returnList.RemoveAll(x => acceptedMemberTypes.Contains(x.MemberType) == false);

            //Sort by size and then by name
            returnList.Sort(SortMembersBySizeAndName);
            return returnList;
        }


        private static void GetSizeOfMember(FieldInfo member, ref int size, ref Type genericType, ref bool isGeneric)
        {
            #region Reference type (automatically 4 bytes) since this is used for sorting and our biggest is 4
            if (member.FieldType.IsValueType == false)
            {
                //All reference types will be treated as 4bytes because that is our biggest.
                size = 4;
                return;
            }
            #endregion

            #region Generic type
            if (member.FieldType.IsGenericType)
            {
                Type[] genericTypes = member.FieldType.GetGenericArguments();
                genericType = genericTypes[0];
                size = sizeof(int);
                isGeneric = true;
            }
            #endregion

            #region Array
            else if (member.FieldType.IsArray)
            {
                if (member.FieldType.GetElementType() == typeof(bool) || member.FieldType.GetElementType() == typeof(char))
                {
                    size = 1;
                }
                else
                {
                    size = System.Runtime.InteropServices.Marshal.SizeOf(member.FieldType.GetElementType());
                }
            }
            #endregion

            #region Everything else (primitives)
            else
            {
                if (member.FieldType == typeof(bool) || member.FieldType == typeof(char))
                {
                    size = 1;
                }
                else if (member.FieldType.IsEnum)
                {
                    size = 4;
                }
                else
                {
                    size = System.Runtime.InteropServices.Marshal.SizeOf(member.FieldType);
                }
            }
            #endregion
        }

        private static void GetSizeOfMember(PropertyInfo member, ref int size, ref Type genericType, ref bool isGeneric)
        {
            #region Reference type (automatically 4 bytes) since this is used for sorting and our biggest is 4
            if (member.PropertyType.IsValueType == false)
            {
                //All reference types will be treated as 4bytes because that is our biggest.
                size = 4;
                return;
            }
            #endregion

            #region Generic type
            if (member.PropertyType.IsGenericType)
            {
                Type[] genericTypes = member.PropertyType.GetGenericArguments();
                genericType = genericTypes[0];
                size = sizeof(int);
                isGeneric = true;
            }
            #endregion

            #region Array
            else if (member.PropertyType.IsArray)
            {
                if (member.PropertyType.GetElementType() == typeof(bool) || member.PropertyType.GetElementType() == typeof(char))
                {
                    size = 1;
                }
                else
                {
                    size = System.Runtime.InteropServices.Marshal.SizeOf(member.PropertyType.GetElementType());
                }
            }
            #endregion

            #region Everything else (primitives)
            else
            {
                if (member.PropertyType == typeof(bool) || member.PropertyType == typeof(char))
                {
                    size = 1;
                }
                else if (member.PropertyType.IsEnum)
                {
                    size = 4;
                }
                else
                {
                    size = System.Runtime.InteropServices.Marshal.SizeOf(member.PropertyType);
                }
            }
            #endregion
        }

        private void MapMember(MemberInfo member, MappingMethods mappingMethods, object value)
        {
            Type typeOfMember;

            #region PropertyInfo
            if (member.MemberType == MemberTypes.Property)
            {
                typeOfMember = (member as PropertyInfo).PropertyType;
                if (mappingMethods.HasMethodForType(typeOfMember))
                {
                    MemberMapping methodToCall = mappingMethods.GetMappingForType(typeOfMember);

                    methodToCall(member as PropertyInfo, value);
                }
                else if (mappingMethods.BreakdownUnknownTypes == false)
                {
                    throw new ArgumentException("ObjectMapper found a type to map that it cannot handle," +
                        " fix this by adding a Mapping to your MappingMethods class");
                }
                else if (typeOfMember.IsPrimitive == false)
                {
                    if (value == null)
                    {
                        value = System.Activator.CreateInstance((member as PropertyInfo).PropertyType);
                    }
                    this.MapObject(value, mappingMethods, typeOfMember);
                }
            }
            #endregion

            #region FieldInfo
            else if (member.MemberType == MemberTypes.Field)
            {
                typeOfMember = (member as FieldInfo).FieldType;
                if (mappingMethods.HasMethodForType(typeOfMember))
                {
                    MemberMapping methodToCall = mappingMethods.GetMappingForType(typeOfMember);

                    methodToCall(member as FieldInfo, value);
                }
                else if (mappingMethods.BreakdownUnknownTypes == false)
                {
                    throw new ArgumentException("ObjectMapper found a type to map that it cannot handle," +
                        " fix this by adding a Mapping to your MappingMethods class");
                }
                else if (typeOfMember.IsPrimitive == false)
                {
                    if (value == null)
                    {
                        value = System.Activator.CreateInstance((member as FieldInfo).FieldType);
                    }
                    this.MapObject(value, mappingMethods, typeOfMember);
                }
            }
            #endregion

            else //Not a Property or Field, ERROR! (this should never happen)
            {
                throw new Exception("MemberType not accepted, MapObject only accepts Fields and Properties.");
            }

        }

        #endregion
    }
}
