using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using FlatRedBall.Glue.SaveClasses;

namespace ArrowDataConversion
{
    public abstract class GeneralSaveConverter
    {

        protected virtual void AddVariableToNos(NamedObjectSave toReturn, string memberName, object currentValue)
        {
            CustomVariableInNamedObject cvino = new CustomVariableInNamedObject();
            cvino.Member = memberName;
            cvino.Value = currentValue;
            toReturn.InstructionSaves.Add(cvino);
        }

        public virtual void AddVariablesForAllProperties(object saveObject, NamedObjectSave toAddTo)
        {
            toAddTo.InstructionSaves.Clear();

            foreach (var field in saveObject.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object currentValue = field.GetValue(saveObject);

                if (!MemberInvestigator.IsDefault(saveObject, field.Name, currentValue))
                {
                    string memberName = field.Name;

                    AddVariableToNos(toAddTo, memberName, currentValue);
                }
            }

            foreach (var property in saveObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                object currentValue = property.GetValue(saveObject, null);

                if (!MemberInvestigator.IsDefault(saveObject, property.Name, currentValue))
                {
                    string memberName = property.Name;

                    AddVariableToNos(toAddTo, memberName, currentValue);
                }
            }
        }

        
    }
}
