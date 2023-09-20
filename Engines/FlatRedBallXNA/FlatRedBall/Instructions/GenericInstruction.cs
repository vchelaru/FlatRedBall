using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Instructions.Interpolation;

namespace FlatRedBall.Instructions
{
    #region GenericInstruction abstract class

    #region XML Docs
    /// <summary>
    /// Base class for typed Instructions.  This type can be used
    /// to identify if an Instruction is a generic Instruction.
    /// </summary>
    #endregion
    public abstract class GenericInstruction : Instruction
    {

        /// <summary>
        /// Returns the FullName of the Type that this GenericInstruction operates on.
        /// </summary>
        public abstract string TypeAsString
        {
            get;
        }

        /// <summary>
        /// The Name of the member (such as "X" or "Width") that this instruction modifies.
        /// </summary>
        public abstract string Member
        {
            get;
            internal set;
        }

        /// <summary>
        /// 
        /// </summary>
        public abstract Type MemberType
        {
            get;
        }

        /// <summary>
        /// The FullName of the type of the Member that is being modified.  For example, this would return "System.String" for a float member like X.
        /// </summary>
        public abstract string MemberTypeAsString
        {
            get;
        }

        /// <summary>
        /// Returns a ToString() representation of the member.
        /// </summary>
        public abstract string MemberValueAsString
        {
            get;
        }

        /// <summary>
        /// Returns the value of the member casted as an object.
        /// </summary>
		public abstract object MemberValueAsObject
		{
			get;
			set;
		}

        /// <summary>
        /// Sets the object which this instruction operates on.
        /// </summary>
        /// <param name="target">The object that this instruction operates on.</param>
        internal abstract void SetTarget(object target);


        public abstract void InterpolateBetweenAndExecute(GenericInstruction otherInstruction, float thisRatio);

    }
    #endregion

    /// <summary>
    /// Generic method of setting a particular variable at a given time.
    /// </summary>
    /// <typeparam name="TargetType">The type of object to operate on (ex. PositionedObject)</typeparam>
    /// <typeparam name="ValueType">The type of the value.  For example, the X value in PositionedObject is float.</typeparam>

    public class Instruction<TargetType, ValueType> : GenericInstruction// where T:class
    {
        #region Fields
        private TargetType mTarget;
        private ValueType mValue;
        private string mMember;

        #endregion

        #region Properties

        public override object Target
        {
            get { return mTarget; }
            set { mTarget = (TargetType)value; }
        }

        public override string TypeAsString
        {
            get
            {
                return typeof(TargetType).FullName;
            }
        }

        public override string Member
        {
            get
            {
                return mMember;
            }
            internal set
            {
                mMember = value;
            }
        }

        public override Type MemberType => typeof(ValueType);

        public override string MemberTypeAsString => typeof(ValueType).FullName;

        public override string MemberValueAsString
        {
            get
            {
                if (mValue != null)
                {
                    if (mValue is bool)
                        return mValue.ToString().ToLowerInvariant();
                    else
                        return mValue.ToString();
                }
                else
                    return "null";
            }
        }

        public override object MemberValueAsObject
        {
            get => mValue;
            set { mValue = ((ValueType) value) ; }
        }

        public ValueType Value
        {
            get { return mValue; }
            set { mValue = value; }
        }

        #endregion

        #region Methods

        #region Constructors

        #region XML Docs
        /// <summary>
        /// Used when deserializing .istx files.  Not to be called otherwise.
        /// </summary>
        #endregion
        public Instruction()
        {

        }

        /// <summary>
        /// To be used when inheriting from this class since you won't need the property's name
        /// </summary>
        /// <param name="targetObject">The object to operate on (ex. a PositionedObject)</param>
        /// <param name="value">The value to set to the property when the instruction is executed</param>
        /// <param name="timeToExecute">Absolute time to executing this instruction</param>
        protected Instruction(TargetType targetObject, ValueType value, double timeToExecute)
            : this(targetObject, null, value, timeToExecute)
        { }

        /// <param name="targetObject">The object to operate on (ex. a PositionedObject)</param>
        /// <param name="member">The name of the property to set</param>
        /// <param name="value">The value to set to the property when the instruction is executed</param>
        /// <param name="timeToExecute">Absolute time to executing this instruction</param>
        public Instruction(TargetType targetObject, string member, ValueType value, double timeToExecute)
        {
            mTarget = targetObject;
            mMember = member;
            mValue = value;
            mTimeToExecute = timeToExecute;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Uses reflection to set the target object's property.
        /// </summary>
        /// <remarks>If you need more performance out of a section, you can simply
        /// inherit from this generic class and override the Execute method to avoid 
        /// delegating the call to the late binder class.</remarks>
        public override void Execute()
        {
           
            LateBinder<TargetType>.Instance.SetValue(mTarget, mMember, mValue);

        }

        public override void ExecuteOn(object target)
        {
            if (target is TargetType)
            {
                LateBinder<TargetType>.Instance.SetProperty<ValueType>((TargetType)target, mMember, mValue);
            }
            else
            {
                throw new System.ArgumentException("The target passed to ExecuteOn is not " + typeof(TargetType).Name);
            }
        }

        public override void InterpolateBetweenAndExecute(GenericInstruction otherInstruction, float thisRatio)
        {
            IInterpolator<ValueType> interpolator = InstructionManager.GetInterpolator(mValue.GetType(), mMember) as
                IInterpolator<ValueType>;

            ValueType interpolatedValue = interpolator.Interpolate(mValue,
                ((ValueType)otherInstruction.MemberValueAsObject),
                thisRatio);

            LateBinder<TargetType>.Instance.SetProperty<ValueType>(mTarget, mMember, interpolatedValue);
        }

        internal override void SetTarget(object target)
        {
            mTarget = (TargetType)target;
        }

        public override string ToString()
        {
            return mMember + " = " + mValue + ", Time = " + mTimeToExecute;
        }
        #endregion

        #endregion
    }

}
