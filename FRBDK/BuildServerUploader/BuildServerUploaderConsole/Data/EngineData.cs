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

        /// <summary>
        /// Every NuGet package this engine ships. All of them are versioned together at release
        /// time and pushed in one step, so a project restores a matched set instead of pairing a
        /// current engine package with whatever Gum happened to be in its Libraries folder.
        /// </summary>
        public List<PackageData> Packages { get; set; } = new List<PackageData>();

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
