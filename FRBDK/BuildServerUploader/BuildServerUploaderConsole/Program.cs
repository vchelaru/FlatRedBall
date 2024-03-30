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
        public const string ZipAndUploadTemplates = "zipanduploadtemplates";
        public const string ChangeVersion = "changeversion";
        public const string CopyDllsToTemplates = "copytotemplates";
        public const string ZipAndUploadFrbdk = "zipanduploadfrbdk";
        public const string ChangeEngineVersion = "changeengineversion";
        public const string ChangeFrbdkVersion = "changefrbdkversion";
    }


    public class Program
    {
        private static string _defaultDirectory;
        private static readonly List<ProcessStep> ProcessSteps = new List<ProcessStep>();
        private static readonly IResults Results = new TraceResults();

        static void Main(string[] args)
        {
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
                    case CommandLineCommands.CopyDllsToTemplates:
                        CreateCopyToTemplatesSteps();
                        break;
                    case CommandLineCommands.Upload:
                        CreateUploadProcessSteps();
                        break;
                    case CommandLineCommands.ZipAndUploadTemplates:
                        CreateZipAndUploadTemplates(args);
                        break;
                    case CommandLineCommands.ZipAndUploadFrbdk:
                        CreateZipAndUploadFrbdk(args);
                        break;
                    case CommandLineCommands.ChangeEngineVersion:
                        CreateChangeEngineVersion();
                        break;
                    case CommandLineCommands.ChangeFrbdkVersion:
                        CreateChangeFrbdkVersion();
                        break;
                    case "":
                        break;
                    default:
                        CreateUploadProcessSteps();
                        break;
                }


            }
            else // I think this is used for debugging only
            {
                //CreateUploadProcessSteps("DailyBuild");
                //ProcessSteps.Add(new ZipTemplates(Results));
                //ProcessSteps.Add(new CreateChangeVersionProcessSteps());
                //CreateChangeVersionProcessSteps();

                //CreateUploadProcessSteps(null);
                ProcessSteps.Add(new PublishGlue(Results));

                //CreateCopyToInstallerSteps(true);
            }

            ExecuteSteps();

        }

        private static void CreateChangeEngineVersion()
        {
            ProcessSteps.Add(new UpdateAssemblyVersions(Results, UpdateType.Engine));
        }

        private static void CreateChangeFrbdkVersion()
        {
            ProcessSteps.Add(new UpdateAssemblyVersions(Results, UpdateType.FRBDK));
        }

        private static void CreateZipAndUploadTemplates(string[] args)
        {
            ProcessSteps.Add(new CopyBuiltEnginesToReleaseFolder(Results));

            ProcessSteps.Add(new ZipTemplates(Results));

            if (args.Length < 3)
            {
                throw new Exception("Expected 3 arguments: {operation} {username} {password}, but only got " + args.Length + "arguments");
            }

            ProcessSteps.Add(new UploadFilesToFrbServer(Results, UploadType.EngineAndTemplatesOnly, args[1], args[2]));
        }

        private static void CreateZipAndUploadFrbdk(string[] args)
        {
            ProcessSteps.Add(new CopyFrbdkAndPluginsToReleaseFolder(Results));
            ProcessSteps.Add(new ZipFrbdk(Results));

            //??
            //ProcessSteps.Add(new ZipGum(Results));

            if (args.Length < 3)
            {
                throw new Exception("Expected 3 arguments: {operation} {username} {password}, but only got " + args.Length + "arguments");
            }

            ProcessSteps.Add(new UploadFilesToFrbServer(Results, UploadType.FrbdkOnly, args[1], args[2]));
        }

        private static void CreateCopyToTemplatesSteps()
        {
            ProcessSteps.Add(new CopyBuiltEnginesToTemplateFolder(Results));
        }

        private static void CreateChangeVersionProcessSteps()
        {
            ProcessSteps.Add(new UpdateAssemblyVersions(Results, UpdateType.Engine));
            ProcessSteps.Add(new UpdateAssemblyVersions(Results, UpdateType.FRBDK));
        }

        private static void CreateUploadProcessSteps()
        {
            // I don't think we need publish....
            // Users still need VS 2022 for msbuild
            // and that installs .net 6
            //ProcessSteps.Add(new PublishGlue(Results));
            // Maybe this should be after?
            ProcessSteps.Add(new BuildGlue(Results));
            ProcessSteps.Add(new CopyFrbdkAndPluginsToReleaseFolder(Results));
            ProcessSteps.Add(new AddRunFlatRedBallBatch(Results));


            ProcessSteps.Add(new CopyBuiltEnginesToReleaseFolder(Results));
            ProcessSteps.Add(new ZipFrbdk(Results));
            ProcessSteps.Add(new ZipGum(Results));
            // No need to zip the engine - we upload each individually.
            //ProcessSteps.Add(new ZipEngine(Results));
            ProcessSteps.Add(new ZipTemplates(Results));

            ProcessSteps.Add(new UploadFilesToFrbServer(Results, UploadType.Entire, null, null));
        }

        private static void ExecuteSteps()
        {
            for (int i = 0; i < ProcessSteps.Count; i++)
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
