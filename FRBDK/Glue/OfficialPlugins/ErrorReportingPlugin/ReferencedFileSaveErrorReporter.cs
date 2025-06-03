using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using Gum.DataTypes;
using OfficialPlugins.ErrorReportingPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OfficialPlugins.ErrorReportingPlugin
{
    internal class ReferencedFileSaveErrorReporter : ErrorReporterBase
    {
        private readonly GlueState _glueState;
        private readonly ObjectFinder _objectFinder;

        public ReferencedFileSaveErrorReporter(
            GlueState glueState,
            ObjectFinder objectFinder)
        {
            _glueState = glueState;
            _objectFinder = objectFinder;

        }

        public override ErrorViewModel[] GetAllErrors()
        {
            List<ErrorViewModel> errors = new List<ErrorViewModel>();

            FillWithSameNamedFiles(errors);

            FillWithContentPipelineMismatches(errors);

            FillWithUnreferencedCsprojFiles(errors);

            return errors.ToArray();
        }

        private void FillWithUnreferencedCsprojFiles(List<ErrorViewModel> errors)
        {
            var projectDirectory = _glueState.CurrentGlueProjectDirectory;
            if(string.IsNullOrEmpty(projectDirectory))
            {
                return;
            }

            try
            {
                FilePath directoryToStartAt = _glueState.CurrentSlnFileName.GetDirectoryContainingThis();

                var files = Directory.GetFiles(directoryToStartAt.FullPath, "*.*", SearchOption.AllDirectories)
                    .Select(item => new FilePath(item)).ToHashSet();

                FillWithMissingFiles(errors, _glueState.CurrentMainProject, files);

                foreach (var syncedProject in _glueState.SyncedProjects)
                {
                    if (syncedProject is VisualStudioProject vsProject)
                    {
                        FillWithMissingFiles(errors, vsProject, files);
                    }
                }
            }
            catch
            {
                // if we have an exception such as permissions based, we tolerate it and move on

            }
        }

        private void FillWithMissingFiles(List<ErrorViewModel> errors, VisualStudioProject currentMainProject, HashSet<FilePath> filesOnDisk)
        {
            var directory = currentMainProject.Directory;

            foreach (var file in currentMainProject.EvaluatedItems)
            {
                var copyMetadata = file.GetMetadata("CopyToOutputDirectory");
                // Glue always sets PreserveNewest. Do we want to check for other types?
                // For the error I'm working on for 6/2/2025 this would catch it.
                if(copyMetadata?.EvaluatedValue == "PreserveNewest")
                {
                    FilePath filePath = directory + file.EvaluatedInclude;
                    if(!filesOnDisk.Contains(filePath) &&
                        // fallback to .exists if it's not in the dictionary. This is slower,
                        // but will catch files referenced outside the .sln folder:
                        !filePath.Exists() )
                    {
                        var vm = new MissingCsprojReferencedFile(currentMainProject, filePath);
                        errors.Add(vm);
                    }
                }
            }
        }

        private void FillWithContentPipelineMismatches(List<ErrorViewModel> errors)
        {
            var filesUsingContentPipeline = new Dictionary<FilePath, ReferencedFileSave>();
            var filesNotUsingContentPipeline = new Dictionary<FilePath, ReferencedFileSave>();

            var allFiles = _objectFinder.GetAllReferencedFiles();

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
