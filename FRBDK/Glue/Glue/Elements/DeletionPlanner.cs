using System;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SaveClasses.Helpers;
using FlatRedBall.IO;
using GlueFormsCore.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace FlatRedBall.Glue.Elements
{
    /// <summary>
    /// Works out everything a delete will do <b>before</b> any of it happens, and expresses it as a
    /// <see cref="DeleteOptionsViewModel"/> the user answers in one dialog.
    ///
    /// Nothing here touches UI or mutates the project, which is the whole reason it is a separate class:
    /// deleting a Screen used to ask its questions from four different places part-way through the delete
    /// (the "are you sure", the per-derived-screen inheritance reset inside <c>RemoveScreen</c>, the Gum
    /// plugin's own prompt inside <c>ReactToScreenRemoved</c>, and the leftover-files dialog at the end).
    /// See GitHub issue #429.
    ///
    /// Every other delete Glue has - objects, files, states, state categories, variables - was folded in
    /// the same way afterwards, so there is one shape of delete rather than four. See GitHub issue #2032.
    /// </summary>
    public static class DeletionPlanner
    {
        /// <summary>
        /// Tag used for the "reset the inheritance for X" options, so the code performing the delete can
        /// find them again without matching on display text.
        /// </summary>
        public class ResetInheritanceTag
        {
            public GlueElement DerivedElement { get; set; }

            public override bool Equals(object obj) =>
                obj is ResetInheritanceTag other && other.DerivedElement == DerivedElement;

            public override int GetHashCode() => DerivedElement?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// Tag used for the "remove the object X" options a file removal offers, so the removal can find
        /// them again without matching on display text.
        /// </summary>
        public class RemoveNamedObjectTag
        {
            public NamedObjectSave NamedObject { get; set; }

            public override bool Equals(object obj) =>
                obj is RemoveNamedObjectTag other && other.NamedObject == NamedObject;

            public override int GetHashCode() => NamedObject?.GetHashCode() ?? 0;
        }

        public static DeleteOptionsViewModel CreateForScreen(ScreenSave screen)
        {
            var viewModel = CreateForElement(screen);

            foreach (var inheriting in ObjectFinder.Self.GetAllScreensThatInheritFrom(screen))
            {
                AddResetInheritanceOption(viewModel, inheriting);
            }

            FinishPlan(viewModel);

            return viewModel;
        }

        public static DeleteOptionsViewModel CreateForEntity(EntitySave entity)
        {
            var viewModel = CreateForElement(entity);

            foreach (var nos in ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(entity.Name))
            {
                viewModel.ObjectsToRemove.Add(nos.ToString());
            }

            foreach (var inheriting in ObjectFinder.Self.GetAllEntitiesThatInheritFrom(entity))
            {
                AddResetInheritanceOption(viewModel, inheriting);
            }

            FinishPlan(viewModel);

            return viewModel;
        }

        /// <summary>
        /// Objects had a delete dialog of their own - <c>RemoveObjectWindow</c> - which predated this view
        /// model and could show neither options nor the files the delete would leave behind.
        /// </summary>
        public static DeleteOptionsViewModel CreateForNamedObjects(IList<NamedObjectSave> namedObjects)
        {
            var viewModel = new DeleteOptionsViewModel
            {
                Message = "Would you like to delete:\n" +
                    string.Join("\n", namedObjects.Select(item => item.ToString())),
                ProjectRootForDisplay = ToCanonicalPath(GlueState.Self.CurrentGlueProjectDirectory)
            };

            foreach (var namedObject in namedObjects)
            {
                var owner = ObjectFinder.Self.GetElementContaining(namedObject);

                if (owner == null)
                {
                    continue;
                }

                var alsoRemoved = GluxCommands.GetObjectsToRemoveIfRemoving(namedObject, owner);

                AddAll(alsoRemoved.CustomVariables);
                AddAll(alsoRemoved.SubObjectsInList);
                AddAll(alsoRemoved.CollisionRelationships);
                AddAll(alsoRemoved.DerivedNamedObjects);
                AddAll(alsoRemoved.EventResponses);
            }

            viewModel.RefreshFilesToRemove();

            return viewModel;

            void AddAll<T>(IEnumerable<T> items)
            {
                foreach (var item in items)
                {
                    viewModel.ObjectsToRemove.Add(item.ToString());
                }
            }
        }

        /// <summary>
        /// Tag used for the "remove the variable X" options a state delete offers.
        /// </summary>
        public class RemoveCustomVariableTag
        {
            public CustomVariable CustomVariable { get; set; }

            public override bool Equals(object obj) =>
                obj is RemoveCustomVariableTag other && other.CustomVariable == CustomVariable;

            public override int GetHashCode() => CustomVariable?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// Deleting a state used to ask a plain yes/no and then, from inside the delete, one popup per
        /// variable it had orphaned. The variables are options on the same dialog now.
        ///
        /// Only an older project reaches those options: a state delete leaves a variable dangling only when
        /// it empties the element's uncategorized <c>States</c> list, and Glue's UI no longer lets you make
        /// an uncategorized state. A project made today gets the confirm and nothing else.
        /// </summary>
        public static DeleteOptionsViewModel CreateForState(StateSave state)
        {
            var element = ObjectFinder.Self.GetElementContaining(state);

            var viewModel = CreateSimplePlan($"Are you sure you want to delete {state}?");

            foreach (var variable in element?.CustomVariables ?? Enumerable.Empty<CustomVariable>())
            {
                if (CustomVariableHelper.WouldStateBeMissingFor(variable, element, state))
                {
                    viewModel.AddOption(
                        $"Remove the variable {variable}, which would no longer have any states associated with it",
                        new RemoveCustomVariableTag { CustomVariable = variable });
                }
            }

            viewModel.RefreshFilesToRemove();

            return viewModel;
        }

        /// <summary>
        /// Deleting a category takes every variable of that category's type with it - not a choice, so the
        /// dialog lists them rather than offering them.
        /// </summary>
        public static DeleteOptionsViewModel CreateForStateCategory(StateSaveCategory category)
        {
            var viewModel = CreateSimplePlan($"Are you sure you want to delete {category}?");

            foreach (var variable in GetVariablesOfCategory(category))
            {
                viewModel.ObjectsToRemove.Add(variable.ToString());
            }

            viewModel.RefreshFilesToRemove();

            return viewModel;
        }

        /// <summary>
        /// The variables <c>GluxCommands.RemoveStateSaveCategory</c> removes along with the category: the
        /// ones typed as the category itself.
        /// </summary>
        public static List<CustomVariable> GetVariablesOfCategory(StateSaveCategory category)
        {
            var owner = ObjectFinder.Self.GetElementContaining(category);
            var project = ObjectFinder.Self.GlueProject;

            if (owner == null || project == null)
            {
                return new List<CustomVariable>();
            }

            var qualifiedCategoryName = owner.Name.Replace("\\", ".") + "." + category.Name;

            return project.Screens.Cast<GlueElement>()
                .Concat(project.Entities)
                .SelectMany(item => item.CustomVariables)
                .Where(item => item.Type == qualifiedCategoryName)
                .ToList();
        }

        public static DeleteOptionsViewModel CreateForCustomVariable(CustomVariable variable) =>
            CreateSimplePlan($"Are you sure you want to delete {variable}?");

        /// <summary>
        /// A plan with nothing but the confirm - the delete it describes orphans no files and offers no
        /// choices, so the dialog is the one question it needs to ask.
        /// </summary>
        static DeleteOptionsViewModel CreateSimplePlan(string message) =>
            new DeleteOptionsViewModel
            {
                Message = message,
                ProjectRootForDisplay = ToCanonicalPath(GlueState.Self.CurrentGlueProjectDirectory)
            };

        /// <summary>
        /// Removing a file used to ask one "the object X references the file Y, what would you like to do?"
        /// per object, each raised from inside the removal itself, and a separate message box for every
        /// object defined by a base element. Those become options and warnings on one dialog here.
        /// </summary>
        public static DeleteOptionsViewModel CreateForReferencedFile(ReferencedFileSave file)
        {
            var viewModel = new DeleteOptionsViewModel
            {
                // Not "delete" - the file is only removed from the project unless the user picks otherwise
                // below.
                Message = $"Are you sure you want to remove {file}?",
                ProjectRootForDisplay = ToCanonicalPath(GlueState.Self.CurrentGlueProjectDirectory)
            };

            var container = file.GetContainer();

            foreach (var nos in container?.NamedObjects ?? Enumerable.Empty<NamedObjectSave>())
            {
                if (nos.SourceType != SourceType.File || nos.SourceFile != file.Name)
                {
                    continue;
                }

                if (nos.DefinedByBase)
                {
                    // Removing it here wouldn't stick - it comes from the base element - so this is the one
                    // thing a delete tells the user about rather than offering to do.
                    viewModel.Warnings.Add(
                        $"The object {nos} is using the file {file}, but the file is being removed. " +
                        "The project may be broken until this object is fixed.");
                }
                else
                {
                    viewModel.AddOption($"Remove the object {nos}, which references this file",
                        new RemoveNamedObjectTag { NamedObject = nos });
                }
            }

            viewModel.AlwaysRemovedFiles.AddRange(GetFilesThatWouldBeRemoved(file));
            viewModel.RefreshFilesToRemove();

            return viewModel;
        }

        /// <summary>
        /// The files removing <paramref name="file"/> would orphan: the file itself once nothing else
        /// references it, plus the data class generated for a CSV that was the last of its type.
        /// </summary>
        public static List<string> GetFilesThatWouldBeRemoved(ReferencedFileSave file)
        {
            var toReturn = new List<string>();

            if (file == null || ObjectFinder.Self.GlueProject == null)
            {
                return toReturn;
            }

            // A wildcard file is deleted from disk by the removal itself rather than offered as a choice -
            // there is no exclusion pattern support, so leaving it on disk would just re-add it.
            if (!file.IsCreatedByWildcard && !IsReferencedByAnythingOtherThan(file))
            {
                toReturn.Add(ToCanonicalPath(file.GetRelativePath()));
            }

            AddCsvDataClassIfOrphaned(toReturn, file);

            return toReturn.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        static bool IsReferencedByAnythingOtherThan(ReferencedFileSave file)
        {
            var filePath = GlueCommands.Self.GetAbsoluteFilePath(file);

            var referencedElsewhere = ObjectFinder.Self.GetAllReferencedFiles()
                .Where(item => item != file)
                .Any(item => GlueCommands.Self.GetAbsoluteFilePath(item) == filePath);

            return referencedElsewhere || FileReferenceManager.Self.IsFileReferencedRecursively(filePath);
        }

        /// <summary>
        /// A CSV generates a data class shared by every CSV of the same type, so it is only orphaned when the
        /// one being removed was the last of them - and never when the user wrote the class by hand.
        /// Mirrors what <c>GluxCommands.ReactToRemovalIfCsv</c> does, one step ahead of it.
        /// </summary>
        static void AddCsvDataClassIfOrphaned(List<string> toReturn, ReferencedFileSave file)
        {
            if (!file.IsCsvOrTreatedAsCsv)
            {
                return;
            }

            if (ObjectFinder.Self.GlueProject.GetCustomClassReferencingFile(file.Name) != null)
            {
                return;
            }

            var typeName = file.GetTypeForCsvFile();

            var otherCsvOfSameType = ObjectFinder.Self.GetAllReferencedFiles()
                .Any(item => item != file && item.IsCsvOrTreatedAsCsv && item.GetTypeForCsvFile() == typeName);

            if (!otherCsvOfSameType)
            {
                toReturn.Add(ToCanonicalPath("DataTypes/" + typeName + ".Generated.cs"));
            }
        }

        static DeleteOptionsViewModel CreateForElement(GlueElement element)
        {
            var viewModel = new DeleteOptionsViewModel
            {
                Element = element,
                Message = $"Are you sure you want to delete {element}?",
                ProjectRootForDisplay = ToCanonicalPath(GlueState.Self.CurrentGlueProjectDirectory)
            };

            viewModel.AlwaysRemovedFiles.AddRange(GetFilesThatWouldBeRemoved(element));

            return viewModel;
        }

        /// <summary>
        /// One spelling for every file in a delete: absolute, with forward slashes. The lists feeding the
        /// dialog come from several places - a ReferencedFileSave's content-relative path, an element's
        /// code path, a plugin's <c>FilePath.FullPath</c> - and used to reach the user exactly as each
        /// source happened to spell it, so the same list mixed separators and mixed absolute with relative.
        /// </summary>
        public static string ToCanonicalPath(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                return file;
            }

            var absolute = FileManager.IsRelative(file)
                ? GlueCommands.Self.GetAbsoluteFileName(file, false)
                : file;

            return absolute.Replace('\\', '/');
        }

        static void AddResetInheritanceOption(DeleteOptionsViewModel viewModel, GlueElement derivedElement)
        {
            viewModel.AddOption(
                $"Reset the inheritance for {derivedElement.Name}",
                new ResetInheritanceTag { DerivedElement = derivedElement });
        }

        /// <summary>
        /// Lets plugins add their own options (and the files those options bring with them) before the
        /// dialog is shown, then brings the file list in line with what is checked.
        /// </summary>
        static void FinishPlan(DeleteOptionsViewModel viewModel)
        {
            PluginManager.FillDeleteOptions(viewModel.Element, viewModel);
            viewModel.RefreshFilesToRemove();
        }

        /// <summary>
        /// The files deleting <paramref name="element"/> would orphan: its own code and JSON files, plus
        /// any file it references that nothing else in the project references.
        ///
        /// <c>GluxCommands.RemoveScreen</c>/<c>RemoveEntityAsync</c> call <see cref="FillWithOwnedFiles"/>
        /// from here rather than keeping their own copy, so the list the dialog shows up front cannot
        /// drift from the list the delete actually produces.
        /// </summary>
        public static List<string> GetFilesThatWouldBeRemoved(GlueElement element)
        {
            var toReturn = new List<string>();

            if (element == null)
            {
                return toReturn;
            }

            var referencedElsewhere = ObjectFinder.Self.GlueProject == null
                ? new HashSet<string>()
                : GetFilePathsReferencedOutsideOf(element);

            foreach (var rfs in element.ReferencedFiles)
            {
                var relativePath = rfs.GetRelativePath();
                if (!referencedElsewhere.Contains(relativePath))
                {
                    toReturn.Add(ToCanonicalPath(relativePath));
                }
            }

            FillWithOwnedFiles(toReturn, element);

            return toReturn.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Adds the code and JSON files an element owns outright - its custom and generated .cs, its event
        /// files and factory if they exist on disk, and its .glsj/.glej. Shared by the planner (to show the
        /// user) and by the delete itself (to act on), all as canonical paths.
        /// </summary>
        public static void FillWithOwnedFiles(List<string> filesThatCouldBeRemoved, GlueElement element)
        {
            var elementName = element.Name;

            Add(elementName + ".cs");
            Add(elementName + ".Generated.cs");

            AddIfExists(elementName + ".Event.cs");
            AddIfExists(elementName + ".Generated.Event.cs");
            AddIfExists("Factories/" + FileManager.RemovePath(elementName) + "Factory.Generated.cs");

            var extension = element is ScreenSave
                ? GlueProjectSave.ScreenExtension
                : GlueProjectSave.EntityExtension;
            Add(elementName + "." + extension);

            void Add(string relativeFile) => filesThatCouldBeRemoved.Add(ToCanonicalPath(relativeFile));

            void AddIfExists(string relativeFile)
            {
                if (System.IO.File.Exists(GlueCommands.Self.GetAbsoluteFileName(relativeFile, false)))
                {
                    Add(relativeFile);
                }
            }
        }

        static HashSet<string> GetFilePathsReferencedOutsideOf(GlueElement element)
        {
            var glueProject = ObjectFinder.Self.GlueProject;

            var otherElements = glueProject.Screens.Cast<GlueElement>()
                .Concat(glueProject.Entities)
                .Where(item => item != element);

            var toReturn = otherElements
                .SelectMany(item => item.ReferencedFiles)
                .Concat(glueProject.GlobalFiles)
                .Select(item => item.GetRelativePath())
                .ToHashSet();

            return toReturn;
        }
    }
}
