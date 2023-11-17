using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO.Csv;
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

            if(glueProject != null)
            {
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

                    var filePath = GlueCommands.Self.GetAbsoluteFilePath(rfs);

                    // See if this has a duplicate 
                    CsvCodeGenerator.DeserializeToRcr(
                        rfs.CsvDelimiter, 
                        filePath,
                        out RuntimeCsvRepresentation rcr, out bool succeeded);
                
                    if(succeeded)
                    {
                        var duplicateError = CsvCodeGenerator.GetDuplicateMessageIfDuplicatesFound(
                            rcr,
                            rfs.CreatesDictionary,
                            filePath.FullPath);

                        if(!string.IsNullOrEmpty(duplicateError))
                        {
                            var vm = new CsvDuplicateItemInFileViewModel();
                            vm.FilePath = filePath;
                            vm.UpdateDetails();
                            errors.Add(vm);
                        
                        }
                    }
                }
            }

            return errors.ToArray();
        }
    }
}
