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
    internal class MissingSourceObjectErrorViewModel : ErrorViewModel
    {
        public override string UniqueId => Details;

        CustomVariable customVariable;
        GlueElement owner;

        public MissingSourceObjectErrorViewModel(CustomVariable customVariable, GlueElement owner)
        {
            this.customVariable = customVariable;
            this.owner = owner;

            this.Details = $"The variable {customVariable} references an object that does not exist {customVariable.SourceObject}.";
        }

        public override void HandleDoubleClick()
        {
            GlueState.Self.CurrentCustomVariable = customVariable;
        }


        public static bool GetIfHasError(GlueElement owner, CustomVariable variable)
        {
            if(string.IsNullOrEmpty(variable.SourceObject))
            {
                return false;
            }
            if (owner is ScreenSave asScreen)
            {
                if (GlueState.Self.CurrentGlueProject.Screens.Contains(owner) == false)
                {
                    return false;
                }
            }
            else if (owner is EntitySave asEntity)
            {
                if (GlueState.Self.CurrentGlueProject.Entities.Contains(owner) == false)
                {
                    return false;
                }
            }

            if (owner.CustomVariables.Contains(variable) == false)
            {
                return false;
            }

            if(variable.SourceObject == null)
            {
                return false;
            }

            var foundObject = owner.GetNamedObjectRecursively(variable.SourceObject);
            if(foundObject != null)
            {
                return false;
            }

            return true;
        }

        public override bool GetIfIsFixed()
        {
            return !GetIfHasError(owner, customVariable);
        }
    }
}
