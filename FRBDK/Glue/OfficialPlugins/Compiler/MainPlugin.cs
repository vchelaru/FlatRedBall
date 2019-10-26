using FlatRedBall.Glue.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.ComponentModel.Composition;
using OfficialPlugins.Compiler.ViewModels;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Managers;
using System.Windows;
using OfficialPlugins.Compiler.CodeGeneration;
using System.Net.Sockets;

namespace OfficialPlugins.Compiler
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        #region Fields/Properties

        MainControl control;

        Compiler compiler;
        Runner runner;
        CompilerViewModel viewModel;

        public override string FriendlyName
        {
            get
            {
                return "Glue Compiler";
            }
        }

        public override Version Version
        {
            get
            {
                // 0.4 introduces:
                // - multicore building
                // - Removed warnings and information when building - now we just show start, end, and errors
                // - If an error occurs, a popup appears telling the user that the game crashed, and to open Visual Studio
                // 0.5
                // - Support for running content-only builds
                // 0.6
                // - Added VS 2017 support
                // 0.7
                // - Added a list of MSBuild locations
                return new Version(0, 7);
            }
        }

        #endregion

        public override void StartUp()
        {
            CreateControl();

            CreateToolbar();

            this.ReactToFileChangeHandler += HandleFileChanged;
            this.ReactToLoadedGlux += HandleGluxLoaded;
            this.ReactToUnloadedGlux += HandleGluxUnloaded;

            compiler = new Compiler();
            runner = new Runner();
            runner.IsRunningChanged += HandleIsRunningChanged;
        }

        private void HandleIsRunningChanged(object sender, EventArgs e)
        {
            viewModel.IsRunning = runner.IsRunning;
        }

        private void HandleGluxUnloaded()
        {
            viewModel.CompileContentButtonVisibility = Visibility.Collapsed;
        }

        private void HandleGluxLoaded()
        {
            UpdateCompileContentVisibility();

            TaskManager.Self.Add(MainCodeGenerator.GenerateAll, "Generate Glue Control Code");
        }

        private void UpdateCompileContentVisibility()
        {
            bool shouldShowCompileContentButton = false;

            if (GlueState.Self.CurrentMainProject != null)
            {
                shouldShowCompileContentButton = GlueState.Self.CurrentMainProject != GlueState.Self.CurrentMainContentProject;

                if (!shouldShowCompileContentButton)
                {
                    foreach (var mainSyncedProject in GlueState.Self.SyncedProjects)
                    {
                        if (mainSyncedProject != mainSyncedProject.ContentProject)
                        {
                            shouldShowCompileContentButton = true;
                            break;
                        }
                    }
                }

            }

            if (shouldShowCompileContentButton)
            {
                viewModel.CompileContentButtonVisibility = Visibility.Visible;
            }
            else
            {
                viewModel.CompileContentButtonVisibility = Visibility.Collapsed;
            }
        }

        private void HandleFileChanged(string fileName)
        {
            bool shouldBuildContent = viewModel.AutoBuildContent &&
                GlueState.Self.CurrentMainProject != GlueState.Self.CurrentMainContentProject &&
                GlueState.Self.CurrentMainContentProject.IsFilePartOfProject(fileName);

            if(shouldBuildContent)
            {
                control.PrintOutput($"{DateTime.Now.ToLongTimeString()} Building for changed file {fileName}");

                BuildContent(OutputSuccessOrFailure);
            }
            
        }

        private void CreateToolbar()
        {
            var toolbar = new RunnerToolbar();
            toolbar.RunClicked += HandleToolbarRunClicked;
            base.AddToToolBar(toolbar, "Standard");
        }

        private void HandleToolbarRunClicked(object sender, EventArgs e)
        {
            PluginManager.ReceiveOutput("Building Project. See \"Build\" tab for more information...");
            Compile((succeeded) =>
            {
                if (succeeded)
                {
                    bool hasErrors = GetIfHasErrors();
                    if (hasErrors)
                    {
                        var runAnywayMessage = "Your project has content errors. To fix them, see the Errors tab. You can still run the game but you may experience crashes. Run anyway?";

                        GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(runAnywayMessage, () => runner.Run());
                    }
                    else
                    {
                        PluginManager.ReceiveOutput("Building succeeded. Running project...");

                        runner.Run();
                    }
                }
                else
                {
                    PluginManager.ReceiveError("Building failed. See \"Build\" tab for more information.");


                }
            });
        }

        private void CreateControl()
        {
            control = new MainControl();
            viewModel = new CompilerViewModel();
            viewModel.Configuration = "Debug";
            control.DataContext = viewModel;

            base.AddToTab(
                PluginManager.BottomTab, control, "Build");
            AssignControlEvents();
        }

        private void AssignControlEvents()
        {
            control.BuildClicked += delegate
            {
                Compile();
            };

            control.StopClicked += (not, used) =>
            {
                runner.Stop();
            };

            control.RestartGameClicked += (not, used) =>
            {
                viewModel.IsPaused = false;
                runner.Stop();
                Compile((succeeded) =>
                {
                    if (succeeded)
                    {
                        runner.Run();
                    }
                });
            };

            control.RestartGameCurrentScreenClicked += async (not, used) =>
            {
                string screenName = null;

                try
                {
                    await CommandSending.CommandSender.SendCommand("GetCurrentScreen");
                }
                catch(SocketException)
                {
                    // do nothing, may not have been able to communicate, just output
                    control.PrintOutput("Could not get the game's screen, restarting game from startup screen");
                }
                viewModel.IsPaused = false;
                runner.Stop();
                Compile(async (succeeded) =>
                {
                    if (succeeded)
                    {
                        await runner.Run(screenName);
                    }
                });
            };

            control.RestartScreenClicked += async (not, used) =>
            {
                viewModel.IsPaused = false;
                try
                {
                    control.PrintOutput("Sending command: RestartScreen");
                    var response = await CommandSending.CommandSender.SendCommand("RestartScreen");
                    if(response == "true")
                    {
                        control.PrintOutput($"Succeeded");
                    }
                    else
                    {
                        control.PrintOutput($"Response: {response}");
                    }
                }
                catch (Exception e)
                {
                    control.PrintOutput("Failed to restart screen:\n" + e.ToString());
                }
            };

            control.BuildContentClicked += delegate
            {
                BuildContent(OutputSuccessOrFailure);
            };

            control.RunClicked += delegate
            {
                Compile((succeeded) =>
                {
                    if (succeeded)
                    {
                        runner.Run();
                    }
                    else
                    {
                        var runAnywayMessage = "Your project has content errors. To fix them, see the Errors tab. You can still run the game but you may experience crashes. Run anyway?";

                        GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(runAnywayMessage, () => runner.Run());
                    }
                });
            };

            control.PauseClicked += (not, used) =>
            {
                viewModel.IsPaused = true;
                CommandSending.CommandSender.SendCommand("TogglePause");
                
            };

            control.UnpauseClicked += (not, used) =>
            {
                viewModel.IsPaused = false;
                CommandSending.CommandSender.SendCommand("TogglePause");

            };
        }

        private static bool GetIfHasErrors()
        {
            var errorPlugin = PluginManager.AllPluginContainers
                                .FirstOrDefault(item => item.Plugin is ErrorPlugin.MainErrorPlugin)?.Plugin as ErrorPlugin.MainErrorPlugin;

            var hasErrors = errorPlugin?.HasErrors == true;
            return hasErrors;
        }

        private void OutputSuccessOrFailure(bool succeeded)
        {
            if (succeeded)
            {
                control.PrintOutput($"{DateTime.Now.ToLongTimeString()} Build succeeded");
            }
            else
            {
                control.PrintOutput($"{DateTime.Now.ToLongTimeString()} Build failed");

            }
        }

        private void BuildContent(Action<bool> afterCompile = null)
        {
            compiler.BuildContent(control.PrintOutput, control.PrintOutput, afterCompile, viewModel.Configuration);
        }

        private void Compile(Action<bool> afterCompile = null)
        {
            compiler.Compile(control.PrintOutput, control.PrintOutput, afterCompile, viewModel.Configuration);
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }
    }
}
