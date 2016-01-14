using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace FlatRedBall.Instructions
{
    /// <summary>
    /// A list of InstructionBlueprint objects
    /// 
    /// <remarks>Can be used to quickly create InstructionLists for a specific target.</remarks>
    /// </summary>
    public class InstructionBlueprintList : List<InstructionBlueprint>
    {

        #region Fields

        #endregion

        #region Properties


        #endregion

        #region Methods

        #region Constructors

        public InstructionBlueprintList() : base() { }

        public InstructionBlueprintList(int count) : base(count) { }

        #endregion

        #region Public Methods

        #region Xml Comments
        /// <summary>
        /// Creates an InstructionList containing Instructions that were created by the InstructionBlueprints in
        /// this list. 
        /// </summary>
        /// <param name="target">The object that the Instructions will be executed on.</param>
        /// <param name="currentTime">The current time to be used as an offset for each Instruction's execution.</param>
        #endregion
        public InstructionList BuildInstructionList(object target, double currentTime)
        {
            InstructionList instructions = new InstructionList();

            FillInstructionList(target, currentTime, instructions);

            return instructions;

        }

        public void FillInstructionList(object target, double currentTime, InstructionList listToFill)
        {

            foreach (InstructionBlueprint template in this)
            {

                if (template.IsInitialized)
                {
                    listToFill.Add(template.BuildInstruction(target, currentTime));
                }
            }
        }

        #endregion

        public void InsertionSortAscendingTimeToExecute()
        {

            int whereInstructionBelongs;
            for (int i = 0 + 1; i < this.Count; i++)
            {
                if ((this[i]).Time < (this[i - 1]).Time)
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
                        if ((this[i]).Time >= (this[whereInstructionBelongs]).Time)
                        {
                            Insert(whereInstructionBelongs + 1, this[i]);
                            base.RemoveAt(i + 1);
                            break;
                        }
                        else if (whereInstructionBelongs == 0 && (this[i]).Time < (this[0]).Time)
                        {
                            Insert(0, this[i]);
                            base.RemoveAt(i + 1);
                            break;
                        }
                    }
                }
            }
        }

        #endregion


    }
}