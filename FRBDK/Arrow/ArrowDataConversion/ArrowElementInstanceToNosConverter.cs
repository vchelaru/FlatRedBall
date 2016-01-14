using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Arrow.DataTypes;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.SaveClasses;

namespace ArrowDataConversion
{
    public class ArrowElementInstanceToNosConverter : GeneralSaveConverter
    {

        public NamedObjectSave ArrowElementInstanceToNos(ArrowElementInstance elementInstance)
        {
            NamedObjectSave toReturn = new NamedObjectSave();
            toReturn.InstanceName = elementInstance.Name;
            toReturn.SourceType = SourceType.Entity;
            toReturn.SourceClassType = "Entities/" + elementInstance.Type;

            foreach (var sourceVariable in elementInstance.Variables)
            {
                toReturn.InstructionSaves.Add(sourceVariable.Clone<CustomVariableInNamedObject>());

            }

            return toReturn;
        }

        public override void AddVariablesForAllProperties(object saveObject, NamedObjectSave toModify)
        {
            ArrowElementInstance elementInstance = saveObject as ArrowElementInstance;
            toModify.InstructionSaves.Clear();
            foreach (var variable in elementInstance.Variables)
            {

                var toAdd = variable.Clone<CustomVariableInNamedObject>();
                toModify.InstructionSaves.Add(toAdd);

            }
        }

        
    }
}
