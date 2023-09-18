using System;
using System.IO;
using FlatRedBall.IO;

namespace BuildServerUploaderConsole.Processes
{
    public class DirectoryHelper
    {
        public static void DeleteDirectory(string targetDir)
        {
            if (Directory.Exists(targetDir))
            {
                string[] files = Directory.GetFiles(targetDir);
                string[] dirs = Directory.GetDirectories(targetDir);

                foreach (string file in files)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }

                foreach (string dir in dirs)
                {
                    DeleteDirectory(dir);
                }

                int numberOfTries = 0;
                while (numberOfTries < 4)
                {
                    try
                    {
                        Directory.Delete(targetDir, true);
                        break;
                    }
                    catch
                    {
                        System.Threading.Thread.Sleep(30);
                        numberOfTries++;
                    }
                }

                if (numberOfTries == 4)
                {
                    throw new Exception("Failed to delete\n\n" + targetDir);
                }
            }
        }

        public static string CheckoutDirectory
        {
            get
            {
                return FileManager.MakeAbsolute("../../../../../../../");

            }
        }

        /// <summary>
        /// The root FlatRedBall folder (the directory of the github project)
        /// </summary>
        public static string FlatRedBallDirectory => FileManager.MakeAbsolute("../../../../../../");


        /// <summary>
        /// Returns FlatRedBallDirectory + "Engines/"
        /// </summary>
        public static string EngineDirectory => FlatRedBallDirectory + "Engines/";

        public static string TemplateDirectory => FlatRedBallDirectory + "Templates/";

        public static string AddOnsDirectory => EngineDirectory + "FlatRedBallAddOns/FlatRedBallAddOns/";

        public static string ReleaseDirectory => FileManager.MakeAbsolute("../../ReleaseFiles/");

        public static string FrbdkForZipReleaseDirectory => ReleaseDirectory + @"FRBDK For Zip\";

        public static string FrbdkDirectory => FlatRedBallDirectory + "FRBDK/";

        public static string GumRootDirectory
        {
            get
            {
                return FileManager.MakeAbsolute("../../../../../../../Gum/");
            }
        }

        public static string GumBuildDirectory
        {
            get
            {
                return GumRootDirectory + "Gum/bin/Debug/";
            }
        }

        public static string GluePublishDestinationFolder
        {
            get
            {
                // This is the output from: dotnet publish GlueFormsCore.csproj -r win-x86 -c DEBUG
                // Update - we don't do a publish anymore that seems to cause problems, so instead we
                // just copy out of the regular built folder
                return DirectoryHelper.FrbdkDirectory + @"Glue\Glue\bin\Debug\";

            }
        }
    }
}
