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

        public static string EngineDirectory
        {
            get
            {
                return FileManager.MakeAbsolute("../../../../../../Engines/");
            }
        }

        public static string TemplateDirectory
        {
            get
            {
                return FileManager.MakeAbsolute("../../../../../../Templates/");
            }
        }

        public static string AddOnsDirectory
        {
            get
            {
                return FileManager.MakeAbsolute("../../../../../../Engines/FlatRedBallAddOns/FlatRedBallAddOns/");
            }
        }

        public static string ReleaseDirectory
        {
            get
            {
                return FileManager.MakeAbsolute("../../ReleaseFiles/");
            }
        }

        public static string FrbdkDirectory
        {
            get
            {
                return FileManager.MakeAbsolute("../../../../../../FRBDK/");
            }
        }

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
    }
}
