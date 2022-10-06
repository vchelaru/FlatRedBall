using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildServerUploaderConsole.Processes
{
    internal class BuildGlue : ProcessStep
    {
        public BuildGlue(IResults results) : base("Do a dotnet build to build all plugins", results)
        {

        }

        public override void ExecuteStep()
        {
            string executable = "dotnet";
            string args = "build \"Glue with All.sln\" -c DEBUG";

            var processStart = new System.Diagnostics.ProcessStartInfo(
                executable, args);
            processStart.WorkingDirectory = DirectoryHelper.FrbdkDirectory + @"Glue\";

            //processStart.CreateNoWindow = true;
            //processStart.RedirectStandardOutput = true;
            //processStart.UseShellExecute = false;
            //processStart.CreateNoWindow = true;
            processStart.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            //processStart.RedirectStandardOutput = true;
            //processStart.RedirectStandardError = true;
            processStart.UseShellExecute = false;
            processStart.RedirectStandardOutput = true;
            processStart.RedirectStandardError = true;
            //Process p = Process.Start(startInfo);

            Results.WriteMessage($"{executable} {args} in {processStart.WorkingDirectory}");
            //dotnet publish GlueFormsCore.csproj -c DEBUG
            var process = System.Diagnostics.Process.Start(processStart);

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            //if (!Directory.Exists(DirectoryHelper.GluePublishDestinationFolder))
            //{
            //    var errorLines = output.Split('\n')
            //        .Where(item => !string.IsNullOrWhiteSpace(item)
            //                // Include all lines because the provide context about which project is being built...
            //                //&& item.Contains (" error ")
            //                )
            //        .Select(item => item?.Trim());

            //    foreach (var item in errorLines)
            //    {
            //        System.Console.WriteLine(item);
            //    }

            //    throw new Exception($"dotnet publish finished, but the directory {DirectoryHelper.GluePublishDestinationFolder} is not present");
            //}
        }
    }

}
