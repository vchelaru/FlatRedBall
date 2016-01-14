using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.GuiDisplay;

namespace UnitTests
{
    [TestFixture]
    class ExposedVariableTests
    {
        EntitySave mEntitySave;
        EntitySave mDerivedEntitySave;

        EntitySave mContainerBaseEntity;
        EntitySave mContainerDerivedEntity;

        EntitySave mEntityWithCategorizedThatShareVariables;

        EntitySave mCsvContainerEntitySave;

        

        [TestFixtureSetUp]
        public void Initialize()
        {
            OverallInitializer.Initialize();

            ExposedVariableManager.Initialize();
            mEntitySave = CreateEntitySaveWithStates("ExposedVariableEntity");
            mEntitySave.ImplementsIVisible = true;
            ObjectFinder.Self.GlueProject.Entities.Add(mEntitySave);

            mDerivedEntitySave = new EntitySave();
            mDerivedEntitySave.BaseEntity = mEntitySave.Name;
            mDerivedEntitySave.Name = "DerivedExposedVariableEntity";
            ObjectFinder.Self.GlueProject.Entities.Add(mDerivedEntitySave);

            mEntityWithCategorizedThatShareVariables = new EntitySave();
            mEntityWithCategorizedThatShareVariables.Name = "ExposedVariableTestEntityWithCategorizedThatShareVariables";
            ObjectFinder.Self.GlueProject.Entities.Add(mEntityWithCategorizedThatShareVariables);
            StateSaveCategory category = new StateSaveCategory();
            category.SharesVariablesWithOtherCategories = true; // this is important - it means that it won't make a new enum or property, so it is just the "CurrentState" variable
            category.Name = "Category1";
            mEntityWithCategorizedThatShareVariables.StateCategoryList.Add(category);
            StateSave stateSave = new StateSave();
            stateSave.Name = "CategorizedState1";
            category.States.Add(stateSave);

            mContainerBaseEntity = new EntitySave();
            mContainerBaseEntity.Name = "ExposedVariableTestContainerBaseEntity";
            ObjectFinder.Self.GlueProject.Entities.Add(mContainerBaseEntity);
            NamedObjectSave namedObjectSave = new NamedObjectSave();
            namedObjectSave.InstanceName = mEntitySave.Name + "Instance";
            namedObjectSave.SourceType = SourceType.Entity;
            namedObjectSave.SourceClassType = mEntitySave.Name;
            mContainerBaseEntity.NamedObjects.Add(namedObjectSave);
            CustomVariable tunneledVariable = new CustomVariable();
            tunneledVariable.Name = "TunneledStateVariable";
            tunneledVariable.SourceObject = namedObjectSave.InstanceName;
            tunneledVariable.SourceObjectProperty = "Current" + mEntitySave.StateCategoryList[0].Name + "State";
            tunneledVariable.Type = mEntitySave.StateCategoryList[0].Name;
            tunneledVariable.SetByDerived = true;
            mContainerBaseEntity.CustomVariables.Add(tunneledVariable);

            mContainerDerivedEntity = new EntitySave();
            mContainerDerivedEntity.Name = "ExposedVariableTestContainerDerivedEntity";
            ObjectFinder.Self.GlueProject.Entities.Add(mContainerDerivedEntity);
            mContainerDerivedEntity.BaseEntity = mContainerBaseEntity.Name;
            mContainerDerivedEntity.UpdateFromBaseType();
            mContainerDerivedEntity.GetCustomVariable(tunneledVariable.Name).DefaultValue = mEntitySave.StateCategoryList[0].States[0].Name;

            CreateCsvContainerEntitySave();
        }

        private void CreateCsvContainerEntitySave()
        {
            mCsvContainerEntitySave = new EntitySave();
            mCsvContainerEntitySave.Name = "CsvContainerEntityInExposedVariableTests";
            ObjectFinder.Self.GlueProject.Entities.Add(mCsvContainerEntitySave);

            ReferencedFileSave rfs = new ReferencedFileSave();
            rfs.SourceFile = "Whatever.csv";
            rfs.Name = "Whatever.csv";
            mCsvContainerEntitySave.ReferencedFiles.Add(rfs);
        }

