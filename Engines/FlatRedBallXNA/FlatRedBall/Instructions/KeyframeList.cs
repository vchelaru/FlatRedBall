using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using System.Reflection;

namespace FlatRedBall.Instructions
{
    public class KeyframeList : List<InstructionList>
    {
        #region Fields
        string mName;

        MethodInstruction mInstructionToExecuteAtEnd;

        #endregion

        #region Properties

        #region XML Docs
        /// <summary>
        /// Gets a KeyframeList by name.  Returns null if none is found
        /// </summary>
        /// <param name="keyframeName">Name of the KeyframeList to return.</param>
        /// <returns>Reference to the KeyframeList with the specified name.</returns>
        #endregion
        public InstructionList this[string keyframeName]
        {
            get
            {
                for (int i = this.Count - 1; i > -1; i--)
                {
                    if (this[i].Name == keyframeName)
                    {
                        return this[i];
                    }
                }

                //nothing found, return null
                return null;
            }
        }

        public MethodInstruction InstructionToExecuteAtEnd
        {
            get { return mInstructionToExecuteAtEnd; }
            set { mInstructionToExecuteAtEnd = value; }
        }

        public string Name
        {
            get { return mName; }
            set { mName = value; }
        }
        #endregion

        #region Methods

        #region Public Methods

        public InstructionList CreateVelocityListAtTime(double time)
        {
            // This will create instructions for all goal starting at the argument
            // time and after.
            InstructionList instructionListAtTime = KeyframeAtOrBefore(time);

            int indexToBeginAt = IndexOf(instructionListAtTime);

            InstructionList instructionList = new InstructionList();

            for (int i = indexToBeginAt; i < Count; i++)
            {
                instructionList.AddRange(CreateVelocityListAtIndex(i));

                if(i != indexToBeginAt || 
                    (instructionListAtTime.Count != 0 && instructionListAtTime[0].TimeToExecute >= time))
                {
                    foreach (Instruction instruction in this[i])
                    {
                        instructionList.Add(instruction.Clone());
                    }
                }
            }

            if (mInstructionToExecuteAtEnd != null && instructionList.Count != 0)
            {
                MethodInstruction lastInstruction = 
                    mInstructionToExecuteAtEnd.Clone<MethodInstruction>();

                lastInstruction.TimeToExecute =
                    instructionList[instructionList.Count - 1].TimeToExecute;

                instructionList.Add(lastInstruction);

            }


            return instructionList;
        }

        public InstructionList CreateVelocityListAtIndex(int keyframeIndex)
        {
            if (keyframeIndex > Count - 1)
            {
                // The user passed either the last instruction index or an out-of-bounds index,
                // so return an empty list.
                return new InstructionList(0);
            }

            else
            {
                #region Get keyframeAtIndex and keyframeAfter

                InstructionList keyframeAtIndex = this[keyframeIndex];

                InstructionList keyframeAfter = null;
                
                double timeBetween;

                if (keyframeIndex == Count - 1)
                {
                    // If it's the last keyframe, use the same keyframe to stop all velocity
                    keyframeAfter = keyframeAtIndex;
                    timeBetween = 1; // To prevent NaN values
                }
                else
                {
                    keyframeAfter = this[keyframeIndex + 1];
                    timeBetween = keyframeAfter[0].TimeToExecute - keyframeAtIndex[0].TimeToExecute;
                }
                #endregion

                InstructionList instructionList = new InstructionList();

                #region Loop through all instructions and create interpolation instructions

                for (int i = 0; i < keyframeAtIndex.Count; i++)
                {
                    Instruction instructionAtIndex = keyframeAtIndex[i];
                    GenericInstruction instructionAtIndexAsGeneric = instructionAtIndex as GenericInstruction;

                    Type typeOfTarget = instructionAtIndexAsGeneric.Target.GetType();

                    bool shouldInterpolate =
                        instructionAtIndexAsGeneric.MemberValueAsObject != null &&

                        instructionAtIndexAsGeneric != null &&
                        InstructionManager.HasInterpolatorForType(
                            instructionAtIndexAsGeneric.MemberValueAsObject.GetType());

                    string velocityMemberName = InstructionManager.GetVelocityForState(
                        instructionAtIndexAsGeneric.Member);

                    if (shouldInterpolate && !string.IsNullOrEmpty(velocityMemberName))
                    {
                        Type typeOfValue = instructionAtIndexAsGeneric.MemberValueAsObject.GetType();
                        //bool hasInterpolated = false;

                        GenericInstruction instructionAfterAsGeneric = keyframeAfter[i] as GenericInstruction;

                        if (instructionAfterAsGeneric != null &&
                            instructionAtIndexAsGeneric.Target == instructionAfterAsGeneric.Target &&
                            instructionAtIndexAsGeneric.Member == instructionAfterAsGeneric.Member)
                        {
                            // We've found the instruction!  Create a velocity instruction
                            Instruction instruction = CreateInstructionFor(
                                instructionAtIndexAsGeneric.Target,
                                velocityMemberName,
                                instructionAtIndexAsGeneric.MemberValueAsObject,
                                instructionAfterAsGeneric.MemberValueAsObject,
                                timeBetween,
                                instructionAtIndexAsGeneric.TimeToExecute,
                                typeOfTarget,
                                typeOfValue);

                            instruction.TimeToExecute = instructionAtIndex.TimeToExecute;

                            instructionList.Add(instruction);

                        }


                    }
                    // switch on the Type, but can't use a Switch statement

                }
                #endregion

                return instructionList;
            }
        }

