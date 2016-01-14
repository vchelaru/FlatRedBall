using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Instructions;

namespace EditorObjects
{
    public delegate void AfterUpdateDelegate(object objectUndone);

    public abstract class PropertyComparer
    {
        #region Fields

        protected Dictionary<string, AfterUpdateDelegate> mAfterUpdateDelegates = new Dictionary<string, AfterUpdateDelegate>();

        #endregion

        #region Properties

        public abstract Type GenericType
        {
            get;
        }

        #endregion

        #region Methods

        public AfterUpdateDelegate GetAfterUpdateDelegateForMember(string memberName)
        {
            if (mAfterUpdateDelegates.ContainsKey(memberName))
            {
                return mAfterUpdateDelegates[memberName];
            }
            else
            {
                return null;
            }
        }

        public void SetAfterUpdateDelegateForMember(string memberName, AfterUpdateDelegate afterUpdateDelegate)
        {
            mAfterUpdateDelegates.Add(memberName, afterUpdateDelegate);
        }

        #endregion
    }

    public class PropertyComparer<T> : PropertyComparer where T : new()
    {
        #region Fields

        Type mType = typeof(T);

        // The key is the actual object being watched while
        // the value in the Dictionary is the copy of the object.
        protected Dictionary<T, T> mObjectsWatching = new Dictionary<T, T>();



        List<TypedMemberBase> mMembersWatching = new List<TypedMemberBase>();

        #endregion

        #region Properties

        public override Type GenericType
        {
            get { return typeof(T); }
        }

        #endregion

        #region Methods

        public void AddMemberWatching<U>(string memberToWatch) where U : IEquatable<U>
        {
            mMembersWatching.Add(new TypedMember<U>(memberToWatch));
        }

        public void AddObject(T objectToWatch, T instanceToUseAsClone) 
        {
            if (instanceToUseAsClone == null)
            {
                throw new ArgumentNullException("instanceToUseAsClone cannot be null");
            }

            mObjectsWatching.Add(objectToWatch, instanceToUseAsClone);


            UpdateWatchedObject(objectToWatch);

        }

        public void ClearObjects()
        {
            mObjectsWatching.Clear();
        }

        public bool Contains(T item)
        {
            return mObjectsWatching.ContainsKey(item);
        }

        public void GetAllChangedMemberInstructions(List<InstructionList> list)
        {
            // This is not made virtual because all it does is call the
            // version that takes a 2nd argument.  The 2nd argument-version
            // will be called on the most derived class due to polymorphism.  Therefore,
            // only the 2nd argument version will be virtual.
            
            GetAllChangedMemberInstructions(list, true);
        }

        public virtual void GetAllChangedMemberInstructions(List<InstructionList> list, bool createNewList)
        {
            foreach (KeyValuePair<T, T> kvp in mObjectsWatching)
            {
                InstructionList changes = GetChangedMemberInstructions(kvp.Key);

                if (changes.Count != 0)
                {
                    if (createNewList || list.Count == 0)
                    {
                        list.Add(changes);
                    }
                    else
                    {
                        list[list.Count - 1].AddRange(changes);
                    }

                }
            }
        }

        public virtual InstructionList GetChangedMemberInstructions(T objectToWatch)
        {
            InstructionList listToReturn = new InstructionList();

            Type type = typeof(T);
            
            foreach (TypedMemberBase member in mMembersWatching)
            {
                PropertyInfo propertyInfo = type.GetProperty(member.MemberName);

                if (propertyInfo != null)
                {
                    object currentValue = propertyInfo.GetValue(objectToWatch, null);
                    object lastValue = propertyInfo.GetValue( mObjectsWatching[objectToWatch], null);

                    
                    if(member.IsMemberValueEqual<T>(objectToWatch, mObjectsWatching[objectToWatch]) == false)
                    {
                        Instruction<T, object> instruction = new Instruction<T, object>(
                            objectToWatch, member.MemberName, lastValue, 0);

                        listToReturn.Add(instruction);
                    }

                    FlatRedBall.Instructions.Reflection.LateBinder<T>.Instance.SetProperty<object>(
                        mObjectsWatching[objectToWatch], member.MemberName, currentValue);
                }
                else
                {
                    FieldInfo fieldInfo = type.GetField(member.MemberName);

                    if (fieldInfo == null)
                    {
                        throw new MemberAccessException("Cannot find member by the name of " + member + " in the UndoManager");
                    }

                    object currentValue = fieldInfo.GetValue(objectToWatch);
                    object lastValue = fieldInfo.GetValue(mObjectsWatching[objectToWatch]);

                    if (member.IsMemberValueEqual<T>(objectToWatch, mObjectsWatching[objectToWatch]) == false)
                    {
                        Instruction<T, object> instruction = new Instruction<T, object>(
                            objectToWatch, member.MemberName, lastValue, 0);

                        listToReturn.Add(instruction);
                    }

                    FlatRedBall.Instructions.Reflection.LateBinder<T>.Instance.SetProperty<object>(
                        mObjectsWatching[objectToWatch], member.MemberName, currentValue);

                }
            }
             

            return listToReturn;
        }

        public virtual void UpdateWatchedObject(T objectToUpdate)
        {
            T instanceToUseAsClone = mObjectsWatching[objectToUpdate];

            
            foreach (TypedMemberBase member in mMembersWatching)
            {
                PropertyInfo propertyInfo = mType.GetProperty(member.MemberName);

                if (propertyInfo != null)
                {
                    object currentValue = propertyInfo.GetValue(objectToUpdate, null);
                    object lastValue = propertyInfo.GetValue(instanceToUseAsClone, null);

                    FlatRedBall.Instructions.Reflection.LateBinder<T>.Instance.SetProperty<object>(
                        instanceToUseAsClone, member.MemberName, currentValue);
                }
                else
                {
                    FieldInfo fieldInfo = mType.GetField(member.MemberName);

                    if (fieldInfo == null)
                    {
                        throw new MemberAccessException("Cannot find member by the name of " + member + " in the UndoManager");
                    }

                    object currentValue = fieldInfo.GetValue(objectToUpdate);
                    object lastValue = fieldInfo.GetValue(mObjectsWatching[objectToUpdate]);

                    FlatRedBall.Instructions.Reflection.LateBinder<T>.Instance.SetProperty<object>(
                        instanceToUseAsClone, member.MemberName, currentValue);
                }
            }
            
        }

        #endregion
    }
}
