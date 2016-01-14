using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using FRBDKUpdater.Actions;
using FlatRedBall.IO;
using Ionic.Zip;
using OfficialPlugins.FrbdkUpdater;
using System.Collections.Generic;

namespace FRBDKUpdater
{
    public partial class FrmMain : Form
    {
        private Settings _settings = new Settings();
        
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        string mFileNameToLoad;

        #region Properties
        
        public string UserMessage
        {
            get
            {
                return this.Text;
            }
            set
            {
                this.Text = value;
            }
        }

        public bool Succeeded
        {
            get;
            private set;
        }


        #endregion

        
        public FrmMain()
        {
            InitializeComponent();
        }

        public FrmMain(string fileNameToLoad)
            : this()
        {
            Settings.UserAppPath = FileManager.GetDirectory(fileNameToLoad);
            mFileNameToLoad = fileNameToLoad;
        }

        private void FrmMainLoad(object sender, EventArgs e)
        {
            try
            {
                var extension = FileManager.GetExtension(mFileNameToLoad);

                List<string> stringList = new List<string>();

                if (extension == UpdaterRuntimeSettings.RuntimeSettingsExtension)
                {
                    var updaterRuntimeSettings = UpdaterRuntimeSettings.FromFile(mFileNameToLoad);
                    if (string.IsNullOrEmpty(updaterRuntimeSettings.LocationToSaveFile))
                    {
                        throw new Exception("UserRuntimeSettings LocationToSaveFile is null.  Loaded settings from " + Settings.UserAppPath);
                    }

                    _settings = Settings.GetSettings(updaterRuntimeSettings);
                    Logger.Log("Loading UpdaterRuntimeSettings");

                    if (string.IsNullOrEmpty(_settings.SaveFile))
                    {
                        throw new Exception("The settings SaveFile was null when loading from UpdaterRuntimeSettings");
                    }
                }
                else
                {
                    FrbdkUpdaterSettings tempSettings;

                    if (string.IsNullOrEmpty(mFileNameToLoad))
                    {
                        throw new Exception("The command line argument must not be null");
                    }

                    tempSettings = FrbdkUpdaterSettings.LoadSettings(mFileNameToLoad);
                    stringList.Add("Selected source:" + tempSettings.SelectedSource);
                    _settings = Settings.GetSettings(tempSettings);
                    Logger.Log("Loading FrbdkUpdaterSettings");

                    if (string.IsNullOrEmpty(_settings.SaveFile))
                    {
                        throw new Exception("The settings SaveFile was null when loading from the FRBDKUpdaterSettings");
                    }
                }



                Messaging.ShowAlerts = !_settings.Passive;

                if (!String.IsNullOrEmpty(_settings.Title))
                    UserMessage = _settings.Title;

                var downloader = new Downloader(_settings);

                downloader.ReportProgress += DownloaderOnReportProgress;
                downloader.ErrorOccured += DownloaderOnErrorOccured;
                downloader.DownloadComplete += DownloaderOnDownloadComplete;



                downloader.Start();
            }
            catch(Exception outerException)
            {
                MessageBox.Show(outerException.ToString());
            }
        }

        private void DownloaderOnErrorOccured(object sender, DownloaderErrorEventArgs downloaderErrorEventArgs)
        {
            Messaging.AlertError(downloaderErrorEventArgs.Error, Settings.UserAppPath);
            Logger.LogAndShowImmediately(Settings.UserAppPath, downloaderErrorEventArgs.Error);
        }

        private void DownloaderOnDownloadComplete(object sender, DownloaderCompleteEventArgs downloaderCompleteEventArgs)
        {
            bool successful = downloaderCompleteEventArgs.Successful;

            if (successful)
            {
                successful = ActionStarter.CleanAndZipAction(_settings.DirectoryToClear, _settings.SaveFile, _settings.ExtractionPath);
            }

            if (!successful)
            {
                Close();
                Messaging.AlertError("Clean and unzip failed", Settings.UserAppPath);
                Succeeded = false;
                return;
            }
            else
            {
                Succeeded = true;
                pbValue.BeginInvoke(new Action(HandleDownloadSucceeded));
            }
        }


        private void HandleDownloadSucceeded()
        {
            this.CloseButton.Enabled = true;
            this.lblSpeed.Text = "Download complete";

            // Create a timer to auto-close this in 3 seconds so the user doesn't wonder what's up:
            var timer = new System.Windows.Forms.Timer();
            timer.Enabled = true;
            timer.Interval = 1000; // milliseconds
            timer.Tick += delegate
            {
                CloseButton_Click(null, null);
            };

            timer.Start();
            
        }

        private void StartGlueAndCloseThis()
        {
            Hide();

            //Start Glue back up and make sure it's in focus
            if (!string.IsNullOrEmpty(_settings.ApplicationToRunAfterWorkIsDone) &&
                File.Exists(_settings.ApplicationToRunAfterWorkIsDone))
            {
                var process = Process.Start(_settings.ApplicationToRunAfterWorkIsDone);
                Thread.Sleep(200);
                if (process != null)
                {
                    IntPtr hWnd = process.MainWindowHandle;
                    var startTime = DateTime.Now;
                    while (hWnd == IntPtr.Zero && DateTime.Now - startTime < new TimeSpan(0, 0, 30))
                    {
                        Thread.Sleep(100);
                        hWnd = process.MainWindowHandle;
                        Application.DoEvents();
                    }

                    if (hWnd != IntPtr.Zero)
                        SetForegroundWindow(hWnd);
                }
            }

            Close();
        }

        private void DownloaderOnReportProgress(object sender, DownloaderProgressEventArgs downloaderProgressEventArgs)
        {
            pbValue.BeginInvoke(
                new EventHandler(
                    delegate
                        {
                            lblSpeed.Text = downloaderProgressEventArgs.Speed;
                            pbValue.Value = downloaderProgressEventArgs.ProgressPercentage;
                        }));
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            StartGlueAndCloseThis();
        }
    }
}
