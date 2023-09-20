using FRBDKUpdater;
using FRBDKUpdater.Actions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ToolsUtilities;
using ToolsUtilitiesStandard.Network;

namespace UpdaterWpf.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Settings _settings = new Settings();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        UpdaterRuntimeSettings updaterRuntimeSettings;

        CancellationTokenSource cancellationTokenSource;

        public GeneralResponse GeneralResponse { get; private set; }

        public Visibility CancelButtonVisibility
        {
            get => CancelButton.Visibility;
            set => CancelButton.Visibility = value;
        }

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += HandleLoaded;
        }

        public MainWindow(string settingsFileName, UpdaterRuntimeSettings updaterRuntimeSettings)
            : this()
        {
            Settings.UserAppPath = FileManager.GetDirectory(settingsFileName);
            this.updaterRuntimeSettings = updaterRuntimeSettings;
        }

        private async void HandleLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                List<string> stringList = new List<string>();

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

                Messaging.ShowAlerts = !_settings.Passive;

                if (!String.IsNullOrEmpty(_settings.Title))
                    Title = _settings.Title;

                await PerformDownload();
            }
            catch (Exception outerException)
            {
                MessageBox.Show(outerException.ToString());
            }

        }

        private async Task PerformDownload()
        {
            using var cts = new CancellationTokenSource();
            cancellationTokenSource = cts;
            var downloader = new Downloader(_settings);

            downloader.ReportProgress += DownloaderOnReportProgress;
            downloader.ErrorOccured += DownloaderOnErrorOccured;

            ToolsUtilities.GeneralResponse downloadResponse = GeneralResponse.SuccessfulResponse;
            try
            {
                downloadResponse = await downloader.PerformDownload(cts.Token);
                CancelButton.Visibility = Visibility.Hidden;
                await DownloaderOnDownloadComplete(downloadResponse);
            }
            catch(OperationCanceledException)
            {
                downloadResponse = GeneralResponse.UnsuccessfulWith("Download Cancelled");
            }

        }

        private void DownloaderOnErrorOccured(object sender, DownloaderErrorEventArgs downloaderErrorEventArgs)
        {
            Messaging.AlertError(downloaderErrorEventArgs.Error, Settings.UserAppPath);
            //Logger.LogAndShowImmediately(Settings.UserAppPath, downloaderErrorEventArgs.Error);
            GeneralResponse = GeneralResponse.UnsuccessfulWith(downloaderErrorEventArgs.Error);
            DialogResult = false;
        }

        private async Task DownloaderOnDownloadComplete(GeneralResponse generalResponse)
        {
            if(generalResponse.Succeeded)
            {
                bool cleanAndUnzipSuccessful = ActionStarter.CleanAndZipAction(_settings.DirectoryToClear, _settings.SaveFile, _settings.ExtractionPath);

                if(!cleanAndUnzipSuccessful)
                {
                    generalResponse = GeneralResponse.UnsuccessfulWith("Could not clear/unzip destination folder");
                }
            }

            if(generalResponse.Succeeded)
            {
                //this.CloseButton.IsEnabled = true;
                this.SpeedLabel.Text = "Download complete";
                await Task.Delay(1000);
            }
            this.GeneralResponse = generalResponse;

            if(DialogResult == null)
            {
                DialogResult = generalResponse.Succeeded;
            }
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
                        //Application.DoEvents();
                    }

                    if (hWnd != IntPtr.Zero)
                        SetForegroundWindow(hWnd);
                }
            }

            Close();
        }



        private void DownloaderOnReportProgress(long? totalLength, long downloadedSoFar)
        {

            SpeedLabel.Text =
                NetworkManager.ToMemoryDisplay(downloadedSoFar) + "/" +
                NetworkManager.ToMemoryDisplay(totalLength);
                //downloaderProgressEventArgs.Speed;

            if (totalLength != null)
            {
                ProgressBarInstance.Value = 100 * ((double)downloadedSoFar/totalLength.Value);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            cancellationTokenSource?.Cancel();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
