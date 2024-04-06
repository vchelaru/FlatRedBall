using System;
using System.Globalization;
using FlatRedBall.IO;
using System.Collections.Generic;
using BuildServerUploaderConsole.Data;
using System.Linq;

namespace BuildServerUploaderConsole.Processes
{
    public enum UpdateType
    {
        Engine,
        FRBDK
    }

    class UpdateAssemblyVersions : ProcessStep
    {
        public static readonly string VersionStringForThisRun =
            DateTime.Now.ToString("yyyy.M.d") + "." + (int)DateTime.Now.TimeOfDay.TotalMinutes;

        public static string GetVersionString(bool isBeta)
        {
            var toReturn = VersionStringForThisRun;
            if(isBeta)
            {
                toReturn += "-beta";
            }
            return toReturn;
        }

        UpdateType UpdateType { get; set; }

        bool IsBeta { get; set; }

        public UpdateAssemblyVersions(IResults results, UpdateType updateType, bool isBeta) : base("Updates the AssemblyVersion in all FlatRedBall projects.", results)
        {
            IsBeta = isBeta;
            this.UpdateType = updateType;
        }

        public override void ExecuteStep()
        {

            switch(UpdateType)
            {
                case UpdateType.Engine:

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
                                ModifyAssemblyInfoVersion(file, GetVersionString(IsBeta));
                                Results.WriteMessage("Modified " + file + " to " + GetVersionString(IsBeta));
                            }
                        }
                    }


                    // If we list a csproj, then update that:
                    foreach (var engine in AllData.Engines)
                    {
                        if (!string.IsNullOrEmpty(engine.EngineCSProjLocation))
                        {
                            var csProjAbsolute = DirectoryHelper.CheckoutDirectory + engine.EngineCSProjLocation;
                            ModifyCsprojAssemblyInfoVersion(csProjAbsolute, GetVersionString(IsBeta));
                            Results.WriteMessage("Modified " + csProjAbsolute + " to " + GetVersionString(IsBeta));

                        }
                    }

                    UpdateTemplateNugets();

                    Results.WriteMessage("Engine and Template assembly versions updated to " + GetVersionString(IsBeta));


                    break;
                case UpdateType.FRBDK:

                    //ModifyAssemblyInfoVersion(DirectoryHelper.FrbdkDirectory + @"\Glue\Glue\Properties\AssemblyInfo.cs", VersionString);
                    ModifyCsprojAssemblyInfoVersion(DirectoryHelper.FrbdkDirectory + @"Glue\Glue\GlueFormsCore.csproj", GetVersionString(IsBeta));
                    ModifyAssemblyInfoVersion(DirectoryHelper.FrbdkDirectory + @"Glue\GlueSaveClasses\Properties\AssemblyInfo.cs", GetVersionString(IsBeta));

                    ModifyCsprojAssemblyInfoVersion(DirectoryHelper.FrbdkDirectory + @"AnimationEditor\PreviewProject\AnimationEditor.csproj", GetVersionString(IsBeta));
                    break;
            }




            //Save Version String for uploading
            var destination = DirectoryHelper.ReleaseDirectory + @"\SingleDlls\VersionInfo.txt";
            if(IsBeta)
            {
                destination = DirectoryHelper.ReleaseDirectory + @"\SingleDlls\VersionInfo-beta.txt";
            }
            FileManager.SaveText(GetVersionString(IsBeta), DirectoryHelper.ReleaseDirectory + destination);
            Results.WriteMessage("VersionInfo file created.");
        }

        private void UpdateTemplateNugets()
        {
            var engineName = "FlatRedBallDesktopGLNet6";
            var templateName = "FlatRedBallDesktopGLNet6Template";

            UpdateTemplateNuget(engineName, templateName);

            UpdateTemplateNuget("FlatRedBall.FNA", "FlatRedBallDesktopFnaTemplate");

            UpdateTemplateNuget("FlatRedBallAndroid", "FlatRedBallAndroidMonoGameTemplate");

            UpdateTemplateNuget("FlatRedBalliOS", "FlatRedBalliOSMonoGameTemplate");
        }

        private void UpdateTemplateNuget(string engineName, string templateName)
        {
            var matchingEngine = AllData.Engines.First(item => item.EngineCSProjLocation?.Contains($"{engineName}.csproj") == true);
            var templateLocation = matchingEngine.TemplateCsProjFolder + templateName + ".csproj";
            ModifyNugetVersionInAssembly(DirectoryHelper.TemplateDirectory + templateLocation, engineName, GetVersionString(IsBeta));
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

        private void ModifyNugetVersionInAssembly(string csprojLocation, string packageName, string versionString)
        {
            if (System.IO.File.Exists(csprojLocation) == false)
            {
                throw new ArgumentException($"Could not find file {csprojLocation}");
            }

            string csprojText = FileManager.FromFileText(csprojLocation);

            csprojText = System.Text.RegularExpressions.Regex.Replace(csprojText,
                        $"<PackageReference Include=\"{packageName}\" Version=\"[0-9]*.[0-9]*.[0-9]*.[0-9]*\" />",
                        $"<PackageReference Include=\"{packageName}\" Version=\"{versionString}\" />");

            Results.WriteMessage("Modified " + csprojLocation + $" to have FlatRedBall Nuget package {versionString}");


            FileManager.SaveText(csprojText, csprojLocation);
        }

    }
}
