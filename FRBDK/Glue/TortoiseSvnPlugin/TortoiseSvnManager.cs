using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using FlatRedBall.IO;
using FlatRedBall.Glue;

namespace PluginTestbed.TortoiseSvnPlugin
{
    public class TortoiseSvnManager
    {

        #region Public Methods

        public void PerformCleanup()
        {
            string arguments = "/command:cleanup /path:\"" + ProjectManager.ProjectRootDirectory + "\"";

            CallTortoiseProcessWithArguments(arguments);
        }

        public void PerformSvnUpdate()
        {
            CallTortoiseProcessWithArguments("/command:update /path:\"" + ProjectManager.ProjectRootDirectory + "\"", true);


        }

        public void PerformSvnCommit()
        {
            CallTortoiseProcessWithArguments("/command:commit /path:\"" + ProjectManager.ProjectRootDirectory + "\"", true);
        }

        public void RemoveFromVersionControl(string file)
        {
            string arguments = "/command:remove /path:\"" + file + "\"";

            CallTortoiseProcessWithArguments(arguments);
        }

        //public bool HasFileBeenModified(string file)
        //{
        //    bool hasBeenModified = false;

        //    string svnExecutableLocation = ProjectRootDirectory +
        //        "\\Tools\\Subversion\\svn.exe";
        //    if (System.IO.File.Exists(svnExecutableLocation))
        //    {
        //        Process process = CallProcessWithArguments(svnExecutableLocation, "status \"" + file + "\"", false);

        //        using (process.StandardOutput)
        //        {
        //            string str;
        //            string revisionInfo = "";
        //            while ((str = process.StandardOutput.ReadLine()) != null)
        //            {
        //                if (str.StartsWith("M "))
        //                {
        //                    hasBeenModified = true;
        //                }
        //            }
        //        }
        //    }
        //    return hasBeenModified;
        //}
        
        public void SeeLogFor(string file)
        {
            string arguments = "/command:log /path:\"" + file + "\"";

            CallTortoiseProcessWithArguments(arguments);
        }
        
        //public void ShowRevisionNumber()
        //{
        //    string svnExecutableLocation = ProjectRootDirectory +
        //        "\\Tools\\Subversion\\svnversion.exe";

        //    if (System.IO.File.Exists(svnExecutableLocation))
        //    {
        //        Process process = CallProcessWithArguments(svnExecutableLocation, ProjectRootDirectory, false);

        //        using (process.StandardOutput)
        //        {

        //            string str;
        //            string revisionInfo = "";
        //            while ((str = process.StandardOutput.ReadLine()) != null)
        //            {
        //                revisionInfo = str;
        //            }

        //            MessageBox.Show(revisionInfo + "\n\nFor info on what this means, see\n\nhttp://www.linxit.de/svnbook/en/1.1/re57.html");
        //        }
        //    }
        //}

        #endregion

        #region Private Methods

        private void CallTortoiseProcessWithArguments(string arguments)
        {
            CallProcessWithArguments("C:/Program Files/TortoiseSVN/bin/TortoiseProc.exe", arguments);
        }

        private void CallTortoiseProcessWithArguments(string arguments, bool showWindow)
        {
            CallProcessWithArguments("C:/Program Files/TortoiseSVN/bin/TortoiseProc.exe", arguments, showWindow);
        }

        private void CallProcessWithArguments(string processFileName, string arguments)
        {
            CallProcessWithArguments(processFileName, arguments, false);
        }

        private Process CallProcessWithArguments(string processFileName, string arguments, bool showWindow)
        {
            Process process = new Process();

            process.StartInfo.FileName = "\"" + processFileName + "\"";
            process.StartInfo.Arguments = arguments;

            process.StartInfo.UseShellExecute = showWindow;
            process.StartInfo.RedirectStandardOutput = !showWindow;
            process.StartInfo.RedirectStandardError = !showWindow;
            process.StartInfo.CreateNoWindow = !showWindow;

            process.Start();

            return process;
        }

        #endregion
    }
}
