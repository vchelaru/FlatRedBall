using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using OfficialPlugins.Compiler.CommandSending;
using OfficialPlugins.Compiler.Dtos;
using OfficialPlugins.Compiler.ViewModels;
using OfficialPlugins.GameHost.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace OfficialPlugins.Compiler.Managers
{
    public class GameHostController : Singleton<GameHostController>
    {
        PluginTab glueViewSettingsTab;
        CompilerViewModel compilerViewModel;
        GlueViewSettingsViewModel glueViewSettingsViewModel;
        MainControl mainControl;
        GameHostView gameHostView;
        public void Initialize(GameHostView gameHostView, MainControl mainControl, 
            CompilerViewModel compilerViewModel, GlueViewSettingsViewModel glueViewSettingsViewModel,
            PluginTab glueViewSettingsTab)
        {
            this.gameHostView = gameHostView;
            this.compilerViewModel = compilerViewModel;
            this.glueViewSettingsViewModel = glueViewSettingsViewModel;
            this.mainControl = mainControl;
            this.glueViewSettingsTab = glueViewSettingsTab;
            var runner = Runner.Self;
            gameHostView.StopClicked += (not, used) =>
            {
                runner.KillGameProcess();
            };

            gameHostView.RestartGameClicked += async (not, used) =>
            {
                compilerViewModel.IsPaused = false;
                runner.KillGameProcess();
                var succeeded = await Compile();
                if (succeeded)
                {
                    // don't change if it's in edit mode or not here
                    await runner.Run(preventFocus: false);
                }
                else
                {
                    GlueCommands.Self.DialogCommands.FocusTab("Build");
                }
            };

            gameHostView.StartInEditModeClicked += StarRunInEditMode;

            gameHostView.RestartGameCurrentScreenClicked += async (not, used) =>
            {
                var wasEditChecked = compilerViewModel.IsEditChecked;
                // This is the screen that the game is currently on...
                var screenName = await CommandSending.CommandSender.GetScreenName();
                // ...but we may not want to restart on this screen if it's the edit entity screen:
                var isEntityViewingScreen = screenName == "GlueControl.Screens.EntityViewingScreen";
                string commandLineArgs = await GetCommandLineArgs(isRunning:true);

                compilerViewModel.IsPaused = false;
                runner.KillGameProcess();
                var compileSucceeded = await Compile();
                GeneralResponse runResponse = GeneralResponse.UnsuccessfulResponse;
                if (compileSucceeded)
                {
                    // don't change if it's in edit mode
                    runResponse = await runner.Run(preventFocus: false, commandLineArgs);
                }
                else
                {
                    GlueCommands.Self.DialogCommands.FocusTab("Build");
                }
                if (wasEditChecked && runResponse.Succeeded)
                {
                    compilerViewModel.IsEditChecked = true;

                    if (isEntityViewingScreen)
                    {
                        await RefreshManager.Self.PushGlueSelectionToGame();
                    }
                }

                if (!runResponse.Succeeded)
                {
                    mainControl.PrintOutput(runResponse.Message);
                }
            };

            gameHostView.RestartScreenClicked += async (not, used) =>
            {
                compilerViewModel.IsPaused = false;
                await CommandSender.Send(new RestartScreenDto());
            };

            gameHostView.AdvanceOneFrameClicked += async (not, used) =>
            {
                await CommandSender.Send(new AdvanceOneFrameDto());
            };


            gameHostView.PauseClicked += async (not, used) =>
            {
                compilerViewModel.IsPaused = true;
                await CommandSender.Send(new TogglePauseDto());
            };

            gameHostView.UnpauseClicked += async (not, used) =>
            {
                compilerViewModel.IsPaused = false;
                await CommandSender.Send(new TogglePauseDto());
            };

            gameHostView.SettingsClicked += (not, used) =>
            {
                if(glueViewSettingsTab.IsShown)
                {
                    glueViewSettingsTab.Hide();
                }
                else
                {
                    glueViewSettingsTab.Show();
                    glueViewSettingsTab.Focus();
                }
            };

            gameHostView.FocusOnSelectedObjectClicked += async (not, used) =>
            {
                var selectedNos = GlueState.Self.CurrentNamedObjectSave;
                if (selectedNos != null)
                {
                    await RefreshManager.Self.PushGlueSelectionToGame(bringIntoFocus: true);

                }
            };


        }

        private async Task<string> GetCommandLineArgs(bool isRunning)
        {
            string args = null;
            if (isRunning)
            {

                var screenName = await CommandSending.CommandSender.GetScreenName();
                // ...but we may not want to restart on this screen if it's the edit entity screen:
                var isEntityViewingScreen = screenName == "GlueControl.Screens.EntityViewingScreen";
                args = isEntityViewingScreen ? null : screenName;
            }
            else
            {
                var currentScreen = GlueState.Self.CurrentScreenSave;
                var currentEntity = GlueState.Self.CurrentEntitySave;

                if (currentScreen != null && !currentScreen.IsAbstract)
                {
                    args =
                       GlueState.Self.ProjectNamespace + "." + currentScreen.Name.Replace("\\", ".").Replace("/", ".");
                }
                else if (currentEntity != null)
                {
                    args = "GlueControl.Screens.EntityViewingScreen";
                }
                else if(!string.IsNullOrEmpty( GlueState.Self.CurrentGlueProject.StartUpScreen))
                {
                    args =
                       GlueState.Self.ProjectNamespace + "." + GlueState.Self.CurrentGlueProject.StartUpScreen.Replace("\\", ".").Replace("/", ".");
                }
            }

            var project = GlueState.Self.CurrentGlueProject;
            if(project?.DisplaySettings?.AllowWindowResizing == true && glueViewSettingsViewModel.EmbedGameInGameTab)
            {
                if(!string.IsNullOrEmpty(args))
                {
                    args += " ";
                }
                args += "AllowWindowResizing=false";
            }

            return args;
        }

        private void StarRunInEditMode(object sender, EventArgs e)
        {
            
            compilerViewModel.IsEditChecked = true;
            TaskManager.Self.Add(async () =>
            {
                var runner = Runner.Self;

                var succeeded = await Compile();
                if (succeeded)
                {
                    string commandLineArgs = await GetCommandLineArgs(isRunning:false);

                    var runResponse = await runner.Run(preventFocus: false, runArguments:commandLineArgs);
                    if (runResponse.Succeeded)
                    {
                        compilerViewModel.IsEditChecked = true;
                    }
                    succeeded = runResponse.Succeeded;
                }
                else
                {
                    GlueCommands.Self.DialogCommands.FocusTab("Build");
                }

            }, "Starting in edit mode", TaskExecutionPreference.AddOrMoveToEnd);
        }

        public async Task<bool> Compile()
        {
            var compiler = Compiler.Self;

            // does it already have it?
            var existingProcess = Runner.Self.TryFindGameProcess(false);

            if(existingProcess != null)
            {
                Runner.Self.KillGameProcess(existingProcess);
            }

            compilerViewModel.IsCompiling = true;
            var toReturn = await compiler.Compile(
                mainControl.PrintOutput,
                mainControl.PrintOutput,
                compilerViewModel.Configuration,
                compilerViewModel.IsPrintMsBuildCommandChecked);
            compilerViewModel.IsCompiling = false;
            return toReturn;
        }
    }
}
