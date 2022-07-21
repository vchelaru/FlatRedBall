using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.ErrorReportingPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.ErrorReportingPlugin
{
    internal class ReferencedFileSaveErrorReporter : ErrorReporterBase
    {
        public override ErrorViewModel[] GetAllErrors()
        {
            List<ErrorViewModel> errors = new List<ErrorViewModel>();

            FillWithSameNamedFiles(errors);

            return errors.ToArray();
        }

        private void FillWithSameNamedFiles(List<ErrorViewModel> errors)
        {
            var project = GlueState.Self.CurrentGlueProject;
            FillWithSameNamedFiles(project.GlobalFiles, errors);
        }

        private void FillWithSameNamedFiles(List<FlatRedBall.Glue.SaveClasses.ReferencedFileSave> files, List<ErrorViewModel> errors)
        {
            for(int i = 0; i < files.Count; i++)
            {
                var first = files[i];

                if(first.LoadedAtRuntime == false)
                {
                    continue;
                }

                for(int j = i+1; j < files.Count; j++)
                {
                    var second = files[j];

                    if(second.LoadedAtRuntime == false)
                    {
                        continue;
                    }

                    var firstName = first.GetInstanceName();
                    var secondName = second.GetInstanceName();

                    if(firstName == secondName )
                    {
                        var vm = new SameNamedReferencedFilesViewModel(first, second);

                        errors.Add(vm);
                    }
                }
            }
        }
    }
}
