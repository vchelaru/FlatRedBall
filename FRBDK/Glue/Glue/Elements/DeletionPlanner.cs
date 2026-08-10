using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
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

        static DeleteOptionsViewModel CreateForElement(GlueElement element)
        {
            var viewModel = new DeleteOptionsViewModel
            {
                Element = element,
                Message = $"Are you sure you want to delete {element}?"
            };

            viewModel.AlwaysRemovedFiles.AddRange(GetFilesThatWouldBeRemoved(element));

            return viewModel;
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
                    toReturn.Add(relativePath);
                }
            }

            FillWithOwnedFiles(toReturn, element);

            return toReturn.Distinct().ToList();
        }

        /// <summary>
        /// Adds the code and JSON files an element owns outright - its custom and generated .cs, its event
        /// files and factory if they exist on disk, and its .glsj/.glej. Shared by the planner (to show the
        /// user) and by the delete itself (to act on).
        /// </summary>
        public static void FillWithOwnedFiles(List<string> filesThatCouldBeRemoved, GlueElement element)
        {
            var elementName = element.Name;

            filesThatCouldBeRemoved.Add(elementName + ".cs");
            filesThatCouldBeRemoved.Add(elementName + ".Generated.cs");

            AddIfExists(elementName + ".Event.cs");
            AddIfExists(elementName + ".Generated.Event.cs");

            var factoryName = "Factories/" + FileManager.RemovePath(elementName) + "Factory.Generated.cs";
            var absoluteFactoryName = GlueCommands.Self.GetAbsoluteFileName(factoryName, false);
            if (System.IO.File.Exists(absoluteFactoryName))
            {
                filesThatCouldBeRemoved.Add(absoluteFactoryName);
            }

            var extension = element is ScreenSave
                ? GlueProjectSave.ScreenExtension
                : GlueProjectSave.EntityExtension;
            filesThatCouldBeRemoved.Add(elementName + "." + extension);

            void AddIfExists(string relativeFile)
            {
                if (System.IO.File.Exists(GlueCommands.Self.GetAbsoluteFileName(relativeFile, false)))
                {
                    filesThatCouldBeRemoved.Add(relativeFile);
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
