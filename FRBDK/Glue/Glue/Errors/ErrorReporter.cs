using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Errors
{
    public static class ErrorReporter
    {
        static List<string> mFilesAlreadyReported = new List<string>();
        // used to invoke.
        static Form mForm;

        static Timer mTimer;

        static List<Tuple<string, string>> mErrorMessageAndFiles = new List<Tuple<string, string>>();

        public static void Initialize(Form form)
        {
            mForm = form;
            mTimer = new Timer();
            mTimer.Tick += mTimer_Tick;
            mTimer.Interval = 250;
            mTimer.Start();

        }

        static void mTimer_Tick(object sender, EventArgs e)
        {
            mTimer.Stop();
            lock (mFilesAlreadyReported)
            {
                while (mErrorMessageAndFiles.Count != 0)
                {
                    string first = mErrorMessageAndFiles[0].Item1;
                    string second = mErrorMessageAndFiles[0].Item2;
                    mErrorMessageAndFiles.RemoveAt(0); 
                    MessageBox.Show(first, second);
                    Plugins.PluginManager.ReceiveError(first);

                }
            }
            mTimer.Start();

        }

        public static void ReportError(string fileName, string error, bool forceError)
        {

            // Victor Chelaru
            // June 15, 2024 
            // Sometimes there is a CSV error reported here with an empty string. Not sure why,
            // so I have a breakpoint here to see if I can figure it out.
            System.Diagnostics.Debugger.Break();

            if (mForm == null)
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
