using FlatRedBall.Glue.Errors;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.FilesPlugin.ErrorReporting
{
    internal class FilesErrorReporter : IErrorReporter
    {
        public ErrorViewModel[] GetAllErrors()
        {
            return new ErrorViewModel[0];
        }
    }
}
