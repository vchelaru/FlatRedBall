using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.SaveClasses.Helpers;
using FlatRedBall.Glue.SetVariable;
using EditorObjects.IoC;

namespace UnitTests
{
    [TestFixture]
    class StateTests
    {
        #region Fields

        EntitySave mEntitySave;
        EntitySave mDerivedEntitySave;

        EntitySave mContainerEntitySave;
        EntitySave mDerivedContainerEntitySave;

        NamedObjectSave mEntitySaveInstance;
        NamedObjectSave mDerivedSaveInstance;

        CustomVariable mExposedStateVariable;
        CustomVariable mExposedStateInCategoryVariable;
        CustomVariable mRenamedExposedUncategorizedStateVariable;
        CustomVariable mRenamedExposedCategorizedStateVariable;

        CustomVariable mTunneledUncategorizedStateInContainer;

        #endregion

        [TestFixtureSetUp]
        public void Initialize()
        {
            OverallInitializer.Initialize();

            CreateEntitySave();

            mDerivedEntitySave = new EntitySave();
            mDerivedEntitySave.Name = "StateTestDerivedEntity";
            mDerivedEntitySave.BaseEntity = mEntitySave.Name;
            mDerivedEntitySave.UpdateFromBaseType();
            ObjectFinder.Self.GlueProject.Entities.Add(mDerivedEntitySave);

            CreateContainerEntitySave();

            CreateDerivedContainerEntitySave();

            Container.Set(new StateSaveSetVariableLogic());
        }

        private void CreateDerivedContainerEntitySave()
        {
            mDerivedContainerEntitySave = new EntitySave();
            mDerivedContainerEntitySave.Name = "StateDerivedContainerEntitySave";
        }

        private void CreateContainerEntitySave()
        {
            mEntitySaveInstance = new NamedObjectSave();
            mEntitySaveInstance.InstanceName = "StateEntityInstance";
            mEntitySaveInstance.SourceType = SourceType.Entity;
            mEntitySaveInstance.SourceClassType = mEntitySave.Name;

            mDerivedSaveInstance = new NamedObjectSave();
            mDerivedSaveInstance.InstanceName = "StateDerivedEntityInstance";
            mDerivedSaveInstance.SourceType = SourceType.Entity;
            mDerivedSaveInstance.SourceClassType = mDerivedEntitySave.Name;

            mContainerEntitySave = new EntitySave();
            mContainerEntitySave.Name = "StateEntityContainer";

            mContainerEntitySave.NamedObjects.Add(mEntitySaveInstance);
            mContainerEntitySave.NamedObjects.Add(mDerivedSaveInstance);


            mTunneledUncategorizedStateInContainer = new CustomVariable();
            mTunneledUncategorizedStateInContainer.Name = "TunneledUncategorizedStateVariable";
            mTunneledUncategorizedStateInContainer.SourceObject = mEntitySaveInstance.InstanceName;
            mTunneledUncategorizedStateInContainer.SourceObjectProperty = mRenamedExposedUncategorizedStateVariable.Name;
            mContainerEntitySave.CustomVariables.Add(mTunneledUncategorizedStateInContainer);


            ObjectFinder.Self.GlueProject.Entities.Add(mContainerEntitySave);

            
        }

