using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
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

        public void Initialize(GameHostView gameHostControl, MainControl mainControl, CompilerViewModel compilerViewModel, GlueViewSettingsViewModel glueViewSettingsViewModel,
            PluginTab glueViewSettingsTab)
        {
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

            gameHostControl.RestartGameCurrentScreenClicked += async (not, used) =>
            {
                var wasEditChecked = compilerViewModel.IsEditChecked;
                var screenName = await CommandSending.CommandSender.GetScreenName(glueViewSettingsViewModel.PortNumber);


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
                await CommandSender.Send(new RestartScreenDto(), glueViewSettingsViewModel.PortNumber);
            };

            gameHostControl.AdvanceOneFrameClicked += async (not, used) =>
            {
                await CommandSender.Send(new AdvanceOneFrameDto(), glueViewSettingsViewModel.PortNumber);
            };


            gameHostControl.PauseClicked += async (not, used) =>
            {
                compilerViewModel.IsPaused = true;
                await CommandSender.Send(new TogglePauseDto(), glueViewSettingsViewModel.PortNumber);
            };

            gameHostControl.UnpauseClicked += async (not, used) =>
            {
                compilerViewModel.IsPaused = false;
                await CommandSender.Send(new TogglePauseDto(), glueViewSettingsViewModel.PortNumber);
            };

            gameHostControl.SettingsClicked += (not, used) =>
            {
                ShowSettingsTab();
            };


            async Task<bool> Compile()
            {
                var compiler = Compiler.Self;
                compilerViewModel.IsCompiling = true;
                var toReturn = await compiler.Compile(
                    mainControl.PrintOutput,
                    mainControl.PrintOutput,
                    compilerViewModel.Configuration);
                compilerViewModel.IsCompiling = false;
                return toReturn;
            }

            void ShowSettingsTab()
            {
                glueViewSettingsTab.Show();
                glueViewSettingsTab.Focus();
            }

        }
    }
}
