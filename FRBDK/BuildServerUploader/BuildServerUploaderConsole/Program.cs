using System;
using System.Collections.Generic;
using System.IO;
using BuildServerUploaderConsole.Processes;
using FlatRedBall.IO;

namespace BuildServerUploaderConsole
{
    public static class CommandLineCommands
    {
        public const string Upload = "upload";
        public const string ChangeVersion = "changeversion";
        public const string CopyToFrbdkInstallerTool = "copytoinstaller";
        public const string CopyDllsToTemplates = "copytotemplates";
    }


    public class Program
    {
        private static string _defaultDirectory;
        private static readonly List<ProcessStep> ProcessSteps = new List<ProcessStep>();
        private static readonly IResults Results = new TraceResults();

        static void Main(string[] args)
        {

            // debug:
            UploadFilesToFrbServer.BuildBackupFile();

            // We used to send emails on an error
            // but now the build server does this automatically
            // so we don't have to.
            FileManager.PreserveCase = true;

            _defaultDirectory = Directory.GetCurrentDirectory();

            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case CommandLineCommands.Upload:
                        CreateUploadProcessSteps(args[1]);
                        break;
                    case CommandLineCommands.ChangeVersion:
                        CreateChangeVersionProcessSteps();
                        break;
                    case CommandLineCommands.CopyToFrbdkInstallerTool:
                        CreateCopyToInstallerSteps(false);
                        break;
                    case CommandLineCommands.CopyDllsToTemplates:
                        CreateCopyToTemplatesSteps();
                        break;
                    case "":
                        break;
                    default:
                        CreateUploadProcessSteps("DailyBuild");
                        break;
                }

                
            }
            else
            {
                CreateCopyToInstallerSteps(true);
            }

            ExecuteSteps();
            
        }

        private static void CreateCopyToTemplatesSteps()
        {
            ProcessSteps.Add(new CopyBuiltEnginesToTemplateFolder(Results));
        }

        private static void CreateChangeVersionProcessSteps()
        {
            ProcessSteps.Add(new UpdateAssemblyVersions(Results));
        }

        private static void CreateUploadProcessSteps(string buildType)
        {
            ProcessSteps.Add(new CopyFrbdkToReleaseFolder(Results));
            ProcessSteps.Add(new CopyBuiltEnginesToReleaseFolder(Results));
            ProcessSteps.Add(new ZipFrbdk(Results));
            // No need to zip the engine - we upload each individually.
            //ProcessSteps.Add(new ZipEngine(Results));
            ProcessSteps.Add(new ZipTemplates(Results));

            UploadType type;
            switch (buildType)
            {
                case "Monthly":
                    type = UploadType.Monthly;
                    break;
                case "Weekly":
                    type = UploadType.Weekly;
                    break;
                default:
                    type = UploadType.DailyBuild;
                    break;
            }

            ProcessSteps.Add(new UploadFilesToFrbServer(Results, type));
        }

        private static void CreateCopyToInstallerSteps(bool debug)
        {
            if (!debug)
            {
                ProcessSteps.Add(new CopyFrbdkToReleaseFolder(Results));
                ProcessSteps.Add(new CopyBuiltEnginesToReleaseFolder(Results));
                ProcessSteps.Add(new ZipFrbdk(Results));
            }


            ProcessSteps.Add(new UnzipToInstaller(Results));
        }

        private static void ExecuteSteps()
        {
            foreach (ProcessStep processStep in ProcessSteps)
            {
                processStep.ExecuteStep();
            }

        }

        public static string DefaultDirectory
        {
            get { return _defaultDirectory; }
        }

        
    }
}
