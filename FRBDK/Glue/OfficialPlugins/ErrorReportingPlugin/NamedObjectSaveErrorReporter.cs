using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.ErrorReportingPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OfficialPlugins.ErrorReportingPlugin
{
    internal class NamedObjectSaveErrorReporter : ErrorReporterBase
    {
        public override ErrorViewModel[] GetAllErrors()
        {
            List<ErrorViewModel> errors = new List<ErrorViewModel>();

            FillWithBadSetByDerived(errors);

            FillWithBadInstantiateByBaseDerived(errors);

            // This could eventually be moved to an object file, but for now...
            FillWithBadFileRelatedProperties(errors);

            return errors.ToArray();
        }

        private void FillWithBadFileRelatedProperties(List<ErrorViewModel> errors)
        {
            var project = GlueState.Self.CurrentGlueProject;
            foreach (var screen in project.Screens)
            {
                var availableSourceFiles = AvailableFileStringConverter.GetAvailableOptions(screen, true, false)
                    .Select(item => item.ToLowerInvariant())
                    .ToList();

                foreach (var nos in screen.AllNamedObjects)
                {
                    FillWithBadFileRelatedProperties(nos, availableSourceFiles, errors);
                }
            }
            foreach (var entity in project.Entities)
            {
                var availableSourceFiles = AvailableFileStringConverter.GetAvailableOptions(entity, true, false)
                    .Select(item => item.ToLowerInvariant())
                    .ToList();

                foreach (var nos in entity.AllNamedObjects)
                {
                    FillWithBadFileRelatedProperties(nos, availableSourceFiles, errors);
                }
            }
        }


        private void FillWithBadFileRelatedProperties(NamedObjectSave nos, List<string> availableSourceFiles, List<ErrorViewModel> errors)
        { 
            if (nos.SourceType == SourceType.File)
            {
                if(!string.IsNullOrEmpty(nos.SourceFile))
                { 
                    if(availableSourceFiles.Contains(nos.SourceFile.ToLowerInvariant()) == false)
                    {
                        var error = new MissingNamedObjectSourceFileViewModel(nos);
                        errors.Add(error);
                    }
                }

                if (!string.IsNullOrEmpty(nos.SourceFile) &&
                    !string.IsNullOrEmpty(nos.SourceName) && 
                    !nos.IsEntireFile)
                {
                    var availableObjects = AvailableNameablesStringConverter.GetAvailableNamedObjectSourceNames(nos);

                    if(!availableObjects.Contains(nos.SourceName))
                    {
                        var error = new MissingNamedObjectSourceNameErrorViewModel(nos);
                        errors.Add(error);
                    }
                }
            }
        }

        private void FillWithBadSetByDerived(List<ErrorViewModel> errors)
        {
            foreach(var screen in GlueState.Self.CurrentGlueProject.Screens)
            {
                FillWithBadSetByDerived(screen, errors);
            }

            foreach(var entity in GlueState.Self.CurrentGlueProject.Entities)
            {
                FillWithBadSetByDerived(entity, errors);
            }
        }

        private void FillWithBadInstantiateByBaseDerived(List<ErrorViewModel> errors)
        {
            foreach (var screen in GlueState.Self.CurrentGlueProject.Screens)
            {
                FillWithBadInstantiateByBaseDerived(screen, errors);
            }

            foreach (var entity in GlueState.Self.CurrentGlueProject.Entities)
            {
                FillWithBadInstantiateByBaseDerived(entity, errors);
            }
        }

        private void FillWithBadSetByDerived(GlueElement derivedElement, List<ErrorViewModel> errors)
        {
            var baseElements = ObjectFinder.Self.GetAllBaseElementsRecursively(derivedElement);
            foreach(var derivedNos in derivedElement.AllNamedObjects)
            {
                if(derivedNos.DefinedByBase == false)
                {
                    // This is defined here, make sure there are no objects in base objects
                    // with the same name which are not SetByDerived

                    foreach(var baseElement in baseElements)
                    {
                        var baseNos = baseElement.AllNamedObjects
                            .FirstOrDefault(item => item.InstanceName== derivedNos.InstanceName);

                        if(baseNos != null && baseNos.SetByDerived == false && baseNos.ExposedInDerived == false)
                        {
                            var errorVm = new InvalidSetByDerivedErrorViewModel(baseNos, derivedNos);
                            errors.Add(errorVm);
                        }
                    }
                }
            }
        }

        private void FillWithBadInstantiateByBaseDerived(GlueElement derivedElement, List<ErrorViewModel> errors)
        {
            var baseElements = ObjectFinder.Self.GetAllBaseElementsRecursively(derivedElement);
            foreach (var derivedNos in derivedElement.AllNamedObjects)
            {
                var hasError = InvalidInstantiateByBaseErrorViewModel.GetIfHasError(derivedNos, derivedElement, baseElements);

                if(hasError)
                {
                    var errorVm = new InvalidInstantiateByBaseErrorViewModel(derivedNos);
                    errors.Add(errorVm);

                }
            }
        }
    }
}
