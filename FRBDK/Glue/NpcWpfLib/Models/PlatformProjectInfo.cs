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

    /// <summary>
    /// A template the dotnet CLI owns rather than one this repo zips and uploads. FlatRedBall 2 ships
    /// its templates as a nuget package, so creating one of these is `dotnet new`, not download and
    /// unzip - and none of the renaming, guid rewriting or zip-pipeline bookkeeping applies, because
    /// the CLI does that itself.
    /// </summary>
    public class DotnetNewProjectInfo : PlatformProjectInfo
    {
        /// <summary>The nuget package holding the template, e.g. FlatRedBall2.Templates.</summary>
        public string TemplatePackageId;

        /// <summary>The template's short name, e.g. frb2-desktop.</summary>
        public string TemplateShortName;

        /// <summary>
        /// The project the created game should be opened by, relative to the created folder, using
        /// {ProjectName} for the name the user chose. `dotnet new frb2-desktop` produces a .Common and
        /// a .Desktop, and .Common is the one Glue edits.
        /// </summary>
        public string ProjectToOpenPattern;
    }

}
