using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.Compiler
{
    class Runner
    {
        internal void Run()
        {
            var projectFileName = GlueState.Self.CurrentMainProject.FullFileName;
            var projectDirectory = FileManager.GetDirectory(projectFileName);
            var executableName = FileManager.RemoveExtension(FileManager.RemovePath(projectFileName));
            // todo - make the plugin smarter so it knows where the .exe is really located
            var exeLocation = projectDirectory + "bin/x86/debug/" + executableName + ".exe";

            if(System.IO.File.Exists(exeLocation))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = exeLocation;
                startInfo.WorkingDirectory = FileManager.GetDirectory(exeLocation);

                var process = System.Diagnostics.Process.Start(startInfo);

                process.EnableRaisingEvents = true;
                process.Exited += HandleProcessExit;
            }
        }

        private void HandleProcessExit(object sender, EventArgs e)
        {
            var process = sender as Process;

            if(process.ExitCode != 0)
            {
                System.Windows.MessageBox.Show("Oh no! The game crashed. Run from Visual Studio for more information on the error.");
            }
        }
    }
}
