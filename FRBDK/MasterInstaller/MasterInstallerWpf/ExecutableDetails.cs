using System;
using System.Collections.Generic;

namespace MasterInstaller
{
    public struct ExecutableDetails
    {
        public string ExecutableName;
        public List<string> AdditionalFiles;
        public Action<object> ExtraLogic;
        public string[] Parameters;
        public bool RunAsAdministrator;
    }
}
