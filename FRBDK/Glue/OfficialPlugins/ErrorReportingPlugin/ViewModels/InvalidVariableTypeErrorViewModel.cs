using FlatRedBall.Glue.Elements;
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

        public static bool GetIfHasError(GlueElement owner, CustomVariable variable)
        {
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

            var type = variable.Type;

            var doesTypeExist = true;

            if (variable.Type.Contains("."))
            {
                var baseDefiningVariable = ObjectFinder.Self.GetRootCustomVariable(variable);

                // for now we'll skip anything that is a tunneled variable because that gets way more complicated, and
                // this check was added to catch missing CSV references
                if (string.IsNullOrEmpty(baseDefiningVariable?.SourceObject))
                {
                    // it better be a CSV or state or Texture
                    var found =
                        variable.Type == "Microsoft.Xna.Framework.Graphics.Texture2D" ||
                        variable.GetIsCsv() ||
                        variable.GetIsVariableState() ||
                        variable.GetIsBaseElementType();

                    if (!found)
                    {
                        doesTypeExist = false;
                    }
                }
            }
            return !doesTypeExist;
        }

        public override bool GetIfIsFixed()
        {
            return!GetIfHasError(owner, customVariable);
        }
    }
}
