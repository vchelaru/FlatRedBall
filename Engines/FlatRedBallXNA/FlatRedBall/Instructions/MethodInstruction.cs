using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace FlatRedBall.Instructions
{
    #region XML Docs
    /// <summary>
    /// Base abstract class for the generic MethodInstruction class.
    /// </summary>
    /// <remarks>
    /// This class is provided to support lists of MethodInstructions.
    /// </remarks>
    #endregion
    public abstract class MethodInstruction : Instruction
    {


    }

    #region XML Docs
    /// <summary>
    /// Generic Instruction class which calls a method when executed.
    /// </summary>
    /// <typeparam name="T">The type of the object which contains the method to be called.</typeparam>
    #endregion
    public class MethodInstruction<T> : MethodInstruction
    {
        #region Fields
        T mTarget;
        object[] mArguments;

        MethodInfo mMethodInfo;

        #endregion

        #region Properties

        public override object Target
        {
            get { return mTarget; }
            set { mTarget = (T)value; }
        }

        #endregion

        #region Methods
        public MethodInstruction(T targetObject, string methodToCall, object[] arguments, double timeToExecute)
        {
            mTarget = targetObject;
            mArguments = arguments;

            mTimeToExecute = timeToExecute;

            try
            {
#if WINDOWS_8
                mMethodInfo = typeof(T).GetTypeInfo().GetDeclaredMethod(methodToCall);
#else
                mMethodInfo = typeof(T).GetMethod(methodToCall,
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);
#endif
            }
            catch (AmbiguousMatchException e)
            {
                throw new AmbiguousMatchException(
                    "The class " + typeof(T) + " has multiple methods named " + methodToCall + 
                    ".  You should use the MethodInstruction constrctor that takes a MethodInfo.",
                    e
                    );
            }


            if (mMethodInfo == null)
            {
                throw new MemberAccessException("Cannot find a method by the name of " +
                    methodToCall + " in the " + typeof(T).Name + " class.");
            }        
        }

        public MethodInstruction(T targetObject, MethodInfo methodInfo, 
            object[] arguments, double timeToExecute)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException("The argument methodInfo is null.  This must be a non-null reference.");
            }

            mTarget = targetObject;
            mMethodInfo = methodInfo;
            mArguments = arguments;
            mTimeToExecute = timeToExecute;
        }

        public override void Execute()
        {
            mMethodInfo.Invoke(
                mTarget, mArguments);
        }

        public override void ExecuteOn(object target)
        {
            if (target is T)
            {
                mMethodInfo.Invoke(
                    target, mArguments);
            }
            else
            {
                throw new System.ArgumentException("The target passed to ExecuteOn is not an IAttachable");
            }

        }

        #endregion
    }
}
