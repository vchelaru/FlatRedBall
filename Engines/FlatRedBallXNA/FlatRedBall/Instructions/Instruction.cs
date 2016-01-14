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

        #region XML Docs
        /// <summary>
        /// The system time to execute the instruction at.
        /// </summary>
        /// <remarks>
        /// The TimeManager.CurrentTime property is used for comparisons.
        /// </remarks>
        #endregion
        public double TimeToExecute
        {
            get { return mTimeToExecute; }
            set { mTimeToExecute = value; }
        }

        #region XML Docs
        /// <summary>
        /// The amount of time to add to the instruction for cycled execution.  Default of 0 
        /// instructs the executing logic to not cycle the Instruction.
        /// </summary>
        #endregion
        public double CycleTime
        {
            get { return mCycleTime; }
            set { mCycleTime = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets reference to the object that is the target of the Instruction.
        /// </summary>
        #endregion
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
