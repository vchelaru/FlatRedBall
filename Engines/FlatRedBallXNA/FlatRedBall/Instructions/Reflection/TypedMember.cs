using System;
using System.Collections.Generic;
using System.Text;

using System.Reflection;

namespace FlatRedBall.Instructions.Reflection
{
    public enum Modifier
    {
        Public,
        Private,
        Internal
    }

    #region XML Docs
    /// <summary>
    /// Base class for the generic TypedMember class.
    /// </summary>
    #endregion
    public abstract class TypedMemberBase
    {
        public string MemberName;

        public Modifier Modifier
        {
            get;
            set;
        }

        /// <summary>
        /// This exists so we can create typed members for types that may not be available in the current assembly.
        /// It enables Glue to generate code for types it doesn't understand.
        /// </summary>
        public string CustomTypeName
        {
            get;
            set;
        }

		public abstract Type MemberType
		{
			get;
		}

        public abstract bool IsMemberValueEqual<U>(U object1, U object2);

        public TypedMemberBase()
        {
            Modifier = Reflection.Modifier.Public;
        }

		public static TypedMemberBase GetTypedMember(string memberName, Type memberType)
		{
			TypedMemberBase typedMember = null;

            Type typedMemberType = null;
            
            try
            {
                typedMemberType = typeof(TypedMember<>).MakeGenericType(memberType);
            }
            catch(ArgumentException)
            {
                // Maybe it's not an equatable, so try unequatable:
                typedMemberType = typeof(TypedMemberUnequatable<>).MakeGenericType(memberType);
            }

			typedMember = Activator.CreateInstance(typedMemberType, memberName) as TypedMemberBase;

			return typedMember;
		}

        public static TypedMemberBase GetTypedMemberUnequatable(string memberName, Type memberType)
		{
			TypedMemberBase typedMember = null;

            Type typedMemberType = null;

            if (memberType == null)
            {
                typedMemberType = typeof(TypedMemberUnequatable<>).MakeGenericType(typeof(object)); 

            }
            else
            {
                typedMemberType = typeof(TypedMemberUnequatable<>).MakeGenericType(memberType);
            }


			typedMember = Activator.CreateInstance(typedMemberType, memberName) as TypedMemberBase;

			return typedMember;
		}

        public override string ToString()
        {
            return MemberType + " " + MemberName;
        }
    }

    public class TypedMemberUnequatable<T> : TypedMemberBase
    {
        protected Type mType;

		public override Type MemberType
		{
			get { return typeof(T); }
		}

        public TypedMemberUnequatable(string propertyName)
        {
            MemberName = propertyName;
            mType = typeof(T);
        }

        public override bool IsMemberValueEqual<U>(U object1, U object2)
        {
            return false;

        }


    }

    #region XML Docs
    /// <summary>
    /// Class containing information about a member which can tell if two instances have
    /// identical members.
    /// </summary>
    /// <typeparam name="T">The type of the member.</typeparam>
    #endregion
    public class TypedMember<T> : TypedMemberUnequatable<T> where T : IEquatable<T>
    {

        public TypedMember(string propertyName)
            : base(propertyName)
        {

        }

        public override bool IsMemberValueEqual<U>(U object1, U object2)
        {
            Type type = typeof(U);
            PropertyInfo propertyInfo = type.GetProperty(MemberName);

            if (propertyInfo != null)
            {
                T value1 = (T)(propertyInfo.GetValue(object1, null));
                T value2 = (T)(propertyInfo.GetValue(object2, null));

                if (value1 != null)
                    return value1.Equals(value2);
                else
                    return (value2 == null);
           }
            else
            {
                FieldInfo fieldInfo = type.GetField(MemberName);

                if (fieldInfo == null)
                {
                    throw new MemberAccessException("Cannot find member by the name of " + MemberName + " in the UndoManager");
                }

                T value1 = ((T)fieldInfo.GetValue(object1));
                T value2 = ((T)fieldInfo.GetValue(object2));

                if (value1 != null)
                    return value1.Equals(value2);
                else
                    return (value2 == null);
            }
        }
    }
}
