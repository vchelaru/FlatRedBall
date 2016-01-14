using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Instructions.Pause
{
    abstract class UnpauseInstruction<T> : Instruction
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// The game time when the instruction was created.  This is compared to the
        /// TimeManager's CurrentTime property when Execute is called to delay the instructions
        /// by the appropriate amount of time.
        /// </summary>
        #endregion
        protected double mCreationTime;
        protected T mTarget;

        #endregion

        #region Properties

        public override object Target
        {
            get { return mTarget; }
            set { mTarget = (T)value; }
        }

        #endregion

        #region Methods

        public UnpauseInstruction(T target)
        {
            mCreationTime = TimeManager.CurrentTime;
            mTarget = target;
        }


        public override void ExecuteOn(object target)
        {
            throw new NotImplementedException("PauseInstructions can't be executed on a different target.");
        }

        #endregion

    }
}
