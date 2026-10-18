using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace FlatRedBall.Instructions
{
    /// <summary>
    /// Instruction which calls a Static class' method.
    /// </summary>
    [Obsolete("Use DelegateInstruction or Call on IInstructibles")]
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
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Looks up the method by name on the type. Under trimming or Native AOT the method may have been removed. Pass a MethodInfo instead.")]
        public StaticMethodInstruction(Type type, string methodToCall, object[] arguments, double timeToExecute)
        {
            mMethodInfo =
                type.GetMethod(methodToCall);

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
