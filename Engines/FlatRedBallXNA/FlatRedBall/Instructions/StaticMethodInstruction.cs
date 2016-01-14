using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace FlatRedBall.Instructions
{
    #region XML Docs
    /// <summary>
    /// Instruction which calls a Static class' method.
    /// </summary>
    /// <remarks>
    /// This class must be used instead of 
    /// FlatRedBall.Instructions.MethodInstruction
    /// because MethodInstruction takes a reference
    /// to an instance of the object containing the method
    /// to call.  If the object is a static class then the
    /// compiler will complain about static objects being used
    /// as arguments to methods.
    /// </remarks>
    #endregion
    public class StaticMethodInstruction : Instruction
    {
        #region Fields
        object[] mArguments;

        MethodInfo mMethodInfo;

        #endregion

        public override object Target
        {
            // Static methods have no target.
            get { return null; }
            set { throw new InvalidOperationException(); }
        }

        #region Methods
        public StaticMethodInstruction(Type type, string methodToCall, object[] arguments, double timeToExecute)
        {
#if WINDOWS_8
            mMethodInfo =
                type.GetTypeInfo().GetDeclaredMethod(methodToCall);
#else
            mMethodInfo =
                type.GetMethod(methodToCall);
#endif

            mArguments = arguments;

            mTimeToExecute = timeToExecute;
        }

        public StaticMethodInstruction(MethodInfo methodInfo, object[] arguments, double timeToExecute)
        {
            mMethodInfo = methodInfo;
            mArguments = arguments;
            mTimeToExecute = timeToExecute;
        }

        public override void Execute()
        {
            mMethodInfo.Invoke(
                null, mArguments);

        }

        public override void ExecuteOn(object target)
        {
            // Still executes the same since static methods have no targets
            Execute();
        }        

        #endregion

    }
}
