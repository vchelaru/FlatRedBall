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
                GlueCommands.Self.DoOnUiThread(() =>
               {

                   errorListViewModel.Errors.Clear();

                   var missingFiles = ErrorCreateRemoveLogic.GetMissingFileErrorViewModels();

                   foreach (var missingFile in missingFiles)
                   {
                       errorListViewModel.Errors.Add(missingFile);
                   }
               });
            }
            , "Refresh all errors");
        }

        internal static void HandleFileChange(FilePath filePath, ErrorListViewModel errorListViewModel)
        {
            TaskManager.Self.AddSync(() =>
            {
                GlueCommands.Self.DoOnUiThread(() =>
                {
                    // Add new errors:
                    ErrorCreateRemoveLogic.AddNewErrorsForChangedFile(filePath, errorListViewModel);

                    ErrorCreateRemoveLogic.RemoveFixedErrorsForChangedFile(filePath, errorListViewModel);
                });

            }, $"Handle file change {filePath}");
        }

        internal static void HandleReferencedFileRemoved(ReferencedFileSave removedFile, ErrorListViewModel errorListViewModel)
        {
            TaskManager.Self.AddSync(() =>
            {
                GlueCommands.Self.DoOnUiThread(() =>
                {
                    ErrorCreateRemoveLogic.RemoveFixedErrorsForRemovedRfs(removedFile, errorListViewModel);
                });

            }, $"Handle referenced file removed {removedFile}");
        }

        internal static void HandleFileReadError(FilePath filePath, ErrorListViewModel errorListViewModel)
        {
            TaskManager.Self.AddSync(() =>
            {
                GlueCommands.Self.DoOnUiThread(() =>
                {
                    ErrorCreateRemoveLogic.AddNewErrorsForFileReadError(filePath, errorListViewModel);
                });

            }, $"Handle file read error {filePath}");

        }
    }
}
