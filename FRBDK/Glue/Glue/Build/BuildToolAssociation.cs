using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.IO;
using System.Diagnostics;
using EditorObjects.Parsing;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace EditorObjects.SaveClasses
{
    [TypeConverter(typeof(PropertySorter))]
    public class BuildToolAssociation
    {
        #region Properties

        [Category("Primary Arguments"), PropertyOrder(100)]
        [EditorAttribute(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string BuildTool
        {
            get;
            set;
        }

        public string BuildToolProcessed
        {
            get
            {
                var toReturn = BuildTool;
                if(toReturn != null)
                {
                    var folder = FileManager.GetDirectory(System.Reflection.Assembly.GetEntryAssembly().Location);
                    toReturn = toReturn.Replace("%Glue%", folder);
                }

                return toReturn;
            }
        }

        [Category("Primary Arguments"), PropertyOrder(110)]
        public bool IsBuildToolAbsolute
        {
            get;
            set;
        }

        [Category("Primary Arguments"), PropertyOrder(200)]
        public string SourceFileType
        {
            get;
            set;
        }

        [Category("Primary Arguments"), PropertyOrder(300)]
        public string DestinationFileType
        {
            get;
            set;
        }

        [Category("Primary Arguments"), PropertyOrder(350)]
        public bool IncludeDestination
        {
            get;
            set;
        }

        [Category("Secondary Arguments"), PropertyOrder(400)]
        public string SourceFileArgumentPrefix
        {
            get;
            set;
        }

        [Category("Secondary Arguments"), PropertyOrder(500)]
        public string DestinationFileArgumentPrefix
        {
            get;
            set;
        }



        [Category("Secondary Arguments"), PropertyOrder(600)]
        public string ExternalArguments
        {
            get;
            set;
        }

        public string ExampleCommandLine
        {
            get
            {
                var executable = GetExecutable();
                var arguments = GetArguments($"c:\\Input.{SourceFileType}", $"c:\\Output.{DestinationFileType}", null);
                return $"{GetExecutable()} {arguments}";
            }
        }

        #endregion

        public BuildToolAssociation()
        {
            IncludeDestination = true;
        }

        public BuildToolAssociation Clone()
        {
            return (BuildToolAssociation) this.MemberwiseClone();
        }


        public string PerformBuildOn(string absoluteSourceFile, string absoluteDestinationFile, string additionalArguments, 
            Action<string> printOutput, Action<string> printError, bool runAsync = false)
        {
            if (FileManager.IsRelative(absoluteSourceFile))
            {
                throw new ArgumentException("absoluteSourceFile needs to be an absolute path");
            }
            if (FileManager.IsRelative(absoluteDestinationFile))
            {
                throw new ArgumentException("absoluteDestinationFile needs to be an absolute path");
            }



            string errorString = "";

            string executable = GetExecutable();
            string arguments = GetArguments(absoluteSourceFile, absoluteDestinationFile, additionalArguments);
            if (!File.Exists(executable))
            {
                var removed = FileManager.RemoveDotDotSlash(executable);

                if(removed != executable)
                {
                    errorString = $"Error trying to build\n{absoluteSourceFile}\nCould not find the tool:\n{removed}\noriginal:\n{executable}";
                }
                else
                {
                    errorString = "Could not find the tool " + executable;
                }
            }
            else
            {
                Process process = CreateProcess("\"" + executable + "\"", arguments, FileManager.GetDirectory(absoluteSourceFile));

                if (printOutput != null)
                {
                    printOutput(process.StartInfo.FileName + " " + process.StartInfo.Arguments);
                }

                absoluteSourceFile = FileManager.Standardize(absoluteSourceFile).Replace("/", "\\");
                absoluteDestinationFile = FileManager.Standardize(absoluteDestinationFile).Replace("/", "\\");


                if (runAsync)
                {
                    new Task(
                        () => RunProcess(absoluteSourceFile, absoluteDestinationFile, printOutput, printError, errorString, executable, process)).Start();
                }
                else
                {
                    errorString = RunProcess(absoluteSourceFile, absoluteDestinationFile, printOutput, printError, errorString, executable, process);
                }
            }


            return errorString;
        }

        private string GetArguments(string absoluteSourceFile, string absoluteDestinationFile, string additionalArguments)
        {
            absoluteSourceFile = FileManager.Standardize(absoluteSourceFile).Replace("/", "\\");
            absoluteDestinationFile = FileManager.Standardize(absoluteDestinationFile).Replace("/", "\\");

            string destinationDirectory = FileManager.GetDirectory(absoluteDestinationFile);

            if (!System.IO.Directory.Exists(destinationDirectory))
            {
                System.IO.Directory.CreateDirectory(destinationDirectory);
            }


            string arguments = GetArgumentsForProcess(absoluteSourceFile, absoluteDestinationFile, additionalArguments);
            return arguments;
        }

        private string GetExecutable()
        {
            string executable = BuildToolProcessed;

            if (!string.IsNullOrEmpty(executable) && FileManager.IsRelative(executable))
            {
                executable = FileManager.RelativeDirectory + executable;
            }

            return executable;
        }

        private static Process CreateProcess(string executable, string arguments, string workingDirectory)
        {
            Process process = new Process();

            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = executable;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            process.StartInfo.WorkingDirectory = workingDirectory;

            return process;
        }

        private string GetArgumentsForProcess(string sourceFile, string destinationFile, string additionalArguments)
        {
            string arguments = null;
            if (!string.IsNullOrEmpty(SourceFileArgumentPrefix))
            {
                arguments = SourceFileArgumentPrefix + " \"" + sourceFile + "\"";
            }
            else
            {
                arguments = "\"" + sourceFile + "\"";
            }

            if (IncludeDestination)
            {
                if (!string.IsNullOrEmpty(DestinationFileArgumentPrefix))
                {
                    arguments += DestinationFileArgumentPrefix + " \"" + destinationFile + "\"";
                }
                else
                {
                    arguments += " \"" + destinationFile + "\"";
                }
            }

            if (!string.IsNullOrEmpty(ExternalArguments))
            {
                arguments += " " + ExternalArguments;
            }

            if (!string.IsNullOrEmpty(additionalArguments))
            {
                arguments += " " + additionalArguments;
            }
            return arguments;
        }

        private static string RunProcess(string sourceFile, string destinationFile, Action<string> printOutput, Action<string> printError, string errorString, string executable, Process process)
        {
            process.Start();
            // Unfortunately we need to wait for the build to complete so that we can copy any referenced files that we need
            int numberOfWaitTimes = 0;

            bool doesUserWantToWait = false;
            const int numberOfTimesToWait = 150;
            bool hasUserTerminatedProcess = false;

            StringBuilder outputWhileRunning = new StringBuilder();

            while (!process.HasExited)
            {
                System.Threading.Thread.Sleep(150);
                numberOfWaitTimes++;
                // Not sure why
                // but the TMX to 
                // SCNX tool used to
                // freeze.  I found that
                // if I simply check if the 
                // StandardOutput is not at EndOfStream,
                // then nothing freezes.  I figured this out
                // by attaching to a frozen TmxToScnx.exe process
                // in visual studio and found that it was stuck on
                // a command line output.  I thought I'd have to read
                // from the stream, but just seeing if it's at the end
                // fixes it.
                if (!process.StandardOutput.EndOfStream)
                {   }

                if (numberOfWaitTimes > numberOfTimesToWait && doesUserWantToWait == false)
                {
                    DialogResult dialogResult = MessageBox.Show("The tool\n\n" + executable + "\n\nis taking a long time to build the file\n\n" +
                        sourceFile + "\n\nWould you like to end this build process?", "End process?", MessageBoxButtons.YesNo);

                    if (dialogResult == DialogResult.Yes)
                    {

                        if (!process.HasExited)
                        {
                            // Just in case it happens to exit on its own:
                            try
                            {
                                if (printError != null)
                                {
                                    printError("User cancelled build.  Command line:\n\t" + 
                                                            process.StartInfo.FileName + " " + process.StartInfo.Arguments);
                                }
                                process.Kill();
                            }
                            catch
                            {
                                // do nothing, it happened to exit between the if check and the Kill call
                            }
                        }
                        hasUserTerminatedProcess = true;
                    }
                    else
                    {
                        doesUserWantToWait = true;
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


            if (!File.Exists(destinationFile))
            {
                errorString += "The process\n\n" + executable + "\n\nfailed to build the file\n\n" + destinationFile;

            }
            return errorString;
        }


        public override string ToString()
        {
            string buildTool = "<NO TOOL>";
            string sourceFileType = "<NO SOURCE>";
            string destinationFileType = "<NO DESTINATION>";

            if (!string.IsNullOrEmpty(SourceFileType))
            {
                sourceFileType = "*." + SourceFileType;
            }

            if (!string.IsNullOrEmpty(DestinationFileType))
            {
                destinationFileType = "*." + DestinationFileType;
            }

            if (!string.IsNullOrEmpty(BuildToolProcessed))
            {
                buildTool = FileManager.RemovePath(BuildToolProcessed);
            }

            return sourceFileType + " -> " + buildTool + " -> " +
                destinationFileType;
        }
    }
}
