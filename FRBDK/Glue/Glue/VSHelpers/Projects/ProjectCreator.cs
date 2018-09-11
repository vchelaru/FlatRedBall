using System;
using System.Linq;
using FlatRedBall.IO;
using Microsoft.Build.Evaluation;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using System.Collections.Generic;

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

                var shouldThrowException = true;
                string locationToOpen = null;

                bool isMissingMonoGame = exceptionMessage.Contains("MonoGame.Content.Builder.targets\"");
                string message;
                if(isMissingMonoGame)
                {
                    message = $"Could not load the project {fileName}\nbecause MonoGame files are missing. Try installing MonoGame, then try opening the project in Glue again.\n\n";
                    locationToOpen = "http://teamcity.monogame.net/repository/download/MonoGame_PackagingWindows/latest.lastSuccessful/MonoGameSetup.exe?guest=1";
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
                    Plugins.ExportedImplementations.GlueCommands.Self.DialogCommands.ShowMessageBox(message);

                    if(locationToOpen != null)
                    {
                        System.Diagnostics.Process.Start(locationToOpen);
                    }
                }
            }

            ProjectBase projectBase = null;

            if(didErrorOccur == false)
            {
                projectBase = CreatePlatformSpecificProject(coreVisualStudioProject, fileName);
            }

#if GLUE
            // It may be null if the project is of an unknown type.  
            // We'll handle that problem outside of this function.
            if (projectBase != null)
            {
                projectBase.Saving += FlatRedBall.Glue.IO.FileWatchManager.IgnoreNextChangeOnFile;
                // Saving seems to cause 2 file changes, so we're going to ignore 2, what a hack!
                projectBase.Saving += FlatRedBall.Glue.IO.FileWatchManager.IgnoreNextChangeOnFile;
            }
#endif

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


            foreach (var call in loadCalls)
            {
                if(preProcessorConstants.Contains(call.Preprocessor))
                {
                    toReturn = call.Func();
                    break;
                }
            }

    

            message = null;
            if(toReturn == null)
            {
                message = $"Could not determine project type from preprocessor directives." +
                    $"\nThe project beign loaded has the folowing preprocessor directives\"{preProcessorConstants}\"" +
                    $"\nThe following are preprocessor directives to determine project type:";

                foreach(var preprocessor in loadCalls)
                {
                    message += "\n" + preprocessor.Preprocessor;
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
            foreach (var property in coreVisualStudioProject.Properties)
            {
                if (property.Name == "DefineConstants")
                {
                    preProcessorConstants += ";" + property.EvaluatedValue;
                }
            }
            return preProcessorConstants;
        }

    }
}