        private void CreateEntitySave()
        {
            mEntitySave = ExposedVariableTests.CreateEntitySaveWithStates("StateEntity");
            ObjectFinder.Self.GlueProject.Entities.Add(mEntitySave);

            mExposedStateVariable = new CustomVariable();
            mExposedStateVariable.Name = "CurrentState";
            mExposedStateVariable.Type = "VariableState";
            mExposedStateVariable.SetByDerived = true;
            mEntitySave.CustomVariables.Add(mExposedStateVariable);

            mExposedStateInCategoryVariable = new CustomVariable();
            mExposedStateInCategoryVariable.Name = "CurrentStateCategoryState";
            mExposedStateInCategoryVariable.Type = "StateCategory";
            mExposedStateInCategoryVariable.SetByDerived = true;
            mEntitySave.CustomVariables.Add(mExposedStateInCategoryVariable);

            mRenamedExposedUncategorizedStateVariable = new CustomVariable();
            mRenamedExposedUncategorizedStateVariable.Name = "RenamedVariable";
            mRenamedExposedUncategorizedStateVariable.Type = "VariableState";
            mRenamedExposedUncategorizedStateVariable.SetByDerived = true;
            mEntitySave.CustomVariables.Add(mRenamedExposedUncategorizedStateVariable);

            mRenamedExposedCategorizedStateVariable = new CustomVariable();
            mRenamedExposedCategorizedStateVariable.Name = "RenamedCategorizedVariable";
            mRenamedExposedCategorizedStateVariable.Type = "StateCategory";
            mEntitySave.CustomVariables.Add(mRenamedExposedCategorizedStateVariable);

            // Let's add some states now
            StateSave stateSave = new StateSave();
            stateSave.Name = "FirstState";
            mEntitySave.States.Add(stateSave);

            stateSave = new StateSave();
            stateSave.Name = "SecondState";
            mEntitySave.States.Add(stateSave);

            StateSaveCategory category = new StateSaveCategory();
            category.Name = "SharedVariableCategory";
            mEntitySave.StateCategoryList.Add(category);

            stateSave = new StateSave();
            stateSave.Name = "SharedStateSave";
            category.States.Add(stateSave);

        }