        public void InsertionSortAscendingTimeToExecute()
        {

            int whereInstructionBelongs;
            for (int i = 0 + 1; i < this.Count; i++)
            {
                // If there are empty instructions, just move along
                if (this[i].Count == 0 || this[i - 1].Count == 0)
                    return;

                if ((this[i])[0].TimeToExecute < (this[i - 1])[0].TimeToExecute)
                {
                    if (i == 1)
                    {
                        Insert(0, this[i]);
                        base.RemoveAt(i + 1);
                        continue;
                    }

                    for (whereInstructionBelongs = i - 2; whereInstructionBelongs > -1; whereInstructionBelongs--)
                    {
                        if ((this[i])[0].TimeToExecute >= (this[whereInstructionBelongs])[0].TimeToExecute)
                        {
                            Insert(whereInstructionBelongs + 1, this[i]);
                            base.RemoveAt(i + 1);
                            break;
                        }
                        else if (whereInstructionBelongs == 0 && (this[i])[0].TimeToExecute < (this[0])[0].TimeToExecute)
                        {
                            Insert(0, this[i]);
                            base.RemoveAt(i + 1);
                            break;
                        }
                    }
                }
            }
        }

        public InstructionList KeyframeAtOrAfter(double time)
        {
            // For now just do a linear search - can make this binary for speed if this becomes a performance problem
            foreach (InstructionList instructionList in this)
            {
                if (instructionList.Count != 0)
                {
                    if (instructionList[0].TimeToExecute >= time)
                    {
                        return instructionList;
                    }
                }
            }

            return null;
        }

        public InstructionList KeyframeAtOrBefore(double time)
        {
            InstructionList lastInstructionBeforeTime = null;
            // For now just do a linear search - can make this binary for speed if this becomes a performance problem
            foreach (InstructionList instructionList in this)
            {
                if (instructionList.Count != 0)
                {
                    if(lastInstructionBeforeTime == null || 
                        (instructionList[0].TimeToExecute <= time && instructionList[0].TimeToExecute > lastInstructionBeforeTime[0].TimeToExecute ))
                    {
                        lastInstructionBeforeTime = instructionList;
                    }
                }
            }

            return lastInstructionBeforeTime;
        }

        public void SetState(double time, bool setVelocity)
        {
            InstructionList keyframeBefore = KeyframeAtOrBefore(time);
            InstructionList keyframeAfter = KeyframeAtOrAfter(time);

            if (keyframeBefore == null && keyframeAfter == null)
            {
                return;
            }

            else if (keyframeBefore == keyframeAfter)
            {
                keyframeBefore.Execute();
            }
            else if (keyframeBefore != null && keyframeAfter == null)
            {
                keyframeBefore.Execute();
            }
            else if (keyframeAfter != null && keyframeBefore == null)
            {
                keyframeAfter.Execute();
            }
            else // the two keyframes are not the same, and neither are null
            {
                double timeAfter = keyframeAfter[0].TimeToExecute;
                double timeBefore = keyframeBefore[0].TimeToExecute;

                double range = timeAfter - timeBefore;

                double ratioAfter = (time - timeBefore) / range;
                double ratioBefore = 1 - ratioAfter;

                for (int i = 0; i < keyframeBefore.Count; i++)
                {
                    Instruction instructionBefore = keyframeBefore[i];

                    GenericInstruction instructionBeforeAsGeneric = instructionBefore as GenericInstruction;

                    object memberValueAsObject = instructionBeforeAsGeneric.MemberValueAsObject;
                    // DO MORE HERE

                    bool shouldInterpolate =
                        instructionBeforeAsGeneric.MemberValueAsObject != null &&
                        instructionBeforeAsGeneric != null &&
                        InstructionManager.HasInterpolatorForType(
                            instructionBeforeAsGeneric.MemberValueAsObject.GetType());

                    if (shouldInterpolate)
                    {
                        bool hasInterpolated = false;

                        GenericInstruction instructionAfterAsGeneric =
                            keyframeAfter[i] as GenericInstruction;

                        if (instructionAfterAsGeneric != null &&
                            instructionBeforeAsGeneric.Target == instructionAfterAsGeneric.Target &&
                            instructionBeforeAsGeneric.Member == instructionAfterAsGeneric.Member)
                        {
                            // We've found the instruction!  Interpolate!
                            instructionBeforeAsGeneric.InterpolateBetweenAndExecute(instructionAfterAsGeneric, (float)ratioBefore);

                            hasInterpolated = true;
                        }

                        if (hasInterpolated == false)
                        {
                            // for now, fail.  Eventually will want to search the entire KeyframeAfter to see if it
                            // contains a matching instruction for interpolation
                            throw new NotImplementedException("The keyframes instructions do not match up.  Cannot interpolate.");
                        }

                    }
                    else
                    {
                        keyframeBefore[i].Execute();
                    }

                }

            }


        }

