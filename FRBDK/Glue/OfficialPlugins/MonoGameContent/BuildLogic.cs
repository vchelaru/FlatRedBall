using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.MonoGameContent
{
    public class BuildLogic : Singleton<BuildLogic>
    {
        const string commandLineBuildExe =
            @"C:\Program Files (x86)\MSBuild\MonoGame\v3.0\Tools\MGCB.exe";

        public void RefreshBuiltFilesFor(ProjectBase project)
        {


            /////////////Early Out///////////////////
            bool builderExists = System.IO.File.Exists(commandLineBuildExe);
            if (builderExists == false)
            {
                // error? output?
                return;
            }

            ////////////End Early Out////////////////

            var allReferencedFileSaves = GlueState.Self.CurrentGlueProject.GetAllReferencedFiles();

            allReferencedFileSaves = allReferencedFileSaves.Distinct((a, b) => a.Name == b.Name).ToList();

            bool needsMonoGameFilesBuilt = project is DesktopGlProject;

            if (needsMonoGameFilesBuilt)
            {

                var musicFiles = allReferencedFileSaves.Where(item => IsBuiltByContentPipeline(item)).ToList();

                foreach (var musicFile in musicFiles)
                {
                    TryHandleReferencedFile(project, musicFile);
                }
            }

            //foreach (var file in GlueState.Self.CurrentGlueProject.GetAllReferencedFiles())
        }


        public void TryHandleReferencedFile(ProjectBase project, ReferencedFileSave referencedFile)
        {
            bool isBuilt = IsBuiltByContentPipeline(referencedFile);

            if(isBuilt)
            {
                TryPerformBuildAndAdd(referencedFile, project);
            }
            else
            {
                TryRemoveXnbReferences(project, referencedFile);

            }
        }

        private static void TryRemoveXnbReferences(ProjectBase project, ReferencedFileSave referencedFile)
        {
            ContentItem contentItem = GetContentItem(referencedFile, project, createEvenIfProjectTypeNotSupported:true);

            var fullFileName = GlueCommands.Self.FileCommands.GetFullFileName(referencedFile);
            string destinationDirectory = GetDestinationDirectory(fullFileName);

            string absoluteToAddNoExtension = destinationDirectory +
                FileManager.RemovePath(FileManager.RemoveExtension(referencedFile.Name));

            TaskManager.Self.AddSync(() =>
           {

               bool didRemove = false;


               foreach (string extension in contentItem.GetBuiltExtensions())
               {
                   var absoluteFile = absoluteToAddNoExtension + "." + extension;

                    // remove any built file from the project if referenced - cleanup in case the user switched between content pipeline or not.
                    var item = project.GetItem(absoluteFile, true);

                   if (item != null)
                   {
                       project.RemoveItem(item);
                       didRemove = true;
                   }
               }

               if (didRemove)
               {
                   GlueCommands.Self.TryMultipleTimes(project.Save, 5);
               }
           }, $"Removing XNB references for {referencedFile}");
        }

        private static ContentItem GetContentItem(ReferencedFileSave referencedFileSave, ProjectBase project, bool createEvenIfProjectTypeNotSupported)
        {
            string extension = FileManager.GetExtension(referencedFileSave.Name);

            ContentItem contentItem = null;

            if (extension == "mp3" || extension == "ogg")
            {
                contentItem = ContentItem.CreateMp3Build();
            }
            else if(extension == "wav")
            {
                contentItem = ContentItem.CreateWavBuild();
            }
            else if(extension == "png")
            {
                contentItem = ContentItem.CreateTextureBuild();
            }


            if (contentItem != null)
            {
                // According to docs, the following are available:
                // Windows
                // iOS
                // Android
                // DesktopGL
                // MacOSX
                // WindowsStoreApp
                // NativeClient
                // PlayStation4
                // WindowsPhone8
                // RaspberryPi
                // PSVita
                // XboxOne
                // Switch
                if (project is DesktopGlProject)
                {
                    contentItem.Platform = "DesktopGL";
                }
                else if (project is AndroidProject)
                {
                    contentItem.Platform = "Android";
                }
                else if (createEvenIfProjectTypeNotSupported == false)
                {
                    contentItem = null;
                }
            }

            if(contentItem != null)
            {
                string contentDirectory = FlatRedBall.Glue.ProjectManager.ContentDirectory;
                string projectDirectory = GlueState.Self.CurrentGlueProjectDirectory;
                string builtXnbRoot = FileManager.RemoveDotDotSlash(projectDirectory + "../BuiltXnbs/");
                var fullFileName = GlueCommands.Self.FileCommands.GetFullFileName(referencedFileSave);
                contentItem.BuildFileName = fullFileName;
                GetDestinationDirectory(fullFileName);

                // remove the trailing slash:
                contentItem.OutputDirectory = builtXnbRoot;
                contentItem.IntermediateDirectory = builtXnbRoot + "obj/" +
                    FileManager.RemoveExtension(referencedFileSave.Name);
            }
            return contentItem;
        }

        private static string GetDestinationDirectory(string fullFileName)
        {
            string contentDirectory = FlatRedBall.Glue.ProjectManager.ContentDirectory;
            string projectDirectory = GlueState.Self.CurrentGlueProjectDirectory;
            string destinationDirectory = FileManager.GetDirectory(fullFileName);
            destinationDirectory = FileManager.MakeRelative(destinationDirectory, contentDirectory);
            destinationDirectory = FileManager.RemoveDotDotSlash(projectDirectory + "../BuiltXnbs/" + destinationDirectory);

            return destinationDirectory;
        }

        private void TryPerformBuildAndAdd(ReferencedFileSave referencedFile, ProjectBase project)
        {
            ContentItem contentItem;
            contentItem = GetContentItem(referencedFile, project, createEvenIfProjectTypeNotSupported:false);

            var fullFileName = GlueCommands.Self.FileCommands.GetFullFileName(referencedFile);
            string destinationDirectory = GetDestinationDirectory(fullFileName);

            // The monogame content builder seems to be doing an incremental build - 
            // it's being told to do so through the command line params, and it's not replacing
            // files unless they're actually built, but it's SLOW!!! We can put our own check here
            // and make it much faster
            if(contentItem != null)
            {


                TaskManager.Self.AddSync(
                () =>
                {
                    // If the user closes the project while the startup is happening, just skip the task - no need to build
                    if(GlueState.Self.CurrentGlueProject != null)
                    {

                        if(contentItem.GetIfNeedsBuild(destinationDirectory))
                        {
                            PerformBuild(contentItem);
                        }

                        string relativeToAddNoExtension =
                            FileManager.RemoveExtension(referencedFile.Name);

                        string absoluteToAddNoExtension = destinationDirectory +
                            FileManager.RemovePath(FileManager.RemoveExtension(referencedFile.Name));

                        foreach(var extension in contentItem.GetBuiltExtensions())
                        {
                            AddFileToProject(project, 
                                absoluteToAddNoExtension + "." + extension, 
                                relativeToAddNoExtension + "." + extension);

                        }
                    }
                },
                "Building MonoGame Content " + fullFileName);
            }

        }

        private static void PerformBuild(ContentItem contentItem)
        {
            string contentDirectory = FlatRedBall.Glue.ProjectManager.ContentDirectory;
            string workingDirectory = contentDirectory;

            string commandLine = contentItem.GenerateCommandLine();
            var process = new Process();

            process.StartInfo.Arguments = commandLine;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = commandLineBuildExe;
            process.StartInfo.WorkingDirectory = contentDirectory;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;

            process.StartInfo.CreateNoWindow = true;

            process.Start();

            var errorString = process.StandardError.ReadToEnd() + "\n\n" + process.StandardOutput.ReadToEnd();
            PluginManager.ReceiveOutput($"Building: {commandLineBuildExe} {commandLine}");

            while (process.HasExited == false)
            {
                System.Threading.Thread.Sleep(100);
            }
        }


        private bool IsBuiltByContentPipeline(ReferencedFileSave file)
        {
            string extension = FileManager.GetExtension(file.Name);

            bool isRequired = extension == "mp3" ||
                extension == "ogg" ||
                extension == "wav";

            if (isRequired)
            {
                return true;
            }
            else if (file.UseContentPipeline)
            {
                return extension == "png";
            }
            return false;
        }

        private void AddFileToProject(ProjectBase project, string absoluteFile, string link)
        {
            //project.AddContentBuildItem(file, SyncedProjectRelativeType.Linked, false);


            var item = project.GetItem(absoluteFile, true);
            if (item == null)
            {
                item = project.AddContentBuildItem(absoluteFile, SyncedProjectRelativeType.Linked, false);

                link = "Content\\" + link;

                if (project is AndroidProject)
                {
                    link = "Assets\\" + link;
                }
                link = project.ProcessLink(link);
                item.SetLink(link.Replace("/", "\\"));

                GlueCommands.Self.TryMultipleTimes(project.Save, 5);

                // save?

                //    PluginManager.ReceiveOutput("Added " + buildItem.EvaluatedInclude + " through the file " + whatToAddToProject);
                //    wasAnythingChanged = true;
            }

        }
    }
}
