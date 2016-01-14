using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using EnvDTE;
using EnvDTE80;
using FrbXnaReleaseAssistant;
using FlatRedBall.IO;
using System.IO;
using VSLangProj;
using Thread = System.Threading.Thread;


namespace FlatRedBall.Glue.VSHelpers
{
    public static class IdeManager
    {

        #region Fields
        private static DTE2 mInstance;
        private static string mCurrentSolution;
        private static Thread mDebugThread;

        static bool mHasOnlyExpress;

        #endregion

        #region Properties

        public static bool HasOnlyExpress
        {
            set { mHasOnlyExpress = value; }
            get { return mHasOnlyExpress; }
        }

        public static bool IsDebugging
        {
            get
            {
                try
                {
                    return (mInstance != null &&
                        (mInstance.Debugger.CurrentMode == EnvDTE.dbgDebugMode.dbgRunMode));
                }
                catch (System.Runtime.InteropServices.COMException comException)
                {
                    //Instance is most likely in the middle of a build.
                    return true;
                }
            }
        }

        #endregion

        #region Methods
        private static void BusyAction(Action action)
        {
            var tryAgain = true;
            while (tryAgain)
            {
                tryAgain = false;
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    if (ex.ToString().ToLower().Contains("busy"))
                    {
                        tryAgain = true;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        public static void AddProjectsToSolution(string solutionFileName, List<string> projectFileNames)
        {
            Project newProject = null;

            //Register MessageFilter to prevent failed RPC messages
            MessageFilter.Register();

            BusyAction(() => mInstance.Solution.Open(solutionFileName));

            BusyAction(() =>
            {
                if (!mInstance.Solution.IsOpen)
                {
                    throw new Exception(solutionFileName + " did not open!");
                }
            });

            foreach (var projectFileName in projectFileNames)
            {
                var exists = false;
                var name = projectFileName;
                foreach (Project project in mInstance.Solution.Projects)
                {
                    if (project.FileName.ToLower() == name.ToLower())
                    {
                        exists = true;
                        newProject = project;
                        break;
                    }
                }

                if (!exists)
                    newProject = mInstance.Solution.AddFromFile(projectFileName);

                //Add dependency for projects that reference it's dll
                foreach (Project project in mInstance.Solution.Projects)
                {
                    var proj = project.Object as VSProject;

                    if (proj != null)
                    {
                        foreach (Reference reference in proj.References)
                        {
                            Reference reference1 = reference;
                            Project project1 = newProject;
                            BusyAction(() =>
                                           {
                                               if (reference1.Name == project1.Name)
                                               {
                                                   mInstance.Solution.SolutionBuild.BuildDependencies.Item(
                                                       project1.UniqueName).AddProject(project1.UniqueName);
                                               }
                                           });
                        }
                    }
                }
            }

            BusyAction(() => mInstance.Solution.Close(true));

        }

        public static void BuildAndLaunchProject(string projectFileName)
        {
            string solutionFileName = LocateSolution(projectFileName);

            int buildResults = BuildSolution(solutionFileName, true);

            if (buildResults == 0)
            {
                mInstance.Solution.SolutionBuild.Debug();


                mDebugThread = new Thread(new ThreadStart(WaitForDebugClose));
                mDebugThread.Start();

            }

        }

        private static int BuildSolution(string fileName, bool convertToX86)
        {

            //Separate name of Solution
            string solutionName = FlatRedBall.IO.FileManager.RemovePath(fileName);

            //Register MessageFilter to prevent failed RPC messages
            MessageFilter.Register();

            mInstance.Solution.Open(fileName);

            mInstance.Solution.SolutionBuild.SolutionConfigurations.Item("Debug").Activate();
            object o = mInstance.Solution.SolutionBuild.ActiveConfiguration;

            if (convertToX86)
            {
                foreach (EnvDTE.Project proj in mInstance.Solution.Projects)
                {
                    if (proj.ConfigurationManager == null)
                        continue;
                    System.Diagnostics.Trace.WriteLine(proj.Name);
                    EnvDTE.Configuration config = proj.ConfigurationManager.ActiveConfiguration;
                    EnvDTE.Property prop = config.Properties.Item("PlatformTarget");
                    prop.Value = "x86";
                }
            }

            mInstance.Solution.SolutionBuild.Build(true);

            if (mInstance.Solution.SolutionBuild.LastBuildInfo == 0)
            {
                // instanceHandle.Quit();
                // MessageFilter.Revoke();
                return 0;
            }
            else
            {
                mInstance.MainWindow.Activate();
                MessageFilter.Revoke();
                return mInstance.Solution.SolutionBuild.LastBuildInfo;
            }

        }

        //private static DTE2 CreateInstance(string version)
        //{
        //    try
        //    {
        //        DTE2 instanceHandle;
        //        instanceHandle = (EnvDTE80.DTE2)Microsoft.VisualBasic.Interaction.CreateObject("VisualStudio.DTE." + version, "");
        //        instanceHandle.SuppressUI = true;
        //        instanceHandle.UserControl = false;

        //        return instanceHandle;
        //    }
        //    catch
        //    {
        //        System.Windows.Forms.MessageBox.Show("Some features in Glue require the non-Express version of Visual Studio.  We apologize for the inconvenience.  You should still be able to run Glue normally, but you will not be able to perform some tasks automatically.");
        //        mHasOnlyExpress = true;
        //        return null;
        //    }
        //}

        private static void WaitForDebugClose()
        {
            while (!IdeManager.HasOnlyExpress && IdeManager.IsDebugging)
            {
                //Thread spins while debugging.
            }

            CloseSolution();
            //System.Windows.Forms.MessageBox.Show("Closing Solution.");
        }

        public static string LocateSolution(string projectFileName)
        {
            List<String> dirFileList = null;
            List<String> parentDirFileList = null;
            string directory = FileManager.GetDirectory(projectFileName);
            string baseFile = FileManager.RemovePath(FileManager.RemoveExtension(projectFileName));
            string solutionFileName = "";

            #region Search directory for "filename".sln
            dirFileList = FileManager.GetAllFilesInDirectory(directory, "sln", 0);

            if (dirFileList.Count != 0)
            {
                foreach (string file in dirFileList)
                {
                    if (FileManager.RemovePath(file).StartsWith(baseFile))
                    {
                        solutionFileName = file;
                        break;
                    }
                }
            }
            #endregion

            #region Search one directory above for "filename".sln
            if (string.IsNullOrEmpty(solutionFileName))
            {
                parentDirFileList = FileManager.GetAllFilesInDirectory(FileManager.GetDirectory(directory), "sln", 0);

                if (parentDirFileList.Count != 0)
                {
                    foreach (string file in parentDirFileList)
                    {
                        if (FileManager.RemovePath(file).StartsWith(baseFile))
                        {
                            solutionFileName = file;
                            break;
                        }
                    }
                }
            }
            #endregion

            #region Search same directory for *.sln

            if (string.IsNullOrEmpty(solutionFileName) && dirFileList.Count != 0)
                solutionFileName = dirFileList[0];
            #endregion

            #region Search parent directory for *.sln
            if (string.IsNullOrEmpty(solutionFileName) && parentDirFileList.Count != 0)
                solutionFileName = parentDirFileList[0];
            #endregion

            #region This MUST be an FSB project, but search another directory up for sln
            if (string.IsNullOrEmpty(solutionFileName))
            {
                dirFileList = FileManager.GetAllFilesInDirectory(FileManager.GetDirectory(FileManager.GetDirectory(directory)),
                                                                 "sln", 0);
                if (dirFileList.Count != 0)
                    solutionFileName = dirFileList[0];
            }
            #endregion

            if (string.IsNullOrEmpty(solutionFileName))
            {
                throw new FileNotFoundException();
            }

            return solutionFileName;

        }

        public static void Initialize(string version)
        {
            if (mInstance == null)
                mInstance = CreateInstance(version);
        }

        public static void Initialize()
        {
            Initialize("9.0");
        }

        public static void ReleaseInstance()
        {
            mInstance.Quit();
            MessageFilter.Revoke();
            mInstance = null;

        }

        public static void StopDebugging()
        {
            try
            {
                mInstance.Debugger.Stop(false);
            }
            catch
            {
                // Vic says - I one time got a crash here saying that the instance wasn't debugging.
                // So.... I just thought ok, that's fine, let's continue.
            }
        }

        public static void CloseInstance()
        {
            if (mInstance != null)
                mInstance.Quit();
        }

        public static void CloseSolution()
        {
            bool closed = false;

            if (mInstance != null)
            {
                while (closed == false)
                {
                    try
                    {
                        mInstance.Solution.Close(false);
                        closed = true;
                    }
                    catch (System.Runtime.InteropServices.COMException e)
                    {

                    }
                }

            }
        }
        #endregion

        internal static string ShowProjectCreationWizard()
        {
            CloseSolution();
            mInstance.ExecuteCommand("File.NewProject", "");
            string toReturn = "";


            if (mInstance.Solution.IsOpen)
            {
                EnvDTE.Project newProject = null;
                foreach (EnvDTE.Project project in mInstance.Solution.Projects)
                {
                    if (!project.FileName.Contains(".Web"))
                    {
                        newProject = project;
                    }
                }

                toReturn = newProject.FileName;
            }


            CloseSolution();

            return toReturn;
        }
    }
}
