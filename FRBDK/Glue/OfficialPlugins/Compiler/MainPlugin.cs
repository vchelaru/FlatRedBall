using FlatRedBall.Glue.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.ComponentModel.Composition;

namespace OfficialPlugins.Compiler
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        MainControl control;

        Compiler compiler;
        Runner runner;

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
                return new Version(0, 1);
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
                Compile((succeeded) =>
                {
                    if (succeeded) runner.Run();
                });
            };
            base.AddToToolBar(toolbar, "Standard");
        }

        private void CreateControl()
        {
            control = new MainControl();

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
            compiler.Compile(control.PrintOutput, control.PrintOutput, afterCompile);
        }
    }
}
