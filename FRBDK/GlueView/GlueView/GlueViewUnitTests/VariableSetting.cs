using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall.Glue;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.Elements;

namespace GlueViewUnitTests
{
    [TestFixture]
    public class VariableSetting
    {
        bool mRaiseExceptionOnVariable = false;

        ScreenSave mScreenSave;

        EntitySave mEntitySave;
        EntitySave mDerivedEntitySave;

        ElementRuntime mElementRuntime;
        ElementRuntime mDerivedElementRuntime;
        ElementRuntime mContainedElementRuntime;

        [TestFixtureSetUp]
        public void Initialize()
        {
            OverallInitializer.Initialize();


            CreateElementRuntime();

            CreateDerivedElementRuntime();

            CreateContainerElementRuntime();

            CreateScreenSave();

        }

        private void CreateScreenSave()
        {
            mScreenSave = new ScreenSave();
            mScreenSave.Name = "ScreenSaveInVariableSettingTest";
            ObjectFinder.Self.GlueProject.Screens.Add(mScreenSave);

            NamedObjectSave nos = new NamedObjectSave();
            nos.SourceType = SourceType.Entity;
            nos.SourceClassType = mEntitySave.Name;
            nos.UpdateCustomProperties();
            nos.InstanceName = "TestObject1";
            nos.SetPropertyValue("X", 150);

            mScreenSave.NamedObjects.Add(nos);
        }

        private void CreateDerivedElementRuntime()
        {
            mDerivedEntitySave = new EntitySave();
            mDerivedEntitySave.Name = "DerivedVariableSettingEntity";
            mDerivedEntitySave.BaseEntity = mEntitySave.Name;
            ObjectFinder.Self.GlueProject.Entities.Add(mDerivedEntitySave);
            mDerivedEntitySave.UpdateFromBaseType();
            mDerivedEntitySave.GetCustomVariable("CurrentState").DefaultValue = "Uncategorized";


            mDerivedElementRuntime = new ElementRuntime(mDerivedEntitySave, null, null, null, null);


        }

        void CreateElementRuntime()
        {
            mEntitySave = new EntitySave { Name = "VariableSettingEntity" };

            ObjectFinder.Self.GlueProject.Entities.Add(mEntitySave);
            
            var xVariable = new CustomVariable
            {
                Name = "X",
                Type = "float",
                DefaultValue = 0
            };
            mEntitySave.CustomVariables.Add(xVariable);

            // Needs the Z variable exposed so that the uncategorized state below can use it:
            var zVariable = new CustomVariable
            {
                Name = "Z",
                Type = "float",
                DefaultValue = 0
            };
            mEntitySave.CustomVariables.Add(zVariable);

            CustomVariable stateVariable = new CustomVariable
            {
                Name = "CurrentStateSaveCategoryState",
                Type = "StateSaveCategory",
                DefaultValue = "FirstState"
            };
            stateVariable.SetByDerived = true;
            mEntitySave.CustomVariables.Add(stateVariable);


            CustomVariable uncategorizedStateVariable = new CustomVariable
            {
                Name = "CurrentState",
                Type = "VariableState",
                //DefaultValue = "Uncategorized", // Don't set a default value - the derived will do this
                SetByDerived = true
            };
            mEntitySave.CustomVariables.Add(uncategorizedStateVariable);




            StateSave uncategorized = new StateSave();
            uncategorized.Name = "Uncategorized";
            InstructionSave instruction = new InstructionSave();
            instruction.Member = "Z";
            instruction.Value = 8.0f;
            uncategorized.InstructionSaves.Add(instruction);
            mEntitySave.States.Add(uncategorized);
            
            StateSave stateSave = new StateSave();
            stateSave.Name = "FirstState";
            instruction = new InstructionSave();
            instruction.Member = "X";
            instruction.Value = 10;
            stateSave.InstructionSaves.Add(instruction);

            StateSave secondStateSave = new StateSave();
            secondStateSave.Name = "SecondState";
            instruction = new InstructionSave();
            instruction.Member = "X";
            instruction.Value = -10;
            secondStateSave.InstructionSaves.Add(instruction);


            StateSaveCategory category = new StateSaveCategory();
            category.Name = "StateSaveCategory";
            category.States.Add(stateSave);
            category.States.Add(secondStateSave);

            mEntitySave.StateCategoryList.Add(category);
            mElementRuntime = new ElementRuntime(mEntitySave, null, null, null, null);
            mElementRuntime.AfterVariableApply += AfterVariableSet;



        }

        void CreateContainerElementRuntime()
        {

            EntitySave containerEntitySave = new EntitySave { Name = "ContainerVariableSetting" };
            ObjectFinder.Self.GlueProject.Entities.Add(containerEntitySave);
            NamedObjectSave nos = new NamedObjectSave();
            nos.SourceType = SourceType.Entity;
            nos.InstanceName = mEntitySave.Name + "Instance";
            nos.SourceClassType = mEntitySave.Name;
            containerEntitySave.NamedObjects.Add(nos);

            nos.UpdateCustomProperties();
            nos.SetPropertyValue("CurrentStateSaveCategoryState", "SecondState");

            mContainedElementRuntime = new ElementRuntime(containerEntitySave, null, null, null, null);

            // This thing is attached - we need to check its relativeX
            //if (mContainedElementRuntime.ContainedElements[0].X != -10.0f)
            if (mContainedElementRuntime.ContainedElements[0].RelativeX != -10.0f)
            {
                throw new Exception("Categorized states on contained NamedObjectSave Elements aren't setting values properly");
            }
        }

        [Test]
        public void TestVariableSettingUsingStatesFromBase()
        {
            // The variable should have been set when the ElementRuntime was instantiated, so let's
            // test that:
            if (mDerivedElementRuntime.Z != (float)mEntitySave.GetState("Uncategorized").InstructionSaves[0].Value)
            {
                throw new Exception("State setting on variables in derived types that use states from their base types is not working properly");
            }


        }

        [Test]
        public void TestEventRaisingForNull()
        {
            var entitySave = new EntitySave { Name = "EventRaisingEntity" };
            var xVariable = new CustomVariable
            {
                Name = "X",
                Type = "float",
                DefaultValue = ""
            };

            entitySave.CustomVariables.Add(xVariable);
            mElementRuntime = new ElementRuntime(entitySave, null, null, null, null);


            mElementRuntime.AfterVariableApply += AfterVariableSet;
        }

        [Test]
        public void TestSettingVariableCreatedInBAse()
        {
            CustomVariable variable = 
                mDerivedElementRuntime.GetCustomVariable("CurrentStateSaveCategoryState");

            ElementRuntime sourceElement;
            string variableName;
            
            mDerivedElementRuntime.GetSourceElementAndVariableName(
                variable, out sourceElement, out variableName);

            if (sourceElement != mDerivedElementRuntime)
            {
                throw new Exception("GEtSourceElementAndVariableName should be returning the base ElementRuntime as the containing element but it's not.");
            }
            


            mDerivedElementRuntime.SetCustomVariable(variable);



        }

        [Test]
        public void TestScreenSave()
        {
            ElementRuntime er = new ElementRuntime(mScreenSave, null, null, null, null);

            float objectX = er.ContainedElements[0].X;

            if (objectX != 150)
            {
                throw new Exception("variables on named objects in Screens aren't being applied");
            }
        }

        void AfterVariableSet(object sender, VariableSetArgs args)
        {
            int m = 3;
        }

    }
}
