using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildServerUploaderConsole.Processes
{
    public class PublishGlue : ProcessStep
    {
        public PublishGlue(IResults results) : base("Do a dotnet publish to create a distributable Glue", results)
        {

        }

        public override void ExecuteStep()
        {
            string executable = "dotnet";
            string args = "publish GlueFormsCore.csproj -r win-x86 -c DEBUG";
            var processStart = new System.Diagnostics.ProcessStartInfo(
                executable, args);
            processStart.WorkingDirectory = DirectoryHelper.FrbdkDirectory + @"Glue\Glue\";

            //processStart.CreateNoWindow = true;
            //processStart.RedirectStandardOutput = true;
            //processStart.UseShellExecute = false;
            //processStart.CreateNoWindow = true;
            processStart.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

            Results.WriteMessage($"{executable} {args} in {processStart.WorkingDirectory}");
            //dotnet publish GlueFormsCore.csproj -r win-x86 -c DEBUG
            var process = System.Diagnostics.Process.Start(processStart);
            process.WaitForExit();

        }
    }
}
