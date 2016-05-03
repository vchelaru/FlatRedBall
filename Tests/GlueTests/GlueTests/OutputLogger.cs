using System;
using System.Diagnostics;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace GlueTests
{
    class OutputLogger : Logger
    {
        public bool ShowWarnings { get; set; }

        public bool ShowMessages { get; set; }

        public override void Initialize(Microsoft.Build.Framework.IEventSource eventSource)
        {
            if (eventSource == null) return; ;

            //Register for the ProjectStarted, TargetStarted, and ProjectFinished events
            eventSource.ProjectStarted += eventSource_ProjectStarted;
            eventSource.TargetStarted += eventSource_TargetStarted;
            eventSource.ProjectFinished += eventSource_ProjectFinished;
            eventSource.MessageRaised += eventSource_MessageRaised;
            eventSource.WarningRaised += eventSource_WarningRaised;
            eventSource.ErrorRaised += eventSource_ErrorRaised;
        }

        private void eventSource_ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            Trace.WriteLine(@"Error: " + e.Message);
        }

        private void eventSource_WarningRaised(object sender, BuildWarningEventArgs e)
        {
            if(ShowWarnings)
                Trace.WriteLine(@"Warning: " + e.Message);
        }

        private void eventSource_MessageRaised(object sender, BuildMessageEventArgs e)
        {
            if(ShowMessages)
                Trace.WriteLine(@"Message: " + e.Message);
        }

        void eventSource_ProjectStarted(object sender, ProjectStartedEventArgs e)
        {
            Trace.WriteLine(@"Project Started: " + e.ProjectFile);
        }

        void eventSource_ProjectFinished(object sender, ProjectFinishedEventArgs e)
        {
            Trace.WriteLine(@"Project Finished: " + e.ProjectFile);
        }

        void eventSource_TargetStarted(object sender, TargetStartedEventArgs e)
        {
            if (Verbosity == LoggerVerbosity.Detailed)
            {
                Trace.WriteLine("Target Started: " + e.TargetName);
            }
        }
    }
}
