using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.StartupScreenPlugin.Errors
{
    class ErrorReporter : ErrorReporterBase
    {
        public override ErrorViewModel[] GetAllErrors()
        {
            List<ErrorViewModel> listToReturn = new List<ErrorViewModel>();

            TryCreateErrorForAbstractStartup(listToReturn);

            return listToReturn.ToArray();
        }

        private void TryCreateErrorForAbstractStartup(List<ErrorViewModel> errorList)
        {
            var startupScreen = GlueState.Self.CurrentGlueProject?.StartUpScreen;

            ////////////Early Out///////////////////////////
            if(string.IsNullOrEmpty(startupScreen))
            {
                return;
            }
            //////////End Early Out/////////////////////////

            var screen = ObjectFinder.Self.GetScreenSave(startupScreen);

            if(screen != null && StartupAbstractErrorViewModel.IsAbstract(screen))
            {
                var error = new StartupAbstractErrorViewModel(screen);
                errorList.Add(error);
            }

        }
    }
}
