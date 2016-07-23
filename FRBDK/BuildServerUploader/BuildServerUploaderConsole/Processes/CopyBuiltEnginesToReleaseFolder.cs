using BuildServerUploaderConsole.Data;
using System.Collections.Generic;

namespace BuildServerUploaderConsole.Processes
{


    class CopyBuiltEnginesToReleaseFolder : ProcessStep
    {





        private static readonly List<CopyInformation> _copyInformation = new List<CopyInformation>();

        static void Add(string file, string category)
        {
            _copyInformation.Add(CopyInformation.CreateEngineCopy(file, category));
        }

        public static List<CopyInformation> CopyInformationList
        {
            get
            {
                if (_copyInformation.Count == 0)
                {
                    foreach(var engine in AllData.Engines)
                    {
                        foreach(var file in engine.DebugFiles)
                        {
                            Add(file, engine.RelativeToLibrariesDebugFolder);
                        }
                        foreach (var file in engine.ReleaseFiles)
                        {
                            Add(file, engine.RelativeToLibrariesReleaseFolder);
                        }
                    }
                    
                }
                return _copyInformation;
            }
        }



        public CopyBuiltEnginesToReleaseFolder(IResults results)
            : base(
            @"Copy all built engine files (.dll) to release folder", results)
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
