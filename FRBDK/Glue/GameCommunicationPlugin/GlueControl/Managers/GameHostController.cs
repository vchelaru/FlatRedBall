using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GameCommunicationPlugin.GlueControl.CommandSending;
using GameCommunicationPlugin.GlueControl.Dtos;
using GameCommunicationPlugin.GlueControl.ViewModels;
using OfficialPlugins.GameHost.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using CompilerLibrary.ViewModels;
using FlatRedBall.Glue;
using CompilerLibrary.Error;

namespace GameCommunicationPlugin.GlueControl.Managers
{
    public class GameHostController
    {
        PluginTab glueViewSettingsTab;
        CompilerViewModel compilerViewModel;
        GlueViewSettingsViewModel glueViewSettingsViewModel;
        GameHostView gameHostView;
        
        private Func<string, string, Task<string>> _eventCallerWithAction;
        private RefreshManager _refreshManager;

        public void Initialize(GameHostView gameHostView, Action<string> output,
            CompilerViewModel compilerViewModel, GlueViewSettingsViewModel glueViewSettingsViewModel,
            PluginTab glueViewSettingsTab, Func<string, string, Task<string>> eventCallerWithAction, RefreshManager refreshManager)
        {
            _eventCallerWithAction = eventCallerWithAction;
            _refreshManager = refreshManager;
            this.gameHostView = gameHostView;
            this.compilerViewModel = compilerViewModel;
            this.glueViewSettingsViewModel = glueViewSettingsViewModel;
            this.glueViewSettingsTab = glueViewSettingsTab;
            gameHostView.StopClicked += async (not, used) =>
            {
                await PluginManager.CallPluginMethodAsync("Compiler Plugin", "KillGameProcess");
            };

            gameHostView.RestartGameClicked += async (not, used) =>
            {
                compilerViewModel.IsPaused = false;
                await PluginManager.CallPluginMethodAsync("Compiler Plugin", "KillGameProcess");
                var succeeded = await Compile();
                if (succeeded)
                {
                    await PluginManager.CallPluginMethodAsync("Compiler Plugin", "DoRun", 
                        false, // preventFocus
                        string.Empty, new GeneralResponse());
                }
                else
                {
                    GlueCommands.Self.DialogCommands.FocusTab(Localization.Texts.Build);
                }
            };

            gameHostView.StartInEditModeClicked += StartRunInEditMode;

            gameHostView.RestartGameCurrentScreenClicked += async (not, used) =>
            {
                var wasEditChecked = compilerViewModel.IsEditChecked;
                // This is the screen that the game is currently on...
                var screenName = await CommandSender.Self.GetScreenName();
                // ...but we may not want to restart on this screen if it's the edit entity screen:
                var isEntityViewingScreen = screenName == "GlueControl.Screens.EntityViewingScreen";
                string commandLineArgs = await GetCommandLineArgs(isRunning:true);

                compilerViewModel.IsPaused = false;
                await PluginManager.CallPluginMethodAsync("Compiler Plugin", "KillGameProcess");

                var compileSucceeded = await Compile();
                bool runSucceeded = false;
                string runError = "";
                if (compileSucceeded)
                {

                    GeneralResponse response = new GeneralResponse();
                    await PluginManager.CallPluginMethodAsync("Compiler Plugin", "DoRun",
                        false, // prevent focus
                        commandLineArgs, // RunArguments
                        response);

                    runSucceeded = response.Succeeded;
                }
                else
                {
                    GlueCommands.Self.DialogCommands.FocusTab(Localization.Texts.Build);
                }
                if (wasEditChecked && compileSucceeded && runSucceeded)
                {
                    compilerViewModel.IsEditChecked = true;

                    if (isEntityViewingScreen)
                    {
                        await _refreshManager.PushGlueSelectionToGame();
                    }
                }

                if (!runSucceeded)
                {
                    output(runError);
                }
            };

            gameHostView.RestartScreenClicked += async (not, used) =>
            {
                compilerViewModel.IsPaused = false;
                await CommandSender.Self.Send(new RestartScreenDto());
            };

            gameHostView.AdvanceOneFrameClicked += async (not, used) =>
            {
                await CommandSender.Self.Send(new AdvanceOneFrameDto());
            };


            gameHostView.PauseClicked += async (not, used) =>
            {
                compilerViewModel.IsPaused = true;
                await CommandSender.Self.Send(new TogglePauseDto());
            };

            gameHostView.UnpauseClicked += async (not, used) =>
            {
                compilerViewModel.IsPaused = false;
                await CommandSender.Self.Send(new TogglePauseDto());
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
                    await _refreshManager.PushGlueSelectionToGame(bringIntoFocus: true);

                }
            };

