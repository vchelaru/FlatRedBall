using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SaveClasses.Helpers;
using FlatRedBall.Glue.SetVariable;
using EditorObjects.IoC;

namespace UnitTests
{

    [TestFixture]
    class StateSaveCategoryTests
    {
        EntitySave mEntitySave;
        StateSaveCategory mCategory;

        StateSaveCategory mCategory2;


        CustomVariable mCustomVariable;

        [TestFixtureSetUp]
        public void Initialize()
        {
            Container.Set(new StateSaveCategorySetVariableLogic());

            mEntitySave = new EntitySave();

            mCategory = new StateSaveCategory();
            mCategory.Name = "Category1";
            mCategory.SharesVariablesWithOtherCategories = true;
            mEntitySave.StateCategoryList.Add(mCategory);

            mCategory2 = new StateSaveCategory();
            mCategory2.Name = "Category2";
            mCategory2.SharesVariablesWithOtherCategories = true;
            mEntitySave.StateCategoryList.Add(mCategory);

            StateSave state1 = new StateSave();
            state1.Name = "State1";
            mCategory.States.Add(state1);

            StateSave stateInCategory2 = new StateSave();
            stateInCategory2.Name = "State1InCategory2";
            mCategory2.States.Add(stateInCategory2);


            mCustomVariable = new CustomVariable();
            mCustomVariable.Name = "CurrentState";
            mCustomVariable.Type = "VariableState";
            mCustomVariable.DefaultValue = "State1";
            mEntitySave.CustomVariables.Add(mCustomVariable);

             //need to test the case where a variable shouldn't be changed because it's not part of the category that got changed.
             //Also, need to handle a case where the variable becomes an uncategorized variable, but there alread is one...do we allow it?
        }


        [Test]
        public void TestSharesVariables()
        {
            bool throwAway = false;
            mCategory.SharesVariablesWithOtherCategories = false;
            Container.Get < StateSaveCategorySetVariableLogic>().ReactToStateSaveCategoryChangedValue(
                mCategory, "SharesVariablesWithOtherCategories", true, mEntitySave, ref throwAway);
            if (mCustomVariable.Type != mCategory.Name)
            {
                throw new Exception("Changing SharesVariablesWithOtherCategories is not changin the type of variables");
            }


            mCategory.SharesVariablesWithOtherCategories = true;
            Container.Get<StateSaveCategorySetVariableLogic>().ReactToStateSaveCategoryChangedValue(
                mCategory, "SharesVariablesWithOtherCategories", false, mEntitySave, ref throwAway);
            if (mCustomVariable.Type != "VariableState")
            {
                throw new Exception("Changing SharesVariablesWithOtherCategories is not changin the type of variables");
            }




            mCategory2.SharesVariablesWithOtherCategories = false;
            Container.Get<StateSaveCategorySetVariableLogic>().ReactToStateSaveCategoryChangedValue(
                mCategory2, "SharesVariablesWithOtherCategories", true, mEntitySave, ref throwAway);
            if (mCustomVariable.Type != "VariableState")
            {
                throw new Exception("The variable should not have changed in response to this change");
            }


            mCategory2.SharesVariablesWithOtherCategories = true;
            Container.Get<StateSaveCategorySetVariableLogic>().ReactToStateSaveCategoryChangedValue(
                mCategory2, "SharesVariablesWithOtherCategories", false, mEntitySave, ref throwAway);
            if (mCustomVariable.Type != "VariableState")
            {
                throw new Exception("The variable should not have changed in response to this change");
            }
        }

        // This is handled in the NameVerifierTests class
        //[Test]
        //public void TestInvalidNaming()
        //{



        //}
    }
}
