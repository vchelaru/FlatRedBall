using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.SaveClasses.Helpers;

namespace UnitTests
{
    [TestFixture]
    public class CustomVariableTests
    {
        EntitySave mEntitySave;
        EntitySave mContainerEntitySave;
        EntitySave mDerivedEntitySave; // Derives from mEntitySave

        NamedObjectSave mBaseNosInContainer;
        NamedObjectSave mDerivedNosInContainer;

        NamedObjectSave mTextInBase;

        CustomVariable mSetByDerivedVariable;


        CustomVariable mExposedStateInCategoryVariable;

        [TestFixtureSetUp]
        public void Initialize()
        {
            OverallInitializer.Initialize();


            CreateEntitySave();

            CreateDerivedEntitySave();

            CreateContainerEntitySave();



        }

        private void CreateEntitySave()
        {
            mEntitySave = ExposedVariableTests.CreateEntitySaveWithStates("CustomVariableEntity");
            mExposedStateInCategoryVariable = new CustomVariable();
            mExposedStateInCategoryVariable.Name = "CurrentStateCategoryState";
            mExposedStateInCategoryVariable.Type = "StateCategory";
            mExposedStateInCategoryVariable.SetByDerived = true;
            mEntitySave.CustomVariables.Add(mExposedStateInCategoryVariable);

            mSetByDerivedVariable = new CustomVariable();
            mSetByDerivedVariable.Type = "float";
            mSetByDerivedVariable.Name = "SomeVariable";
            mSetByDerivedVariable.SetByDerived = true;
            mEntitySave.CustomVariables.Add(mSetByDerivedVariable);

            mTextInBase = new NamedObjectSave();
            mTextInBase.InstanceName = "TextObject";
            mTextInBase.SourceType = SourceType.FlatRedBallType;
            mTextInBase.SourceClassType = "Text";
            mEntitySave.NamedObjects.Add(mTextInBase);


            CustomVariable customVariable = new CustomVariable();
            customVariable.Name = "TunneledDisplayText";
            customVariable.SourceObject = mTextInBase.InstanceName;
            customVariable.SourceObjectProperty = "DisplayText";
            customVariable.Type = "string";
            customVariable.OverridingPropertyType = "int";
            mEntitySave.CustomVariables.Add(customVariable);


            ObjectFinder.Self.GlueProject.Entities.Add(mEntitySave);
        }

        private void CreateDerivedEntitySave()
        {
            mDerivedEntitySave = new EntitySave();
            mDerivedEntitySave.Name = "DerivedCustomVariableTestsEntity";
            ObjectFinder.Self.GlueProject.Entities.Add(mDerivedEntitySave);

            mDerivedEntitySave.BaseEntity = mEntitySave.Name;
            mDerivedEntitySave.UpdateFromBaseType();
        }

        private void CreateContainerEntitySave()
        {
            mContainerEntitySave = new EntitySave();
            mContainerEntitySave.Name = "ContainerCustomVariableEntity";

            mBaseNosInContainer = new NamedObjectSave();
            mBaseNosInContainer.InstanceName = mEntitySave.Name + "Instance";
            mBaseNosInContainer.SourceType = SourceType.Entity;
            mBaseNosInContainer.SourceClassType = mEntitySave.Name;
            mContainerEntitySave.NamedObjects.Add(mBaseNosInContainer);

            CustomVariable customVariable = new CustomVariable();
            customVariable.Name = "TunneledCategorizedStateVariable";
            customVariable.SourceObject = mBaseNosInContainer.InstanceName;
            customVariable.SourceObjectProperty = "CurrentStateCategoryState";
            customVariable.Type = "StateCategory";
            mContainerEntitySave.CustomVariables.Add(customVariable);

            mDerivedNosInContainer = new NamedObjectSave();
            mDerivedNosInContainer.InstanceName = "DerivedNosInContainer";
            mDerivedNosInContainer.SourceType = SourceType.Entity;
            mDerivedNosInContainer.SourceClassType = mDerivedEntitySave.Name;
            mDerivedNosInContainer.UpdateCustomProperties();
            mContainerEntitySave.NamedObjects.Add(mDerivedNosInContainer);


            ObjectFinder.Self.GlueProject.Entities.Add(mContainerEntitySave);
        }

        [Test]
        public void Test()
        {
            if (!mExposedStateInCategoryVariable.GetIsVariableState())
            {
                throw new Exception("Variables that expose states in categories are not returning IsVariableState as true");

            }

            if (!mContainerEntitySave.CustomVariables[0].GetIsVariableState())
            {
                throw new Exception("Variables that tunnel states in categories are not returning IsVariableState as true");
            }

            if (!mDerivedEntitySave.CustomVariables[0].GetIsVariableState())
            {
                throw new Exception("Variables in derived types that are defined in base which expose states are not returning IsVariableState as true");
            }
        }

