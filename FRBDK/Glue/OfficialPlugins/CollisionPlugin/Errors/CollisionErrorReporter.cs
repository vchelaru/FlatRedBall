using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPluginsCore.CollisionPlugin.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OfficialPluginsCore.CollisionPlugin.Errors
{
    class CollisionErrorReporter : IErrorReporter
    {
        public ErrorViewModel[] GetAllErrors()
        {
            List<ErrorViewModel> errorList = null;
            var project = GlueState.Self.CurrentGlueProject;

            foreach(var namedObject in ObjectFinder.Self.GetAllNamedObjects())
            {
                var error = TryCreateErrorFor(namedObject);

                if(error != null)
                {
                    if(errorList == null)
                    {
                        errorList = new List<ErrorViewModel>();
                    }
                    errorList.Add(error);
                }
            }

            return errorList?.ToArray();
        }

        private ErrorViewModel TryCreateErrorFor(NamedObjectSave namedObject)
        {
            if(namedObject.IsCollisionRelationship())
            {
                var container = ObjectFinder.Self.GetElementContaining(namedObject);

                var errorMessage = CollisionRelationshipErrorViewModel.TryGetErrorMessageFor(namedObject, container);

                if(!string.IsNullOrWhiteSpace( errorMessage ))
                {
                    return new CollisionRelationshipErrorViewModel(namedObject, container);
                }
            }
            return null;
        }
    }
}
