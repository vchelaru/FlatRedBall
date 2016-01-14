using System;

namespace FlatRedBall.Instructions
{
    /// <summary>
    /// Represents an Instruction which can execute an instruction.  This is commonly used
    /// with IInstructables to perform logic at a delayed time.
    /// </summary>
    public class DelegateInstruction : Instruction
    {

        private Action mAction;

        /// <summary>
        /// The target for DelegateInstruction doesn't have any internal purpose - it's used purely to 
        /// help games track what has generated instructions.
        /// </summary>
        /// <remarks>
        /// This used to be a null getter only, but now it stores data so that
        /// projects like Gum can assign owners of an instruction.
        /// </remarks>
        public override object Target
        {
            get;
            set;
        }

        public static DelegateInstruction<T> Create<T>(Action<T> action, T target) where T : class
        {
            return new DelegateInstruction<T>(action, target);
        }
        public static DelegateInstruction<T, U> Create<T, U>(Action<T, U> action, T target, U target2) where T : class
        {
            return new DelegateInstruction<T, U>(action, target, target2);
        }
        public DelegateInstruction(Action action)
        {
            mAction = action;
        }

        

        public override void Execute()
        {
            mAction();
        }

        public override void ExecuteOn(object target)
        {
            mAction();
        }

        public override string ToString()
        {
            return "Delegate Instruction at " + this.TimeToExecute;
        }
    }

    public class DelegateInstruction<T> : Instruction 
        //where T : class
    {
        private T mTarget;
        private Action<T> mAction;


        public DelegateInstruction(Action<T> action, T target)
            : this(action, target , 0)
        { }

        public DelegateInstruction(Action<T> action, T target, double timeToExecute)
        {
            ValidateTarget(target);
            this.mTarget = target;
            this.mAction = action;
            this.TimeToExecute = timeToExecute;
        }

        public override object Target
        {
            get { return this.mTarget; }
            set { mTarget = (T)value; }
        }

        public override void Execute()
        {
            this.mAction(this.mTarget);
        }

        public override void ExecuteOn(object target)
        {
            T castedTarget = (T)((object)target);
            ValidateTarget(castedTarget);
            this.mAction(castedTarget);
        }

        private static void ValidateTarget(T castedTarget)
        {
            if (castedTarget == null) throw new ArgumentException(string.Format("target must be of type {0}, and non-null", typeof(T)));
        }

        public override string ToString()
        {
            return "Delegate Instruction at " + this.TimeToExecute;
        }
    }

    public class DelegateInstruction<T, U> : Instruction where T : class
    {
        private T mTarget;
        private U mTarget2;
        private Action<T, U> mAction;


        public DelegateInstruction(Action<T, U> action, T target, U target2)
            : this(action, target, target2, 0)
        { }

        public DelegateInstruction(Action<T, U> action, T target, U target2, double timeToExecute)
        {
            ValidateTarget(target);
            this.mTarget = target;
            this.mTarget2 = target2;
            this.mAction = action;
            this.TimeToExecute = timeToExecute;
        }

        public override object Target
        {
            get { return this.mTarget; }
            set { mTarget = (T)value; }
        }

        public object Target2
        {
            get { return this.mTarget2; }
        }

        public override void Execute()
        {
            this.mAction(this.mTarget, mTarget2);
        }

        public override void ExecuteOn(object target)
        {
            T castedTarget = target as T;
            ValidateTarget(castedTarget);
            this.mAction(castedTarget, default(U));
        }

        public virtual void ExecuteOn(object target, object target2)
        {
            T castedTarget = target as T;
            U castedTarget2 = (U)target2;
            ValidateTarget(castedTarget);
            this.mAction(castedTarget, castedTarget2);
        }
        private static void ValidateTarget(T castedTarget)
        {
            if (castedTarget == null) throw new ArgumentException(string.Format("target must be of type {0}, and non-null", typeof(T)));
        }

        public override string ToString()
        {
            return "Delegate Instruction at " + this.TimeToExecute;
        }
    }







}
