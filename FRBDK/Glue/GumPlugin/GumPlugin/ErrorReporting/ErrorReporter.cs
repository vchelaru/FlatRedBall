using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using Gum.DataTypes;
using GumPlugin.Managers;
using GumPluginCore.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FrbObjectFinder = FlatRedBall.Glue.Elements.ObjectFinder;

namespace GumPluginCore.ErrorReporting
{
    class ErrorReporter : ErrorReporterBase
    {
        public override ErrorViewModel[] GetAllErrors()
        {
            List<ErrorViewModel> errors = new List<ErrorViewModel>();

            FillWithSameNamedAnimationsAndCategories(errors);

            FillWithReferencedFilesNotInGumx(errors);

            return errors.ToArray();
        }

        private void FillWithReferencedFilesNotInGumx(List<ErrorViewModel> errors)
        {
            var allReferencedFiles = FrbObjectFinder.Self.GetAllReferencedFiles();
            var gumProject = AppState.Self.GumProjectSave;
            var gumProjectFolder = AppState.Self.GumProjectFolder;

            foreach (var rfs in allReferencedFiles)
            {
                var extension = FileManager.GetExtension(rfs.Name);
                if(extension == "gusx")
                {
                    // is it referenced?
                    var absoluteFile = GlueCommands.Self.GetAbsoluteFilePath(rfs);

                    var relativeToGumFolder = absoluteFile.RemoveExtension().RelativeTo(gumProjectFolder).Substring("Screens/".Length);

                    var references = gumProject.ScreenReferences.Any(item => item.Name.ToLowerInvariant() == relativeToGumFolder.ToLowerInvariant());

                    if(!references)
                    {
                        // we have a problem!
                        var vm = new ReferencedFileNotInGumxViewModel(absoluteFile);
                        errors.Add(vm);
                    }
                }
            }
        }

        private void FillWithSameNamedAnimationsAndCategories(List<ErrorViewModel> errors)
        {
            var project = AppState.Self.GumProjectSave;
            if (project != null)
            {
                foreach (var screen in project.Screens)
                {
                    FillWithSameNamedAnimationsAndCategories(screen, errors);
                }
                foreach(var component in project.Components)
                {
                    FillWithSameNamedAnimationsAndCategories(component, errors);
                }
            }
        }

        private void FillWithSameNamedAnimationsAndCategories(ElementSave element, List<ErrorViewModel> errors)
        {
            var animations = AnimationLogic.GetAnimationsFor(element);
            if(animations != null)
            {
                foreach(var category in element.Categories)
                {
                    var matchingAnimation = animations.Animations
                        .FirstOrDefault(item => item.Name + "Animation" == category.Name);

                    if(matchingAnimation != null)
                    {
                        var error = new AnimationCategoryNamingError(
                            element.Name, category.Name, matchingAnimation.Name);

                        errors.Add(error);
                    }
                }
            }
        }
    }
}
