using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace FlatRedBall.Instructions
{
    #region XML Comments
    /// <summary>
    /// A class that can be used to quickly create create identical Instructions for individual targets.
    /// </summary>
    /// <remarks>Cannot be serialized as an InstructionBlueprint, but can be converted into an InstructionSave via 
    /// InstructionSave.FromInstructionBlueprint().</remarks>
    #endregion
    public class InstructionBlueprint
    {
        #region Fields

        private string mMemberName;
        private Type mMemberType;
        private object mMemberValue;
        private Type mTargetType;
        private double mTime;
        
        private ConstructorInfo mCurrentConstructor;

        #endregion

        #region Properties

        public string MemberName
        {
            get { return mMemberName; }
            set
            {
                mMemberName = value;
            }
        }


        public Type MemberType
        {
            get { return mMemberType; }
            set
            {
                mMemberType = value;
                CreateConstructor();
            }
        }

        public object MemberValue
        {
            get { return mMemberValue; }
            set
            {
                mMemberValue = value;
            }
        }

        public Type TargetType
        {
            get { return mTargetType; }
            set
            {
                mTargetType = value;
                CreateConstructor();
            }
        }

        public double Time
        {
            get { return mTime; }
            set { mTime = value; }
        }

        public bool IsInitialized
        {
            get { return mMemberName != null && mMemberValue != null; }
        }

        #endregion

        #region Methods

        public InstructionBlueprint()
        {
            Time = 0;

        }

        static object[] mFourObjectArray = new object[4];
        static Type[] mFourTypeArray = new Type[4];
        static Type[] mTwoTypeArray = new Type[2];

        public InstructionBlueprint(Type targetType, string memberName, Type memberType, object memberValue, double time)
        {
            TargetType = targetType;
            MemberName = memberName;
            MemberType = memberType;
            MemberValue = memberValue;
            Time = time;
        }

        /// <summary>
        /// Builds an Instruction using the information stored in the InstructionBlueprint.
        /// </summary>
        /// <param name="target">The object that the returned Instruction will execute on</param>
        /// <param name="currentTime">The current time to use as an offset for the Instruction's Time of execution</param>
        /// <throws exception="ArgumentException">If target's type is not this InstructionBlueprint's TargetType</throws>
        /// <throws exception="NullReferenceException">If this InstructionBlueprint was fully initialized before this call to BuildInstruction.</throws>
        /// <returns>An Instruction created from this InstructionBlueprint's information</returns>
        public GenericInstruction BuildInstruction(object target, double currentTime){
            GenericInstruction toReturn;

            
            //Make sure the target is a compatible type
            if (!TargetType.IsInstanceOfType(target))
            {
                throw new ArgumentException("This InstructionBlueprint's TargetType is " + TargetType + " and doesn't match the argument's type, " +
                                                target.GetType());
            }
            else if (String.IsNullOrEmpty(MemberName) || MemberType == null)
            {
                throw new NullReferenceException("The InstructionBlueprint " + " has not been fully initialized and therefore cannot be made into an Instruction.");
            }
            else
            {
                mFourObjectArray[0] = target;
                mFourObjectArray[1] = MemberName;
                mFourObjectArray[2] = MemberValue;
                mFourObjectArray[3] = Time + currentTime;

                toReturn = mCurrentConstructor.Invoke(mFourObjectArray) as GenericInstruction;

            }
            return toReturn;
        }

        private void CreateConstructor()
        {
            if (TargetType != null && MemberType != null)
            {
                mTwoTypeArray[0] = TargetType;
                mTwoTypeArray[1] = MemberType;

                Type genType = typeof(Instruction<,>).MakeGenericType(mTwoTypeArray);

                mFourTypeArray[0] = TargetType;
                mFourTypeArray[1] = typeof(string);
                mFourTypeArray[2] = MemberType;
                mFourTypeArray[3] = typeof(double);

                mCurrentConstructor = genType.GetConstructor(mFourTypeArray);
           
            }
        }

        public override string ToString()
        {
            string toReturn = "";

            if (this.IsInitialized)
                toReturn += this.MemberName + "  =  " + this.MemberValue + " @ " + String.Format("{0:0.######}", Time) + " sec.";
            else
                toReturn = "Uninitialized Blueprint";

            return toReturn;
        }

        #endregion


    }
}
