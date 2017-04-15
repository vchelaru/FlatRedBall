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
            ///////////////early out////////////////
            if (IsBuiltByContentPipeline(referencedFile) == false)
            {
                return;
            }
            /////////////end early out////////////

            TryPerformBuild(referencedFile, project);

        }

        private static ContentItem GetContentItem(ReferencedFileSave referencedFileSave)
        {
            string extension = FileManager.GetExtension(referencedFileSave.Name);

            ContentItem contentItem = null;

            if (extension == "mp3" || extension == "ogg")
            {
                contentItem = ContentItem.CreateMp3Build();
            }
            if(extension == "wav")
            {
                contentItem = ContentItem.CreateWavBuild();
            }
            

            if(contentItem != null)
            { 
                contentItem.Platform = "DesktopGL"; // todo = change this for different platforms

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

        private void TryPerformBuild(ReferencedFileSave referencedFile, ProjectBase project)
        {
            ContentItem contentItem;
            contentItem = GetContentItem(referencedFile);

            if(contentItem != null)
            {

                string commandLine = contentItem.GenerateCommandLine();

                string contentDirectory = FlatRedBall.Glue.ProjectManager.ContentDirectory;
                string workingDirectory = contentDirectory;
                var fullFileName = GlueCommands.Self.FileCommands.GetFullFileName(referencedFile);
                string destinationDirectory = GetDestinationDirectory(fullFileName);

                TaskManager.Self.AddSync(
                () =>
                {
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
                },
                "Building MonoGame Content " + fullFileName);
            }

        }

        

        private bool IsBuiltByContentPipeline(ReferencedFileSave file)
        {
            string extension = FileManager.GetExtension(file.Name);

            return extension == "mp3" || extension == "ogg" || extension == "wav";
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

                item.SetLink(link.Replace("/", "\\"));

                GlueCommands.Self.TryMultipleTimes(project.Save, 5);

                // save?

                //    PluginManager.ReceiveOutput("Added " + buildItem.EvaluatedInclude + " through the file " + whatToAddToProject);
                //    wasAnythingChanged = true;
            }

        }
    }
}
