using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OfficialPlugins.StateDataPlugin.StateError
{
    public class MissingStateViewModel : ErrorViewModel
    {
        public GlueElement GlueElement { get; set; }
        public CustomVariable ElementVariable { get; set; }
        public NamedObjectSave NamedObjectSave { get; set; }
        public CustomVariableInNamedObject NamedObjectVariableClone { get; set; }
        public override void HandleDoubleClick()
        {
            if(NamedObjectSave != null)
            {
                GlueState.Self.CurrentNamedObjectSave = NamedObjectSave;
            }
        }

        public override bool GetIfIsFixed()
        {
            // fixed if....

            // Element has been removed
            var foundElement = ObjectFinder.Self.GetElement(GlueElement.Name);
            if(foundElement == null)
            {
                return true;
            }

            if (ElementVariable != null)
            {
                // has the variable been removed been removed?
                var foundElementVariable =
                    GlueElement.CustomVariables.FirstOrDefault(item => item.Name == ElementVariable.Name);

                if (foundElementVariable == null)
                {
                    return true;
                }
            }
            else if(NamedObjectVariableClone != null)
            {
                var nos = foundElement.GetNamedObject(NamedObjectSave.InstanceName);

                if(nos == null)
                {
                    return true;
                }

                var variable = nos.GetCustomVariable(NamedObjectVariableClone.Member);

                if(variable == null)
                {
                    return true;
                }

                if(variable.Value as string != NamedObjectVariableClone.Value as string)
                {
                    return true;
                }
            }

            return false;
        }

        public override string UniqueId => Details;


        public void RefreshDetail()
        {
            Details = $"{NamedObjectSave?.InstanceName}.{NamedObjectVariableClone?.Member} in {GlueElement} " +
                $"references invalid state {NamedObjectVariableClone.Value}";

        }

    }
}
