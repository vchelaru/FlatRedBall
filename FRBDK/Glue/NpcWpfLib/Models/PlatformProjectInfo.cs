using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolsUtilities;

namespace Npc
{
    public class PlatformProjectInfo
    {
        /// <summary>The engine version an entry belongs to. The platform dropdown groups on these.</summary>
        public const string Frb1Category = "FlatRedBall 1";
        public const string Frb2Category = "FlatRedBall 2";
        public const string OtherCategory = "Other";

        /// <summary>
        /// The dropdown row's main line, such as "Desktop - MonoGame". Deliberately short and free of the
        /// engine version, which <see cref="Category"/> supplies as a group header instead.
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>The group header this entry sits under - one of the category constants above.</summary>
        public string Category { get; set; }

        /// <summary>
        /// The dropdown row's muted second line, such as "Windows, Mac, Linux · .NET 9". This is where
        /// everything that used to make the single-line names unreadably long lives. Null hides the line.
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// <see cref="Category"/> and <see cref="FriendlyName"/> joined. A closed ComboBox draws no group
        /// header, so "Desktop - MonoGame" on its own would not say which engine version is selected -
        /// exactly the confusion the short names are meant to remove. The closed box shows this instead.
        /// </summary>
        public string QualifiedFriendlyName =>
            string.IsNullOrEmpty(Category) ? FriendlyName : $"{Category} - {FriendlyName}";

        public string Namespace;
        public string ZipName;
        public string Url;
        public FilePath LocalSourceFile;
        public bool SupportedInGlue;

        public override string ToString() => QualifiedFriendlyName;
    }

    public class AddNewLocalProjectOption : PlatformProjectInfo
    {
        public AddNewLocalProjectOption()
        {
            FriendlyName = "Select Local Project...";
            Category = OtherCategory;
        }
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
