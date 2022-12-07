using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Errors
{
    public class CsvErrorReporter : ErrorReporterBase
    {
        public override ErrorViewModel[] GetAllErrors()
        {
            var glueProject = GlueState.Self.CurrentGlueProject;

            List<ErrorViewModel> errors = new List<ErrorViewModel>();

            var csvs = glueProject.GetAllReferencedFiles()
                .Where(item => item.IsCsvOrTreatedAsCsv)
                .ToArray();
            var customClasses = glueProject.CustomClasses;
            foreach (var rfs in csvs)
            {
                var classWithError = customClasses.FirstOrDefault(item =>
                {
                    return CsvAndCustomClassSameName.IsError(rfs, item);
                });

                if (classWithError != null)
                {
                    var error = new CsvAndCustomClassSameName(rfs, classWithError);

                    errors.Add(error);
                }

                // See if this has a duplicate 
                

            }

            return errors.ToArray();
        }
    }
}
