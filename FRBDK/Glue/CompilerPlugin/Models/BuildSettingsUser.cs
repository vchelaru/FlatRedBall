using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerPlugin.Models
{
    public class BuildSettingsUser
    {
        public string CustomMsBuildLocation { get; set; }

        public bool UseMsBuildServer { get; set; }
    }
}
