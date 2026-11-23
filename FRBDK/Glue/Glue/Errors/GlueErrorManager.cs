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
        }

        /// <summary>
        /// Every current error: what each reporter's <see cref="ErrorReporterBase.GetAllErrors"/> returns now,
        /// plus errors added directly that are not fixed. The same set the Errors tab shows after a refresh,
        /// computed without touching the tab. A reporter that throws is returned in
        /// <paramref name="reporterFailures"/> rather than stopping the others.
        /// </summary>
        public List<ErrorViewModel> GetAllCurrentErrors(out List<string> reporterFailures)
        {
            var toReturn = new List<ErrorViewModel>();
            reporterFailures = new List<string>();

            foreach (var reporter in ErrorReporters.ToList())
            {
                try
                {
                    var reported = reporter.GetAllErrors();
                    if (reported != null)
                    {
                        toReturn.AddRange(reported);
                    }
                }
                catch (Exception e)
                {
                    reporterFailures.Add($"{reporter.GetType().FullName} threw: {e}");
                }
            }

            toReturn.AddRange(errors.Where(item => !item.GetIfIsFixed()));

            return toReturn
                .GroupBy(item => item.UniqueId)
                .Select(group => group.First())
                .ToList();
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
