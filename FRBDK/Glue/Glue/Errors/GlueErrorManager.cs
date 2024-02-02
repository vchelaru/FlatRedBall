using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Errors
{
    public class GlueErrorManager
    {
        public List<ErrorReporterBase> ErrorReporters { get; private set; } = new List<ErrorReporterBase>();

        List<ErrorViewModel> errors = new List<ErrorViewModel>();

        public IEnumerable<ErrorViewModel> Errors => errors;

        public void Add(ErrorReporterBase errorReporter) => ErrorReporters.Add(errorReporter);

        public void Add(ErrorViewModel error)
        {
            var isAlreadyReferenced = errors.Any(item => item.UniqueId == error.UniqueId);

            if(!isAlreadyReferenced)
            {
                errors.Add(error);
            }

            // Vic says - I don't like this. I think maybe this should get moved out of a plugin?
            // Need to think about it a bit...
            PluginManager.CallPluginMethod("Error Window Plugin", "RefreshAllErrors");
        }

        public void ClearFixedErrors()
        {
            errors.RemoveAll(item => item.GetIfIsFixed());

            lock (GlueState.ErrorListSyncLock)
            {
                for (int i = GlueState.Self.ErrorList.Errors.Count - 1; i > -1; i--)
                {
                    if(GlueState.Self.ErrorList.Errors[i].GetIfIsFixed())
                    {
                        GlueState.Self.ErrorList.Errors.RemoveAt(i);
                    }
                }
            }
        }

        public void ClearFixedErrors(List<ErrorViewModel> errorsToCheck)
        {
            foreach (var error in errorsToCheck)
            {
                if(error.GetIfIsFixed())
                {
                    var id = error.UniqueId;
                    errors.RemoveAll(item => item.UniqueId == id);

                    lock (GlueState.ErrorListSyncLock)
                    {
                        GlueState.Self.ErrorList.Errors.RemoveAll(item => item.UniqueId == id);
                    }
                }
            }
        }
    }
}
