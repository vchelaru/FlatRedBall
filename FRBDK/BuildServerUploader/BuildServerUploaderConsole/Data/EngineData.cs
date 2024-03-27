using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuildServerUploaderConsole.Data
{
    public class EngineData
    {
        public string Name { get; set; }
        public List<EngineFileData> Files { get; set; } = new List<EngineFileData>();

        /// <summary>
        /// Files used by this engine in debug mode, relative to the Github root folder.
        /// </summary>
        public List<string> DebugFiles { get; set; } = new List<string>();

        /// <summary>
        /// Files used by this engine in release mode, relative to the Github root folder.
        /// </summary>
        public List<string> ReleaseFiles { get; set; } = new List<string>();

        public string RelativeToLibrariesDebugFolder { get; set; }
        public string RelativeToLibrariesReleaseFolder { get; set; }
        public string TemplateCsProjFolder { get; set; }

        public string EngineCSProjLocation { get; set; }

        public string TemplateName
        {
            get
            {
                var firstIndex = TemplateCsProjFolder.Replace("\\", "/").IndexOf("/");

                return TemplateCsProjFolder.Substring(0, firstIndex);
            }
        }

        public override string ToString()
        {
            return $"{TemplateCsProjFolder}{TemplateName}";
        }
    }
}
