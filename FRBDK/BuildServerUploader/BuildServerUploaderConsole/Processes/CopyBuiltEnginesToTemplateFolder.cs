using BuildServerUploaderConsole.Data;
using System.Collections.Generic;

namespace BuildServerUploaderConsole.Processes
{


    class CopyBuiltEnginesToTemplateFolder : ProcessStep
    {

        private static readonly List<CopyInformation> mCopyInformation = new List<CopyInformation>();

        static void Add(string file, string destination)
        {
            mCopyInformation.Add(CopyInformation.CreateTemplateCopy(file, destination));
        }

        static void AddFrbdk(string file, string destination)
        {
            mCopyInformation.Add(CopyInformation.CreateFrbdkTemplateCopy(file, destination));
        }

        static void AddDirectory(string fileName, string destination)
        {
            mCopyInformation.AddRange(CopyInformation.CopyDirectory(fileName, destination));
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
                        targetDirectory = engine.TemplateFolder + @"Libraries\" + engine.RelativeToLibrariesDebugFolder;

                        foreach(var file in engine.DebugFiles)
                        {
                            Add(file, targetDirectory);
                        }

                        targetDirectory = engine.TemplateFolder + @"Libraries\" + engine.RelativeToLibrariesReleaseFolder;

                        foreach (var file in engine.ReleaseFiles)
                        {
                            Add(file, targetDirectory);
                        }
                    }

                    // June 2 2020
                    // Glue plugin template
                    // is broken anyway, no one
                    // uses it. Now that we're on NET 
                    // Core, the paths are different too
                    // so let's just get rid of it.
                    //string sourcePrefix = @"Glue\Glue\Bin\Debug\";
                    //string destination = @"GluePluginTemplate\GluePluginTemplate\Libraries\XnaPc";
                    //// Glue Plugin Template
                    //AddFrbdk(sourcePrefix + "FlatRedBall.dll", destination);
                    //AddFrbdk(sourcePrefix + "EditorObjectsXna.dll", destination);
                    //AddFrbdk(sourcePrefix + "Glue.exe", destination);
                    //AddFrbdk(sourcePrefix + "FlatRedBall.PropertyGrid.dll", destination);
                    //AddFrbdk(sourcePrefix + "GlueSaveClasses.dll", destination);
                    //AddFrbdk(sourcePrefix + "FlatRedBall.Plugin.dll", destination);

                    
                    //AddDirectory(sourcePrefix, @"GluePluginTemplate\TestWithGlue\Glue");

                    
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
