using System;
using System.Collections.Generic;
using FlatRedBall.IO;
using System.Windows.Forms;
using System.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Npc.ViewModels;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Managers;
using L = Localization;

namespace FlatRedBall.Glue.Projects;

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
            createdProject = $@"{viewModel.FinalDirectory}\{viewModel.ProjectName}\{viewModel.ProjectName}.csproj";
        }

        if (!String.IsNullOrEmpty(createdProject))
        {
            await GlueCommands.Self.LoadProjectAsync(createdProject);

            await TaskManager.Self.WaitForAllTasksFinished();

            if(GlueState.Self.CurrentGlueProject == null)
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox(L.Texts.CouldNotLoadProjectSeeOutput);
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

    public static void CreateNewSyncedProject()
    {
        if(ProjectManager.ProjectBase == null)
        {
            MessageBox.Show(L.Texts.CouldNotCreateSyncedProject);
            return;
        }

        // Gotta find the .sln of this project so we can put the synced project in there
        var directory = GlueState.Self.CurrentSlnFileName?.GetDirectoryContainingThis();

        var viewModel = NewProjectHelper.RunNewProjectCreator(directory, 
            GlueState.Self.ProjectNamespace, creatingSyncedProject:true);

        if (viewModel != null)
        {
            var createdProject = $@"{viewModel.FinalDirectory}\{viewModel.ProjectName}\{viewModel.ProjectName}.csproj";
            // The return value could be null if the project being added
            // already exists as a synced project, but this should never be
            // the case here.
            var newProjectBase = ProjectManager.AddSyncedProject(createdProject);
            ProjectManager.SyncedProjects[^1].SaveAsRelativeSyncedProject = true;

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