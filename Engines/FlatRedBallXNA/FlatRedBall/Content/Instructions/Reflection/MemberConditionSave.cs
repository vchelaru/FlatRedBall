using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Instructions.Reflection;

namespace FlatRedBall.Content.Instructions.Reflection
{
    #region XML Docs
    /// <summary>
    /// A savable class containing the information for a MemberCondition.
    /// </summary>
    /// <typeparam name="PropertyType">The type of the property to be compared.</typeparam>
    #endregion
    public class MemberConditionSave<PropertyType>
        where PropertyType : IComparable, IEquatable<PropertyType>
    {
        public string Member;

        public Operator Operator;

        public PropertyType Value;

        public static MemberConditionSave<PropertyType> FromMemberCondition<ObjectType>(MemberCondition<ObjectType, PropertyType> memberCondition)
            where ObjectType : IComparable, IEquatable<PropertyType>
        {
            MemberConditionSave<PropertyType> memberConditionSave = new MemberConditionSave<PropertyType>();

            memberConditionSave.Member = memberCondition.Member;

            memberConditionSave.Operator = memberCondition.Operator;

            memberConditionSave.Value = memberCondition.Value;

            return memberConditionSave;
        }

        public MemberCondition<ObjectType, PropertyType> ToMemberCondition<ObjectType>()
        {
            MemberCondition<ObjectType, PropertyType> newInstance = 
                new MemberCondition<ObjectType, PropertyType>(
                    default(ObjectType), Member, Value, this.Operator);

            return newInstance;
        }

    }
}
