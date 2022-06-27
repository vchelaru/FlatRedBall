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

        public static string FlatRedBallDirectory => FileManager.MakeAbsolute("../../../../../../");


        public static string EngineDirectory => FlatRedBallDirectory + "Engines/";

        public static string TemplateDirectory => FlatRedBallDirectory + "Templates/";

        public static string AddOnsDirectory => EngineDirectory + "FlatRedBallAddOns/FlatRedBallAddOns/";

        public static string ReleaseDirectory
        {
            get
            {
                return FileManager.MakeAbsolute("../../ReleaseFiles/");
            }
        }

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
                return DirectoryHelper.FrbdkDirectory + @"Glue\Glue\bin\DEBUG\netcoreapp3.0\win-x86\publish\";

            }
        }
    }
}
