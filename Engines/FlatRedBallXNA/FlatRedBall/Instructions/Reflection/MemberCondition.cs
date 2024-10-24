using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace FlatRedBall.Instructions.Reflection
{
    #region Enums

    #region XML Docs
    /// <summary>
    /// Enumeration used to define relationships between two values.  Used in MemberCondition.
    /// </summary>
    #endregion
    public enum Operator
    {
        EqualTo,
        GreaterThan,
        LessThan
    }
    #endregion

    #region Base abstract class MemberCondition 
    #region XML Docs
    /// <summary>
    /// Base class for the generic MemberCondition class provided for List storage.
    /// </summary>
    #endregion
    public abstract class MemberCondition
    {
        public abstract string Member
        {
            get;
            set;
        }

        public abstract bool Result
        {
            get;
        }

        public Operator Operator
        {
            get;
            set;
        }

        internal abstract object SelectedObjectAsObject
        {
            get;
            set;
        }
    }

    #endregion

    #region XML Docs
    /// <summary>
    /// Class which can be used to query the relationship of a member relative to a value.  This
    /// can be used in scripting and trigger data.
    /// </summary>
    /// <typeparam name="ObjectType">The type of the object containing the property to compare.</typeparam>
    /// <typeparam name="PropertyType">The type of the property to compare.</typeparam>
    #endregion
    public class MemberCondition<ObjectType, PropertyType> : MemberCondition
        where PropertyType : IComparable
    {
        #region Fields

        PropertyInfo mPropertyInfo;
        FieldInfo mFieldInfo;

        string mMember;

        #endregion

        #region Properties

        public override string Member
        {
            get
            {
                return mMember;
            }
            set
            {
                mMember = value;

                Type type = typeof(ObjectType);
                

                mPropertyInfo = type.GetProperty(mMember);
                mFieldInfo = type.GetField(mMember);
            }
        }

        public override bool Result
        {
            get
            {
                PropertyType currentValue = default(PropertyType);

                if (mPropertyInfo != null)
                {
                    currentValue = LateBinder<ObjectType>.Instance.GetProperty<PropertyType>(
                        SelectedObject, mMember);
                }
                else
                {
                    currentValue = LateBinder<ObjectType>.Instance.GetField<PropertyType>(
                        SelectedObject, mMember);
                }
                
                
                switch (this.Operator)
                {
                    case Operator.EqualTo:

                        if (Value is IComparable<PropertyType>)
                        {
                            return ((IComparable<PropertyType>)Value).CompareTo(currentValue) == 0;

                        }
                        else
                        {
                            return Value.Equals(currentValue);
                        }



                        //break;

                    case Operator.GreaterThan:

                        return Value.CompareTo(currentValue) < 0;

                        //break;

                    case Operator.LessThan:

                        return Value.CompareTo(currentValue) > 0;

                        //break;
                }

                return false;
            }
        }

        public ObjectType SelectedObject
        {
            get;
            set;
        }

        internal override object SelectedObjectAsObject
        {
            get
            {
                return SelectedObject;
            }
            set
            {
                if (value is ObjectType)
                {
                    SelectedObject = (ObjectType)value;
                }
                else
                {
                    throw new InvalidOperationException("The assigned object is not of type " + 
                        typeof(ObjectType).Name);
                }
            }
        }

        public PropertyType Value
        {
            get;
            set;
        }

        #endregion

        #region Methods

        public MemberCondition(ObjectType selectedObject,
            string member, PropertyType value, Operator operatorToUse)
        {
            SelectedObject = selectedObject;
            Member = member;
            Value = value;
            Operator = operatorToUse;
        }

        #endregion
    }
}
