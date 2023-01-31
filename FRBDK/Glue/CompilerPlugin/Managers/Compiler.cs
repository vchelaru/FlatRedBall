using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using CompilerPlugin.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CompilerPlugin.ViewModels;
using CompilerLibrary.ViewModels;

namespace CompilerPlugin.Managers
{
    class Compiler
    {
        List<FilePath> AvailableLocations = new List<FilePath>
        {
            // Update - MSBuild for version 15.0 is not supported anymore because we need to do nuget restore.
            // Going to include MSBuild from VS 2019
            //$@"{FileManager.GetDirectory(Assembly.GetEntryAssembly().Location)}Tools\MSBuild\15.0\MSBuild.exe",
            // Update January 21 2022
            // Using this version of MSBuild is an attempt to get around needing Visual Studio (again)
            // but it won't work because of nuget restore:
            // error MSB4036: The "GetReferenceNearestTargetFrameworkTask" task was not found. 
            // Not sure what to add here, as the answers on stackoverflow suggest running the VS installer
            // So....we'll comment this out for now:
            // https://stackoverflow.com/questions/47797510/the-getreferencenearesttargetframeworktask-task-was-not-found
            //$@"{FileManager.GetDirectory(Assembly.GetEntryAssembly().Location)}Tools\MSBuild\2019\MSBuild.exe",
            // I've excluded the MSBuild 2019 folder to make Glue smaller, but to bring it back in, the following should be added to the csproj:
            /*
    <ItemGroup>
		<Page Remove="Tools\MSBuild\2019\de-DE\*.xaml" />
		<Page Remove="Tools\MSBuild\2019\en-US\*.xaml" />
		<Page Remove="Tools\MSBuild\2019\es-ES\*.xaml" />
		<Page Remove="Tools\MSBuild\2019\fr-FR\*.xaml" />
		<Page Remove="Tools\MSBuild\2019\it-IT\*.xaml" />
		<Page Remove="Tools\MSBuild\2019\ja-JP\*.xaml" />
		<Page Remove="Tools\MSBuild\2019\ko-KR\*.xaml" />
		<Page Remove="Tools\MSBuild\2019\ru-RU\*.xaml" />
		<Page Remove="Tools\MSBuild\2019\zh-CN\*.xaml" />
		<Page Remove="Tools\MSBuild\2019\zh-TW\*.xaml" />
      <None Update="Tools\MSBuild**">
		  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
	  </None>
	  <None Update="Tools\MSBuild\2019\**">
		  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
	  </None>
	  <None Update="Tools\MSBuild\2019\de-DE\**">
		  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
	  </None>
	</ItemGroup>



            // This compiler list is a priority-ordered list of locations to check for MSBuild.
            // The items at the top are the highest priority, while further down are lower priority.
            // Vic has attempted to run Glue from VS 2022 and use the 2022 MSBuild versions, but for some
            // reason this causes weirdness in the built game. He believes there may be multiple issues happening:
            // 1. MSBuild using 2022 will fail if run from Glue built using VS 2019
            // 2. Games built with MSBuild 2022 behave slightly differently than games built with 2019. If using Glue 
            //    built in 2022 and MSBuild 2022, the game will not sit inside the winforms project after clicks. Not sure
            //    why but the game window behaves differently.
            // These may be 2 separate issues, not sure, but Vic is tackling each one at a time. The first issue is documented
            // here:
            // https://stackoverflow.com/questions/70795993/why-does-msbuild-fail-when-run-from-app-built-in-visual-studio-2019?noredirect=1#comment125157308_70795993
            // As of January 21 it has not received any useful answers. Vic is hoping that Matt in FRB can help, or that he can put
            // a large bounty on the question to get attention.
            // In the meantime, the solution is - use Visual Studio 2019, and put 2022 MSBuild.exe at low priority (low on the list)
             * 
             */
            // Update January 2022
            // It turns out that any MSBuild will work fine (at least based on initial tests) so long as the
            // application is run from .exe. Therefore, users will not encounter any problems. However, when debugging,
            // the version of MSBuild being called must match the version of Visual Studio which launched Glue. The setup
            // of preferring MSBuild 2022 should solve most problems. If a user doesn't have 2022 installed, then they will
            // probably be using VS 2019, and that will use 2019 msbuild. If a user does have 2022 installed, they will probably
            // be opening Glue with that.
            @"C:\Program Files\Microsoft Visual Studio\2022\Community\Msbuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Msbuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files\Microsoft Visual Studio\2022\Professional\Msbuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\amd64\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe",
            // See above on why this is low on the list
            @"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe",

        };

        public BuildSettingsUser BuildSettingsUser { get; set; }

        FilePath msBuildLocation;
        private CompilerViewModel _compilerViewModel;

        public Compiler(CompilerViewModel compilerViewModel)
        {
            _compilerViewModel = compilerViewModel;
        }

