using FlatRedBall.Glue.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.ComponentModel.Composition;
using OfficialPlugins.Compiler.ViewModels;

namespace OfficialPlugins.Compiler
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
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
                return new Version(0, 3);
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            CreateControl();

            CreateToolbar();

            compiler = new Compiler();
            runner = new Runner();

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

            control.RunClicked += delegate
            {
                runner.Run();

            };
        }

        private void Compile(Action<bool> afterCompile = null)
        {
            compiler.Compile(control.PrintOutput, control.PrintOutput, afterCompile, viewModel.Configuration);
        }
    }
}
