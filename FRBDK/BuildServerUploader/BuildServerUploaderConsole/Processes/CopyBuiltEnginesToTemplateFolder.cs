using BuildServerUploaderConsole.Data;
using System.Collections.Generic;

namespace BuildServerUploaderConsole.Processes
{


    class CopyBuiltEnginesToTemplateFolder : ProcessStep
    {

        private static readonly List<CopyInformation> mCopyInformation = new List<CopyInformation>();

        static void Add(string file, string destination, string engineName)
        {
            var copyInformation = CopyInformation.CreateTemplateCopy(file, destination);
            copyInformation.DebugInformation = $"for engine {engineName}";
            mCopyInformation.Add(copyInformation);
        }

        public static List<CopyInformation> CopyInformationList
        {
            get
            {
                if (mCopyInformation.Count == 0)
                {
                    string targetDirectory;
                    foreach(var engine in AllData.Engines)
                    {
                        targetDirectory = engine.TemplateCsProjFolder + @"Libraries\" + engine.RelativeToLibrariesDebugFolder;

                        foreach(var file in engine.DebugFiles)
                        {
                            Add(file, targetDirectory, engine.Name);
                        }

                        targetDirectory = engine.TemplateCsProjFolder + @"Libraries\" + engine.RelativeToLibrariesReleaseFolder;

                        foreach (var file in engine.ReleaseFiles)
                        {
                            Add(file, targetDirectory, engine.Name);
                        }
                    }
                }
                return mCopyInformation;
            }
        }



        public CopyBuiltEnginesToTemplateFolder(IResults results)
            : base(
            @"Copy all built engine files (.dll) to templates", results)
        {

        }

        public override void ExecuteStep()
        {
            List<CopyInformation> copyInformationList =
                CopyInformationList;
 

            foreach (CopyInformation ci in copyInformationList)
            {
                ci.PerformCopy(Results, "Copied to " + ci.DestinationFile);

            }
        }
    }
}
