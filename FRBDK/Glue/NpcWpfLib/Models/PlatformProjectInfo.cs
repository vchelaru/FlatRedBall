using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolsUtilities;

namespace Npc
{
    public class PlatformProjectInfo
    {
        public string FriendlyName;
        public string Namespace;
        public string ZipName;
        public string Url;
        public FilePath LocalSourceFile;
        public bool SupportedInGlue;

        public override string ToString()
        {
            if(LocalSourceFile != null)
            {
                return LocalSourceFile.FullPath;
            }
            else
            {
                return FriendlyName;
            }
        }
    }

    public class AddNewLocalProjectOption : PlatformProjectInfo
    {
        public override string ToString() => "Select Local Project...";
    }

}
