using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Elements;

namespace GlueViewUnitTests
{
    [TestFixture]
    public class CustomVariableTests
    {
        [TestFixtureSetUp]
        public void Initialize()
        {
            OverallInitializer.Initialize();
        }

        [Test]
        public void TestStateVariables()
        {
            EntitySave entitySave = new EntitySave();
            entitySave.Name = "CustomVariableTestStateVariableEntity";
            ObjectFinder.Self.GlueProject.Entities.Add(entitySave);

            StateSaveCategory category1 = new StateSaveCategory();
            category1.Name = "Category1";
            category1.SharesVariablesWithOtherCategories = false;
            StateSave stateSave = new StateSave();
            stateSave.Name = "Disabled";
            category1.States.Add(stateSave);

            StateSaveCategory category2 = new StateSaveCategory();
            category2.Name = "Category2";
            category2.SharesVariablesWithOtherCategories = false;
            stateSave = new StateSave();
            stateSave.Name = "Disabled";
            category2.States.Add(stateSave);

            entitySave.StateCategoryList.Add(category1);
            entitySave.StateCategoryList.Add(category2);

            CustomVariable customVariable = new CustomVariable();
            customVariable.Type = "Category2";
            customVariable.DefaultValue = "Disabled";
            customVariable.Name = "CurrentCategory2State";
            entitySave.CustomVariables.Add(customVariable);



            ElementRuntime elementRuntime = new ElementRuntime();
            elementRuntime.Initialize(entitySave, null, null, null, null);

            StateSave foundStateSave = 
                elementRuntime.GetStateSaveFromCustomVariableValue(customVariable, customVariable.DefaultValue);

            if (foundStateSave != category2.States[0])
            {
                throw new Exception("States in categories are not being found properly when referenced through custom variables");
            }

        }

        [Test]
        public void TestTypeOverriding()
        {
            CustomVariable customVariable = new CustomVariable();
            customVariable.Name = "Whatever";
            customVariable.Type = "int";
            customVariable.DefaultValue = 4;

            object value = ElementRuntime.ConvertIfOverriding(
                customVariable, customVariable.DefaultValue);

            if (value is int == false || ((int)value) != 4)
            {
                throw new Exception("Converting variables without overrides ");
            }
            ////////////////////////////////////////////////////////////////////
            customVariable.Type = "string";
            customVariable.OverridingPropertyType = "int";

            value = ElementRuntime.ConvertIfOverriding(
                customVariable, customVariable.DefaultValue);

            if (value is string == false || ((string)value) != "4")
            {
                throw new Exception("Converting variables without overrides ");
            }
            ////////////////////////////////////////////////////////////////////
            customVariable.Type = "string";
            customVariable.OverridingPropertyType = "int";
            customVariable.TypeConverter = "Minutes:Seconds";

            value = ElementRuntime.ConvertIfOverriding(
                customVariable, customVariable.DefaultValue);

            if (value is string == false || ((string)value) != "0:04")
            {
                throw new Exception("Converting int to time is wrong");
            }
            ///////////////////////////////////////////////////////////////////
            customVariable.DefaultValue = 1234;
            customVariable.TypeConverter = "Comma Separating";

            value = ElementRuntime.ConvertIfOverriding(
                customVariable, customVariable.DefaultValue);

            if (value is string == false || ((string)value) != "1,234")
            {
                throw new Exception("Converting variables without overrides ");
            }
            //////////////////////////////////////////////////////////////////
            customVariable.DefaultValue = 1234.0f;
            customVariable.TypeConverter = "Comma Separating";
            customVariable.OverridingPropertyType = "float";

            value = ElementRuntime.ConvertIfOverriding(
                customVariable, customVariable.DefaultValue);

            if (value is string == false || ((string)value) != "1,234.00")
            {
                throw new Exception("Converting variables without overrides ");
            }





            ///////////////////////////////////////////////////////////////////
            customVariable.DefaultValue = 1234.5f;
            customVariable.TypeConverter = "Minutes:Seconds.Hundredths";
            customVariable.OverridingPropertyType = "float";

            value = ElementRuntime.ConvertIfOverriding(
                customVariable, customVariable.DefaultValue);

            if (value is string == false || ((string)value) != "20:34.50")
            {
                throw new Exception("Converting variables without overrides ");
            }


        }
    }
}
