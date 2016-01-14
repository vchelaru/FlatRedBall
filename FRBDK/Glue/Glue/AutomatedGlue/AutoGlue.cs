using System;
using System.IO;
using System.Reflection;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Settings;
using Glue;

namespace FlatRedBall.Glue.AutomatedGlue
{
    public static class AutoGlue
    {
        public static MainGlueWindow MainForm { get; private set; }

        private static string FindFlatRedBallProjects(string path)
        {
            if(path.Length < @"C:\FlatRedBallProjects".Length) throw new Exception("Unable to find FlatRedBallProjects path.");

            if(new DirectoryInfo(path).Name.ToLower() == "flatredballprojects")
            {
                return path;
            }

            return FindFlatRedBallProjects(Path.GetDirectoryName(path));
        }

        private static string FindGlueBin()
        {
            try
            {
                return FindFlatRedBallProjects(Path.GetDirectoryName(Assembly.GetAssembly(typeof(AutoGlue)).Location)) +
                       @"\FRBDK\Glue\Glue\bin\Debug";
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to find Glue Bin at :" + Path.GetDirectoryName(Assembly.GetAssembly(typeof(AutoGlue)).Location), ex);
            }
        }

        public static void Start()
        {
            FlatRedBall.IO.FileManager.RelativeDirectory = FindGlueBin();

            MainForm = new MainGlueWindow();

            GlueGui.ShowGui = false;
            GlueSettingsSave.StopSavesAndLoads = true;
            GlueLayoutSettings.StopSavesAndLoads = true;
            FileAssociationSettings.StopSavesAndLoads = true;
            MainForm.StartUpGlue();
        }
    }
}
