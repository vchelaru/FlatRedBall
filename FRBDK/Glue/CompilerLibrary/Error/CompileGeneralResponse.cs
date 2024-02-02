using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace CompilerLibrary.Error
{
    public class CompileGeneralResponse : GeneralResponse
    {
        public bool WasCancelled { get; set; }

    }
}
