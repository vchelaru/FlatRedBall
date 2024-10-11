using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.ErrorReportingPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
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

            FillWithMissingSourceObjects(errors);

            return errors.ToArray();
        }

        private void FillWithMissingSourceObjects(List<ErrorViewModel> errors)
        {
            var project = GlueState.Self.CurrentGlueProject;
            foreach (var screen in project.Screens)
            {
                foreach (var variable in screen.CustomVariables)
                {
                    FillWithMissingSourceObjects(screen, variable, errors);
                }
            }
            foreach (var entity in project.Entities)
            {
                foreach (var variable in entity.CustomVariables)
                {
                    FillWithMissingSourceObjects(entity, variable, errors);
                }
            }
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
            if (InvalidVariableTypeErrorViewModel.GetIfHasError(element, variable)) 
            {
                errors.Add(new InvalidVariableTypeErrorViewModel(variable, element));
            }
        }

        private void FillWithMissingSourceObjects(GlueElement element, CustomVariable variable, List<ErrorViewModel> errors)
        {
            if (MissingSourceObjectErrorViewModel.GetIfHasError(element, variable))
            {
                errors.Add(new MissingSourceObjectErrorViewModel(variable, element));
            }
        }
    }
}