        [Test]
        public void Test()
        {
            AvailableStates availableStates = new AvailableStates(
                null,
                mEntitySave,
                mExposedStateInCategoryVariable,
                null);


            List<string> listToFill = new List<string>();



            availableStates.CurrentCustomVariable = mExposedStateVariable;
            listToFill.Clear();
            availableStates.GetListOfStates(listToFill, null);
            if (listToFill.Count != NumberOfUncategorizedAndSharedStates(mEntitySave) + 1 || listToFill[1] != "Uncategorized")
            {
                throw new Exception("GetListOfStates isn't properly filtering out categorized states");
            }



            listToFill.Clear();
            availableStates.CurrentCustomVariable = mExposedStateInCategoryVariable;
            availableStates.GetListOfStates(listToFill, null);

            if(listToFill.Count != 2 || listToFill[1] != "StateInCategory1")
            {
                throw new Exception("GetListOfStates isn't properly filtering out uncategorized states");
            }

            // Test getting states for variables that don't use the "CurrentWhatever" naming in categories
            listToFill.Clear();
            availableStates.CurrentElement = mContainerEntitySave;
            availableStates.CurrentNamedObject = mEntitySaveInstance;
            availableStates.CurrentCustomVariable = null;
            availableStates.GetListOfStates(listToFill, mRenamedExposedCategorizedStateVariable.Name);

            if(listToFill.Contains("StateInCategory1") == false)
            {
                throw new Exception("GetListOfStates doesn't work properly on states that are categorized and have variables that don't follow the typical CurrentWhatever naming.");
            }

            // Test getting states for variables that don't use the "CurrentWhatever" naming in categories, and are accessed through inheritance

            listToFill.Clear();
            availableStates.CurrentElement = mContainerEntitySave;
            availableStates.CurrentNamedObject = mDerivedSaveInstance;
            availableStates.CurrentCustomVariable = null;
            availableStates.GetListOfStates(listToFill, mRenamedExposedUncategorizedStateVariable.Name);
            if (listToFill.Count != mEntitySave.States.Count + 1)
            {
                throw new Exception("GetListOfStates on NOS's that are derived doesn't seem to work properly");
            }


            listToFill.Clear();
            availableStates.CurrentElement = mContainerEntitySave;
            availableStates.CurrentCustomVariable = null;
            availableStates.CurrentNamedObject = mEntitySaveInstance;
            availableStates.GetListOfStates(listToFill, "CurrentState");
            if(listToFill.Count != NumberOfUncategorizedAndSharedStates(mEntitySave) + 1 || listToFill[1] != "Uncategorized") // will include "<NONE>"
            {
                throw new Exception("GetListOfStates isn't properly filtering out categorized states");
            }

            // Test getting states for a variable that doesn't use the typical "CurrentWhatever" naming on uncategorized
            listToFill.Clear();
            availableStates.CurrentElement = mContainerEntitySave;
            availableStates.CurrentCustomVariable = null;
            availableStates.CurrentNamedObject = mEntitySaveInstance;
            availableStates.GetListOfStates(listToFill, mRenamedExposedUncategorizedStateVariable.Name);
            if (listToFill.Count != NumberOfUncategorizedAndSharedStates(mEntitySave) + 1 || listToFill[1] != "Uncategorized") // will include "<NONE>"
            {
                throw new Exception("GetListOfStates isn't properly filtering out categorized states");
            }

            // Test getting states for a tunneled variable that doesn't use the typical "CurrentWhatever" naming on uncategorized
            listToFill.Clear();
            availableStates.CurrentElement = mContainerEntitySave;
            availableStates.CurrentCustomVariable = mTunneledUncategorizedStateInContainer;
            availableStates.CurrentNamedObject = null;
            availableStates.GetListOfStates(listToFill, mRenamedExposedUncategorizedStateVariable.Name);
            if (listToFill.Count != NumberOfUncategorizedAndSharedStates(mEntitySave) + 1 || listToFill[1] != "Uncategorized") // will include "<NONE>"
            {
                throw new Exception("GetListOfStates isn't properly filtering out categorized states");
            }



            listToFill.Clear();
            availableStates.CurrentElement = mContainerEntitySave;
            availableStates.CurrentNamedObject = mEntitySaveInstance;
            availableStates.CurrentCustomVariable = null;

            availableStates.GetListOfStates(listToFill, "CurrentStateCategoryState");
            if(listToFill.Count != 2 || listToFill[1] != "StateInCategory1")
            {
                throw new Exception("GetListOfStates isn't properly filtering out uncategorized states");
            }


            string whyItIsntValid;
            if (NameVerifier.IsStateNameValid("Color", null, null, null, out whyItIsntValid))
            {
                throw new Exception("The state name Color should not be a valid name, but Glue allows it");
            }


            listToFill.Clear();
            availableStates.CurrentElement = mDerivedEntitySave;
            availableStates.CurrentCustomVariable = mDerivedEntitySave.CustomVariables[0];
            availableStates.CurrentNamedObject = null;
            availableStates.GetListOfStates(listToFill, mDerivedEntitySave.CustomVariables[0].Name);

            if (listToFill.Count == 0 || listToFill[1] != "Uncategorized")
            {
                throw new Exception("GetListOfStates is not properly finding uncategorized states defined in a base type");
            }

            listToFill.Clear();
            availableStates.CurrentElement = mDerivedEntitySave;
            availableStates.CurrentCustomVariable = mDerivedEntitySave.CustomVariables[1];
            availableStates.CurrentNamedObject = null;
            availableStates.GetListOfStates(listToFill, mDerivedEntitySave.CustomVariables[1].Name);

            if (listToFill.Count == 0 || listToFill[1] != "StateInCategory1")
            {
                throw new Exception("GetListOfStates is not properly finding categorized states defined in a base type");
            }

            // Test CurrentState variable tate in the Container
            listToFill.Clear();
            availableStates.CurrentElement = mContainerEntitySave;
            availableStates.CurrentCustomVariable = null;
            availableStates.CurrentStateSave = null;
            availableStates.CurrentNamedObject = mDerivedSaveInstance;
            availableStates.GetListOfStates(listToFill, "CurrentState");

            if (listToFill.Count != mEntitySave.States.Count + 1)
            {
                throw new Exception("Getting state on NamedObject that is of a derived type that gets its state from the base type is not working properly");

            }

            ////Test adding same name with shared category
            
            //Test shared vs shared
            var sharedCategoryElement = new EntitySave();
            string outString;
            sharedCategoryElement.StateCategoryList.Add(new StateSaveCategory{Name = "First", SharesVariablesWithOtherCategories = true});
            sharedCategoryElement.StateCategoryList[0].States.Add(new StateSave{Name = "State1"});
            sharedCategoryElement.StateCategoryList.Add(new StateSaveCategory{Name = "Second", SharesVariablesWithOtherCategories = true});
            if(NameVerifier.IsStateNameValid("State1", sharedCategoryElement, sharedCategoryElement.StateCategoryList[1], null,
                                          out outString))
            {
                throw new Exception("Should not allow adding same state name between shared categories.");
            }

            //Test shared vs main
            sharedCategoryElement = new EntitySave();
            sharedCategoryElement.StateCategoryList.Add(new StateSaveCategory { Name = "First", SharesVariablesWithOtherCategories = true });
            sharedCategoryElement.StateCategoryList[0].States.Add(new StateSave { Name = "State1" });
            if (NameVerifier.IsStateNameValid("State1", sharedCategoryElement, null, null,
                                          out outString))
            {
                throw new Exception("Should not allow adding same state name in main when exists in shared categories.");
            }

            //Test main vs shared
            sharedCategoryElement = new EntitySave();
            sharedCategoryElement.States.Add(new StateSave{Name = "State1"});
            sharedCategoryElement.StateCategoryList.Add(new StateSaveCategory { Name = "First", SharesVariablesWithOtherCategories = true });
            if (NameVerifier.IsStateNameValid("State1", sharedCategoryElement, sharedCategoryElement.StateCategoryList[0], null,
                                          out outString))
            {
                throw new Exception("Should not allow adding same state name in shared category when exists in main states.");
            }
        }

