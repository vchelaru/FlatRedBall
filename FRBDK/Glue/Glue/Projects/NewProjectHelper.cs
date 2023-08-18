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
using FlatRedBall.Glue.Controls;
using Npc.ViewModels;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Managers;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Projects
{
    public static class NewProjectHelper
    {

        static NewProjectViewModel RunNewProjectCreator()
        {
            return RunNewProjectCreator(null, null, false);
        }

        static NewProjectViewModel RunNewProjectCreator(FilePath directoryForNewProject, string namespaceForNewProject, bool creatingSyncedProject)
        {
            string commandLineArguments = null;
            if (directoryForNewProject != null)
            {

                commandLineArguments = "directory=\"" + directoryForNewProject.FullPath + "\"" +
                " namespace=" + namespaceForNewProject;
            }

            if (string.IsNullOrEmpty(commandLineArguments))
            {
                commandLineArguments = "openedby=glue";
            }
            else
            {
                commandLineArguments += " openedby=glue";
            }

            if (creatingSyncedProject)
            {
                commandLineArguments += " emptyprojects";
            }

            //Process process = Process.Start(processStartInfo);

            //return process;

            var window = new Npc.MainWindow();
            window.ViewModel.OpenSlnFolderAfterCreation = false;
            window.ProcessCommandLineArguments(commandLineArguments);


            NewProjectViewModel viewModelToReturn = null;

            GlueCommands.Self.DialogCommands.MoveToCursor(window);

            if (window.ShowDialog() == true)
            {
                viewModelToReturn = window.ViewModel;
            }
            return viewModelToReturn;
        }

        public async static void CreateNewProject()
        {
            // Run the new project creator
            // First see if the NewProjectCreator is in this directory

            var viewModel = RunNewProjectCreator();

            string createdProject = null;

            if(viewModel != null)
            {
                createdProject = viewModel.FinalDirectory + "\\" + viewModel.ProjectName + "\\" + viewModel.ProjectName + ".csproj";
            }

            if (!String.IsNullOrEmpty(createdProject))
            {
                await GlueCommands.Self.LoadProjectAsync(createdProject);

                await TaskManager.Self.WaitForAllTasksFinished();

                if(GlueState.Self.CurrentGlueProject == null)
                {
                    GlueCommands.Self.DialogCommands.ShowMessageBox("Could not load the project.  Check the output window for more information.");
                }
                else
                {
                    if(viewModel.IsAddGitIgnoreChecked)
                    {
                        PluginManager.CallPluginMethod("Git Plugin", "AddGitIgnore");
                    }

                    // open the project
                    if (viewModel.IsOpenNewProjectWizardChecked)
                    {
                        PluginManager.CallPluginMethod("New Project Wizard", "RunWizard");
                    }
                }

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
            var directory = GlueState.Self.CurrentSlnFileName?.GetDirectoryContainingThis();

            var viewModel = NewProjectHelper.RunNewProjectCreator(directory, 
                GlueState.Self.ProjectNamespace, creatingSyncedProject:true);

            if (viewModel != null)
            {
                string createdProject = null;

                if (viewModel != null)
                {
                    createdProject = viewModel.FinalDirectory + "\\" + viewModel.ProjectName + "\\" + viewModel.ProjectName + ".csproj";
                }

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

                    // This line is slow. Not sure why..maybe the project is saving after every file is added?
                    // I could make it an async call but I want to figure out why it's slow at the core.
                    newProjectBase.SyncTo(ProjectManager.ProjectBase, false);

                    GlueCommands.Self.ProjectCommands.SaveProjects();
                }

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
