using EditorObjects.IoC;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using OfficialPlugins.ErrorPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ErrorPlugin.Logic;

public static class RefreshLogic
{
    /// <summary>
    /// Reruns every error reporter and refills the list, on the calling thread. Queue it through
    /// <see cref="FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces.RefreshCommands.RefreshErrors"/>,
    /// whose AddOrMoveToEnd task coalesces repeated requests into one run.
    /// </summary>
    public static void RefreshAllErrors(ErrorListViewModel errorListViewModel, bool printOutput)
    {
        lock (GlueState.ErrorListSyncLock)
        {
            errorListViewModel.Errors.Clear();
        }

        var errorManager = Container.Get<GlueErrorManager>();

        var reporters = errorManager.ErrorReporters;
        foreach (var reporter in reporters)
        {
            DateTime startTime = DateTime.Now;
            if(printOutput)
            {
                GlueCommands.Self.PrintOutput($"Refreshing errors for reporter {reporter.GetType()}");
            }
            var errors = reporter.GetAllErrors();
            reporter.ErrorsBelongingToThisReporter.Clear();
            if(errors != null)
            {
                reporter.ErrorsBelongingToThisReporter.AddRange(errors);
            }

            var endTime = DateTime.Now;

            if (printOutput)
            {
                var duration = endTime - startTime;

                var durationDisplay = $"{duration.Seconds}.{duration.Milliseconds.ToString("000")}";

                if (duration.Minutes > 0)
                {
                    durationDisplay = $"{duration.Minutes}:{durationDisplay}";
                }

                GlueCommands.Self.PrintOutput($"{reporter.GetType()} {durationDisplay}");
            }

            if(errors != null)
            {
                foreach (var error in errors)
                {
                   lock (GlueState.ErrorListSyncLock)
                   {
                       errorListViewModel.Errors.Add(error);
                   }
                }
            }
        }
        errorManager.ClearFixedErrors();
        foreach (var error in errorManager.Errors)
        {
            lock (GlueState.ErrorListSyncLock)
            {
                errorListViewModel.Errors.Add(error);
            }
        }
    }

    internal static void HandleFileChange(FilePath filePath, ErrorListViewModel errorListViewModel)
    {
        TaskManager.Self.Add(() =>
        {
            // Add new errors:
            ErrorCreateRemoveLogic.AddNewErrorsForChangedFile(filePath, errorListViewModel);

            ErrorCreateRemoveLogic.RemoveFixedErrorsForChangedFile(filePath, errorListViewModel);

        }, $"Handle file change {filePath}");
    }

    internal static void HandleReferencedFileRemoved(ReferencedFileSave removedFile, ErrorListViewModel errorListViewModel)
    {
        TaskManager.Self.Add(() =>
        {
            ErrorCreateRemoveLogic.RemoveFixedErrorsForRemovedRfs(removedFile, errorListViewModel);

        }, $"Handle referenced file removed {removedFile}");
    }

    internal static void HandleFileReadError(FilePath filePath, ErrorListViewModel errorListViewModel)
    {
        TaskManager.Self.Add(() =>
        {
            ErrorCreateRemoveLogic.AddNewErrorsForFileReadError(filePath, errorListViewModel);

        }, $"Handle file read error {filePath}");

    }
}
