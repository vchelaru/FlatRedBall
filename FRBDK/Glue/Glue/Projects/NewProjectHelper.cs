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
using System.Threading.Tasks;
using FlatRedBall.Glue.VSHelpers.Projects;
using System.Security.Cryptography.X509Certificates;

namespace FlatRedBall.Glue.Projects;

public class NewProjectHelper
{
    static async Task<NewProjectViewModel> RunNewProjectCreatorAsync(FilePath directoryForNewProject = null, string namespaceForNewProject = null, bool creatingSyncedProject = false)
    {
        string commandLineArguments = null;
        if (directoryForNewProject != null)
        {
            // Directory is the location of the existing project to use when using synced projects
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

        var responseAsObject = PluginManager.CallPluginMethod("FRB Source", "HasFrbAndGumReposInDefaultLocation");
        var hasDefaultRepositories = responseAsObject as bool? ;
        if(hasDefaultRepositories == true)
        {
            commandLineArguments += " showsourcecheckbox";
        }

        if(!string.IsNullOrEmpty(GlueState.Self.GlueSettingsSave?.DefaultNewProjectDirectory))
        {
            commandLineArguments += $" defaultdestinationdirectory=\"{GlueState.Self.GlueSettingsSave.DefaultNewProjectDirectory}\"";
        }

        //Process process = Process.Start(processStartInfo);

        //return process;

        var window = new Npc.MainWindow();
        window.ViewModel.OpenSlnFolderAfterCreation = false;
        if(creatingSyncedProject)
        {
            window.ViewModel.IsOpenNewProjectWizardChecked = false;
            window.ViewModel.NewProjectWizardVisibility = System.Windows.Visibility.Collapsed;
        }
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

        var viewModel = await RunNewProjectCreatorAsync();

        string createdProject = null;

        if(viewModel != null)
        {
            // Not every template puts the project Glue should open at the same place - a dotnet
            // template can produce several projects, only one of which is the game.
            createdProject = Npc.ProjectCreationHelper.GetProjectToOpen(
                viewModel.SelectedProject, viewModel.ProjectName, viewModel.FinalDirectory);
        }

        GlueCommands.Self.DialogCommands.ShowSpinner("Preparing Project...");

        if(string.IsNullOrEmpty(createdProject))
        {
            GlueCommands.Self.DialogCommands.HideSpinner();
        }
        else //if (!string.IsNullOrEmpty(createdProject))
        {
            // see if the default directory changed:
            var newDefaultDirectory = viewModel.ProjectDestinationLocation;
            if(newDefaultDirectory != GlueState.Self.GlueSettingsSave.DefaultNewProjectDirectory)
            {
                GlueState.Self.GlueSettingsSave.DefaultNewProjectDirectory = newDefaultDirectory;
                GlueCommands.Self.GluxCommands.SaveSettings();
            }

            GlueCommands.Self.DialogCommands.ShowSpinner("Loading Project...");


            await GlueCommands.Self.LoadProjectAsync(createdProject);

            GlueCommands.Self.DialogCommands.ShowSpinner("Waiting for Tasks to finish...");


            await TaskManager.Self.WaitForAllTasksFinished();

            GlueCommands.Self.DialogCommands.HideSpinner();


            if (GlueState.Self.CurrentGlueProject == null)
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox(L.Texts.CouldNotLoadProjectSeeOutput);
            }
            else
            {

                if(viewModel.IsAddGitIgnoreChecked)
                {
                    PluginManager.CallPluginMethod("Git Plugin", "AddGitIgnore");
                }

                if(viewModel.IsIncludeSourceEffectivelyChecked)
                {
                    await PluginManager.CallPluginMethodAsync("FRB Source", "AddFrbSourceToDefaultLocation", GlueState.Self.CurrentMainProject);
                }

                // open the project
                if (viewModel.IsOpenNewProjectWizardChecked)
                {
                    PluginManager.CallPluginMethod("New Project Wizard", "RunWizard");
                }
            }

        }
    }

    public async Task<NewProjectViewModel?> CreateNewSyncedProject()
    {
        //////////////////////Early Out//////////////////////
        if(GlueState.Self.CurrentMainProject == null)
        {
            GlueCommands.Self.PrintError(
                "Can not create a new Synced Project because there is no open project.");
            return null;
        }
        ///////////////////End Early Out/////////////////////

        // Gotta find the .sln of this project so we can put the synced project in there
        var directory = GlueState.Self.CurrentSlnFileName?.GetDirectoryContainingThis();

        var viewModel = await RunNewProjectCreatorAsync(directory, 
            GlueState.Self.ProjectNamespace, creatingSyncedProject:true);

        if (viewModel != null)
        {
            var createdProject = $@"{viewModel.FinalDirectory}\{viewModel.ProjectName}\{viewModel.ProjectName}.csproj";
            // The return value could be null if the project being added
            // already exists as a synced project, but this should never be
            // the case here.
            var newProjectBase = GlueCommands.Self.ProjectCommands.AddSyncedProject(createdProject) as VisualStudioProject;
            ProjectManager.SyncedProjects[^1].SaveAsRelativeSyncedProject = true;

            RemoveDuplicateFiles(newProjectBase);

            await PluginManager.CallPluginMethodAsync("MonoGame Content Plugin", "RefreshXnbsForProject", newProjectBase);

            if (viewModel.IsIncludeSourceEffectivelyChecked)
            {
                await PluginManager.CallPluginMethodAsync("FRB Source", "AddFrbSourceToDefaultLocation", newProjectBase);
            }

            // This line is slow. Not sure why..maybe the project is saving after every file is added?
            // I could make it an async call but I want to figure out why it's slow at the core.
            // Update 10/26/2024 - currentl SaveProjects handles syncing, so we can get rid of this
            // and have it happen automatically when we save:
            //newProjectBase.SyncTo(GlueState.Self.CurrentMainProject, false);

            GlueCommands.Self.ProjectCommands.SaveProjects();



        }

        return viewModel;
    }

    private static void RemoveDuplicateFiles(VisualStudioProject? newProjectBase)
    {
        // Remove Game1.cs so that it will just use the same Game1 of the master project.
        string fileName = "Game1.cs";

        if (File.Exists(newProjectBase.Directory + fileName))
        {
            newProjectBase.RemoveItem(newProjectBase.Directory + fileName);
            File.Delete(newProjectBase.Directory + fileName);
        }

    }
}