            gameHostView.SelectStartupScreenClicked += (not, used) =>
            {
                var startupScreen = GlueState.Self.CurrentGlueProject.StartUpScreen;
                var screenSave = ObjectFinder.Self.GetScreenSave(startupScreen);
                if (screenSave != null)
                {
                    GlueState.Self.CurrentScreenSave = screenSave;
                }
            };
        }

        private async Task<string> GetCommandLineArgs(bool isRunning)
        {
            string args = null;
            if (isRunning)
            {

                var screenName = await CommandSender.Self.GetScreenName();
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

            // This prevents the game from stretching to the game tab in .NET 6, so don't do this in .net 6:
            var is6OrGreater = GlueState.Self.CurrentMainProject.DotNetVersion.Major >= 6;
            if (!is6OrGreater)
            {
                var project = GlueState.Self.CurrentGlueProject;
                if(project?.DisplaySettings?.AllowWindowResizing == true && glueViewSettingsViewModel.EmbedGameInGameTab)
                {
                    if(!string.IsNullOrEmpty(args))
                    {
                        args += " ";
                    }
                    args += "AllowWindowResizing=false";
                }
            }
            else
            {
                var project = GlueState.Self.CurrentGlueProject;
                if (project?.DisplaySettings?.AllowWindowResizing == false && glueViewSettingsViewModel.EmbedGameInGameTab)
                {
                    if (!string.IsNullOrEmpty(args))
                    {
                        args += " ";
                    }
                    args += "AllowWindowResizing=true";
                }
            }

            return args;
        }

        private void StartRunInEditMode(object sender, EventArgs e)
        {
            
            compilerViewModel.IsEditChecked = true;
            TaskManager.Self.Add(async () =>
            {
                var succeeded = await Compile();
                if (succeeded)
                {
                    string commandLineArgs = await GetCommandLineArgs(isRunning:false);

                    GeneralResponse response = new GeneralResponse();
                    await PluginManager.CallPluginMethodAsync("Compiler Plugin", "DoRun", 
                        false, // prevent focus
                        commandLineArgs, // RunArguments
                        response);

                    if (response.Succeeded)
                    {
                        compilerViewModel.IsEditChecked = true;
                    }
                    else
                    {
                        GlueCommands.Self.PrintError(response.Message);
                    }
                    succeeded = response.Succeeded;

                    if(glueViewSettingsViewModel.EmbedGameInGameTab && glueViewSettingsViewModel.EnableLiveEdit)
                    {
                        // Sometimes the game can update the window before the window becomes borderless. If that happens, this
                        // fails to update the position. Need to delay.
                        await Task.Delay(250);
                        
                        GlueCommands.Self.DoOnUiThread(() => gameHostView.SetGameToEmbeddedGameWindow());
                    }
                }
                else
                {
                    GlueCommands.Self.DialogCommands.FocusTab(Localization.Texts.Build);
                }

            }, "Starting in edit mode", TaskExecutionPreference.AddOrMoveToEnd);
        }

        public async Task<bool> Compile()
        {
            var generalResponse = new CompileGeneralResponse();

            await PluginManager.CallPluginMethodAsync("Compiler Plugin", "Compile", 
                compilerViewModel.Configuration,
                compilerViewModel.IsPrintMsBuildCommandChecked,
                generalResponse);

            return generalResponse.Succeeded;
        }
    }
}
