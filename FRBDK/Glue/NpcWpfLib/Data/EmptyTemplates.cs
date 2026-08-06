using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using ToolsUtilities;
using static System.Net.WebRequestMethods;

namespace Npc.Data
{
    // The platform list the New Project wizard shows. Picking an entry downloads the zip the release
    // uploaded, so a platform belongs here only while its engine is registered in the release
    // tooling's AllData -- otherwise the wizard hands out whatever zip was last uploaded, however
    // old, with nothing to say so. NewProjectTemplateListTests pins that.
    public static class EmptyTemplates
    {
        public static List<PlatformProjectInfo> Projects { get; private set; } = new List<PlatformProjectInfo>();

        static EmptyTemplates()
        {
            Add("Desktop GL .NET 9 (Windows, Mac, Linux) - MonoGame", "FlatRedBallDesktopGlMonoGameTemplate",
                "http://files.flatredball.com/content/FrbXnaTemplates/DailyBuild/ZippedTemplates/FlatRedBallDesktopGlMonoGameTemplate.zip");

            // Android and iOS are deliberately absent: their engines stopped building when the
            // net8.0-android and net8.0-ios workloads went end of life, so the newest zip on the
            // server predates that. Restore these entries together with the commented-out blocks in
            // AllData and Engine.yml once the mobile projects are retargeted off net8.

            Add("Web (Browsers) - Kni", "FlatRedBallWebTemplate",
                "https://files.flatredball.com/content/FrbXnaTemplates/DailyBuild/ZippedTemplates/FlatRedBallWebTemplate.zip");

            Add("FNA .NET 7 (Windows, Mac, Linux)", "FlatRedBallDesktopFnaTemplate",
                "http://files.flatredball.com/content/FrbXnaTemplates/DailyBuild/ZippedTemplates/FlatRedBallDesktopFnaTemplate.zip");

            Projects.Add(new AddNewLocalProjectOption());
        }

        static void Add(string friendlyName, string namespaceName, string url, bool supportedInGlue = true)
        {
            var newItem = new PlatformProjectInfo();

            var zipName = FileManager.RemovePath(url);

            newItem.FriendlyName = friendlyName;
            newItem.Namespace = namespaceName;
            newItem.ZipName = zipName;
            newItem.Url = url;
            newItem.SupportedInGlue = supportedInGlue;

            Projects.Add(newItem);
        }
    }
}
