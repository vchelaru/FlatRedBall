using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OfficialPlugins.StateDataPlugin.StateError
{
    class StateErrorReporter : ErrorReporterBase
    {
        public override ErrorViewModel[] GetAllErrors()
        {
            var project = GlueState.Self.CurrentGlueProject;

            var errorList = new List<ErrorViewModel>();

            foreach (var element in project.Screens)
            {
                AddErrorsFor(element, errorList);
            }
            foreach(var element in project.Entities)
            {
                AddErrorsFor(element, errorList);
            }

            return errorList.ToArray();
        }

        private void AddErrorsFor(GlueElement element, List<ErrorViewModel> errorList)
        {
            foreach(var nos in element.AllNamedObjects)
            {
                var nosElement = ObjectFinder.Self.GetElement(nos);

                foreach(var variable in nos.InstructionSaves)
                {
                    if(nosElement != null)
                    {
                        var variableInElement = nosElement.CustomVariables
                            .FirstOrDefault(item => item.Name == variable.Member);

                        if(variableInElement?.GetIsVariableState(nosElement) == true)
                        {
                            var elementVariableType = variableInElement.Type;

                            var category = nosElement.GetStateCategoryRecursively(elementVariableType);

                            if(category != null)
                            {
                                var valueAsString = variable.Value as string;
                                var hasValidState = valueAsString == "<NONE>" || category.GetState(valueAsString) != null ;

                                if(!hasValidState)
                                {
                                    var error = new MissingStateViewModel();
                                    error.GlueElement = element;
                                    error.NamedObjectSave = nos;
                                    error.NamedObjectVariableClone = FileManager.CloneObject( variable);

                                    error.RefreshDetail();

                                    errorList.Add(error);
                                }
                            }
                        }
                    }
                }
            }

        }
    }
}
