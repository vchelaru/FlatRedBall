using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Instructions;
using FlatRedBall;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics.Model;
using FlatRedBall.Graphics;

namespace InstructionEditor
{
    public static class InstructionRecorder
    {
        #region Fields

        static Type mSpriteType = typeof(Sprite);
        static Type mSpriteFrameType = typeof(SpriteFrame);
        static Type mPositionedModelType = typeof(PositionedModel);
        static Type mTextType = typeof(Text);

        #endregion

        #region Methods

        public static void RecordInstructions(InstructionList listToRecordTo, double timeToExecute, 
            List<string> membersToIgnore, Sprite spriteToRecord)
        {
            foreach (string member in EditorData.CurrentSpriteMembersWatching)
            {
                if (membersToIgnore.Contains(member) == false)
                {
                    Type memberType = InstructionManager.GetTypeForMember(mSpriteType, member);

                    Type genericType = typeof(Instruction<,>).MakeGenericType(
                        mSpriteType, memberType);
                    object value = FlatRedBall.Instructions.Reflection.LateBinder<Sprite>.Instance[spriteToRecord, member];

                    Instruction instruction = Activator.CreateInstance(genericType,
                        spriteToRecord, member, value, timeToExecute) as Instruction;

                    listToRecordTo.Add(instruction);
                }
            }
        }

        public static void RecordInstructions(InstructionList listToRecordTo, double timeToExecute,
            List<string> membersToIgnore, SpriteFrame spriteFrame)
        {
            foreach (string member in EditorData.CurrentSpriteMembersWatching)
            {
                if (membersToIgnore.Contains(member) == false)
                {
                    Type memberType = InstructionManager.GetTypeForMember(mSpriteFrameType, member);

                    Type genericType = typeof(Instruction<,>).MakeGenericType(
                        mSpriteFrameType, memberType);
                    object value = FlatRedBall.Instructions.Reflection.LateBinder<SpriteFrame>.Instance[spriteFrame, member];

                    Instruction instruction = Activator.CreateInstance(genericType,
                        spriteFrame, member, value, timeToExecute) as Instruction;

                    listToRecordTo.Add(instruction);
                }
            }
        }


        public static void RecordInstructions(InstructionList listToRecordTo, double timeToExecute,
            List<string> membersToIgnore, PositionedModel modelToRecord)
        {
            foreach (string member in EditorData.CurrentPositionedModelMembersWatching)
            {
                if (membersToIgnore.Contains(member) == false)
                {
                    Type memberType = InstructionManager.GetTypeForMember(mPositionedModelType, member);

                    Type genericType = typeof(Instruction<,>).MakeGenericType(
                        mPositionedModelType, memberType);
                    object value = FlatRedBall.Instructions.Reflection.LateBinder<PositionedModel>.Instance[modelToRecord, member];

                    Instruction instruction = Activator.CreateInstance(genericType,
                        modelToRecord, member, value, timeToExecute) as Instruction;

                    listToRecordTo.Add(instruction);
                }
            }
        }

        public static void RecordInstructions(InstructionList listToRecordTo, double timeToExecute,
            List<string> membersToIgnore, Text textToRecord)
        {
            foreach (string member in EditorData.CurrentTextMembersWatching)
            {
                if (membersToIgnore.Contains(member) == false)
                {
                    Type memberType = InstructionManager.GetTypeForMember(mTextType, member);

                    Type genericType = typeof(Instruction<,>).MakeGenericType(
                        mTextType, memberType);
                    object value = FlatRedBall.Instructions.Reflection.LateBinder<Text>.Instance[textToRecord, member];

                    Instruction instruction = Activator.CreateInstance(genericType,
                        textToRecord, member, value, timeToExecute) as Instruction;

                    listToRecordTo.Add(instruction);
                }
            }
        }

        #endregion
    }
}
