using FlatRedBall.Glue.Managers;
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
    class Compiler
    {
        List<string> AvailableLocations = new List<string>
        {
            @"C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe"
        };
        
        string msBuildLocation;
        string MsBuildLocation
        {
            get
            {
                if(msBuildLocation == null)
                {
                    foreach(var item in AvailableLocations)
                    {
                        if(System.IO.File.Exists(item))
                        {
                            msBuildLocation = item;
                            break;
                        }
                    }
                }

                return msBuildLocation;
            }
        }

        internal void Compile(Action<string> printOutput, Action<string> printError, Action<bool> afterBuilt = null, 
            string configuration = "Debug")
        {
            TaskManager.Self.AddAsyncTask(() =>
            {
                var projectFileName = GlueState.Self.CurrentMainProject.FullFileName;

                bool succeeded = RunMsBuildOnProject(printOutput, printError, configuration, projectFileName);

                afterBuilt?.Invoke(succeeded);
            },
            "Building project");
        }

        private bool RunMsBuildOnProject(Action<string> printOutput, Action<string> printError, string configuration, string projectFileName)
        {
            bool succeeded = true;

            if(MsBuildLocation != null)
            {
                string outputDirectory = GlueState.Self.CurrentGlueProjectDirectory + "bin/x86/Debug/";

                // For info on parameters:
                // https://msdn.microsoft.com/en-us/library/ms164311.aspx?f=255&MSPPError=-2147217396
                // \m uses multiple cores
                string arguments = $"\"{projectFileName}\" " +
                    $"/p:Configuration=\"{configuration}\" " +
                    $"/p:XNAContentPipelineTargetPlatform=\"Windows\" " +
                    $"/p:XNAContentPipelineTargetProfile=\"HiDef\" " +
                    $"/p:OutDir=\"{outputDirectory}\" " +
                    "/m " +
                    "/nologo " +
                    "/verbosity:minimal";

                Process process = CreateProcess("\"" + MsBuildLocation + "\"", arguments);

                printOutput("Build started at " + DateTime.Now.ToLongTimeString());
                // This is noisy and technical. Reducing output window verbosity
                //printOutput(process.StartInfo.FileName + " " + process.StartInfo.Arguments);


                var errorString = RunProcess(printOutput, printError, MsBuildLocation, process);
                if (!string.IsNullOrEmpty(errorString))
                {
                    printError(errorString);
                    succeeded = false;
                }
            }
            else
            {
                string message =
                    $"Could not find msbuild.exe. Looked in the following locations:";

                foreach (var item in AvailableLocations)
                {
                    message += $"\n{item}";
                }

                printError(message);
                succeeded = false;
            }
            return succeeded;
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

        internal void BuildContent(Action<string> printOutput, Action<string> printError, Action<bool> afterBuilt = null, string configuration = "Debug")
        {
            TaskManager.Self.AddAsyncTask(() =>
            {
                bool succeeded = false;

                if(GlueState.Self.CurrentMainProject == GlueState.Self.CurrentMainProject.ContentProject)
                {
                    // eventually use the MG build here...
                    printOutput("Project does not have a dedicated content project");
                }
                else
                {
                    var projectFileName = GlueState.Self.CurrentMainProject.ContentProject.FullFileName;

                    succeeded = RunMsBuildOnProject(printOutput, printError, configuration, projectFileName);
                }

                afterBuilt?.Invoke(succeeded);

            }, "Building Content");
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
