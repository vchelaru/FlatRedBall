using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using Glue;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.Projects
{
    public static class NewProjectHelper
    {

        public static Process RunNewProjectCreator()
        {
            return RunNewProjectCreator(null, null, false);
        }

        public static Process RunNewProjectCreator(string directoryForNewProject, string namespaceForNewProject, bool creatingSyncedProject)
        {

            string directory =
                //FileManager.GetDirectory(FileManager.GetDirectory(Application.ExecutablePath));
                // This causes Glue to look one directory above where the .exe is.  Glue and NewProjectCreator
                // are both now XNA4 tools, so we're going to look in the same directory as Glue.
                FileManager.GetDirectory(Application.ExecutablePath);

            List<string> files = FileManager.GetAllFilesInDirectory(
                directory, "exe");
            // We used to only search 
            string newProjectCreatorFile = null;

            foreach (string file in files)
            {
                if (file.Contains("NewProjectCreator.exe"))
                {
                    newProjectCreatorFile = file;
                    break;
                }
            }
            const string newProjectCreatorLocation = @"..\..\..\..\NewProjectCreator\NewProjectCreator\bin\x86\Debug\NewProjectCreator.exe";
            if (newProjectCreatorFile == null)
            {
                newProjectCreatorFile = newProjectCreatorLocation;
            }

            if (!File.Exists(newProjectCreatorFile))
            {
                string message = "Could not find the new project creator in this directory:\n" +
                    directory +
                    "\nAlso looked in:\n" +
                    newProjectCreatorLocation +
                    "\nBut we did find these files: ";


                foreach (string file in files)
                {
                    message += "\n" + file;

                }

                System.Windows.Forms.MessageBox.Show(message);

                return null;
            }
            else
            {

                ProcessStartInfo processStartInfo = new ProcessStartInfo(newProjectCreatorFile);

                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.UseShellExecute = false;

                if (!string.IsNullOrEmpty(directoryForNewProject))
                {

                    processStartInfo.Arguments = "directory=\"" + directoryForNewProject + "\"" +
                        " namespace=" + namespaceForNewProject;
                }

                if (string.IsNullOrEmpty(processStartInfo.Arguments))
                {
                    processStartInfo.Arguments = "openedby=glue";
                }
                else
                {
                    processStartInfo.Arguments += " openedby=glue";
                }

                if(creatingSyncedProject)
                {
                    processStartInfo.Arguments += " emptyprojects";
                }

                Process process = Process.Start(processStartInfo);

                return process;
            }
        }

        public static void CreateNewProject()
        {

            // Run the new project creator
            // First see if the NewProjectCreator is in this directory

            Process process = NewProjectHelper.RunNewProjectCreator();

            if(process == null)
            {
                MessageBox.Show(@"Unable to create new project because the New Project Creator could not be run.");
                return;
            }

            while (!process.HasExited)
            {
                Thread.Sleep(300);
            }

            string createdProject = GetCreatedProjectName(process);

            if (!String.IsNullOrEmpty(createdProject))
            {
                ProjectLoader.Self.LoadProject(createdProject, null);
            }
        }

        private static bool IsSolutionInFileList(List<string> files)
        {
            foreach (string file in files)
            {
                if (FileManager.GetExtension(file) == "sln")
                {
                    return true;
                }
            }
            return false;
        }

        public static void CreateNewSyncedProject()
        {
            if(ProjectManager.ProjectBase == null)
            {
                MessageBox.Show(@"Can not create a new Synced Project because there is no open project.");
                return;
            }

            // Gotta find the .sln of this project so we can put the synced project in there
            string directory = ProjectManager.ProjectBase.Directory;

            List<string> files = new List<string>();

            FileManager.GetAllFilesInDirectory(directory, "sln", 0, files);

            while (files.Count == 0)
            {
                directory = FileManager.GetDirectory(directory);
                FileManager.GetAllFilesInDirectory(directory, "sln", 0, files);
            }


            Process process = NewProjectHelper.RunNewProjectCreator(directory, ProjectManager.ProjectBase.Name, creatingSyncedProject:true);

            if (process != null)
            {
                while (!process.HasExited)
                {
                    Thread.Sleep(100);
                }

                string createdProject = GetCreatedProjectName(process);

                ProjectBase newProjectBase = null;

                if (!String.IsNullOrEmpty(createdProject))
                {
                    // The return value could be null if the project being added
                    // already exists as a synced project, but this should never be
                    // the case here.
                    newProjectBase = ProjectManager.AddSyncedProject(createdProject);
                    ProjectManager.SyncedProjects[ProjectManager.SyncedProjects.Count - 1].SaveAsRelativeSyncedProject = true;

                    GluxCommands.Self.SaveGlux();

                    // These are now part of the engine, so don't do anything here.
                    // Wait, these may be part of old templates, so let's keep this here.
                    if (File.Exists(newProjectBase.Directory + "Screens/Screen.cs"))
                    {
                        newProjectBase.RemoveItem(newProjectBase.Directory + "Screens/Screen.cs");
                        File.Delete(newProjectBase.Directory + "Screens/Screen.cs");
                    }

                    if (File.Exists(newProjectBase.Directory + "Screens/ScreenManager.cs"))
                    {
                        newProjectBase.RemoveItem(newProjectBase.Directory + "Screens/ScreenManager.cs");
                        File.Delete(newProjectBase.Directory + "Screens/ScreenManager.cs");
                    }

                    // Remove Game1.cs so that it will just use the same Game1 of the master project.
                    if (File.Exists(newProjectBase.Directory + "Game1.cs"))
                    {
                        newProjectBase.RemoveItem(newProjectBase.Directory + "Game1.cs");
                        File.Delete(newProjectBase.Directory + "Game1.cs");
                    }


                    ProjectManager.SaveProjects();
                }

            }

        }

        private static string GetCreatedProjectName(Process process)
        {
            string output = process.StandardOutput.ReadToEnd();

            output = output.Replace("\r\n", "");

            List<string> csProjFiles = FileManager.GetAllFilesInDirectory(output, "csproj");

            if (csProjFiles.Count != 0)
            {
                return csProjFiles[0];
            }
            else
            {
                List<string> javaProjFiles = FileManager.GetAllFilesInDirectory(output, "project");
                if (javaProjFiles.Count != 0)
                {
                    return javaProjFiles[0];
                }
                return String.Empty;
            }
        }

        //private static void CopyInitializeContentsToNewProject(ProjectBase newProject)
        //{
        //    string mainProjectGameFileName = ProjectManager.Project.MakeAbsolute(ProjectManager.GameClassFileName);
        //    string newProjectGameFileName = newProject.MakeAbsolute(ProjectManager.FindGameClass(newProject));

        //    string mainGameContents = FileManager.FromFileText(mainProjectGameFileName);
        //    string newGameContents = FileManager.FromFileText(newProjectGameFileName);

        //    ParsedNamespace mainNamespace = new ParsedNamespace(mainGameContents);
        //    ParsedNamespace newNamespace = new ParsedNamespace(newGameContents);

        //    ParsedClass mainClass = mainNamespace.Classes[0];
        //    ParsedClass newClass = newNamespace.Classes[0];

        //    ParsedMethod mainMethod = mainClass.GetMethod("Initialize");
        //    ParsedMethod newMethod = newClass.GetMethod("Initialize");

        //    int newStartIndex = newMethod.StartIndex;
        //    int newEndIndex = newMethod.EndIndex;

        //    int mainStartIndex = mainMethod.StartIndex;
        //    int mainEndIndex = mainMethod.EndIndex;

        //    string stringToReplace = newClass.Contents.Substring(newStartIndex, newEndIndex - newStartIndex);
        //    string whatToReplaceWith = mainClass.Contents.Substring(mainStartIndex, mainEndIndex - mainStartIndex);

        //    string replaced = newClass.Contents.Replace(stringToReplace, whatToReplaceWith);

        //    int indexOfClass = newGameContents.IndexOf("public class Game1");

        //    string modified = newGameContents.Substring(0, indexOfClass) + replaced + "\n}";// add a } to close the namespace

        //    FileManager.SaveText(modified, newProjectGameFileName);


        //}

    }
}
