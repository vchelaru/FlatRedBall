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
            // We used to send emails on an error
            // but now the build server does this automatically
            // so we don't have to.
            FileManager.PreserveCase = true;

            _defaultDirectory = Directory.GetCurrentDirectory();

            // This app is executed 3 times during a build. The three steps, 
            // in order, but not consecutively executed, are:
            // * Change Versions
            // * Copy DLLs to Templates
            // * Upload

            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case CommandLineCommands.ChangeVersion:
                        CreateChangeVersionProcessSteps();
                        break;
                    case CommandLineCommands.CopyToFrbdkInstallerTool:
                        CreateCopyToInstallerSteps(false);
                        break;
                    case CommandLineCommands.CopyDllsToTemplates:
                        CreateCopyToTemplatesSteps();
                        break;
                    case CommandLineCommands.Upload:
                        CreateUploadProcessSteps(args[1]);
                        break;
                    case "":
                        break;
                    default:
                        CreateUploadProcessSteps("DailyBuild");
                        break;
                }

                
            }
            else // I think this is used for debugging only
            {
                //CreateUploadProcessSteps("DailyBuild");
                //ProcessSteps.Add(new ZipTemplates(Results));
                //ProcessSteps.Add(new CreateChangeVersionProcessSteps());
                CreateChangeVersionProcessSteps();


                //CreateCopyToInstallerSteps(true);
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
            ProcessSteps.Add(new ZipGum(Results));
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
            for(int i = 0; i < ProcessSteps.Count; i++)
            {
                int step1Based = i + 1;
                Results.WriteMessage($"Processing {step1Based}/{ProcessSteps.Count} : {ProcessSteps[i].Message}");
                ProcessSteps[i].ExecuteStep();
            }
        }

        public static string DefaultDirectory
        {
            get { return _defaultDirectory; }
        }

        
    }
}
