using FlatRedBall.Glue.Errors;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.FilesPlugin.ErrorReporting
{
    internal class FilesErrorReporter : ErrorReporterBase
    {
        public override ErrorViewModel[] GetAllErrors()
        {
            return new ErrorViewModel[0];
        }
    }
}
