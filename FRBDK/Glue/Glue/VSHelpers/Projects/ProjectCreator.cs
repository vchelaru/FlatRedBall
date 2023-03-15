using System;
using System.Linq;
using FlatRedBall.IO;
using Microsoft.Build.Evaluation;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using System.Collections.Generic;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Controls;
using System.Diagnostics;
using System.Text.RegularExpressions;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Math.Paths;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class CreateProjectResult
    {
        public ProjectBase Project { get; set; }
        public bool ShouldTryToLoadProject { get; set; } = true;
    }

    public static class ProjectCreator
    {


        public static ProjectBase LoadXnaProjectFor(ProjectBase masterProject, string fileName)
        {
            ProjectBase project = CreateProject(fileName).Project;

            project.Load(fileName);

            project.MasterProjectBase = masterProject;

            project.IsContentProject = true;

            return project;

        }


        public static CreateProjectResult CreateProject(string fileName)
        {
            //Project coreVisualStudioProject = new Project(fileName);
            Project coreVisualStudioProject = null;
            CreateProjectResult result = new CreateProjectResult();

            //SetMsBuildEnvironmentVariable();

            var didErrorOccur = false;
            try
            {
                try
                {
                    coreVisualStudioProject = new Project(fileName, null, null, new ProjectCollection());
                }
                catch (Microsoft.Build.Exceptions.InvalidProjectFileException exception)
                {
                    // This is a bug I haven't been able to figure out, but have asked about here:
                    // https://stackoverflow.com/questions/46384075/why-does-microsoft-build-framework-dll-15-3-not-load-csproj-15-1-does
                    // So I'm going to hack a fix by checking if this has to do with 15.0 vs the other versions:

                    var message = exception.Message;
                    if(exception.Message.Contains("\"15.0\"") && exception.Message.Contains("\"14.0\""))
                    {
                        coreVisualStudioProject = new Project(fileName, null, "14.0", new ProjectCollection());
                    }
                    else
                    {
                        throw exception;
                    }
                }

            }
            catch (Microsoft.Build.Exceptions.InvalidProjectFileException exception)
            {
                didErrorOccur = true;
                var exceptionMessage = exception.Message;

                GlueCommands.Self.PrintError(exceptionMessage);

                var shouldThrowException = true;
                string locationToOpen = null;

                bool isMissingMonoGameTargets = exceptionMessage.Contains("MonoGame.Content.Builder.targets\"");
                string message = null;
                if(isMissingMonoGameTargets)
                {

                    // This gets installed here: C:\Program Files (x86)\MSBuild\MonoGame\v3.0
                    string programFilesX86Path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                    var monogamePath = System.IO.Path.Combine(programFilesX86Path, "MSBuild", "MonoGame", "v3.0", "MonoGame.Content.Builder.targets");
                    var hasMonoGame = System.IO.File.Exists(monogamePath);

                    if(!hasMonoGame)
                    {
                        //locationToOpen = "https://github.com/MonoGame/MonoGame/releases/download/v3.7.1/MonoGameSetup.exe";
                        locationToOpen =
                            "https://github.com/MonoGame/MonoGame/releases/download/v3.7.1/MonoGameSetup.exe";
                        //"https://community.monogame.net/t/monogame-3-7-1/11173";
                        message = $"Could not load the project {fileName}\nbecause MonoGame 3.7.1 files are missing. click OK to open your browser to the MonoGame 3.7.1 install location:\n\n" +
                            locationToOpen + "\n\n" +
                            "Alternatively you can remove the MonoGame content project from your .csproj. You probably don't need it if FlatRedBall is handling your content building." +
                            "To do this, search your .csproj file for the following text and delete it\n\n";
                        message += @"
<ItemGroup>
    <MonoGameContentReference Include=""Content\Content.mgcb"" />
  </ItemGroup>";


                    }
                    else
                    {
                        var path = FileManager.GetDirectory( Environment.GetEnvironmentVariable("MSBUILD_EXE_PATH"));
                        GlueCommands.Self.PrintOutput($"You have MonoGame installed at {monogamePath}\nIt needs to be in {path}. You can manually copy it to fix this problem.");
                    }



                    shouldThrowException = false;
                    result.ShouldTryToLoadProject = false;
                }
                else if(exceptionMessage.Contains("Novell.MonoDroid.CSharp.targets"))
                {
                    message = "This project references Novell.MonoDroid.CSharp.targets, " + 
                        "which is an old file that used to be installed by Xamarin, but which " + 
                        "is now replaced by a different .targets file. You can fix this by editing " +
                        "the .csproj file and changing the reference to the Xamarin version. You can " + 
                        " also look at a new FlatRedBall Android project to see what this looks like.";
                    result.ShouldTryToLoadProject = false;
                }
                else if(exceptionMessage.Contains("Xamarin.Android.CSharp.targets") && exceptionMessage.Contains("dotnet\\sdk\\"))
                {
                    message = @"FlatRedBall cannot load your project. This is likely because you are using .NET 6.0 or newer and the Xamarin targets are not available. To solve this, see the Troubleshooting section here:\n\n
https://flatredball.com/documentation/tools/glue-reference/multi-platform/glue-how-to-create-a-flatredball-android-project/.

This will automatically open when you click the OK button\n\n" + exceptionMessage;
                    locationToOpen = "https://flatredball.com/documentation/tools/glue-reference/multi-platform/glue-how-to-create-a-flatredball-android-project/";
                    result.ShouldTryToLoadProject = false;
                    shouldThrowException = false;
                }
                else if (exceptionMessage.Contains("Xamarin.Android.CSharp.targets"))
                {
                    message = @"Error loading this Android project. Please verify that you have correctly installed the requirements to build Android projects. Opening:
https://docs.microsoft.com/en-us/xamarin/android/get-started/installation/windows

Additional Info:
" + exceptionMessage;
                    locationToOpen = "https://docs.microsoft.com/en-us/xamarin/android/get-started/installation/windows";
                    result.ShouldTryToLoadProject = false;
                    shouldThrowException = false;
                }
                else if (exceptionMessage.Contains("Microsoft.NET.Sdk"))
                {
                    message = $"Could not load the project {fileName}\n" +
                        $"Missing SDK:\n\n" + exceptionMessage;
                }
                else
                {
                    message = $"Could not load the project {fileName}\n" +
                        $"Usually this occurs if the Visual Studio XNA plugin is not installed\n\n";

                }

                if(shouldThrowException)
                {
                    throw new Exception(message, exception);
                }
                else
                {
                    if(!string.IsNullOrEmpty(message))
                    {
                        Plugins.ExportedImplementations.GlueCommands.Self.DialogCommands.ShowMessageBox(message);
                    }

                    if(locationToOpen != null)
                    {
                        //System.Diagnostics.Process.Start(locationToOpen);
                        // From here: https://stackoverflow.com/questions/4580263/how-to-open-in-default-browser-in-c-sharp
                        Process.Start("explorer", locationToOpen);
                    }
                }
            }

            ProjectBase projectBase = null;

            if(didErrorOccur == false)
            {
                projectBase = CreatePlatformSpecificProject(coreVisualStudioProject, fileName);
            }

            // It may be null if the project is of an unknown type.  
            // We'll handle that problem outside of this function.
            if (projectBase != null)
            {
                projectBase.Saving += FlatRedBall.Glue.IO.FileWatchManager.IgnoreNextChangeOnFile;
                // Saving seems to cause 2 file changes, so we're going to ignore 2, what a hack!
                projectBase.Saving += FlatRedBall.Glue.IO.FileWatchManager.IgnoreNextChangeOnFile;
            }

            result.Project = projectBase;
            return result;
        }



        public static ProjectBase CreatePlatformSpecificProject(Project coreVisualStudioProject, string fileName)
        {
            ProjectBase toReturn = null;
            if (FileManager.GetExtension(fileName) == "contentproj")
            {
                toReturn = new XnaContentProject(coreVisualStudioProject);
            }

            string errorMessage = null;

            if (toReturn == null)
            {
                toReturn = TryGetProjectTypeFromDefineConstants(coreVisualStudioProject, out errorMessage);
            }


            if (toReturn == null)
            {
                // If we got here that means that the preprocessor defines don't match what
                // Glue expects.  This is probably bad - Glue generated code will likely not
                // compile, so let's warn the user
                EditorObjects.IoC.Container.Get<IGlueCommands>().DialogCommands.ShowMessageBox(errorMessage);

            }

            return toReturn;
        }

        class PreprocessorAndFunc
        {
            public string Preprocessor;
            public Func<ProjectBase> Func;

            public PreprocessorAndFunc(string preprocessor, Func<ProjectBase> func)
            {
                Preprocessor = preprocessor;
                Func = func;
            }
        }


        private static ProjectBase TryGetProjectTypeFromDefineConstants(Project coreVisualStudioProject, out string message)
        {
            string preProcessorConstants = GetPreProcessorConstantsFromProject(coreVisualStudioProject);

            //string sasfd = ProjectManager.LibrariesPath;

            // Check for other platforms before checking for FRB_XNA because those projects
            // include FRB_XNA in them

            ProjectBase toReturn = null;
            message = null;

            var projectFilePath = new FilePath(coreVisualStudioProject.ProjectFileLocation.File);
            var possibleStandardFile = new FilePath(projectFilePath.GetDirectoryContainingThis().GetDirectoryContainingThis() + "/GameStandard/GameStandard.csproj");

            if(possibleStandardFile.Exists())
            {
                var standardProject = new Project(possibleStandardFile.FullPath, null, null, new ProjectCollection());

                toReturn = new VisualStudioDotNetStandardProject(coreVisualStudioProject, standardProject);
            }
            else
            {
                List<PreprocessorAndFunc> loadCalls = new List<PreprocessorAndFunc>();

                loadCalls.Add(new PreprocessorAndFunc("ANDROID", () => new AndroidProject(coreVisualStudioProject)));
                loadCalls.Add(new PreprocessorAndFunc("IOS", () => new IosMonogameProject(coreVisualStudioProject)));
                loadCalls.Add(new PreprocessorAndFunc("UWP", () => new UwpProject(coreVisualStudioProject)));
                loadCalls.Add(new PreprocessorAndFunc("DESKTOP_GL", () => new DesktopGlProject(coreVisualStudioProject)));
                // Do XNA_4 last, since every 
                // other project type has this 
                // preprocessor type, so every project
                // type would return true here
                loadCalls.Add(new PreprocessorAndFunc("XNA4", () => new Xna4Project(coreVisualStudioProject)));
                // handled above, because it requires 2 projects to construct:
                //loadCalls.Add(new PreprocessorAndFunc("Standard", () => new VisualStudioDotNetStandardProject(coreVisualStudioProject)));


                foreach (var call in loadCalls)
                {
                    if(preProcessorConstants?.Contains(call.Preprocessor) == true)
                    {
                        toReturn = call.Func();
                        break;
                    }
                }
                message = null;
                if(toReturn == null)
                {
                    var areEmpty = string.IsNullOrEmpty(preProcessorConstants);



                    message = $"Could not determine project type from preprocessor directives." +
                        $"\n\nThe project being loaded from {coreVisualStudioProject.ProjectFileLocation} has the folowing preprocessor directives\"{preProcessorConstants}\"";

                    if(areEmpty)
                    {
                        message += "\n\nThis project has no preprocessor directives. An unknown error has occurred.";

                        message += "\n\nThe project has the following properties:";
                        foreach (var property in coreVisualStudioProject.Properties)
                        {
                            message += $"{property.Name} {property.EvaluatedValue}";
                        }
                    }
                    else
                    {
                        message += 
                            $"\n\nThe following are preprocessor directives to determine project type:";

                        foreach(var preprocessor in loadCalls)
                        {
                            message += "\n" + preprocessor.Preprocessor;
                        }
                    }
                }

                if(toReturn == null)
                {
                    var mbmb = new MultiButtonMessageBoxWpf();

                    mbmb.MessageText = "FlatRedBall could not determine the project type. Would you like to manually set the project type?";

                    foreach(var loadCall in loadCalls)
                    {
                        mbmb.AddButton(loadCall.Preprocessor, loadCall.Func);
                    }

                    mbmb.AddButton("No, do not manually set the type", null);

                    var showResult = mbmb.ShowDialog();

                    if(mbmb.ClickedResult is Func<ProjectBase> asFunc)
                    {
                        toReturn = asFunc();
                    }
                }
            }

    


            return toReturn;
        }

        public static string GetPreProcessorConstantsFromProject(Project coreVisualStudioProject)
        {
            string preProcessorConstants = "";

            // Victor Chelaru October 20, 2012
            // We used to just look at the XML and had a broad way of determining the 
            // patterns.  I decided it was time to clean this up and make it more precise
            // so now we use the Properties from the project.

            var properties = coreVisualStudioProject.Properties.Where(item => item.Name == "DefineConstants").ToArray();

            foreach (var property in properties)
            {
                preProcessorConstants += ";" + property.EvaluatedValue;
            }
            return preProcessorConstants;
        }

    }
}
