using FlatRedBall.Glue.Errors;
using Gum.DataTypes;
using GumPlugin.Managers;
using GumPluginCore.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPluginCore.ErrorReporting
{
    class ErrorReporter : ErrorReporterBase
    {
        public override ErrorViewModel[] GetAllErrors()
        {
            List<ErrorViewModel> errors = new List<ErrorViewModel>();

            FillWithSameNamedAnimationsAndCategories(errors);

            return errors.ToArray();
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
