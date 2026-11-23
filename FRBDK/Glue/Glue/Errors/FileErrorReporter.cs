using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.Controls;

namespace FlatRedBall.Glue.Errors
{
    public static class FileErrorReporter
    {
        static List<string> mFilesAlreadyReported = new List<string>();
        static bool mInitialized;

        static Timer mTimer;

        static List<Tuple<string, string>> mErrorMessageAndFiles = new List<Tuple<string, string>>();

        public static void Initialize(Form form)
        {
            mInitialized = true;
            mTimer = new Timer();
            mTimer.Tick += mTimer_Tick;
            mTimer.Interval = 250;
            mTimer.Start();

        }

        /// <summary>
        /// For a host with no message loop, where the WinForms timer would never tick. Errors queue until
        /// <see cref="FlushPendingErrors"/>.
        /// </summary>
        public static void InitializeHeadless()
        {
            mInitialized = true;
        }

        static void mTimer_Tick(object sender, EventArgs e)
        {
            mTimer.Stop();
            FlushPendingErrors();
            mTimer.Start();

        }

        /// <summary>
        /// Reports every queued error to the dialog service and the error output. The editor's timer calls
        /// this every 250ms.
        /// </summary>
        public static void FlushPendingErrors()
        {
            lock (mFilesAlreadyReported)
            {
                while (mErrorMessageAndFiles.Count != 0)
                {
                    string first = mErrorMessageAndFiles[0].Item1;
                    string second = mErrorMessageAndFiles[0].Item2;
                    mErrorMessageAndFiles.RemoveAt(0);
                    DialogService.ShowMessage(first);
                    Plugins.PluginManager.ReceiveError(first);

                }
            }
        }

        public static void ReportError(string fileName, string error, bool forceError)
        {

            // Victor Chelaru
            // June 15, 2024 
            // Sometimes there is a CSV error reported here with an empty string. Not sure why,
            // so I have a breakpoint here to see if I can figure it out.
            // Only with a debugger attached: without one, Debugger.Break asks the user to attach a debugger
            // (a dialog that blocks a headless host) or ends the process.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }

            if (!mInitialized)
            {
                throw new Exception("Initialize must be called first");
            }

            lock (mFilesAlreadyReported)
            {
                bool nullFile = string.IsNullOrEmpty(fileName);

                if (nullFile || !mFilesAlreadyReported.Contains(fileName) || forceError)
                {
                    if (!nullFile)
                    {
                        mFilesAlreadyReported.Add(fileName);
                    }
                    mErrorMessageAndFiles.Add(new Tuple<string, string>(error, ProjectManager.MakeRelativeContent(fileName)));
                }
            }

        }

    }
}
