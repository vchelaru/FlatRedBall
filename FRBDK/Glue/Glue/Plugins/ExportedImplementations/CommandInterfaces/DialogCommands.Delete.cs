using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using GlueFormsCore.Controls;
using GlueFormsCore.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    /// <summary>
    /// The plan-ask-execute flow every Screen and Entity delete goes through.
    ///
    /// Deleting a Screen used to ask up to four questions from four different places, each one a modal
    /// that could end up behind the editor window: the confirm, one popup per derived Screen about
    /// resetting its inheritance, the Gum plugin's own popup about its Screen, and finally the
    /// leftover-files dialog. They were interleaved with the delete itself, which is also why none of it
    /// could be tested. See GitHub issue #429.
    /// </summary>
    partial class DialogCommands
    {
        public async Task<bool> AskToRemoveScreenAsync(ScreenSave screenToRemove, bool askToDeleteFiles = true)
        {
            var viewModel = DeletionPlanner.CreateForScreen(screenToRemove);

            if (!ConfirmDelete(viewModel, askToDeleteFiles))
            {
                return false;
            }

            var filesToRemove = new List<string>();

            await TaskManager.Self.AddAsync(
                async () => await GlueCommands.Self.GluxCommands.RemoveScreenAsync(screenToRemove, viewModel, filesToRemove),
                $"Removing Screen {screenToRemove}");

            await ApplyFileActionAsync(viewModel, filesToRemove, askToDeleteFiles);

            return true;
        }

        public async Task<bool> AskToRemoveEntityAsync(EntitySave entityToRemove, bool askToDeleteFiles = true)
        {
            var viewModel = DeletionPlanner.CreateForEntity(entityToRemove);

            if (!ConfirmDelete(viewModel, askToDeleteFiles))
            {
                return false;
            }

            var filesToRemove = new List<string>();

            await TaskManager.Self.AddAsync(
                async () => await GlueCommands.Self.GluxCommands.RemoveEntityAsync(entityToRemove, viewModel, filesToRemove),
                $"Removing Entity {entityToRemove}");

            await ApplyFileActionAsync(viewModel, filesToRemove, askToDeleteFiles);

            return true;
        }

        static bool ConfirmDelete(DeleteOptionsViewModel viewModel, bool askToDeleteFiles)
        {
            if (!askToDeleteFiles)
            {
                // The caller is handling files itself, so the dialog shouldn't offer to.
                viewModel.AlwaysRemovedFiles.Clear();
                viewModel.RefreshFilesToRemove();
                viewModel.FileAction = FileDeleteAction.Nothing;
            }

            return DialogService.ShowDelete(viewModel);
        }

        /// <summary>
        /// Applies the file action the user picked in the delete dialog. <paramref name="actuallyRemoved"/>
        /// is what the delete itself accumulated; anything in there the plan didn't predict is included so
        /// a file is never left behind silently, and <c>DeletionPlannerTests</c> pins the two lists to each
        /// other so the dialog doesn't quietly under-report.
        /// </summary>
        async Task ApplyFileActionAsync(DeleteOptionsViewModel viewModel, List<string> actuallyRemoved, bool askToDeleteFiles)
        {
            if (!askToDeleteFiles || viewModel.FileAction == FileDeleteAction.Nothing)
            {
                return;
            }

            var files = viewModel.FilesToRemove.Concat(actuallyRemoved).ToList();

            await RemoveFilesAsync(files, deleteFromDisk: viewModel.FileAction == FileDeleteAction.RemoveAndDelete);
        }

        /// <summary>
        /// Asks what to do with a list of leftover files and does it. Used by the delete paths that don't
        /// have their own dialog yet (files, custom variables, states) - Screens and Entities fold this
        /// question into <see cref="DeleteOptionsWindow"/> instead of showing it separately.
        /// </summary>
        public async Task AskWhatToDoWithFilesAsync(List<string> filesToRemove)
        {
            if (filesToRemove == null || filesToRemove.Count == 0)
            {
                return;
            }

            var normalized = NormalizeFilePaths(filesToRemove);
            var root = DeletionPlanner.ToCanonicalPath(GlueState.Self.CurrentGlueProjectDirectory);

            var listBoxWindow = new ListBoxWindowWpf
            {
                Message = "What would you like to do with the following files:\n"
            };

            foreach (var file in normalized)
            {
                listBoxWindow.AddItem(DeleteOptionsViewModel.ToDisplayPath(file, root));
            }

            listBoxWindow.ClearButtons();
            listBoxWindow.AddButton("Nothing - leave them as part of the game project", FileDeleteAction.Nothing);
            listBoxWindow.AddButton("Remove them from the project but keep the files", FileDeleteAction.RemoveFromProject);
            listBoxWindow.AddButton("Remove and delete the files", FileDeleteAction.RemoveAndDelete);

            listBoxWindow.ShowDialog();

            if (listBoxWindow.ClickedOption is FileDeleteAction action && action != FileDeleteAction.Nothing)
            {
                await RemoveFilesAsync(normalized, deleteFromDisk: action == FileDeleteAction.RemoveAndDelete);
            }
        }

        static async Task RemoveFilesAsync(List<string> files, bool deleteFromDisk)
        {
            var normalized = NormalizeFilePaths(files);

            if (normalized.Count == 0)
            {
                return;
            }

            await TaskManager.Self.AddAsync(() =>
            {
                foreach (var file in normalized)
                {
                    FilePath filePath = GlueCommands.Self.GetAbsoluteFileName(file, false);

                    // The file may have been removed in Windows Explorer and only now removed from Glue,
                    // so nothing here assumes it still exists.
                    GlueCommands.Self.ProjectCommands.RemoveFromProjects(filePath, false);

                    if (deleteFromDisk && filePath.Exists())
                    {
                        FileHelper.MoveToRecycleBin(filePath.FullPath);
                    }
                }

                GluxCommands.Self.ProjectCommands.SaveProjects();
            }, "Removing files");
        }

        static List<string> NormalizeFilePaths(List<string> files)
        {
            var toReturn = files
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(DeletionPlanner.ToCanonicalPath)
                .ToList();

            StringFunctions.RemoveDuplicates(toReturn, true);

            return toReturn;
        }
    }
}
