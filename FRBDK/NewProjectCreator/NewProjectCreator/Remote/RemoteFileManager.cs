using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if GLUE
using FlatRedBall.Glue.VSHelpers.Projects;
#endif
using FlatRedBall.IO;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Reflection;
#if GLUE
using FlatRedBall.Glue.VSHelpers;
#endif

namespace NewProjectCreator.Remote
{
    public delegate void SetStatusString(string statusToSet);

    public static class RemoteFileManager
    {
        

        #region Fields
        public static bool HasFinishedDownloading = false;
        public static bool FailedToDownload = false;
        public static string ReasonForFailure = null;

        static ManualResetEvent mManualResetEvent = new ManualResetEvent(false);

        static string mNewestTemplateDirectory;
        static int mNewestYear;

        #endregion


        public static SetStatusString SetStatusString;

        #region Methods

        //public static void Initialize()
        //{

        //    ThreadStart threadStart = new ThreadStart(InitializeAsync);

        //    Thread thread = new Thread(threadStart);

        //    thread.Start();
        //}
        private delegate void SetControlPropertyThreadSafeDelegate(Control control, string propertyName, object propertyValue);

        public static void SetControlPropertyThreadSafe(Control control, string propertyName, object propertyValue)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(new SetControlPropertyThreadSafeDelegate(SetControlPropertyThreadSafe), new object[] { control, propertyName, propertyValue });
            }
            else
            {
                control.GetType().InvokeMember(propertyName, BindingFlags.SetProperty, null, control, new object[] { propertyValue });
            }
        }
        
        //static bool IsRemoteNewer(string zipFile, out string remoteFile)
        //{
        //    if (SetStatusString != null)
        //    {
        //        SetStatusString("Checking remote dates for " + zipFile);
        //    }
        //    //SetControlPropertyThreadSafe(
        //    //    Form1.Self,
        //    //    "InfoBarLabelText",
        //    //    "Checking remote dates for " + zipFile);

        //    FileStruct fileStruct = GetLastUploadFileStructFor(zipFile, out remoteFile);

        //    DateTime lastLocalWriteTime = new DateTime();
            
        //    if(File.Exists(zipFile))
        //    {
        //        lastLocalWriteTime = File.GetLastWriteTime(zipFile);
        //    }

        //    return fileStruct.CreateTime > lastLocalWriteTime;
        //}


        static DateTime ChangeTime(this DateTime dateTime, int year)
        {
            return new DateTime(
                year,
                dateTime.Month,
                dateTime.Day,
                dateTime.Hour,
                dateTime.Minute,
                dateTime.Second,
                dateTime.Millisecond,
                dateTime.Kind);
        }

        #endregion

    }
}
