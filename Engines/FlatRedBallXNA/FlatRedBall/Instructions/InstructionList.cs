using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Instructions
{
    #region XML Docs
    /// <summary>
    /// List of Instructions which also provides methods for common
    /// actions such as sorting and executing the contained Instructions.
    /// </summary>
    #endregion
    public class InstructionList : List<Instruction>
    {
        #region Fields

        string mName;

        #endregion

        #region Properties

        public string Name
        {
            get { return mName; }
            set { mName = value; }
        }

        public bool SortOnAdd
        {
            get;
            set;
        }

        #endregion

        #region Methods

        #region Constructors

        public InstructionList() : base()
        { SortOnAdd = true; }

        public InstructionList(int count) : base(count)
        { SortOnAdd = true; }

        #endregion

        #region Public Methods

        new public void Add(Instruction instruction)
        {
            base.Add(instruction);
            if (SortOnAdd)
            {
                InsertionSortAscendingTimeToExecute();
            }
        }
        

        public new void AddRange(IEnumerable<Instruction> collection)
        {
            base.AddRange(collection);
            InsertionSortAscendingTimeToExecute();
        }


        public InstructionList Clone()
        {
            InstructionList clone = new InstructionList();

            foreach (Instruction i in this)
            {
                clone.Add(i.Clone());
            }
            return clone;
        }

        #region XML Docs
        /// <summary>
        /// Executes all contained Instructions in order of index.  Contained instructions are not removed.
        /// </summary>
        #endregion
        public void Execute()
        {
            foreach (Instruction instruction in this)
            {
                instruction.Execute();
            }
        }


        public void ExecuteAndRemoveOrCyclePassedInstructions()
        {
            ExecuteAndRemoveOrCyclePassedInstructions(TimeManager.CurrentTime);

        }

        public void ExecuteAndRemoveOrCyclePassedInstructions(double currentTime)
        {
            Instruction instruction;

            while (Count > 0 && this[0].TimeToExecute <= currentTime)
            {
                instruction = this[0];

                instruction.Execute();

                // The instruction may have cleared the InstructionList, so we need to test if it did.
                if (Count < 1)
                    continue;

                if (instruction.CycleTime == 0)
                    Remove(instruction);
                else
                {
                    // Vic Says:
                    // Originally this code simply increased the TimeToExecute,
                    // then called InsertionSortAscendingTimeToExecute().  This caused
                    // problems in the InstructionEditor.  When an Animation is playing
                    // and it should be cycled, the individual instructions that are created
                    // by the InstructionSet or AnimationSequence have a cycle time that's not
                    // 0.  This tells the InstructionList to cycle the instruction.  However, due
                    // to the way the instructions were sorting, cycled animations would move near
                    // the end of the instruction, but they'd appear ahead of other instructions that
                    // had the same TimeToExecute.
                    // In other words, if the InstructionList had instructions A, B, C and A's CycleTime
                    // was set to 3, and C's TimeToExecute was also 3, then after the sort, the order would
                    // be B, A, C instead of B, C, A.  To resolve this problem the Sort method was modified to
                    // use slightly different comparison logic than other sort methods in FRB.  This unfortunately
                    // caused some logic errors.  Rather than to make the sorting method more complicated, cycled instructions
                    // will now simply be removed and added, which will move them to the end.  This not only solves the sorting
                    // logic, but it's actually slightly more efficient in most cases because a cycle will usually move an instruction
                    // to the end of the list.
                    instruction.TimeToExecute += instruction.CycleTime;
                    Remove(instruction);
                    Add(instruction);
                }
            }
        }


        public void ExecuteOn(object target)
        {
            foreach (Instruction instruction in this)
            {
                instruction.ExecuteOn(target);
            }
        }


        public int FindIndexAfter(long timeToFindAfter)
        {
            if (Count == 0) return -1;

            for (int i = Count - 1; i > -1; i--)
            {
                if (this[i].TimeToExecute <= timeToFindAfter)
                {
                    if (i == Count - 1) return Count;
                    else return i + 1;

                }

            }
            return 0;
        }


        public Instruction FindInstructionAfter(long timeToFindAfter)
        {
            // TODO:  Make this a binary search
            if (Count == 0) return null;

            for (int i = Count - 1; i > -1; i--)
            {
                if (this[i].TimeToExecute <= timeToFindAfter)
                {
                    if (i == Count - 1) return null;
                    else return this[i + 1];

                }

            }
            return this[0];
        }


        public Instruction FindInstructionBefore(double timeToFindBefore)
        {
            // TODO:  Make this a binary search later

            if (Count == 0) return null;

            for (int i = 0; i < Count; i++)
            {
                if (this[i].TimeToExecute > timeToFindBefore)
                {
                    if (i == 0) return null;
                    else return this[i - 1];
                }
            }

            return this[Count - 1];
        }


        public int GetIndexAfter(double timeToFindAfter)
        {
            if (Count == 0) return -1;

            for (int i = Count - 1; i > -1; i--)
            {
                if (this[i].TimeToExecute <= timeToFindAfter)
                {
                    if (i == Count - 1) return -1;
                    else return i + 1;
                }
            }

            return 0;
        }


        public int GetIndexBefore(double timeToFindBefore)
        {
            if (Count == 0) return -1;

            for (int i = 0; i < Count; i++)
            {
                if (this[i].TimeToExecute > timeToFindBefore)
                {
                    return i - 1;
                }
            }

            return Count - 1;
        }


        public List<string> GetGenericInstructionReferencedMembers()
        {
            List<string> referencedMembers = new List<string>();

            foreach (GenericInstruction instruction in this)
            {
                if (instruction != null && referencedMembers.Contains(instruction.Member) == false)
                {
                    referencedMembers.Add(instruction.Member);
                }
            }

            return referencedMembers;
        }


        public void InsertionSortAscendingTimeToExecute()
        {

            int whereInstructionBelongs;
            for (int i = 0 + 1; i < this.Count; i++)
            {
                if ((this[i]).TimeToExecute < (this[i - 1]).TimeToExecute)
                {
                    if (i == 1)
                    {
                        Insert(0, this[i]);
                        base.RemoveAt(i + 1);
                        continue;
                    }

                    for (whereInstructionBelongs = i - 2; whereInstructionBelongs > -1; whereInstructionBelongs--)
                    {
                        // The following IF statement has been modified a few times.  For a history,
                        // see the ExecuteAndRemoveOrCyclePassedInstructions method.
                        if ((this[i]).TimeToExecute >= (this[whereInstructionBelongs]).TimeToExecute)
                        {
                            Insert(whereInstructionBelongs + 1, this[i]);
                            base.RemoveAt(i + 1);
                            break;
                        }
                        else if (whereInstructionBelongs == 0 && (this[i]).TimeToExecute < (this[0]).TimeToExecute)
                        {
                            Insert(0, this[i]);
                            base.RemoveAt(i + 1);
                            break;
                        }
                    }
                }
            }
        }


        public void ShiftTime(double timeToShiftBy)
        {
            int count = this.Count; // for performance on compact framework

            for (int i = 0; i < count; i++)
            {
                this[i].TimeToExecute += timeToShiftBy;
            }
        }


        public override string ToString()
        {
            if (Count == 0)
            {
                return "No Instructions";
            }
            else
            {
                string returnString = "";

                foreach (Instruction instruction in this)
                {
                    returnString += instruction.ToString() + ", ";
                }

                return returnString;
            }
        }

        #endregion


        #endregion
    }
}
