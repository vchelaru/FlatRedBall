using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;

namespace UnitTests
{
    [TestFixture]
    public class NamedObjectSaveTests
    {
        EntitySave mEntitySave;
        EntitySave mDerivedEntitySave;

        ScreenSave mScreenSave;


        NamedObjectSave mDerivedNos;


        [TestFixtureSetUp]
        public void Initialize()
        {
            OverallInitializer.Initialize();

            CreateEntitySaves();

            CreateScreenSaves();


        }

        private void CreateScreenSaves()
        {
            mScreenSave = new ScreenSave();
            mScreenSave.Name = "NamedObjectSaveTestsScreen";
            ObjectFinder.Self.GlueProject.Screens.Add(mScreenSave);

            mDerivedNos = new NamedObjectSave();
            mDerivedNos.SourceType = SourceType.Entity;
            mDerivedNos.SourceClassType = mDerivedEntitySave.Name;
            mScreenSave.NamedObjects.Add(mDerivedNos);
        }

        private void CreateEntitySaves()
        {
            mEntitySave = new EntitySave();
            mEntitySave.Name = "NamedObjectSaveTestsEntity";
            mEntitySave.ImplementsIVisible = true;
            mEntitySave.ImplementsIWindow = true;

            CustomVariable customVariable = new CustomVariable();
            customVariable.Type = "float";
            customVariable.Name = "X";

            mEntitySave.CustomVariables.Add(customVariable);

            customVariable = new CustomVariable();
            customVariable.Type = "float";
            customVariable.Name = "Y";
            customVariable.SetByDerived = true;
            mEntitySave.CustomVariables.Add(customVariable);

            StateSave stateSave = new StateSave();
            stateSave.Name = "TestState";
            mEntitySave.States.Add(stateSave);

            StateSaveCategory stateSaveCategory = new StateSaveCategory();
            stateSaveCategory.Name = "TestCategory";
            mEntitySave.StateCategoryList.Add(stateSaveCategory);

            StateSave categorizedState = new StateSave();
            categorizedState.Name = "CategorizedState";
            stateSaveCategory.States.Add(categorizedState);
            
            
            
            
            ObjectFinder.Self.GlueProject.Entities.Add(mEntitySave);


            mDerivedEntitySave = new EntitySave();
            mDerivedEntitySave.BaseEntity = mEntitySave.Name;
            mDerivedEntitySave.Name = "NamedObjectSaveTestDerivedEntity";
            ObjectFinder.Self.GlueProject.Entities.Add(mDerivedEntitySave);
        }

        [Test]
        public void TestUpdateNamedObjectProperties()
        {
            mDerivedEntitySave.UpdateFromBaseType();

            // Test to make sure this Entity has inherited the Y variable
            if (mDerivedEntitySave.CustomVariables.Count != 1)
            {
                throw new Exception("Derived Entities are not properly inheriting SetByDerived values");
            }


            mDerivedNos.UpdateCustomProperties();

            int numberOfProperties = mDerivedNos.TypedMembers.Count;

            // It turns out it's okay for NOSs to have null InstructionSaves
            // This makes XML much smaller and makes Glue more efficient in general.
            //if (numberOfProperties == 0)
            //{
            //    throw new Exception("NamedObjectSaves that are not getting their InstructionSaves properly set");
            //}

            if (mDerivedEntitySave.GetTypedMembers().Count != 2)
            {
                throw new Exception("There are an invalid number of typed members for NamedObjectSaves which use Entities that have inheritance");
            }

            // Let's switch the derived NOS to the underived, update its properties, then switch it to the derived and see if there are any problems - there shouldn't be
            mDerivedNos.SourceClassType = mEntitySave.Name;
            mDerivedNos.UpdateCustomProperties();

            mDerivedNos.SourceClassType = mDerivedEntitySave.Name;
            string message = mDerivedNos.GetMessageWhySwitchMightCauseProblems(mEntitySave.Name);

            if (!string.IsNullOrEmpty(message))
            {
                throw new Exception("GetMessageWhySwitchMightCauseProblems is incorrectly reporting problems.  Reported problems:\n" + message);
                 
            }

        }

        [Test]
        public void TestSetPropertyValue()
        {
            NamedObjectSave nos = new NamedObjectSave();
            nos.SourceType = SourceType.FlatRedBallType;
            nos.SourceClassType = "Sprite";

            nos.UpdateCustomProperties();
            nos.SetPropertyValue("Texture", "redball");

            if (nos.InstructionSaves.Count == 0)
            {
                throw new Exception("There should be a Texture instruction save");
            }

            if (nos.InstructionSaves.First(instruction => instruction.Member == "Texture").Type != "Texture2D")
            {
                throw new Exception("The instruction should be of type Texture2D, but it's not");
            }

        }

        [Test]
        public void TestInheritance()
        {
            NamedObjectSave baseListNos = new NamedObjectSave();
            baseListNos.SourceType = SourceType.FlatRedBallType;
            baseListNos.SourceClassType = "PositionedObjectList";
            baseListNos.SourceClassGenericType = mEntitySave.Name;

            NamedObjectSave derivedNos = new NamedObjectSave();
            derivedNos.SourceType = SourceType.Entity;
            derivedNos.SourceClassType = mDerivedEntitySave.Name;

            if (derivedNos.CanBeInList(baseListNos) == false)
            {
                throw new Exception("CanBeInList doesn't properly follow inheritance");
            }
        }
    }
}