        public override string ToString()
        {
            return mName + " Count: " + Count.ToString();
        }

        #endregion

        #region Private Methods

        private Instruction CreateInstructionFor(object target, string velocityMemberName,
            object valueBefore, object valueAfter, double timeBetween, double instructionTime,
            Type typeOfTarget, Type typeOfValue)
        {
            Type type = typeof(Instruction<,>).MakeGenericType(typeOfTarget, typeOfValue);

            object[] arguments = null;
            
            if (typeOfValue == typeof(float))
            {
                float difference = (float)valueAfter - (float)valueBefore;

                if (InstructionManager.IsRotationMember(InstructionManager.GetStateForVelocity(velocityMemberName)))
                {
                    difference = FlatRedBall.Math.MathFunctions.AngleToAngle((float)valueBefore, (float)valueAfter);
                }

                arguments = new object[]
                {
                    target, 
                    velocityMemberName, 
                    difference / (float)timeBetween,
                    instructionTime
                };
            }
            else if (typeOfValue == typeof(double))
            {
                double difference = (double)valueAfter - (double)valueBefore;

                arguments = new object[]
                {
                    target, 
                    velocityMemberName, 
                    difference / (double)timeBetween,
                    instructionTime
                };
            }
            else if (typeOfValue == typeof(int))
            {
                int difference = (int)valueAfter - (int)valueBefore;
                arguments = new object[]
                {
                    target, 
                    velocityMemberName, 
                    (int)(difference / (double)timeBetween),
                    instructionTime
                };
            }
            else if (typeOfValue == typeof(long))
            {
                long difference = ((long)valueAfter - (long)valueBefore);

                arguments = new object[]
                {
                    target, 
                    velocityMemberName, 
                    (long)(difference / (double)timeBetween),
                    instructionTime
                };
            }
            else if (typeOfValue == typeof(byte))
            {
                byte difference = (byte)((byte)valueAfter - (byte)valueBefore);

                arguments = new object[]
                {
                    target, 
                    velocityMemberName, 
                    (byte)(difference / (double)timeBetween),
                    instructionTime
                };
            }
            else if (typeOfValue == typeof(Vector2))
            {
                Vector2 range = (Vector2)valueAfter - (Vector2)valueBefore;
                range.X /= (float)timeBetween;
                range.Y /= (float)timeBetween;

                arguments = new object[]
                {
                    target, 
                    velocityMemberName, 
                    range,
                    instructionTime
                };
            }
            else if (typeOfValue == typeof(Vector3))
            {
                Vector3 range = (Vector3)valueAfter - (Vector3)valueBefore;
                range.X /= (float)timeBetween;
                range.Y /= (float)timeBetween;
                range.Z /= (float)timeBetween;

                arguments = new object[]
                {
                    target, 
                    velocityMemberName, 
                    range,
                    instructionTime
                };
            }
            else if (typeOfValue == typeof(Vector4))
            {
                Vector4 range = (Vector4)valueAfter - (Vector4)valueBefore;
                range.X /= (float)timeBetween;
                range.Y /= (float)timeBetween;
                range.Z /= (float)timeBetween;
                range.W /= (float)timeBetween;

                arguments = new object[]
                {
                    target, 
                    velocityMemberName, 
                    range,
                    instructionTime
                };
            }

            return (Instruction) Activator.CreateInstance(type, arguments);
        }

        #endregion


        #endregion
    }
}
