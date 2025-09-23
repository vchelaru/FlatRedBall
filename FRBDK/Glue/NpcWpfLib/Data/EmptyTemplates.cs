using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using ToolsUtilities;
using static System.Net.WebRequestMethods;

namespace Npc.Data
{
    static class EmptyTemplates
    {
        public static List<PlatformProjectInfo> Projects { get; private set; } = new List<PlatformProjectInfo>();

        static EmptyTemplates()
        {
            Add("Desktop GL .NET 9 (Windows, Mac, Linux) - MonoGame", "FlatRedBallDesktopGlMonoGameTemplate",
                "http://files.flatredball.com/content/FrbXnaTemplates/DailyBuild/ZippedTemplates/FlatRedBallDesktopGlMonoGameTemplate.zip");

            Add("Android .NET (Phone, Tablet, Fire TV) - MonoGame", "FlatRedBallAndroidMonoGameTemplate",
                "http://files.flatredball.com/content/FrbXnaTemplates/DailyBuild/ZippedTemplates/FlatRedBallAndroidMonoGameTemplate.zip");

            Add("iOS .NET (iPhone, iPad, iPod Touch) - MonoGame", "FlatRedBalliOSMonoGameTemplate",
                "http://files.flatredball.com/content/FrbXnaTemplates/DailyBuild/ZippedTemplates/FlatRedBalliOSMonoGameTemplate.zip");

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
