using System;
using System.Windows.Forms;
using FlatRedBall.IO;

namespace FRBDKUpdater
{
    public static class Messaging
    {
        public static bool ShowAlerts { get; set; }

        static Messaging()
        {
            ShowAlerts = true;
        }

        public static void AlertError(string message, Exception ex)
        {
            string whereToSave = null;

            bool failed = true;
            try
            {
                whereToSave = FileManager.UserApplicationDataForThisApplication + "UpdateError.txt";
                Logger.Log(message + @"\n\nException Info:\n" + ex);
                Logger.Flush(whereToSave, false);
                failed = false;
            }
            catch(Exception e)
            {
                // there seems to be an error occuring (reported by users) that errors aren't being saved properly, so let's just display it here
                MessageBox.Show("There was an error in the project:\n" + ex + "\n\n" + "Furthermore, the program failed to log the error:\n" + e);
            }

            if (ShowAlerts && !failed)
            {
                MessageBox.Show(message + "\n\nFor more information, see this location:\n" + whereToSave);
            }
        }

        public static void AlertError(string message, string userApplicationData)
        {
            if (ShowAlerts)
            {
                MessageBox.Show(message);
            }

            Logger.Log(message);
        }

        public static void Alert(string message)
        {
            if (ShowAlerts)
                MessageBox.Show(message);

            Logger.Log(message);
        }
    }
}