        int NumberOfUncategorizedAndSharedStates(IElement element)
        {
            int count = element.States.Count;

            foreach (var category in element.StateCategoryList.Where(x=>x.SharesVariablesWithOtherCategories))
            {
                count += category.States.Count;
            }

            return count;
        }

        [Test]
        public void TestCategories()
        {
            List<string> listToFill = new List<string>();

            AvailableStates availableStates = new AvailableStates(
                null,
                mEntitySave,
                mExposedStateVariable,
                null);


            listToFill.Clear();
            availableStates.GetListOfStates(listToFill, mDerivedEntitySave.CustomVariables[0].Name);

            if (listToFill.Contains("SharedStateSave") == false)
            {
                throw new Exception("GetListOfStates is not returnign states that are categorized but that share variables with others.");
            }


        }

        [Test]
        public void TestRenaming()
        {
            string oldName = "BeforeRename";
            string newName = "AfterRename";

            EntitySave entitySave = new EntitySave();
            entitySave.Name = "StateSaveTestRenamingEntity";
            ObjectFinder.Self.GlueProject.Entities.Add(entitySave);


            StateSave stateSave = new StateSave();
            stateSave.Name = oldName;
            entitySave.States.Add(stateSave);

            CustomVariable customVariable = new CustomVariable();
            customVariable.Type = "VariableState";
            customVariable.DefaultValue = oldName;
            entitySave.CustomVariables.Add(customVariable);

            EntitySave containerEntity = new EntitySave();
            containerEntity.Name = "StateSaveTestRenamingDerivedEntity";
            ObjectFinder.Self.GlueProject.Entities.Insert(0, containerEntity);


            NamedObjectSave nos = new NamedObjectSave();
            nos.SourceType = SourceType.Entity;
            nos.SourceClassType = entitySave.Name;
            nos.CurrentState = stateSave.Name;

            containerEntity.NamedObjects.Add(nos);

            stateSave.Name = newName;

            bool throwAway = true;
            Container.Get<StateSaveSetVariableLogic>().ReactToStateSaveChangedValue(stateSave, null, "Name", oldName, entitySave, ref throwAway);

            if ((string)customVariable.DefaultValue == oldName)
            {
                throw new Exception("Renaming a state doesn't change the default value for variables that reference it.");
            }

            if (nos.CurrentState == oldName)
            {
                throw new Exception("Renaming a state doesn't change the CurrentState for named objects that use it as the default state");
            }
        }


    }
}
