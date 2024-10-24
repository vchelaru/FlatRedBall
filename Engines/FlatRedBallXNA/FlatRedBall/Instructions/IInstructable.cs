using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Instructions.Reflection;
using System.Reflection;
using System.Linq;

namespace FlatRedBall.Instructions
{
    /// <summary>
    /// Provides an interface for objects which can store Instructions.
    /// </summary>
    public interface IInstructable
    {
        /// <summary>
        /// The list of Instructions that this instance owns.  These instructions usually
        /// will execute on this instance; however, this is not a requirement.
        /// </summary>
        InstructionList Instructions
        {
            get;
        }
    }


    internal struct SetHolder<T> : ISet, ITo, IAfter where T : IInstructable
    {
        enum SetOrCall
        {
            Set,
            Call
        }

        SetOrCall mSetOrCall;

        internal T Caller;
        internal string MemberToSet;
        internal object ValueToSet;
        internal Action Action;
        internal Action<T> ActionT;
        internal Type type;


        public IAfter Call(Action Action)
        {
            this.Action = Action;
            mSetOrCall = SetOrCall.Call;
            return this;
        }

        public IAfter Call(Action<T> Action)
        {
            this.ActionT = Action;
            mSetOrCall = SetOrCall.Call;
            return this;
        }

        public ITo Set(string memberToSet)
        {
            MemberToSet = memberToSet;
            mSetOrCall = SetOrCall.Set;
            return this;
        }

        public IAfter To(object value)
        {
            // Assigning a float value to
            // an int (like passing 0 instead
            // of 0.0f) is a common problem.  We
            // should do some checks here to let the
            // user know that something is wrong before
            // the instruction fires and throws an error.
            //  that makes the bug easier to fix.
#if DEBUG 
            var fields = typeof(T).GetFields();
            var properties = typeof(T).GetProperties();
            string memberToSet = this.MemberToSet;
            var foundField = fields.FirstOrDefault(field => field.Name == memberToSet);
            var foundProperty = properties.FirstOrDefault(property => property.Name == memberToSet);

            if (foundField == null && foundProperty == null)
            {
                throw new Exception("The type " + typeof(T) + " does not have a public field or property named " + memberToSet);
            }
            else
            {
                Type type;
                if (foundField != null)
                {
                    type = foundField.FieldType;
                }
                else
                {
                    type = foundProperty.PropertyType;
                }

                if (value != null && !type.IsAssignableFrom(value.GetType()))
                {
                    // let's handle special cases and be more informative
                    if (value is int && type == typeof(float))
                    {
                        throw new Exception("You must set a float value.  For example use 0.0f instead of 0.");
                    }
                    else
                    {
                        throw new Exception("The type " + value + " is invalid");
                    }
                }

            }

#endif


            this.ValueToSet = value;



            return this;
        }

        public Instruction After(double time)
        {
            Instruction toReturn;

            if (mSetOrCall == SetOrCall.Set)
            {
                Type memberType = GetMemberType(MemberToSet);
                Type instructionType = typeof(Instruction<,>).MakeGenericType(typeof(T), memberType);

                GenericInstruction instruction = (GenericInstruction)Activator.CreateInstance(instructionType);

                instruction.TimeToExecute = TimeManager.CurrentTime + time;
                instruction.SetTarget(Caller);
                instruction.Member = MemberToSet;
                instruction.MemberValueAsObject = ValueToSet;
                toReturn = instruction;

            }
            else if (ActionT != null)
            {
                DelegateInstruction<T> delegateInstruction = new DelegateInstruction<T>(ActionT, Caller);
                delegateInstruction.TimeToExecute = TimeManager.CurrentTime + time;

                toReturn = delegateInstruction;
            }
            else
            {
                DelegateInstruction delegateInstruction = new DelegateInstruction(Action);
                delegateInstruction.TimeToExecute = TimeManager.CurrentTime + time;

                toReturn = delegateInstruction;
            }


            Caller.Instructions.Add(toReturn);

            return toReturn;
        }

        Type GetMemberType(string memberName)
        {
            FieldInfo fieldInfo = typeof(T).GetField(memberName);

            if (fieldInfo != null)
            {
                return fieldInfo.FieldType;
            }
            else
            {
                PropertyInfo propertyInfo = typeof(T).GetProperty(memberName);

                if (propertyInfo != null)
                {
                    return propertyInfo.PropertyType;
                }
            }

            throw new Exception("The value " + memberName + " is an invalid member");
        }
    }

    public interface ISet
    {
        ITo Set(string memberToSet);
    }
    public interface ITo
    {
        IAfter To(object value);
    }
    public interface IAfter
    {
        Instruction After(double time);
    }

    public static class InstructionExtensionMethods
    {
        public static IAfter Call<T>(this T caller, Action action) where T : IInstructable
        {
            SetHolder<T> holder = new SetHolder<T>();
            holder.Caller = caller;
            return holder.Call(action);
        }

        public static IAfter Call<T>(this T caller, Action<T> action) where T : IInstructable
        {
            SetHolder<T> holder = new SetHolder<T>();
            holder.Caller = caller;
            return holder.Call(action);
        }

        public static ITo Set<T>(this T caller, string value) where T : IInstructable
        {
            SetHolder<T> holder = new SetHolder<T>();
            holder.Caller = caller;
            return holder.Set(value);
        }
        

    }
}
