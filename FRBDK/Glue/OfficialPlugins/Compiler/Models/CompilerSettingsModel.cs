using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.Compiler.Models
{
    public class CompilerSettingsModel
    {
        public bool GenerateGlueControlManagerCode { get; set; }
        public int PortNumber { get; set; } = 8021;
    }
}
