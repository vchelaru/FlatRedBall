using BuildServerUploaderConsole.Data;
using FlatRedBall.IO;
using System.Collections.Generic;

namespace BuildServerUploaderConsole.Processes
{
    public class ZipTemplates : ProcessStep
    {
        struct ZipProcess
        {
            public string ZipDirectory;
            public string ZipFileName;

            public override string ToString()
            {
                return $"{ZipDirectory}{ZipFileName}";
            }
        }

        static string mDestinationDirectory = DirectoryHelper.ReleaseDirectory;

        public ZipTemplates(IResults results)
            : base(
                @"Zip Templates", results)
        { }

        public override void ExecuteStep()
        {
            string ZipDirectory = DirectoryHelper.ReleaseDirectory + @"ZippedTemplates/";

            if (!System.IO.Directory.Exists(ZipDirectory))
            {
                System.IO.Directory.CreateDirectory(ZipDirectory);
            }

            var zips = new List<ZipProcess>();

            foreach(var engine in AllData.Engines)
            {
                var zipProcess = new ZipProcess
                {
                    ZipDirectory = engine.TemplateName,
                    ZipFileName = engine.TemplateName
                };

                zips.Add(zipProcess);
            }


            foreach (var zipProcess in zips)
            {
                RemoveBinRecursiveFrom(DirectoryHelper.TemplateDirectory + zipProcess.ZipDirectory);


                ZipHelper.CreateZip(Results, ZipDirectory, DirectoryHelper.TemplateDirectory + zipProcess.ZipDirectory, zipProcess.ZipFileName);
            }
        }

        private void RemoveBinRecursiveFrom(string directories)
        {
            var subdirectoriesAsArray = System.IO.Directory.GetDirectories(directories);

            List<string> directoriesToProcess = new List<string>();

            bool wasAnythingDeleted = false;

            foreach (var directory in subdirectoriesAsArray)
            {
                string strippedSubDirectory = FileManager.RemovePath(directory);

                if (strippedSubDirectory.ToLowerInvariant() == "bin" ||
                    strippedSubDirectory.ToLowerInvariant() == "obj")
                {
                    // Use FileManager to delete the directory - It will do it recursively:
                    //System.IO.Directory.Delete(directory);
                    FileManager.DeleteDirectory(directory);
                    wasAnythingDeleted = true;

                }
                else
                {
                    directoriesToProcess.Add(directory);
                }
            }

            // let's go deeper if we haven't found bin/obj yet
            if (!wasAnythingDeleted)
            {
                foreach (var directory in directoriesToProcess)
                {
                    RemoveBinRecursiveFrom(directory);
                }
            }
        }
    }
}
