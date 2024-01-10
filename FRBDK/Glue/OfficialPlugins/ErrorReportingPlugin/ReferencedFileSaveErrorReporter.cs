using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
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

            FillWithContentPipelineMismatches(errors);

            return errors.ToArray();
        }

        private void FillWithContentPipelineMismatches(List<ErrorViewModel> errors)
        {
            var filesUsingContentPipeline = new Dictionary<FilePath, ReferencedFileSave>();
            var filesNotUsingContentPipeline = new Dictionary<FilePath, ReferencedFileSave>();

            var allFiles = ObjectFinder.Self.GetAllReferencedFiles();

            foreach(var rfs in allFiles)
            {
                if(!rfs.LoadedAtRuntime)
                {
                    continue;
                }
                

                var filePath = GlueCommands.Self.GetAbsoluteFilePath(rfs);
                if(rfs.UseContentPipeline)
                {
                    if(filesNotUsingContentPipeline.ContainsKey(filePath))
                    {
                        var vm = new ContentPipelineMismatchViewModel(rfs, filesNotUsingContentPipeline[filePath]);
                        errors.Add(vm);
                    }
                    filesUsingContentPipeline[filePath] = rfs;
                }
                else
                {
                    if(filesUsingContentPipeline.ContainsKey(filePath))
                    {
                        var vm = new ContentPipelineMismatchViewModel(filesUsingContentPipeline[filePath], rfs);
                        errors.Add(vm);
                    }
                    filesNotUsingContentPipeline[filePath] = rfs;
                }
            }
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
