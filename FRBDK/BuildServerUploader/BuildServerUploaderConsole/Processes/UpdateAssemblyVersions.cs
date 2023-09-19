using System;
using System.Globalization;
using FlatRedBall.IO;
using System.Collections.Generic;
using BuildServerUploaderConsole.Data;
using System.Linq;

namespace BuildServerUploaderConsole.Processes
{
    class UpdateAssemblyVersions : ProcessStep
    {
        public static readonly string VersionString =
            DateTime.Today.Date.ToString("d", CultureInfo.CreateSpecificCulture("ja-JP")).Replace("/", ".") + "." +
            (int) DateTime.Now.TimeOfDay.TotalMinutes;

        public UpdateAssemblyVersions(IResults results) : base("Updates the AssemblyVersion in all FlatRedBall projects.", results)
        {
        }

        public override void ExecuteStep()
        {
            string engineDirectory = DirectoryHelper.EngineDirectory;

            List<string> engineFolders = new List<string>();
            engineFolders.Add(engineDirectory + @"FlatRedBallXNA\");
            engineFolders.Add(engineDirectory + @"FlatRedBallMDX\");
            


            foreach (string folder in engineFolders)
            {
                List<string> files = FileManager.GetAllFilesInDirectory(folder, "cs");

                foreach (string file in files)
                {
                    if (file.ToLower().EndsWith("assemblyinfo.cs"))
                    {
                        ModifyAssemblyInfoVersion(file, VersionString);
                        Results.WriteMessage("Modified " + file + " to " + VersionString);
                    }
                }
            }

            // If we list a csproj, then update that:
            foreach(var engine in AllData.Engines)
            {
                if(!string.IsNullOrEmpty(engine.EngineCSProjLocation))
                {
                    var csProjAbsolute = DirectoryHelper.CheckoutDirectory + engine.EngineCSProjLocation;
                    ModifyCsprojAssemblyInfoVersion(csProjAbsolute, VersionString);
                }
            }

            //ModifyAssemblyInfoVersion(DirectoryHelper.FrbdkDirectory + @"\Glue\Glue\Properties\AssemblyInfo.cs", VersionString);
            ModifyCsprojAssemblyInfoVersion(DirectoryHelper.FrbdkDirectory + @"Glue\Glue\GlueFormsCore.csproj", VersionString);
            ModifyAssemblyInfoVersion(DirectoryHelper.FrbdkDirectory + @"Glue\GlueSaveClasses\Properties\AssemblyInfo.cs", VersionString);

            ModifyCsprojAssemblyInfoVersion(DirectoryHelper.FrbdkDirectory + @"AnimationEditor\PreviewProject\AnimationEditor.csproj", VersionString);

            var net6Engine = AllData.Engines.First(item => item.EngineCSProjLocation?.Contains("FlatRedBallDesktopGLNet6.csproj") == true);
            var templateLocation = net6Engine.TemplateFolder + "FlatRedBallDesktopGlNet6Template.csproj";
            ModifyNugetVersionInAssembly(DirectoryHelper.TemplateDirectory + templateLocation, VersionString);

            Results.WriteMessage("Glue assembly versions updated to " + VersionString);



            //Save Version String for uploading
            FileManager.SaveText(VersionString, DirectoryHelper.ReleaseDirectory + @"\SingleDlls\VersionInfo.txt");
            Results.WriteMessage("VersionInfo file created.");
        }

        private static void ModifyAssemblyInfoVersion(string assemblyInfoLocation, string versionString)
        {
            string assemblyInfoText = FileManager.FromFileText(assemblyInfoLocation);

            assemblyInfoText = System.Text.RegularExpressions.Regex.Replace(assemblyInfoText,
                        "AssemblyVersion\\(\"[0-9]+\\.[0-9]+\\.[0-9]+\\.[0-9]+",
                        "AssemblyVersion(\"" + versionString);
            assemblyInfoText = System.Text.RegularExpressions.Regex.Replace(assemblyInfoText,
                        "AssemblyFileVersion\\(\"[0-9]+\\.[0-9]+\\.[0-9]+\\.[0-9]+",
                        "AssemblyFileVersion(\"" + versionString);
            FileManager.SaveText(assemblyInfoText, assemblyInfoLocation);

        }

        private static void ModifyCsprojAssemblyInfoVersion(string csprojLocation, string versionString)
        {
            if(System.IO.File.Exists(csprojLocation) == false)
            {
                throw new ArgumentException($"Could not find file {csprojLocation}");
            }
            // Look for <Version>1.1.0.0</Version>
            string assemblyInfoText = FileManager.FromFileText(csprojLocation);

            assemblyInfoText = System.Text.RegularExpressions.Regex.Replace(assemblyInfoText,
                        "<Version>[0-9]*.[0-9]*.[0-9]*.[0-9]*</Version>",
                        $"<Version>{versionString}</Version>");
            FileManager.SaveText(assemblyInfoText, csprojLocation);

        }

        private static void ModifyNugetVersionInAssembly(string csprojLocation, string versionString)
        {
            if (System.IO.File.Exists(csprojLocation) == false)
            {
                throw new ArgumentException($"Could not find file {csprojLocation}");
            }

            string csprojText = FileManager.FromFileText(csprojLocation);

            csprojText = System.Text.RegularExpressions.Regex.Replace(csprojText,
                        "<PackageReference Include=\"FlatRedBallDesktopGLNet6\" Version=\"[0-9]*.[0-9]*.[0-9]*.[0-9]*\" />",
                        $"<PackageReference Include=\"FlatRedBallDesktopGLNet6\" Version=\"{versionString}\" />");

            FileManager.SaveText(csprojText, csprojLocation);
        }

    }
}
