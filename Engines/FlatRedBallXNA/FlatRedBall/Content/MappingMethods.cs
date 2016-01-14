using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Content
{
    #region XML Docs
    /// <summary>
    /// Delegate representing the method that is to be called for a property to be mapped.
    /// This can be applied by specific type or by generic type.
    /// </summary>
    /// <param name="objectInfo">The member info of the object being mapped.</param>
    /// <param name="objectValue">The instance being mapped.</param>
    #endregion
    public delegate void MemberMapping(MemberInfo objectInfo, object objectValue);

    public abstract class MappingMethods
    {
        #region Fields

        protected Dictionary<Type, MemberMapping> mMappings = new Dictionary<Type, MemberMapping>();

        protected Dictionary<Type, MemberMapping> mMappingsForGenerics = new Dictionary<Type, MemberMapping>();

        protected List<Type> mTypesToIgnore = new List<Type>();

        //Child classes must fill this or else there will be no members.
        protected List<MemberTypes> mAcceptedMemberTypes = new List<MemberTypes>();

        protected BindingFlags mMemberBitMask = BindingFlags.Public |
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        protected int mBytesUsed = 0;

        //If there is a base type, get it's members and append this type's sorted members onto the base type's sorted members.
        protected bool mAppendOntoBase = false;

        #endregion

        #region Properties

        public BindingFlags MemberBitMask
        {
            get { return mMemberBitMask; }
        }

        public List<MemberTypes> AcceptedMemberTypes
        {
            get { return mAcceptedMemberTypes; }
        }

        public bool BreakdownUnknownTypes
        {
            get;
            set;
        }

        public bool AppendOntoBase
        {
            get { return mAppendOntoBase; }
        }

        #endregion

        #region Methods

        public MappingMethods()
        {
            BreakdownUnknownTypes = false;
        }

        public bool IgnoreType(Type typeOfObject)
        {
            return mTypesToIgnore.Contains(typeOfObject);
        }

        public bool HasMethodForType(Type typeOfObject)
        {
            if (typeOfObject.IsEnum)
            {
                typeOfObject = typeof(Enum);
            }

            if(mMappings.ContainsKey(typeOfObject))
            {
                return true;
            }
            else if(typeOfObject.IsGenericType &&
                mMappingsForGenerics.ContainsKey(typeOfObject.GetGenericTypeDefinition()))
            {
                return true;
            }
            else if (BreakdownUnknownTypes) //If we breakdown types, we must have a breakdown method.
            {
                return true;
            }
            
            return false;
        }

        public MemberMapping GetMappingForType(Type typeOfObject)
        {
            if (typeOfObject.IsEnum)
            {
                typeOfObject = typeof(Enum);
            }

            if(mMappings.ContainsKey(typeOfObject))
            {
                return mMappings[typeOfObject];
            }
            else if (typeOfObject.IsGenericType &&
                mMappingsForGenerics.ContainsKey(typeOfObject.GetGenericTypeDefinition()))
            {
                return mMappingsForGenerics[typeOfObject.GetGenericTypeDefinition()];
            }
            else if (BreakdownUnknownTypes)
            {
                if (typeOfObject.GetInterface("ICollection", true) != null)
                {
                    return BreakdownUnknownTypeList;
                }

                return BreakdownUnknownType;
            }
            else
            {
                throw new Exception("Mapping for object was not provided. You should use HasMethodForType to check before calling this method.");
            }
        }

        public virtual void BreakdownUnknownType(MemberInfo objectInfo, object objectValue)
        {
            throw new Exception("If this method is being called, it should have been overriden in it's derived class");
        }

        public virtual void BreakdownUnknownTypeList(MemberInfo objectInfo, object objectValue)
        {
            throw new Exception("If this method is being called, it should have been overriden in it's derived class");
        }

        public virtual void Start() { }

        public virtual void End() { }

        protected int GetSizeOfListType(PropertyInfo objectInfo)
        {
            Type type;
            if (objectInfo.PropertyType.IsArray)
            {
                type = ((PropertyInfo)objectInfo).PropertyType.GetElementType();
            }
            else if (objectInfo.PropertyType.IsGenericType)
            {
                Type[] genericTypes = ((PropertyInfo)objectInfo).PropertyType.GetGenericArguments();
                type = genericTypes[0];
            }
            else //everything else (such as primitives)
            {
                type = ((PropertyInfo)objectInfo).PropertyType;
            }

            if (type == typeof(bool) || type == typeof(char))
            {
                return 1;
            }
            return System.Runtime.InteropServices.Marshal.SizeOf(type);
        }

        protected int GetSizeOfType(Type type)
        {
            if (type == typeof(bool) || type == typeof(char))
            {
                return 1;
            }
            else if (type.IsValueType == false || type.IsEnum)
            {
                //Reference types and enums will be 4bytes.
                return 4;
            }
            return System.Runtime.InteropServices.Marshal.SizeOf(type);
        }

        protected static bool IsStruct(Type t)
        {
            return t.IsValueType && t.IsPrimitive == false && t.IsEnum == false;
        }

        protected void FormatStringReplacement(StringBuilder outputText)
        {
            outputText.Replace("System.Single", "float");
            //Do this as to not change "Single" in names of members
            outputText.Replace(" Single", " float");
            //For fields that are on a new line following a generated \t
            outputText.Replace("\tSingle", "\tfloat");
            //For casts (__*)
            outputText.Replace("(Single", "(float");

            outputText.Replace("System.Boolean", "bool");
            //Do this as to not change "Boolean" in names of members
            outputText.Replace(" Boolean", " bool");
            outputText.Replace("\tBoolean", "\tbool");
            outputText.Replace("(Boolean", "(bool");

            outputText.Replace("System.Int32", "int");
            outputText.Replace("Int32", "int");

            outputText.Replace("System.Int16", "short");
            outputText.Replace("Int16", "short");

            outputText.Replace("System.String", "string");
            //Do this as to not change "String" in names of members
            outputText.Replace(" String", " string");
            outputText.Replace("\tString", "\tstring");
            outputText.Replace("(String", "(string");

            outputText.Replace("System.Double", "double");
            //Do this as to not change "Double" in names of members
            outputText.Replace(" Double", " double");
            outputText.Replace("\tDouble", "\tdouble");
            outputText.Replace("(Double", "(double");

            outputText.Replace("System.Char", "char");
            //Do this as to not change "Char" in names of members
            outputText.Replace(" Char", " char");
            outputText.Replace("\tChar", "\tchar");
            outputText.Replace("(Char", "(char");

            outputText.Replace("Microsoft.Xna.Framework.", "");
        }

        #endregion

    }
}
