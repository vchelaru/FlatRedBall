using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public enum OperationResult
    {
        Success,
        Failure
    }

    public class ValidationResponse
    {
        public OperationResult OperationResult { get; set; }

        public string Message { get; set; }
    }
}
