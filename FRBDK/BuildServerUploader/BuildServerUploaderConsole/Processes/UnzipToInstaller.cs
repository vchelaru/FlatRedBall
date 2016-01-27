using System;
using Ionic.Zip;
using System.IO;
using FlatRedBall.IO;

namespace BuildServerUploaderConsole.Processes
{
    public class UnzipToInstaller : ProcessStep
    {
        public UnzipToInstaller(IResults results)
            : base(
                "Unzip files to installer", results)
        { }

        public override void ExecuteStep()
        {
            if (!Directory.Exists(FileManager.RelativeDirectory + "FRBDK"))
            {
                Directory.CreateDirectory(FileManager.RelativeDirectory + "FRBDK");
            }

            if (!File.Exists(ZipFrbdk.DestinationFile))
            {
                throw new Exception("Could not find " + ZipFrbdk.DestinationFile + " when trying to copy it for unzipping for installer");
            }

            File.Copy(ZipFrbdk.DestinationFile, FileManager.RelativeDirectory + "FRBDK.zip", true);

            if (File.Exists(FileManager.RelativeDirectory + "FRBDK.zip"))
            {
                Results.WriteMessage("Successfully copied FRBDK to the BuildServerUploader folder");
            }
            else
            {
                throw new Exception("Could not find FRBDK for unzipping at " + FileManager.RelativeDirectory + "FRBDK.zip");
            }


            using (ZipFile zip = ZipFile.Read(FileManager.RelativeDirectory + "FRBDK.zip"))
            {
                foreach (ZipEntry e in zip)
                {
                    e.Extract(FileManager.RelativeDirectory + "FRBDK", true);
                }
            }
        }
    }
}
