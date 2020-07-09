using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuildServerUploaderConsole.Data
{
    public class EngineData
    {
        public List<EngineFileData> Files { get; set; } = new List<EngineFileData>();

        public List<string> DebugFiles { get; set; } = new List<string>();

        public List<string> ReleaseFiles { get; set; } = new List<string>();

        public string RelativeToLibrariesDebugFolder { get; set; }
        public string RelativeToLibrariesReleaseFolder { get; set; }
        public string TemplateFolder { get; set; }

        public string TemplateName
        {
            get
            {
                var firstIndex = TemplateFolder.Replace("\\", "/").IndexOf("/");

                return TemplateFolder.Substring(0, firstIndex);
            }
        }

        public override string ToString()
        {
            return $"{TemplateFolder}{TemplateName}";
        }
    }
}
