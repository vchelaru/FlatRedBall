using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.ErrorReportingPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ErrorReportingPlugin
{
    internal class CustomVariableSaveErrorReporter : ErrorReporterBase
    {
        public override ErrorViewModel[] GetAllErrors()
        {
            List<ErrorViewModel> errors = new List<ErrorViewModel>();

            // fill with bad type references:
            FillWithBadTypeReferences(errors);

            return errors.ToArray();
        }

        private void FillWithBadTypeReferences(List<ErrorViewModel> errors)
        {
            var project = GlueState.Self.CurrentGlueProject;
            foreach (var screen in project.Screens)
            {
                foreach(var variable in screen.CustomVariables)
                {
                    FillWithBadTypeReferences(screen, variable, errors);
                }
            }
            foreach (var entity in project.Entities)
            {
                foreach(var variable in entity.CustomVariables)
                {
                    FillWithBadTypeReferences(entity, variable, errors);
                }
            }
        }

        private void FillWithBadTypeReferences(GlueElement element, CustomVariable variable, List<ErrorViewModel> errors)
        {
            var type = variable.Type;

            var doesTypeExist = true;

            if(variable.Type.Contains("."))
            {
                // it better be a CSV or state
                var found = variable.GetIsCsv() || variable.GetIsVariableState();

                if(!found)
                {
                    doesTypeExist = false;
                }
            }

            if(!doesTypeExist)
            {
                var error = new InvalidVariableTypeErrorViewModel(variable, element);
                //var error = asdfasdf;
                errors.Add(error);
            }
        }
    }
}
