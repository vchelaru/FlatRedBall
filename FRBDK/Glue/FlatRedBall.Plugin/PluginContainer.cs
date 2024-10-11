using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.Windows.Forms;
using System.Reflection;
using System.Threading;
using FlatRedBall.Glue.Plugins.Rss;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.Plugins
{
    #region Enums

    public enum DownloadState
    {
        NotStarted,
        Downloading,
        NoConnection,
        Error,
        InformationDownloaded
    }

    #endregion

    public class PluginContainer
    {
        string mCustomAssemblyLocation;

        #region Properties

        public DownloadState DownloadState
        {
            get;
            private set;
        }

        public IPlugin Plugin
        {
            get;
            private set;
        }


        public bool IsEnabled
        {
            get;
            set;
        }

        public string Name
        {
            get 
            {
                string toReturn = "Unnamed Plugin";
                try
                {
                    toReturn = Plugin.FriendlyName;
                }
                catch(Exception e)
                {
                    Fail(e, "Failed getting FriendlyName");

                    toReturn = Plugin.GetType() + " (FriendlyName threw an exception)";
                }

                return toReturn;
            }
        }

        public Exception FailureException
        {
            get;
            private set;
        }

        public string FailureDetails
        {
            get;
            private set;
        }

        public List<Control> Controls
        {
            get;
            set;
        }

        public FilePath AssemblyLocation
        {
            set
            {
                mCustomAssemblyLocation = value.FullPath;
            }
            get
            {
                if (string.IsNullOrEmpty(mCustomAssemblyLocation))
                {
                    Assembly assembly = Plugin.GetType().Assembly;
                    //text += "\n" + assembly.ToString();
                    string fileName = assembly.Location;

                    return fileName;
                }
                else
                {
                    return mCustomAssemblyLocation;
                }
            }

        }

        public string RemoteLocation
        {
            get;
            set;
        }

        public string RemotePlugLocation
        {
            get;
            set;
        }

        public DateTime LastUpdate
        {
            get;
            private set;
        }

        #endregion

        #region Methods

        public PluginContainer(IPlugin plugin)
        {
            Controls = new List<Control>();
            Plugin = plugin;
            IsEnabled = true;
        }

        public void Fail(Exception exception, string details)
        {
            IsEnabled = false;
            FailureException = exception;
            FailureDetails = details;
            try
            {
                this.Plugin.ShutDown(PluginShutDownReason.PluginException);
            }
            catch (Exception e)
            {
                this.FailureDetails += "\nPlugin also failed during shutdown";
            }
        }

        public void TryStartDownload(Action<DownloadState> finishedDownloading)
        {
            if (DownloadState == Plugins.DownloadState.NotStarted && !string.IsNullOrEmpty(RemoteLocation))
            {
                DownloadState = Plugins.DownloadState.Downloading;


                AllFeed.StartDownloadingInformation(RemoteLocation + "/revisions/rss", (feed, state) =>
                    {
                        FinishedDownloadingInformation(feed, state);
                        finishedDownloading(state);
                        
                    }
                );

            }
        }

        public void ResetDownloadState()
        {
            DownloadState = Plugins.DownloadState.NotStarted;
        }

        private void FinishedDownloadingInformation(AllFeed feed, Plugins.DownloadState downloadState)
        {
            if (downloadState == Plugins.DownloadState.InformationDownloaded)
            {
                string timeDownloaded = feed.Items[0].PublishedDate;
                DateTime dateTime = System.DateTime.Parse(timeDownloaded);
                LastUpdate = dateTime;
            }


            this.DownloadState = downloadState;

        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(FailureDetails))
            {
                return Name;
            }
            else
            {
                return Name + "(" + FailureDetails + ")";
            }
        }
        #endregion
    }
}