        [Test]
        public void TestExposingStates()
        {
            List<string> variables = ExposedVariableManager.GetExposableMembersFor(mEntitySave, false).Select(m=>m.Member).ToList();

            if (!variables.Contains("CurrentState"))
            {
                throw new Exception("ExposedVariableManager is not properly returning the CurrentState as an exposable variable");
            }
            if (!variables.Contains("CurrentStateCategoryState"))
            {
                throw new Exception("ExposedVariableManager is not properly returning categorized states as exposable variables");
            }

            // Let's remove uncategorized state to make sure the categorized state is still recognized:
            StateSave stateSave = mEntitySave.States[0];
            mEntitySave.States.RemoveAt(0);
            variables = ExposedVariableManager.GetExposableMembersFor(mEntitySave, false).Select(m=>m.Member).ToList();

            if (!variables.Contains("CurrentStateCategoryState"))
            {
                throw new Exception("ExposedVariableManager is not properly returning categorized states when there are no uncategorized states.");
            }
            // Add it back in case it's needed for other tests.
            mEntitySave.States.Add(stateSave);

            variables = ExposedVariableManager.GetExposableMembersFor(mEntityWithCategorizedThatShareVariables, false).Select(m => m.Member).ToList();
            if (!variables.Contains("CurrentState"))
            {
                throw new Exception("Entities that only have states in categories, but those categories share variables with other categories, are not exposing CurrentState and they should!");
            }

            List<string> listOfStates = new List<string>();
            AvailableStates availableStates = new AvailableStates(null, mContainerDerivedEntity, mContainerDerivedEntity.CustomVariables[0], null);
            availableStates.GetListOfStates(listOfStates, "TunneledStateVariable");
            if (listOfStates.Count == 0 || !listOfStates.Contains("StateInCategory1"))
            {
                throw new Exception("SetByDerived variables that tunnel in to categorized states do not properly return their list through GetListOfStates");
            }


            ScreenSave screenSave = new ScreenSave();
            StateSaveCategory category = new StateSaveCategory();
            category.Name = "Whatever";
            screenSave.StateCategoryList.Add(category);
            StateSave stateInScreen = new StateSave();
            stateInScreen.Name = "First";
            category.States.Add(stateInScreen);
            variables = ExposedVariableManager.GetExposableMembersFor(screenSave, false).Select(item=>item.Member).ToList();

            if (variables.Contains("CurrentState") == false)
            {
                throw new NotImplementedException("Screens with states that are in categories that share variables are not properly returning the CurrentState as a possible variable");
            }
        }

        [Test]
        public void Test()
        {
            string memberType = ExposedVariableManager.GetMemberTypeForEntity("Current" + mEntitySave.StateCategoryList[0].Name + "State",
                mEntitySave);

            if (memberType != mEntitySave.StateCategoryList[0].Name)
            {
                throw new Exception("ExposedVariableManager.GetMemberTypeForEntity didn't return the proper type");
            }


            

            // The base should be able to expose Visible because it's IVisible
            List<string> variables = variables = 
                ExposedVariableManager.GetExposableMembersFor(mEntitySave, false).Select(item=>item.Member).ToList();
            if (!variables.Contains("Visible"))
            {
                throw new Exception("The base Entity that is IVisible does not have the Visible property as an exposable variable and it should");
            }

            // The derived should be able to expose Visible because its base is IVislble
            variables = 
                ExposedVariableManager.GetExposableMembersFor(mDerivedEntitySave, false).Select(item=>item.Member).ToList();
            if (!variables.Contains("Visible"))
            {
                throw new Exception("Derived Entities are not showing Visible as an exposable variable when the base implements IVislble");

            }
        }

        [Test]
        public void TextExposingCsvTypes()
        {
            List<string> availableNewTypeVariables = ExposedVariableManager.GetAvailableNewVariableTypes();

            if (!availableNewTypeVariables.Contains(mCsvContainerEntitySave.ReferencedFiles[0].Name))
            {
                throw new Exception("The type from CSV " + mCsvContainerEntitySave.ReferencedFiles[0].Name + " was not returned as a possible new variable type and it should be");
            }

        }


        public static EntitySave CreateEntitySaveWithStates(string name)
        {
            EntitySave entitySave = new EntitySave();
            entitySave.Name = name;



            StateSaveCategory category = new StateSaveCategory();
            category.Name = "StateCategory";
            StateSave stateSave = new StateSave();
            stateSave.Name = "StateInCategory1";
            category.SharesVariablesWithOtherCategories = false;
            category.States.Add(stateSave);

            entitySave.StateCategoryList.Add(category);

            StateSave uncategoried = new StateSave();
            uncategoried.Name = "Uncategorized";
            entitySave.States.Add(uncategoried);




            return entitySave;
        }
    }
}
