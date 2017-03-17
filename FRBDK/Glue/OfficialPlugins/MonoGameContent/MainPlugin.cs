using FlatRedBall.Glue.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Glue.Managers;
using System.Diagnostics;

namespace OfficialPlugins.MonoGameContent
{
    [Export(typeof (PluginBase))]
    public class MainPlugin : PluginBase
    {
        const string commandLineBuildExe =
            @"C:\Program Files (x86)\MSBuild\MonoGame\v3.0\Tools\MGCB.exe";

        public override string FriendlyName
        {
            get
            {
                return "MonoGame Content Plugin";
            }
        }

        public override Version Version
        {
            get
            {
                return new Version(1, 0, 0);
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            this.ReactToFileChangeHandler += HandleFileChanged;
            this.ReactToLoadedGlux += HandleLoadedGlux;
            this.ReactToLoadedSyncedProject += HandleLoadedSyncedProject;
            this.ReactToNewFileHandler += HandleNewFile;
        }

        private void HandleNewFile(ReferencedFileSave newFile)
        {
            if(GlueState.Self.CurrentMainProject is DesktopGlProject)
            {
                TryHandleReferencedFile(GlueState.Self.CurrentMainProject, newFile);
            }
            foreach(var project in GlueState.Self.SyncedProjects)
            {
                if(project is DesktopGlProject)
                {
                    TryHandleReferencedFile(project, newFile);
                }
            }
        }

        private void HandleLoadedGlux()
        {
            RefreshBuiltFilesFor(GlueState.Self.CurrentMainProject);
        }

        private void HandleLoadedSyncedProject(ProjectBase project)
        {
            RefreshBuiltFilesFor(project);
        }

        private void RefreshBuiltFilesFor(ProjectBase project)
        {


            /////////////Early Out///////////////////
            bool builderExists = System.IO.File.Exists(commandLineBuildExe);
            if(builderExists == false)
            {
                // error? output?
                return;
            }

            ////////////End Early Out////////////////

            var allReferencedFileSaves = GlueState.Self.CurrentGlueProject.GetAllReferencedFiles();

            allReferencedFileSaves = allReferencedFileSaves.Distinct((a, b) => a.Name == b.Name).ToList();

            bool needsMusicFilesBuilt = project is DesktopGlProject;

            if(needsMusicFilesBuilt)
            {
                
                var musicFiles = allReferencedFileSaves.Where(item => IsMusicFile(item)).ToList();

                foreach (var musicFile in musicFiles)
                {
                    TryHandleReferencedFile(project, musicFile);
                }
            }

            //foreach (var file in GlueState.Self.CurrentGlueProject.GetAllReferencedFiles())
        }

        private void TryHandleReferencedFile(ProjectBase project, ReferencedFileSave musicReferencedFile)
        {
            ///////////////early out////////////////
            if(IsMusicFile(musicReferencedFile) == false)
            {
                return;
            }
            /////////////end early out////////////

            ContentItem mp3ContentItem = ContentItem.CreateMp3Build();
            mp3ContentItem.Platform = "DesktopGL"; // todo = change this for different platforms

            string contentDirectory = FlatRedBall.Glue.ProjectManager.ContentDirectory;
            string projectDirectory = GlueState.Self.CurrentGlueProjectDirectory;
            string builtXnbRoot = FileManager.RemoveDotDotSlash( projectDirectory + "../BuiltXnbs/");
            var mp3FileName = GlueCommands.Self.FileCommands.GetFullFileName(musicReferencedFile);

            mp3ContentItem.BuildFileName = mp3FileName;

            string destinationDirectory = FileManager.GetDirectory(mp3FileName);
            destinationDirectory = FileManager.MakeRelative(destinationDirectory, contentDirectory);
            destinationDirectory = FileManager.RemoveDotDotSlash(projectDirectory + "../BuiltXnbs/" + destinationDirectory);

            // remove the trailing slash:
            mp3ContentItem.OutputDirectory = builtXnbRoot;
            mp3ContentItem.IntermediateDirectory = builtXnbRoot + "obj/" +
                FileManager.RemoveExtension(musicReferencedFile.Name);

            string commandLine = mp3ContentItem.GenerateCommandLine();
            string workingDirectory = contentDirectory;
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

                    process.Start();

                    var errorString = process.StandardError.ReadToEnd() + "\n\n" + process.StandardOutput.ReadToEnd();

                    PluginManager.ReceiveOutput($"Building: {commandLineBuildExe} {commandLine}");

                    while (process.HasExited == false)
                    {
                        System.Threading.Thread.Sleep(100);
                    }

                    string relativeToAddNoExtension =
                        FileManager.RemoveExtension(musicReferencedFile.Name);

                    string absoluteToAddNoExtension = destinationDirectory +
                        FileManager.RemovePath(FileManager.RemoveExtension(musicReferencedFile.Name));

                    AddFileToProject(project, absoluteToAddNoExtension + ".xnb", relativeToAddNoExtension + ".xnb");
                    // need to make this depend on project type
                    AddFileToProject(project, absoluteToAddNoExtension + ".ogg", relativeToAddNoExtension + ".ogg");
                },
                "Building MonoGame Content " + mp3FileName);
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

                GlueCommands.Self.TryMultipleTimes(project.Save, 5) ;

                // save?

            //    PluginManager.ReceiveOutput("Added " + buildItem.EvaluatedInclude + " through the file " + whatToAddToProject);
            //    wasAnythingChanged = true;
            }

        }

        private bool IsMusicFile(ReferencedFileSave file)
        {
            string extension = FileManager.GetExtension(file.Name);

            return extension == "mp3" || extension == "ogg";
        }

        private void HandleFileChanged(string fileName)
        {
            // todo - build?
        }
    }
}
