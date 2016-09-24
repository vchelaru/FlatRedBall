using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.Compiler
{
    class Compiler
    {
        internal void Compile(Action<string> printOutput, Action<string> printError, Action<bool> afterBuilt = null, 
            string configuration = "Debug")
        {
            TaskManager.Self.AddAsyncTask(() =>
            {
                var projectFileName = GlueState.Self.CurrentMainProject.FullFileName;
                string executable = @"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe";

                // For info on parameters:
                // https://msdn.microsoft.com/en-us/library/ms164311.aspx?f=255&MSPPError=-2147217396
                // \m uses multiple cores
                string arguments = $"\"{projectFileName}\" " + 
                    $"/p:Configuration=\"{configuration}\" " + 
                    "/m " + 
                    "/nologo " + 
                    "/verbosity:minimal";

                Process process = CreateProcess("\"" + executable + "\"", arguments);

                printOutput("Build started at " + DateTime.Now.ToLongTimeString());
                // This is noisy and technical. Reducing output window verbosity
                //printOutput(process.StartInfo.FileName + " " + process.StartInfo.Arguments);


                var errorString = RunProcess(printOutput, printError, executable, process);
                bool succeeded = true;
                if (!string.IsNullOrEmpty(errorString))
                {
                    printError(errorString);
                    succeeded = false;
                }

                afterBuilt?.Invoke(succeeded);
            },
            "Building project");
        }

        private static string RunProcess(Action<string> printOutput, Action<string> printError, string executable, Process process)
        {
            string errorString = "";
            process.Start();
            
            const int timeToWait = 50;
            bool hasUserTerminatedProcess = false;

            StringBuilder outputWhileRunning = new StringBuilder();

            while (!process.HasExited)
            {
                System.Threading.Thread.Sleep(timeToWait);
                // If we don't read the output, this seems to freeze and never exit
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = process.StandardOutput.ReadLine();
                    if(!string.IsNullOrEmpty(line))
                    {

                        printOutput(line);
                    }
                }
            }

            if (process.ExitCode != 0)
            {
                if (hasUserTerminatedProcess)
                {
                    errorString = "The process\n\n" + executable + "\n\nhas been terminated";
                }
                else
                {
                    errorString = process.StandardError.ReadToEnd() + "\n\n" + process.StandardOutput.ReadToEnd();
                }
            }
            else
            {
                string str;

                while ((str = process.StandardOutput.ReadLine()) != null)
                {
                    if (printOutput != null)
                    {
                        printOutput(str);
                    }
                }

                while ((str = process.StandardError.ReadLine()) != null)
                {
                    if (printError != null)
                    {
                        printError(str);
                    }
                    errorString += str + "\n";
                }
                if (!string.IsNullOrEmpty(errorString))
                {
                    errorString += "\n";
                }
            }
            return errorString;
        }

        private static Process CreateProcess(string executable, string arguments)
        {
            Process process = new Process();

            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = executable;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;


            return process;
        }
    }
}
