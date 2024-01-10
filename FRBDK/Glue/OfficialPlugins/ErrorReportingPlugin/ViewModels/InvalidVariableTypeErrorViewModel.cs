using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ErrorReportingPlugin.ViewModels
{
    public class InvalidVariableTypeErrorViewModel : ErrorViewModel
    {
        public override string UniqueId => Details;

        CustomVariable customVariable;
        GlueElement owner;

        public InvalidVariableTypeErrorViewModel(CustomVariable customVariable, GlueElement owner)
        {
            this.customVariable = customVariable;
            this.owner = owner;

            this.Details = $"The variable {customVariable} is of type {customVariable.Type} which does not exist.  " +
                $"This may be because the type was removed from the project, or because the type is defined in a plugin that is not loaded.";
        }

        public override void HandleDoubleClick()
        {
            GlueState.Self.CurrentCustomVariable = customVariable;
        }

        public override bool GetIfIsFixed()
        {
            if(owner is ScreenSave asScreen)
            {
                if(GlueState.Self.CurrentGlueProject.Screens.Contains(owner) == false)
                {
                    return true;
                }
            }
            else if(owner is EntitySave asEntity)
            {
                if(GlueState.Self.CurrentGlueProject.Entities.Contains(owner) == false)
                {
                    return true;
                }
            }

            if(owner.CustomVariables.Contains(customVariable) == false)
            {
                return true;
            }

            var type = customVariable.Type;

            var doesTypeExist = true;

            if (customVariable.Type.Contains("."))
            {
                // it better be a CSV or state
                var found = customVariable.GetIsCsv() || customVariable.GetIsVariableState() || customVariable.GetIsBaseElementType();

                if (!found)
                {
                    doesTypeExist = false;
                }
            }

            return doesTypeExist;
        }
    }
}
