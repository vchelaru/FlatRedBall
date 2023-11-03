using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using OfficialPlugins.ErrorReportingPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ErrorReportingPlugin
{
    internal class ElementInheritanceErrorReporter : ErrorReporterBase
    {
        public override ErrorViewModel[] GetAllErrors()
        {
            List<ErrorViewModel> errors = new List<ErrorViewModel>();

            FillWithExpectedBaseClassErrors(errors);

            return errors.ToArray();
        }

        private void FillWithExpectedBaseClassErrors(List<ErrorViewModel> viewModel)
        {
            var project = GlueState.Self.CurrentGlueProject;
            if (project == null) return;


            foreach(var screen in project.Screens)
            {
                foreach(var nos in screen.AllNamedObjects)
                {
                    if(ElementMissingBaseErrorViewModel.GetIfHasError(screen, nos))
                    {
                        viewModel.Add(new ElementMissingBaseErrorViewModel(screen, nos));
                    }
                }
            }

            foreach(var entity in project.Entities)
            {
                foreach(var nos in entity.AllNamedObjects)
                {
                    if(ElementMissingBaseErrorViewModel.GetIfHasError(entity, nos))
                    {
                        viewModel.Add(new ElementMissingBaseErrorViewModel(entity, nos));
                    }
                }
            }
        }
    }
}
