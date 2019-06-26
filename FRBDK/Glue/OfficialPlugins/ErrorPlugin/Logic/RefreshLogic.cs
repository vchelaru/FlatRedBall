using EditorObjects.IoC;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.ErrorPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ErrorPlugin.Logic
{
    public static class RefreshLogic
    {
        public static void RefreshAllErrors(ErrorListViewModel errorListViewModel)
        {
            TaskManager.Self.AddSync(() =>
            {
                // todo - need to store a list of IErrorReporter's somewhere, and loop through them
                // here
                lock (GlueState.ErrorListSyncLock)
                {
                    errorListViewModel.Errors.Clear();
                }
                var reporters = Container.Get<List<IErrorReporter>>();
                foreach (var reporter in reporters)
                {
                    var errors = reporter.GetAllErrors();

                    foreach (var error in errors)
                    {
                       lock (GlueState.ErrorListSyncLock)
                       {
                           errorListViewModel.Errors.Add(error);
                       }
                    }
                }
            }
            , "Refresh all errors");
        }

        internal static void HandleFileChange(FilePath filePath, ErrorListViewModel errorListViewModel)
        {
            TaskManager.Self.AddSync(() =>
            {
                    // Add new errors:
                    ErrorCreateRemoveLogic.AddNewErrorsForChangedFile(filePath, errorListViewModel);

                    ErrorCreateRemoveLogic.RemoveFixedErrorsForChangedFile(filePath, errorListViewModel);

            }, $"Handle file change {filePath}");
        }

        internal static void HandleReferencedFileRemoved(ReferencedFileSave removedFile, ErrorListViewModel errorListViewModel)
        {
            TaskManager.Self.AddSync(() =>
            {
                    ErrorCreateRemoveLogic.RemoveFixedErrorsForRemovedRfs(removedFile, errorListViewModel);

            }, $"Handle referenced file removed {removedFile}");
        }

        internal static void HandleFileReadError(FilePath filePath, ErrorListViewModel errorListViewModel)
        {
            TaskManager.Self.AddSync(() =>
            {
                    ErrorCreateRemoveLogic.AddNewErrorsForFileReadError(filePath, errorListViewModel);

            }, $"Handle file read error {filePath}");

        }
    }
}
