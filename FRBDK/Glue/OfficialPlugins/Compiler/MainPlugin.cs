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
                return new Version(0, 6);
            }
        }

        #endregion

        public override void StartUp()
        {
            CreateControl();

            CreateToolbar();

            this.ReactToFileChangeHandler += HandleFileChanged;

            compiler = new Compiler();
            runner = new Runner();

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
                        PluginManager.ReceiveOutput("Building succeeded. Running project...");

                        runner.Run();
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
                runner.Run();

            };
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
