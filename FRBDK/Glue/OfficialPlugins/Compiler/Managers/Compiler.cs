using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.Compiler
{
    class Compiler : Singleton<Compiler>
    {
        List<FilePath> AvailableLocations = new List<FilePath>
        {
            $@"{FileManager.GetDirectory(Assembly.GetEntryAssembly().Location)}Tools\MSBuild\15.0\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\amd64\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe",

        };
        
        FilePath msBuildLocation;
        FilePath MsBuildLocation
        {
            get
            {
                if(msBuildLocation == null)
                {
                    foreach(var item in AvailableLocations)
                    {
                        if(item.Exists())
                        {
                            msBuildLocation = item;
                            break;
                        }
                    }
                }

                return msBuildLocation;
            }
        }

        internal async Task<bool> Compile(Action<string> printOutput, Action<string> printError,  
            string configuration = "Debug")
        {
            var shouldCompile = true;

            //var message = GetMissingFrameworkMessage();
            //if(!string.IsNullOrEmpty(message))
            //{
            //    printError("Cannot build due to missing .NET SDK:\n" + message);
            //    shouldCompile = false;
            //}

            //do we actually want to do this ?
            if(shouldCompile)
            {
                var projectFileName = GlueState.Self.CurrentMainProject.FullFileName;

                var succeeded = false;

                string msBuildPath;
                string additionalArgumentPrefix = "";

                if (MsBuildLocation != null)
                {
                    msBuildPath = MsBuildLocation.FullPath;
                }
                else
                {
                    // try dotnet msbuild
                    msBuildPath = "dotnet";

                    additionalArgumentPrefix = "msbuild ";
                }

                #region Restore Nuget

                {
                    string startOutput = "Nuget Restore started at " + DateTime.Now.ToLongTimeString();
                    string endOutput = "Nuget Restore succeeded";

                    string outputDirectory = GlueState.Self.CurrentGlueProjectDirectory + "bin/x86/Debug/";
                    // For info on parameters:
                    // https://msdn.microsoft.com/en-us/library/ms164311.aspx?f=255&MSPPError=-2147217396
                    // \m uses multiple cores
                    string arguments = 
                        additionalArgumentPrefix +
                        $"\"{projectFileName}\" -t:restore " +
                        "/nologo " +
                        "/verbosity:minimal";

                    succeeded = await StartMsBuildWithParameters(printOutput, printError, startOutput, endOutput, arguments, msBuildPath);
                }

                #endregion

                #region Build

                if(succeeded)
                {
                    string startOutput = "Build started at " + DateTime.Now.ToLongTimeString();
                    string endOutput = "Build succeeded";

                    string outputDirectory = GlueState.Self.CurrentGlueProjectDirectory + "bin/x86/Debug/";
                    // For info on parameters:
                    // https://msdn.microsoft.com/en-us/library/ms164311.aspx?f=255&MSPPError=-2147217396
                    // \m uses multiple cores
                    string arguments =
                        additionalArgumentPrefix +
                        $"\"{projectFileName}\" " +
                        $"/p:Configuration=\"{configuration}\" " +
                        $"/p:XNAContentPipelineTargetPlatform=\"Windows\" " +
                        $"/p:XNAContentPipelineTargetProfile=\"HiDef\" " +
                        $"/p:OutDir=\"{outputDirectory}\" " +
                        "/m " +
                        "/nologo " +
                        "/verbosity:minimal";

                    succeeded = await StartMsBuildWithParameters(printOutput, printError, startOutput, endOutput, arguments, msBuildPath);
                }

                #endregion

                if(!succeeded)
                {
                    string cantFindMsBuildMessage =
                        $"Could not find msbuild.exe. Looked in the following locations:";

                    foreach (var item in AvailableLocations)
                    {
                        cantFindMsBuildMessage += $"\n{item}";
                    }

                    printError(cantFindMsBuildMessage);
                }
                return succeeded;
            }
            return false;
        }

        private async Task<bool> StartMsBuildWithParameters(Action<string> printOutput, Action<string> printError, string startOutput, string endOutput, string arguments, string msbuildLocation)
        {
            Process process = CreateProcess("\"" + msbuildLocation + "\"", arguments);

            printOutput(startOutput);
            // This is noisy and technical. Reducing output window verbosity
            //printOutput(process.StartInfo.FileName + " " + process.StartInfo.Arguments);

            StringBuilder outputStringBuilder = new StringBuilder();
            StringBuilder errorStringBuilder = new StringBuilder();
            var errorString = await Task.Run(() => RunProcess(outputStringBuilder, errorStringBuilder, msbuildLocation, process));

            if (outputStringBuilder.Length > 0)
            {
                printOutput(outputStringBuilder.ToString());
            }

            if (errorStringBuilder.Length > 0)
            {
                printError(errorStringBuilder.ToString());
            }

            bool succeeded = false;

            if (!string.IsNullOrEmpty(errorString))
            {
                printError(errorString);
            }
            else
            {
                succeeded = true;
                printOutput($"{endOutput} at {DateTime.Now.ToLongTimeString()}");
            }

            return succeeded;
        }

        // no longer needed for .NET Core/Standard
        //private string GetMissingFrameworkMessage()
        //{
        //    string whyCantRun = null;

        //    // check if .NET is installed:
        //    // This is where the .NET 4.5.2 SDK 
        //    // installs the files, which seems to 
        //    // be required for running the build tool.
        //    const string dotNet452Directory = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5.2\";

        //    var directoryExists = System.IO.Directory.Exists(dotNet452Directory);
        //    if(directoryExists == false)
        //    {
        //        var sdkLocation = "https://www.microsoft.com/en-us/download/details.aspx?id=42637";
        //        whyCantRun = $"Your computer is missing the .NET framework version 4.5.2. Glue is expecting " +
        //            " it it in the following location:\n{dotNet452Directory}\n\n" +
        //            $"This can be downloaded here: {sdkLocation}";
        //    }

        //    return whyCantRun;
        //}

        private static string RunProcess(StringBuilder printOutput, StringBuilder printError, string processPath, Process process)
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

                        printOutput.AppendLine(line);
                    }
                }
            }

            if (process.ExitCode != 0)
            {
                if (hasUserTerminatedProcess)
                {
                    errorString = "The process\n\n" + processPath + "\n\nhas been terminated";
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
                        printOutput.AppendLine(str);
                    }
                }

                while ((str = process.StandardError.ReadLine()) != null)
                {
                    if (printError != null)
                    {
                        printError.AppendLine(str);
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
