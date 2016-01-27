using System;
using System.Text.RegularExpressions;
using FlatRedBall.IO;
using OfficialPlugins.FrbdkUpdater;

namespace FRBDKUpdater
{
    public class Settings
    {
        static Settings()
        {
            UserAppPath = FileManager.UserApplicationData;
        }

        #region Constants

        private const string DailyBuildRemoteUri = "http://files.flatredball.com/content/FrbXnaTemplates/DailyBuild/";
        private const string StartRemoteUri = "http://files.flatredball.com/content/FrbXnaTemplates/";

        private const string FrbdkFileName = "FRBDK.zip";

        #endregion
        
        #region Properties

        /// <summary>
        /// User app data path.  When switching to admin, need to keep regular user path.
        /// </summary>
        public static string UserAppPath { get; set; }

        /// <summary>
        /// File path on server
        /// </summary>
        public string Url
        {
            get;
            set;
        }

        /// <summary>
        /// Where to save file to on the local machine
        /// </summary>
        public string SaveFile
        {
            get;
            set;
        }

        /// <summary>
        /// Title of Update
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Path the zip will be extracted to
        /// </summary>
        public string ExtractionPath { get; set; }

        /// <summary>
        /// Directory to clear before action
        /// </summary>
        public string DirectoryToClear { get; set; }

        /// <summary>
        /// Application ran after everything is complete
        /// </summary>
        public string ApplicationToRunAfterWorkIsDone { get; set; }

        /// <summary>
        /// Timestamp file to compare to see if there is a newer file that needs to be downloaded
        /// </summary>
        public string LocalFileForTimeStamp { get; set; }

        /// <summary>
        /// Stops messageboxes from showing if true
        /// </summary>
        public bool Passive { get; set; }

        /// <summary>
        /// If true, files are downloaded even if it's already stored in app data
        /// </summary>
        public bool ForceDownload { get; set; }

        #endregion
        
        #region Methods

        public static Settings GetSettings(UpdaterRuntimeSettings runtimeSettings)
        {
            var toReturn = new Settings
                               {
                                   Url = runtimeSettings.FileToDownload,
                                   SaveFile = runtimeSettings.LocationToSaveFile,
                                   Title = runtimeSettings.FormTitle,
                                   Passive =  false,
                                   ForceDownload = true
                               };

            toReturn.LocalFileForTimeStamp =
                UserAppPath + @"FRBDK\" +
                FileManager.RemovePath(FileManager.RemoveExtension(runtimeSettings.LocationToSaveFile))
                 + @"\timestamp.txt";

            return toReturn;
        }

        public static Settings GetSettings(FrbdkUpdaterSettings settings)
        {
            var returnSettings = new Settings {ExtractionPath = settings.SelectedDirectory};

            if (settings.CleanFolder)
            {
                returnSettings.DirectoryToClear = settings.SelectedDirectory;
            }

            returnSettings.ApplicationToRunAfterWorkIsDone = settings.GlueRunPath;
            returnSettings.Passive = settings.Passive;
            returnSettings.ForceDownload = settings.ForceDownload;

            returnSettings.Url = StartRemoteUri + settings.SelectedSource + FrbdkFileName;
            returnSettings.SaveFile = UserAppPath + @"FRBDK\" + settings.SelectedSource + FrbdkFileName;
            returnSettings.LocalFileForTimeStamp = UserAppPath + @"FRBDK\" + settings.SelectedSource + @"\timestamp.txt";

            return returnSettings;
        }

        #endregion
        
    }
}
