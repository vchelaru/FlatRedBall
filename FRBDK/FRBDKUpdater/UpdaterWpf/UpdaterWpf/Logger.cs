using System.Diagnostics;
using System.IO;
using System.Text;
using System;
using ToolsUtilities;

namespace FRBDKUpdater
{
    public static class Logger
    {
        static StringBuilder mBuffer = new StringBuilder();

        public static string GetLogSaveFolder(string appDataPath)
        {
            return appDataPath + @"FRBDK\FRBDKUpdaterLog";
        }

        public static void Log(string message)
        {
            mBuffer.AppendLine(message);
        }

        public static void Flush(string userAppPath, bool showOutputAfterFinished)
        {
            var fileName = GetLogSaveFolder(userAppPath) + DateTime.Now.ToUniversalTime().ToString().Replace("\\", "_").Replace("/", "_").Replace(":", "_") + ".txt";

            while (File.Exists(fileName))
            {
                fileName = StringFunctions.IncrementNumberAtEnd(fileName);
            }

            string directory = FileManager.GetDirectory(fileName);

            Directory.CreateDirectory(directory);


            File.WriteAllText(fileName, mBuffer.ToString());
            
            // This is needed for debugging, but it's kinda annoying for the user to get all these popups
            if (showOutputAfterFinished)
            {
                Process.Start(fileName);
            }
        }

        public static void LogAndShowImmediately(string userAppPath, string message)
        {
            var fileName = userAppPath + @"\FRBDK\FRBDKUpdaterImmediateLog.txt";

            if (File.Exists(fileName))
                File.Delete(fileName);

            File.WriteAllText(fileName, message);

            // Victor Chelaru
            // December 17, 2014
            // Not sure why but this
            // was commented out.  We
            // do want to show the log
            // so 
            Process.Start(fileName);
        }
    }
}
