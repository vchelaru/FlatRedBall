using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Controls;
using ToolsUtilities;

namespace FlatRedBall.Glue.Plugins
{
    public class PluginUpdater
    {
        string remoteUrl;
        Action callback;

        public void StartDownload(string fullUrl, Action callback)
        {
            this.callback = callback;
            remoteUrl = fullUrl;
            string strippedFile = FileManager.RemovePath(fullUrl);

            string destinationDirectory = FileManager.UserApplicationDataForThisApplication + "TempDownloadedPlugins/";

            System.IO.Directory.CreateDirectory(destinationDirectory);
            string destination = destinationDirectory + strippedFile;



            FlatRedBall.Glue.Managers.DownloadManager.Self.DownloadFile(
                fullUrl, destination, HandleComplete);

        }

        void HandleComplete(object sender, AsyncCompletedEventArgs e)
        {
            AfterDownload(e);
        }

        public void AfterDownload(AsyncCompletedEventArgs e)
        {
            if (e.Cancelled == false && e.Error == null)
            {
                string strippedFile = FileManager.RemovePath(remoteUrl);
                string absoluteFile = FileManager.UserApplicationDataForThisApplication + "TempDownloadedPlugins/" + strippedFile;

                bool doesFileExist = System.IO.File.Exists(absoluteFile);
                // try install;
                if (doesFileExist)
                {
                    bool succeeded = PluginManager.InstallPlugin(InstallationType.ForUser, absoluteFile);

                }
            }

            if(callback != null)
            {
                callback();
            }
        }
    }
}
