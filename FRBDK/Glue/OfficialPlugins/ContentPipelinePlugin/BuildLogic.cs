using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
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
        static IGlueState GlueState => EditorObjects.IoC.Container.Get<IGlueState>();
        static IGlueCommands GlueCommands => EditorObjects.IoC.Container.Get<IGlueCommands>();


        const string commandLineBuildExe =
            @"C:\Program Files (x86)\MSBuild\MonoGame\v3.0\Tools\MGCB.exe";

        public void RefreshBuiltFilesFor(ProjectBase project, bool forcePngsToContentPipeline)
        {


            /////////////Early Out///////////////////
            bool builderExists = System.IO.File.Exists(commandLineBuildExe);
            if (builderExists == false)
            {
                // error? output?
                return;
            }

            ////////////End Early Out////////////////

            var allReferencedFileSaves = GlueState.CurrentGlueProject.GetAllReferencedFiles();

            allReferencedFileSaves = allReferencedFileSaves.Distinct((a, b) => a.Name == b.Name).ToList();

            bool needsMonoGameFilesBuilt = GetIfNeedsMonoGameFilesBuilt(project);

            if (needsMonoGameFilesBuilt)
            {

                var filesToBeBuilt = allReferencedFileSaves.Where(item => IsBuiltByContentPipeline(item, forcePngsToContentPipeline)).ToList();

                foreach (var fileToBeBuilt in filesToBeBuilt)
                {
                    TryHandleReferencedFile(project, fileToBeBuilt, forcePngsToContentPipeline);
                }
            }

            //foreach (var file in GlueState.Self.CurrentGlueProject.GetAllReferencedFiles())
        }

        public static bool GetIfNeedsMonoGameFilesBuilt(ProjectBase project)
        {
            return project is DesktopGlProject ||
                project is AndroidProject ||
                project is UwpProject
                // todo: need to support iOS
                ;

        }


        public void TryHandleReferencedFile(ProjectBase project, ReferencedFileSave referencedFile, bool forcePngsToContentPipeline)
        {
            bool isBuilt = IsBuiltByContentPipeline(referencedFile, forcePngsToContentPipeline);

            if(isBuilt)
            {
                TryAddXnbReferencesAndBuild(referencedFile, project, save:true);
            }
            else
            {
                TryRemoveXnbReferences(project, referencedFile);

            }
        }

        public void TryHandleReferencedFile(ProjectBase project, string absoluteFile, bool forcePngsToContentPipeline)
        {
            bool isBuilt = IsBuiltByContentPipeline(absoluteFile, false, forcePngsToContentPipeline);

            if (isBuilt)
            {
                TryAddXnbReferencesAndBuild(absoluteFile, project, saveProjectAfterAdd: true);
            }
            else
            {
                TryRemoveXnbReferences(project, absoluteFile, save: true);

            }
        }


        private void TryRemoveXnbReferences(ProjectBase project, ReferencedFileSave referencedFile)
        {
            var fullFileName = GlueCommands.FileCommands.GetFullFileName(referencedFile);
            TryRemoveXnbReferences(project, fullFileName);
        }

        public void TryRemoveXnbReferences(ProjectBase project, string fullFileName, bool save = true)
        {
            string destinationDirectory = GetDestinationDirectory(fullFileName);

            ContentItem contentItem = GetContentItem(fullFileName, project, createEvenIfProjectTypeNotSupported: true);

            string absoluteToAddNoExtension = destinationDirectory +
                FileManager.RemovePath(FileManager.RemoveExtension(fullFileName));

            if (contentItem != null)
            {


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

                    if (didRemove && save)
                    {
                        GlueCommands.TryMultipleTimes(project.Save, 5);
                    }
                }, $"Removing XNB references for {fullFileName}");
            }
        }

        public static void TryDeleteBuiltXnbFor(ProjectBase project, ReferencedFileSave referencedFileSave, bool forcePngsToContentPipeline)
        {
            bool isBuilt = IsBuiltByContentPipeline(referencedFileSave, forcePngsToContentPipeline);

            ContentItem contentItem = null;

            if(isBuilt)
            {
                contentItem = GetContentItem(referencedFileSave, project, createEvenIfProjectTypeNotSupported: false);
            }

            if(contentItem != null)
            {
                
                foreach(var extension in contentItem.GetBuiltExtensions())
                {
                    var fileToDelete = $"{contentItem.OutputDirectory}{contentItem.OutputFileNoExtension}.{extension}";

                    if(System.IO.File.Exists(fileToDelete))
                    { 
                        GlueCommands.TryMultipleTimes(()=>System.IO.File.Delete(fileToDelete), 5);
                    }
                }
            }
        }

        private static ContentItem GetContentItem(ReferencedFileSave referencedFileSave, ProjectBase project, bool createEvenIfProjectTypeNotSupported)
        {
            var fullFileName = GlueCommands.FileCommands.GetFullFileName(referencedFileSave);
            return GetContentItem(fullFileName, project, createEvenIfProjectTypeNotSupported);
        }

        private static ContentItem GetContentItem(string fullFileName, ProjectBase project, bool createEvenIfProjectTypeNotSupported)
        {
            var contentDirectory = GlueState.ContentDirectory;

            var relativeToContent = FileManager.MakeRelative(fullFileName, contentDirectory);

            string extension = FileManager.GetExtension(fullFileName);

            ContentItem contentItem = null;

            if (extension == "mp3" || extension == "ogg")
            {
                contentItem = ContentItem.CreateMp3Build();
            }
            else if (extension == "wav")
            {
                contentItem = ContentItem.CreateWavBuild();
            }
            else if (extension == "png")
            {
                contentItem = ContentItem.CreateTextureBuild();
            }
            else if(extension == "fx")
            {
                contentItem = ContentItem.CreateEffectBuild();
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
                else if(project is UwpProject)
                {
                    contentItem.Platform = "WindowsStoreApp";
                }
                else if (createEvenIfProjectTypeNotSupported == false)
                {
                    contentItem = null;
                }
            }

            if (contentItem != null)
            {
                string projectDirectory = GlueState.CurrentGlueProjectDirectory;
                string builtXnbRoot = FileManager.RemoveDotDotSlash(projectDirectory + "../BuiltXnbs/");
                contentItem.BuildFileName = fullFileName;

                // remove the trailing slash:
                contentItem.OutputDirectory = builtXnbRoot;
                contentItem.OutputFileNoExtension = FileManager.RemoveExtension(relativeToContent);

                contentItem.IntermediateDirectory = builtXnbRoot + "obj/" +
                    FileManager.RemoveExtension(relativeToContent);
            }
            return contentItem;
        }

        private static string GetDestinationDirectory(string fullFileName)
        {
            string contentDirectory = FlatRedBall.Glue.ProjectManager.ContentDirectory;
            string projectDirectory = GlueState.CurrentGlueProjectDirectory;
            string destinationDirectory = FileManager.GetDirectory(fullFileName);
            destinationDirectory = FileManager.MakeRelative(destinationDirectory, contentDirectory);
            destinationDirectory = FileManager.RemoveDotDotSlash(projectDirectory + "../BuiltXnbs/" + destinationDirectory);

            return destinationDirectory;
        }

        private void TryAddXnbReferencesAndBuild(ReferencedFileSave referencedFile, ProjectBase project, bool save)
        {
            var fullFileName = GlueCommands.FileCommands.GetFullFileName(referencedFile);

            TryAddXnbReferencesAndBuild(fullFileName, project, save);

        }

        public void TryAddXnbReferencesAndBuild(string fullFileName, ProjectBase project, bool saveProjectAfterAdd)
        {
            var contentDirectory = GlueState.ContentDirectory;

            var relativeToContent = FileManager.MakeRelative(fullFileName, contentDirectory);

            ContentItem contentItem;
            contentItem = GetContentItem(fullFileName, project, createEvenIfProjectTypeNotSupported: false);

            string destinationDirectory = GetDestinationDirectory(fullFileName);

            // The monogame content builder seems to be doing an incremental build - 
            // it's being told to do so through the command line params, and it's not replacing
            // files unless they're actually built, but it's SLOW!!! We can put our own check here
            // and make it much faster
            if (contentItem != null)
            {


                TaskManager.Self.AddSync(
                () =>
                {
                    // If the user closes the project while the startup is happening, just skip the task - no need to build
                    if (GlueState.CurrentGlueProject != null)
                    {

                        if (contentItem.GetIfNeedsBuild(destinationDirectory))
                        {
                            PerformBuild(contentItem);
                        }

                        string relativeToAddNoExtension =
                            FileManager.RemoveExtension(relativeToContent);

                        string absoluteToAddNoExtension = destinationDirectory +
                            FileManager.RemovePath(FileManager.RemoveExtension(relativeToContent));

                        foreach (var extension in contentItem.GetBuiltExtensions())
                        {
                            AddFileToProject(project,
                                absoluteToAddNoExtension + "." + extension,
                                relativeToAddNoExtension + "." + extension,
                                saveProjectAfterAdd);

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

            PluginManager.ReceiveOutput($"Building: {commandLineBuildExe} {commandLine}");

            while (process.HasExited == false)
            {
                System.Threading.Thread.Sleep(100);

                while (!process.StandardOutput.EndOfStream)
                {
                    var line = process.StandardOutput.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        GlueCommands.PrintOutput(line);
                    }
                }
            }

            bool builtCorrectly = process.ExitCode == 0;

            string str;
            while ((str = process.StandardOutput.ReadLine()) != null)
            {
                // Currently the content pipeline prints errors as normal output instead of error.
                // We can look at the error code to see if it's an error or not.
                if(builtCorrectly)
                {
                    GlueCommands.PrintOutput(str);
                }
                else
                {
                    GlueCommands.PrintError(str);
                }
            }

            while ((str = process.StandardError.ReadLine()) != null)
            {
                GlueCommands.PrintError(str);
            }




        }


        private static bool IsBuiltByContentPipeline(ReferencedFileSave file, bool forcePngsToContentPipeline)
        {
            return IsBuiltByContentPipeline(file.Name, file.UseContentPipeline, forcePngsToContentPipeline);
        }

        private static bool IsBuiltByContentPipeline(string fileName, bool rfsUseContentPipeline, bool forcePngsToContentPipeline)
        {
            string extension = FileManager.GetExtension(fileName);

            bool isRequired = extension == "mp3" ||
                extension == "ogg" ||
                extension == "wav" ||
                extension == "fx";

            if (isRequired)
            {
                return true;
            }
            else if (rfsUseContentPipeline || forcePngsToContentPipeline)
            {
                return extension == "png";
            }
            return false;
        }

        private void AddFileToProject(ProjectBase project, string absoluteFile, string link, bool saveProjectAfterAdd)
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

                if(saveProjectAfterAdd)
                {
                    GlueCommands.TryMultipleTimes(project.Save, 5);
                }
            }

        }
    }
}
