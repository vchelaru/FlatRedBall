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

namespace OfficialPlugins.Compiler.Managers
{
    public class GameHostController : Singleton<GameHostController>
    {
        PluginTab glueViewSettingsTab;
        CompilerViewModel compilerViewModel;
        MainControl mainControl;
        public void Initialize(GameHostView gameHostControl, MainControl mainControl, CompilerViewModel compilerViewModel, GlueViewSettingsViewModel glueViewSettingsViewModel,
            PluginTab glueViewSettingsTab)
        {
            this.compilerViewModel = compilerViewModel;
            this.mainControl = mainControl;
            this.glueViewSettingsTab = glueViewSettingsTab;
            var runner = Runner.Self;
            gameHostControl.StopClicked += (not, used) =>
            {
                runner.KillGameProcess();
            };

            gameHostControl.RestartGameClicked += async (not, used) =>
            {
                compilerViewModel.IsPaused = false;
                runner.KillGameProcess();
                var succeeded = await Compile();
                if (succeeded)
                {
                    await runner.Run(preventFocus: false);
                }
            };

            gameHostControl.StartInEditModeClicked += async (not, used) =>
            {
                TaskManager.Self.Add(async () =>
                {
                    var succeeded = await Compile();
                    if(succeeded)
                    {
                        await runner.Run(preventFocus: false);
                        compilerViewModel.IsEditChecked = true;
                    }

                }, "Starting in edit mode", TaskExecutionPreference.AddOrMoveToEnd);
            };

            gameHostControl.RestartGameCurrentScreenClicked += async (not, used) =>
            {
                var wasEditChecked = compilerViewModel.IsEditChecked;
                var screenName = await CommandSending.CommandSender.GetScreenName();


                compilerViewModel.IsPaused = false;
                runner.KillGameProcess();
                var succeeded = await Compile();

                if (succeeded)
                {
                    if (succeeded)
                    {
                        await runner.Run(preventFocus: false, screenName);
                        if (wasEditChecked)
                        {
                            compilerViewModel.IsEditChecked = true;
                        }
                    }
                }
            };

            gameHostControl.RestartScreenClicked += async (not, used) =>
            {
                compilerViewModel.IsPaused = false;
                await CommandSender.Send(new RestartScreenDto());
            };

            gameHostControl.AdvanceOneFrameClicked += async (not, used) =>
            {
                await CommandSender.Send(new AdvanceOneFrameDto());
            };


            gameHostControl.PauseClicked += async (not, used) =>
            {
                compilerViewModel.IsPaused = true;
                await CommandSender.Send(new TogglePauseDto());
            };

            gameHostControl.UnpauseClicked += async (not, used) =>
            {
                compilerViewModel.IsPaused = false;
                await CommandSender.Send(new TogglePauseDto());
            };

            gameHostControl.SettingsClicked += (not, used) =>
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

            gameHostControl.FocusOnSelectedObjectClicked += async (not, used) =>
            {
                var selectedNos = GlueState.Self.CurrentNamedObjectSave;
                if (selectedNos != null)
                {
                    await RefreshManager.Self.PushGlueSelectionToGame(bringIntoFocus: true);

                }
            };


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
                compilerViewModel.Configuration);
            compilerViewModel.IsCompiling = false;
            return toReturn;
        }
    }
}
