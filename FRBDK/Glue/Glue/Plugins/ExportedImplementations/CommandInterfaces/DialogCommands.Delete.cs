using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using L = Localization;
using GlueFormsCore.Controls;
using GlueFormsCore.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    /// <summary>
    /// The plan-ask-execute flow every delete in Glue goes through: build a
    /// <see cref="DeleteOptionsViewModel"/> without touching anything, show it once, then apply the answers.
    ///
    /// Deleting a Screen used to ask up to four questions from four different places, each one a modal that
    /// could end up behind the editor window: the confirm, one popup per derived Screen about resetting its
    /// inheritance, the Gum plugin's own popup about its Screen, and finally the leftover-files dialog. They
    /// were interleaved with the delete itself, which is also why none of it could be tested (GitHub issue
    /// #429). Objects, files, states, state categories and variables each had their own version of the same
    /// problem and were folded in afterwards (GitHub issue #2032).
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

        /// <summary>
        /// Removing a file used to ask a question per object using it, from inside the removal, and then the
        /// leftover-files dialog afterwards. Same plan-ask-execute shape as the element deletes now.
        /// </summary>
        public async Task<bool> AskToRemoveReferencedFileAsync(ReferencedFileSave fileToRemove, bool askToDeleteFiles = true)
        {
            if (!GlueState.Self.Find.IfReferencedFileSaveIsReferenced(fileToRemove))
            {
                return false;
            }

            var viewModel = DeletionPlanner.CreateForReferencedFile(fileToRemove);

            if (!ConfirmDelete(viewModel, askToDeleteFiles))
            {
                return false;
            }

            var filesToRemove = new List<string>();

            await GlueCommands.Self.GluxCommands.RemoveReferencedFileAsync(fileToRemove, viewModel, filesToRemove);

            await ApplyFileActionAsync(viewModel, filesToRemove, askToDeleteFiles);

            return true;
        }

        public Task<bool> AskToRemoveObjectAsync(NamedObjectSave namedObjectToRemove, bool saveAndRegenerate = true) =>
            AskToRemoveObjectListAsync(new List<NamedObjectSave> { namedObjectToRemove }, saveAndRegenerate);

        /// <summary>
        /// Objects used to have a delete dialog of their own, <c>RemoveObjectWindow</c>, which predated the
        /// shared view model and could show neither options nor the files the delete leaves behind.
        /// </summary>
        public async Task<bool> AskToRemoveObjectListAsync(List<NamedObjectSave> namedObjectsToRemove, bool saveAndRegenerate = true)
        {
            if (!CanRemoveObjects(namedObjectsToRemove))
            {
                return false;
            }

            var viewModel = DeletionPlanner.CreateForNamedObjects(namedObjectsToRemove);

            if (!DialogService.ShowDelete(viewModel))
            {
                return false;
            }

            var owners = namedObjectsToRemove
                .Select(ObjectFinder.Self.GetElementContaining)
                .Where(item => item != null)
                .ToHashSet();

            var filesToRemove = new List<string>();

            await GlueCommands.Self.GluxCommands
                .RemoveNamedObjectListAsync(namedObjectsToRemove, true, true, filesToRemove);

            await ApplyFileActionAsync(viewModel, filesToRemove, askToDeleteFiles: true);

            TaskManager.Self.AddOrRunIfTasked(() =>
            {
                // Nodes aren't directly removed in the code above. Instead, a "refresh nodes" method is called,
                // which may remove unneeded nodes, but event raising is suppressed. Therefore, we have to
                // explicitly do it here:
                foreach (var owner in owners)
                {
                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(owner);
                }
            }, "Refreshing tree nodes");

            if (saveAndRegenerate)
            {
                foreach (var owner in owners)
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(owner);
                }

                GluxCommands.Self.ProjectCommands.SaveProjects();

                foreach (var owner in owners)
                {
                    _ = GluxCommands.Self.SaveElementAsync(owner);
                }
            }

            return true;
        }

        /// <summary>
        /// Deleting a state used to be a plain yes/no, followed - from inside the delete - by one popup per
        /// variable it had orphaned. Those variables are options on the same dialog now.
        /// </summary>
        public Task<bool> AskToRemoveStateAsync(StateSave stateToRemove)
        {
            var viewModel = DeletionPlanner.CreateForState(stateToRemove);

            if (!DialogService.ShowDelete(viewModel))
            {
                return Task.FromResult(false);
            }

            GlueCommands.Self.GluxCommands.RemoveState(stateToRemove, viewModel);

            return Task.FromResult(true);
        }

        public Task<bool> AskToRemoveStateCategoryAsync(StateSaveCategory categoryToRemove)
        {
            var viewModel = DeletionPlanner.CreateForStateCategory(categoryToRemove);

            if (!DialogService.ShowDelete(viewModel))
            {
                return Task.FromResult(false);
            }

            GlueCommands.Self.GluxCommands.RemoveStateSaveCategory(categoryToRemove);

            return Task.FromResult(true);
        }

        public async Task<bool> AskToRemoveCustomVariableAsync(CustomVariable variableToRemove, bool askToDeleteFiles = true)
        {
            var viewModel = DeletionPlanner.CreateForCustomVariable(variableToRemove);

            if (!ConfirmDelete(viewModel, askToDeleteFiles))
            {
                return false;
            }

            var filesToRemove = new List<string>();

            GlueCommands.Self.GluxCommands.RemoveCustomVariable(variableToRemove, filesToRemove);

            await ApplyFileActionAsync(viewModel, filesToRemove, askToDeleteFiles);

            return true;
        }

        /// <summary>
        /// An object a base element exposes to (or has set by) derived elements can't be deleted at all, so this
        /// is an error rather than one of the questions the delete dialog asks.
        /// </summary>
        static bool CanRemoveObjects(List<NamedObjectSave> namedObjectsToRemove)
        {
            // It's possible to set ExposedInDerived and SetByDerived independently, but conceptually
            // SetByDerived requires the property to be protected so it can be set. SetByDerived is a "stronger"
            // form of ExposedInDerived, so we still want to keep the relationship intact.
            var baseRestrictingDeletes = namedObjectsToRemove
                .Where(item => item.DefinedByBase)
                .Select(ObjectFinder.Self.GetRootDefiningObject)
                .Where(definingNos => definingNos?.ExposedInDerived == true || definingNos?.SetByDerived == true)
                .ToList();

            if (baseRestrictingDeletes.Count == 0)
            {
                return true;
            }

            GlueCommands.Self.DialogCommands.ShowMessageBox(
                $"{L.Texts.ExposedInDerivedCannotDelete}\n" +
                string.Join("\n", baseRestrictingDeletes.Select(item => item.ToString())));

            return false;
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
