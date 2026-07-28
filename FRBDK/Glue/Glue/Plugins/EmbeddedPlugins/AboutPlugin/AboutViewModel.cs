using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.AboutPlugin
{
    public class AboutViewModel : ViewModel
    {
        public string CopyrightText
        {
            get => Get<string>();
            set => Set(value);
        }

        public Version Version
        {
            get => Get<Version>();
            set => Set(value);
        }

        [DependsOn(nameof(Version))]
        public string VersionNumberText => Version?.ToString() ?? "--";

        public DateTime? LatestVersionOnline
        {
            get => Get<DateTime?>();
            set => Set(value);
        }

        [DependsOn(nameof(LatestVersionOnline))]
        public string LatestVersionText => LatestVersionOnline?.ToString("yyyy.M.d") ?? "--";

        public string DownloadStatusText
        {
            get => Get<string>(); set => Set(value);
        }

        [DependsOn(nameof(LatestVersionText))]
        [DependsOn(nameof(Version))]
        [DependsOn(nameof(IsDownloading))]
        public Visibility DownloadButtonVisibility
        {
            get
            {
                if(IsDownloading)
                {
                    return Visibility.Collapsed;
                }
                else if(Version.TryParse(LatestVersionText, out Version latestVersionOnline))
                {
                    return latestVersionOnline > Version ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        public bool IsDownloading
        {
            get => Get<bool>();
            set => Set(value);
        }

        public DateTime? LatestDailyBuildOnline
        {
            get => Get<DateTime?>();
            set => Set(value);
        }

        [DependsOn(nameof(LatestDailyBuildOnline))]
        public string LatestDailyBuildText => LatestDailyBuildOnline?.ToString("yyyy.M.d HH:mm") ?? "--";

        public string DailyBuildDownloadStatusText
        {
            get => Get<string>(); set => Set(value);
        }

        public bool IsDownloadingDailyBuild
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(LatestDailyBuildOnline))]
        [DependsOn(nameof(IsDownloadingDailyBuild))]
        public Visibility DailyBuildDownloadButtonVisibility =>
            !IsDownloadingDailyBuild && LatestDailyBuildOnline != null ? Visibility.Visible : Visibility.Collapsed;

        public string GluxVersionText
        {
            get => Get<string>();
            set => Set(value);
        }

        public int? DllSyntaxVersion
        {
            get => Get<int?>();
            set => Set(value);
        }

        public bool IsUsingSource
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(DllSyntaxVersion))]
        [DependsOn(nameof(IsUsingSource))]
        public string DllSyntaxVersionToShow
        {
            get
            {
                if (IsUsingSource)
                {
                    return "Using Engine Source";
                }
                else
                {
                    return DllSyntaxVersion?.ToString() ?? "Unknown";
                }
            }
        }

        public string MainProjectTypeText
        {
            get => Get<string>();
            set => Set(value);
        }

        internal async void DoInstallUpdate()
        {
            var result = DialogService.ShowConfirm("This will download the latest version of FlatRedBall and install it.  This will overwrite any existing FRBDK.  Are you sure you want to do this?");
            if(result == DialogButton.Yes)
            {
                IsDownloading = true;
                var location = "https://files.flatredball.com/content/FrbXnaTemplates/DailyBuild/FRBDK.zip";
                var destination = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FRBDK.zip");

                var succeeded = await DownloadWithProgress(location, destination, status => DownloadStatusText = status);

                IsDownloading = false;

                if (!succeeded)
                {
                    return;
                }

                // Get the currently running EXE location
                var currentExeLocation = System.Reflection.Assembly.GetEntryAssembly().Location;
                var directory = FlatRedBall.IO.FileManager.GetDirectory(currentExeLocation);

                var directoryAbove = FlatRedBall.IO.FileManager.GetDirectory(directory);
                if(FlatRedBall.IO.FileManager.GetAllFilesInDirectory(directoryAbove).Any(item=>item.EndsWith("Run FlatRedBall.bat")))
                {
                    directory = directoryAbove;
                }

                // Create a command line script which will delete the current directory, then unzip from destination to the folder of the current directory
                var command =
    @"timeout /T 3 /NOBREAK & ";

                // delete:
                command += $"rd \"{directory}\" /S /Q & ";

                command += $"powershell -Command \"Expand-Archive -Path '{destination}' -DestinationPath '{directory}'\" & ";

                command += $"cd \"{directory}\" & ";
                command += $"\"{directory}\\Run FlatRedBall.bat\"";
                var processStartInfo = new ProcessStartInfo("cmd.exe");
                processStartInfo.Arguments = "/K " + command;

                Process.Start(processStartInfo);

                GlueCommands.Self.CloseGlue();
            }
        }

        internal async void DoInstallDailyBuild()
        {
            var result = DialogService.ShowConfirm("This will download the latest daily build and install it. This will overwrite the current install. Daily builds are unstable and not officially supported - are you sure you want to do this?");
            if (result == DialogButton.Yes)
            {
                IsDownloadingDailyBuild = true;
                var location = "https://github.com/vchelaru/FlatRedBall/releases/download/glue-daily/GlueDailyBuild.zip";
                var destination = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GlueDailyBuild.zip");

                var succeeded = await DownloadWithProgress(location, destination, status => DailyBuildDownloadStatusText = status);

                IsDownloadingDailyBuild = false;

                if (!succeeded)
                {
                    return;
                }

                // Daily builds are a flat zip of GlueFormsCore's own bin folder (no "Run FlatRedBall.bat" wrapper),
                // so it always installs directly into the folder of the currently running exe.
                var currentExeLocation = System.Reflection.Assembly.GetEntryAssembly().Location;
                var directory = FlatRedBall.IO.FileManager.GetDirectory(currentExeLocation);

                var command = @"timeout /T 3 /NOBREAK & ";
                command += $"rd \"{directory}\" /S /Q & ";
                command += $"powershell -Command \"Expand-Archive -Path '{destination}' -DestinationPath '{directory}'\" & ";
                command += $"cd \"{directory}\" & ";
                command += $"\"{directory}\\GlueFormsCore.exe\"";
                var processStartInfo = new ProcessStartInfo("cmd.exe");
                processStartInfo.Arguments = "/K " + command;

                Process.Start(processStartInfo);

                GlueCommands.Self.CloseGlue();
            }
        }

        private static async System.Threading.Tasks.Task<bool> DownloadWithProgress(string location, string destination, Action<string> reportStatus)
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(location, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Error: " + response.StatusCode);
                return false;
            }

            var contentLength = response.Content.Headers.ContentLength;

            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var outputStream = System.IO.File.OpenWrite(destination))
            {
                var buffer = new byte[8192];
                var totalBytesRead = 0L;
                var bytesRead = 0;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    totalBytesRead += bytesRead;
                    var percentage = totalBytesRead * 1d / (contentLength ?? totalBytesRead) * 100;

                    outputStream.Write(buffer, 0, bytesRead);

                    reportStatus($"{ToMem(totalBytesRead)} / {ToMem(contentLength)}\n{percentage:0.0}%");
                }
            }

            return true;
        }

        static string ToMem(long? bytes)
        {
            if (bytes == null)
            {
                return "Unknown";
            }
            else if (bytes < 1024)
            {
                return $"{bytes}b";
            }
            else if (bytes < 1024 * 1024)
            {
                return $"{bytes / 1024}kb";
            }
            //else if(bytes < 1024L * 1024L * 1024L)
            {
                var mb = bytes / (1024 * 1024);

                var extraKb = bytes - (mb * 1024 * 1024);

                var hundredKb = extraKb / (100 * 1000);

                return $"{bytes / (1024 * 1024)}.{hundredKb:0}mb";
            }
        }



        internal async void RefreshVersionInfo()
        {
            
            var location = "https://files.flatredball.com/content/FrbXnaTemplates/DailyBuild/FRBDK.zip";

            HttpResponseMessage response = null;

            try
            {
                await GlueCommands.Self.TryMultipleTimes(async () =>
                {
                    using var client = new HttpClient();
                    var request = new HttpRequestMessage(HttpMethod.Head, new Uri(location));
                    response = await client.SendAsync(request);
                });
            }
            catch(Exception)
            {
                // do nothing, perhaps offline?
            }
            if (response?.IsSuccessStatusCode == true)
            {
                if (response.Headers.TryGetValues("Last-Modified", out var values))
                {
                    var lastModified = values.FirstOrDefault();
                    DateTime dateTime;
                    if (DateTime.TryParse(lastModified, out dateTime))
                    {
                        LatestVersionOnline = dateTime.ToLocalTime();
                    }
                }
                else if(response.Content.Headers.TryGetValues("Last-Modified", out var contentValues))
                {
                    var lastModified = contentValues.FirstOrDefault();
                    DateTime dateTime;
                    if (DateTime.TryParse(lastModified, out dateTime))
                    {
                        LatestVersionOnline = dateTime.ToLocalTime();
                    }
                }
            }
        }

        internal async void RefreshDailyBuildInfo()
        {
            var location = "https://github.com/vchelaru/FlatRedBall/releases/download/glue-daily/GlueDailyBuild.zip";

            HttpResponseMessage response = null;

            try
            {
                await GlueCommands.Self.TryMultipleTimes(async () =>
                {
                    using var client = new HttpClient();
                    var request = new HttpRequestMessage(HttpMethod.Head, new Uri(location));
                    response = await client.SendAsync(request);
                });
            }
            catch (Exception)
            {
                // do nothing, perhaps offline?
            }
            if (response?.IsSuccessStatusCode == true)
            {
                if (response.Headers.TryGetValues("Last-Modified", out var values) ||
                    response.Content.Headers.TryGetValues("Last-Modified", out values))
                {
                    var lastModified = values.FirstOrDefault();
                    if (DateTime.TryParse(lastModified, out var dateTime))
                    {
                        LatestDailyBuildOnline = dateTime.ToLocalTime();
                    }
                }
            }
        }
    }
}