        [Test]
        public void TestSetByDerivedVariables()
        {
            TypedMemberBase tmb = GetTypedMemberBase(mDerivedNosInContainer, mSetByDerivedVariable.Name);

            bool isCsv = NamedObjectPropertyGridDisplayer.GetIfIsCsv(
                mDerivedNosInContainer, tmb.MemberName);
            if (isCsv)
            {
                throw new Exception("String veriables are improperly being identified as CSVs");
            }

        }

        [Test]
        public void TestVariableReordering()
        {
            List<NamedObjectSave> list = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(mEntitySave);
            if (list.Count == 0)
            {
                throw new Exception("Should have found some NamedObjects but didn't");
            }

            CustomVariable variable = new CustomVariable();

            variable.Name = "ToBeMoved";
            variable.Type = "float";

            mEntitySave.CustomVariables.Add(variable);

            mBaseNosInContainer.UpdateCustomProperties();

            // Update November 28, 2012
            // It's okay for this to be null
            // now.  What matters is that the 
            // typed members are not null.
            //if (mBaseNosInContainer.GetCustomVariable("ToBeMoved") == null)
            //{
            //    throw new Exception("Could not find the variable ToBeMoved in the base Entity NOS, but it should have it because the derived was given it and then Glue was told to update NOSs accordingly");
            //}
            if (mBaseNosInContainer.TypedMembers.First(member => member.MemberName == "ToBeMoved") == null)
            {
                throw new Exception("Could not find the variable ToBeMoved in the base Entity NOS, but it should have it because the derived was given it and then Glue was told to update NOSs accordingly");

            }
            // Now let's reorder by putting ToBeMoved at the very beginning, then make sure the NOS updates:
            mEntitySave.CustomVariables.Remove(variable);
            mEntitySave.CustomVariables.Insert(0, variable);

            mBaseNosInContainer.UpdateCustomProperties();
            if (mBaseNosInContainer.TypedMembers[0].MemberName != variable.Name)
            {

                throw new Exception("Reordering variables in EntitySaves does not reorder the variables in NOS's that use the EntitySave and it should!");

            }

        }

        [Test]
        public void TestOverridingPropertyTypes()
        {
            string type = ExposedVariableManager.GetMemberTypeForNamedObject(mBaseNosInContainer, "TunneledDisplayText");

            if (type != "int")
            {
                throw new Exception("GetMemberTypeForNamedObject isn't returning the overriding type");
            }

        }

        [Test]
        public void TestInterpolationCharacteristics()
        {
            CustomVariable variable = new CustomVariable();
            variable.Name = "Whatever";
            variable.Type = "int";

            if (CustomVariableHelper.GetInterpolationCharacteristic(variable, null) !=
                InterpolationCharacteristic.NeedsVelocityVariable)
            {
                throw new Exception("int varaibles should be interpolatable if given a velocity variable, but this one says it can't.");
            }

            variable.Name = "X";
            var characteristic = CustomVariableHelper.GetInterpolationCharacteristic(variable, new EntitySave());
            if (characteristic != InterpolationCharacteristic.CanInterpolate)
            {
                throw new Exception("int varaibles should be able to interpolate, but this one says it can't.");
            }

            variable.Name = "Whatever";
            variable.Type = "string";

            if (CustomVariableHelper.GetInterpolationCharacteristic(variable, null) !=
                InterpolationCharacteristic.CantInterpolate)
            {
                throw new Exception("string varaibles should not be able to interpolate, but this one says it can (or might be able to).");
            }



            variable.OverridingPropertyType = "int";

            if (CustomVariableHelper.GetInterpolationCharacteristic(variable, null) ==
                InterpolationCharacteristic.CantInterpolate)
            {
                throw new Exception("OverridingPropertyType variables are not properly setting whether they can interpolate or not");
            }

            
        }

        [Test]
        public void TestStateVariables()
        {
            bool result = CustomVariableHelper.IsStateMissingFor(mExposedStateInCategoryVariable, mEntitySave);

            if (result)
            {
                throw new Exception("This variable does have a state associated with it, so this shouldn't be true");
            }

            // Make a dummy state that should have its state missing
            CustomVariable variable = new CustomVariable();
            variable.Name = "CurrentCategoryThatDoesntExistState";
            variable.Type = "CategoryThatDoesntExist";
            result = CustomVariableHelper.IsStateMissingFor(variable, mEntitySave);
            if (!result)
            {
                throw new Exception("This variable does not have an associated category");
            }
        }

        TypedMemberBase GetTypedMemberBase(NamedObjectSave nos, string name)
        {
            
            foreach (TypedMemberBase typedMemberBase in nos.TypedMembers)
            {
                if (typedMemberBase.MemberName == name)
                {
                    return typedMemberBase;
                }
            }

            return null;
        }

    }
}