        FilePath MsBuildLocation
        {
            get
            {
                if(msBuildLocation == null)
                {
                    FilePath filePath = BuildSettingsUser.CustomMsBuildLocation;
                    if(filePath?.Exists() == true)
                    {
                        msBuildLocation = filePath;
                    }
                    else
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
                }

                return msBuildLocation;
            }
        }

        Process currentMsBuildProcess = null;
        bool didUserKillProcess = false;

        public void CancelBuild()
        {
            didUserKillProcess = true;
            if(currentMsBuildProcess != null)
            {
                currentMsBuildProcess.Kill();
            }
        }

        internal async Task<bool> Compile(Action<string> printOutput, Action<string> printError,
            string configuration = "Debug", bool printMsBuildCommand = false)
        {
            try
            {
                while (_compilerViewModel.IsCompiling)
                {
                    await Task.Delay(100);
                }

                didUserKillProcess = false;
                _compilerViewModel.IsCompiling = true;

                // At one point I was trying to resolve the VS 22 vs 19 issue and I ran MSBuild through a batch
                // file. It didn't matter, still broken :(
                //var batFile = @"C:\Users\vchel\Documents\GitHub\FlatRedBall\FRBDK\Glue\Glue\BuildMyGame.bat";
                //var exists = System.IO.File.Exists(batFile);
                //var process = System.Diagnostics.Process.Start(batFile);

                //return true;

                var shouldCompile = true;

                //var message = GetMissingFrameworkMessage();
                //if(!string.IsNullOrEmpty(message))
                //{
                //    printError("Cannot build due to missing .NET SDK:\n" + message);
                //    shouldCompile = false;
                //}

                //do we actually want to do this ?
                var projectFileName = GlueState.Self.CurrentMainProject?.FullFileName;
                if (shouldCompile && projectFileName != null)
                {

                    var succeeded = false;

                    string msBuildPath;
                    string additionalArgumentPrefix = "";

                    var is6OrGreater = GlueState.Self.CurrentMainProject.DotNetVersionNumber >= 6;
                    if (MsBuildLocation != null && !is6OrGreater)
                    {
                        msBuildPath = MsBuildLocation.FullPath;
                    }
                    else
                    {
                        // try dotnet msbuild
                        msBuildPath = "dotnet";

                        additionalArgumentPrefix = "msbuild ";
                    }

                    // To load a .NET 5+ project, we have to call
                    // Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults(); in MainGlueWindow which adjusts
                    // MSBUILD_EXE_PATH environment variable.
                    // Unfortuantely, changing this variable also affects Visual Studio so that it can't open
                    // projects. To make VS open projects correctly, undo this variable assignment.
                    var environmentBefore = Environment.GetEnvironmentVariable("MSBUILD_EXE_PATH");
                    try
                    {

                        Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", null);

                        #region Restore Nuget

                        {
                            string startOutput = "Nuget Restore started at " + DateTime.Now.ToLongTimeString();
                            string endOutput = "Nuget Restore succeeded";

                            // For info on parameters:
                            // https://msdn.microsoft.com/en-us/library/ms164311.aspx?f=255&MSPPError=-2147217396
                            // \m uses multiple cores
                            string arguments =
                                additionalArgumentPrefix +
                                $"\"{projectFileName}\" -t:restore " +
                                "/nologo " +
                                "/verbosity:minimal";

                            if (printMsBuildCommand)
                            {
                                printOutput?.Invoke($"\"{msBuildPath}\" {arguments}");
                            }

                            succeeded = await StartMsBuildWithParameters(printOutput, printError, startOutput, endOutput, arguments, msBuildPath);
                        }

                        #endregion

                        #region Build

                        if (succeeded && !didUserKillProcess)
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

                            if (printMsBuildCommand)
                            {
                                printOutput?.Invoke($"\"{msBuildPath}\" {arguments}");
                            }

                            succeeded = await StartMsBuildWithParameters(printOutput, printError, startOutput, endOutput, arguments, msBuildPath);
                        }

                        #endregion

                    }
                    finally
                    {
                        Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", environmentBefore);
                    }
                    if (!succeeded)
                    {
                        if(didUserKillProcess)
                        {
                            printOutput("Build cancelled by user");
                        }
                        else
                        {
                            var fileExists = System.IO.File.Exists(msBuildPath);
                            if (!fileExists && msBuildPath != "dotnet")
                            {
                                string cantFindMsBuildMessage =
                                    $"Could not find msbuild.exe. Looked in the following locations:";

                                foreach (var item in AvailableLocations)
                                {
                                    cantFindMsBuildMessage += $"\n{item}";
                                }

                                printError(cantFindMsBuildMessage);
                            }

                        }
                    }
                    return succeeded;
                }
                return false;
            }
            finally
            {
                _compilerViewModel.IsCompiling = false;
            }
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

        private string RunProcess(StringBuilder printOutput, StringBuilder printError, string processPath, Process process)
        {
            string errorString = "";
            const int timeToWait = 50;
            bool hasUserTerminatedProcess = false;

            StringBuilder outputWhileRunning = new StringBuilder();

            process.Start();
            currentMsBuildProcess= process;

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

            currentMsBuildProcess = process;


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
