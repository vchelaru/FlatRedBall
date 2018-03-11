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

        }

        private void HandleGluxUnloaded()
        {
            viewModel.CompileContentButtonVisibility = Visibility.Collapsed;
        }

        private void HandleGluxLoaded()
        {
            bool shouldShow = false;

            if (GlueState.Self.CurrentMainProject != null)
            {
                shouldShow = GlueState.Self.CurrentMainProject != GlueState.Self.CurrentMainContentProject;

                if(!shouldShow)
                {
                    foreach(var mainSyncedProject in GlueState.Self.SyncedProjects)
                    {
                        if(mainSyncedProject != mainSyncedProject.ContentProject)
                        {
                            shouldShow = true;
                            break;
                        }
                    }
                }

            }

            if(shouldShow)
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
            toolbar.RunClicked += delegate
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

                            GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(runAnywayMessage, runner.Run);
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
            };
            base.AddToToolBar(toolbar, "Standard");
        }

        private void CreateControl()
        {
            control = new MainControl();
            viewModel = new CompilerViewModel();
            viewModel.Configuration = "Debug";
            control.DataContext = viewModel;

            base.AddToTab(
                PluginManager.BottomTab, control, "Build");

            control.BuildClicked += delegate
            {
                Compile();
            };

            control.BuildContentClicked += delegate
            {
                BuildContent(OutputSuccessOrFailure);
            };

            control.RunClicked += delegate
            {
                bool hasErrors = GetIfHasErrors();
                if (hasErrors)
                {
                    var runAnywayMessage = "Your project has content errors. To fix them, see the Errors tab. You can still run the game but you may experience crashes. Run anyway?";

                    GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(runAnywayMessage, runner.Run);
                }
                else
                {
                    runner.Run();
                }

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
