using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Instructions
{
    #region XML Docs
    /// <summary>
    /// Class that supports the execution of custom logic at a future time.
    /// </summary>
    /// <remarks>
    /// Instructions are either stored and executed through the InstructionManager or
    /// managed IInstructable instances.
    /// </remarks>
    #endregion
    public abstract class Instruction
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// The system time to execute the instruction at.
        /// </summary>
        #endregion
        protected double mTimeToExecute;

        #region XML Docs
        /// <summary>
        /// The amount of time to add to the instruction for cycled execution.  Default of 0
        /// instructs the executing logic to not cycle the Instruction.
        /// </summary>
        #endregion
        protected double mCycleTime = 0; // A 0 cycle time means the Instruction will not loop.

        #endregion

        #region Properties

        /// <summary>
        /// The game time to check for execution.
        /// </summary>
        /// <remarks>
        /// The TimeManager.CurrentTime property is used for comparisons.
        /// </remarks>
        public double TimeToExecute
        {
            get { return mTimeToExecute; }
            set { mTimeToExecute = value; }
        }

        /// <summary>
        /// The amount of time between executions for an instruction that cycles. If this value is 0, the
        /// instruction does not cycle. CycleTime is only considered after an instructions first execution.
        /// This means that an instruction may be scheuduled to execute in 10 seconds, then after its first execution
        /// it will repeat every second thereafter.
        /// </summary>
        /// <example>
        /// Setting a CycleTime of 1 means that the instruction will execute ever 1 second after its first execution.
        /// </example>
        public double CycleTime
        {
            get { return mCycleTime; }
            set { mCycleTime = value; }
        }

        /// <summary>
        /// Gets reference to the object that is the target of the Instruction.
        /// </summary>
        public abstract object Target
        {
            get;
            set;
        }

        #endregion

        #region Methods

        #region XML Docs
        /// <summary>
        /// Creates and redturns a member-wise clone.
        /// </summary>
        /// <returns>The clone of the calling instance.</returns>
        #endregion
        public virtual Instruction Clone()
        {
            return (Instruction)MemberwiseClone();
        }

        public virtual T Clone<T>() where T : Instruction
        {
            return (T)MemberwiseClone();
        }

        #region XML Docs
        /// <summary>
        /// Executes the Instruction.
        /// </summary>
        #endregion
        public abstract void Execute();

        #region XML Docs
        /// <summary>
        /// Executes an instruction on the target passed as an argument
        /// </summary>
        #endregion
        public abstract void ExecuteOn(object target);

        #endregion
    }


}
