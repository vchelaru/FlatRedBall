using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using FlatRedBall.IO;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System.Threading.Tasks;
using System.Net.Http;
using System.Runtime.CompilerServices;
using FlatRedBall.Glue.Managers;

namespace OfficialPlugins.FrbUpdater
{
    public partial class UpdateWindow : Form
    {
        private class FileData
        {
            public string ProjectType { get; set; }
            public string FileName { get; set; }

            public string DiskFile { get; set; }
            public string ServerFile { get; set; }
            public string ProjectFile { get; set; }
            public string TimestampFile { get; set; }

            public override string ToString()
            {
                return "From: " + ServerFile + " to " + DiskFile;
            }
        }

        private FrbUpdaterSettings _settings;

        private const string DailyBuildRemoteUri = "http://files.flatredball.com/content/FrbXnaTemplates/DailyBuild/SingleDlls/";
        private const string StartRemoteUri = "http://files.flatredball.com/content/FrbXnaTemplates/";

        private const string LibraryFolder = "Libraries";

        private string _savePath;           //Where to save files to
        private string _url;                //path on server
        private readonly string _userAppPath = FileManager.UserApplicationData; //User app data path.  When switching to admin, need to keep regular user path.
        private readonly List<FileData> mFoundFiles = new List<FileData>();
        private readonly List<FileData> mDownloadedFiles = new List<FileData>();

        private readonly FrbUpdaterPlugin _plugin;

        private string _speed = String.Empty;
        private string _fileName = String.Empty;
        private FileData _currentFile = null;

        private bool _downloadedFile = false;

        public UpdateWindow(FrbUpdaterPlugin plugin)
        {
            InitializeComponent();
            _plugin = plugin;
        }

        private async void FrmMainLoad(object sender, EventArgs e)
        {
            _settings = FrbUpdaterSettings.LoadSettings(_userAppPath);

            switch (_settings.SelectedSource)
            {
                case "Daily Build":
                    _url = DailyBuildRemoteUri;
                    _savePath = _userAppPath + @"\FRBDK\DailyBuild\SingleDlls";
                    break;
                case "Current":
                    var date = DateTime.Now;

                    // This seems to cause problems for the user, so we're going to put a limit on this.
                    // We don't want to go back more than 10 years, so that woul be 120 months.  I just randomly
                    // picked 10 years.
                    // Update - this actually hits the web server, so we should probably not do 10 years.  Let's go to 1 year
                    int numberOfTries = 0;
                    while (!await IsMonthValid(date) && numberOfTries < 1*12)
                    {
                        PluginManager.ReceiveOutput("Attempting to find valid month, but the following date is invalid: " + date);
                        date = date.AddMonths(-1);
                        numberOfTries++;
                    }

                    _url = StartRemoteUri + date.ToString("yyyy") + "/" + date.ToString("MMMM") + "/SingleDlls/";
                    _savePath = _userAppPath + @"\FRBDK\Current\SingleDlls\";
                    break;
                default:
                    //Month Year
                    //Ex: July 2011
                    var regex = new Regex(@"\w*\s\d\d\d\d");

                    if (regex.IsMatch(_settings.SelectedSource))
                    {
                        var items = _settings.SelectedSource.Trim().Split(' ');

                        var year = items[1].Trim();
                        var month = items[0].Trim();

                        _url = StartRemoteUri + year + "/" + month + "/SingleDlls/";
                        _savePath = _userAppPath + @"\FRBDK\" + year + @"\" + month + @"\SingleDlls\";
                    }
                    else
                    {
                        throw new Exception("Unknown Sync Point.");
                    }

                    break;
            }

            await Task.Run(() =>
            {
                PopulateFiles();

                DoAllDownloadLogic();

                this.Invoke(new ActionDelegate(Close));
            });
        }

        private async Task<bool> IsMonthValid(DateTime date)
        {
            const string path = "http://files.flatredball.com/content/FrbXnaTemplates/";
            HttpClient httpClient = new HttpClient();

            try
            {
                //This Month
                var curPath = path + date.ToString("yyyy") + "/" + date.ToString("MMMM") + "/FlatRedBallInstaller.exe";
                var url = new Uri(curPath);
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (HttpRequestException)
            {
                return false;
            }

        }

        private void PopulateFiles()
        {
            while (_plugin.GlueState.GetProjects() == null || _plugin.GlueState.GetProjects().Count == 0 || _plugin.GlueState.GetProjects()[0] == null)
            {
                return;
            }
            mFoundFiles.Clear();

            foreach (var project in _plugin.GlueState.GetProjects())
            {
                string directory = project.Directory + LibraryFolder + "/";
                if (Directory.Exists(directory))
                {
                    var allFiles = FileManager.GetAllFilesInDirectory(directory);

                    foreach (var absoluteFileName in allFiles)
                    {
                        string fileName = FileManager.MakeRelative(absoluteFileName, directory);

                        AddFileToFoundFiles(project, fileName, absoluteFileName);

                    }

                    // We used to explicitly add .dll and .xml files here, but
                    // some engines now have release and debug folders.  If so, we
                    // need to capture those, and I don't want to modify the code to
                    // have to handle those files explicitly - it should just check and
                    // download whatever exists.
                }
                else
                {
                    MessageBox.Show("Library Folder doesn't exist for project " + project.FullFileName);
                }
            }

        }

        private void AddFileToFoundFiles(FlatRedBall.Glue.VSHelpers.Projects.ProjectBase project, string fileName, string absoluteFileName)
        {
            mFoundFiles.Add(new FileData
            {
                ProjectType = project.FolderName,
                FileName = fileName,
                ProjectFile = absoluteFileName,
                ServerFile = _url + fileName,
                DiskFile = _savePath + @"\" + fileName,
                TimestampFile = _savePath + @"\" + FileManager.RemovePath(fileName) + "-timestamp.txt"
            });
        }

        private void DoDownloadAndSaveAllFiles(List<FileData> filesToDownload, List<FileData> successfullyDownloadedFiles)
        {
            bool cancelled = false;

            foreach (var fileData in filesToDownload)
            {
                if (cancelled)
                {
                    break;
                }

                _currentFile = fileData;

                var url = new Uri(fileData.ServerFile);

#pragma warning disable SYSLIB0014 // Type or member is obsolete. Fixing this is a pain since it would also require fixing DoDownloadAndSaveFile
                var request = (HttpWebRequest)WebRequest.Create(url);
#pragma warning restore SYSLIB0014 // Type or member is obsolete
                HttpWebResponse response = null;
                try
                {
                    response = (HttpWebResponse)request.GetResponse();

                    DoDownloadAndSaveFile(fileData, url, response);
                    PluginManager.ReceiveOutput("Successfully downloaded " + fileData.ServerFile);
                    successfullyDownloadedFiles.Add(fileData);
                    response.Close();
                }
                catch (WebException)
                {
                    // Victor Chelaru
                    // October 11, 2015
                    // We used to notify
                    // the user that a file
                    // failed to download, but
                    // many projects have files
                    // which cannot be updated through
                    // this command. This isn't a bad thing,
                    // so we don't want to tell the user that
                    // errors occurred:
                    //PluginManager.ReceiveError("Unable to download " + fileData.ServerFile);
                    continue;
                }

            }
        }

        private async void DoDownloadAndSaveFile(FileData fileData, Uri url, HttpWebResponse response)
        {
            int bytesDownloaded = 0;
            var fileSize = response.ContentLength;
            var fileTimeStamp = response.LastModified;

            using (var mClient = new HttpClient())
            {
                if (!AlreadyDownloaded(fileData, fileTimeStamp))
                {
                    using (var webStream = await mClient.GetStreamAsync(url))
                    {
                        using (var fileStream = new FileStream(fileData.DiskFile, FileMode.Create, FileAccess.Write,
                                                        FileShare.None))
                        {
                            _fileName = fileData.FileName;

                            int bytesRead = 0;
                            var byteBuffer = new byte[fileSize];

                            if (webStream != null)
                            {
                                var start = DateTime.Now;

                                while ((bytesRead = webStream.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
                                {
                                    fileStream.Write(byteBuffer, 0, bytesRead);
                                    bytesDownloaded += bytesRead;


                                    ActionDelegate updateDelegate = () =>
                                    {

                                        _speed =
                                            (int)
                                            ((bytesDownloaded / 1000f) / ((DateTime.Now - start).TotalMilliseconds / 1000f)) +
                                            @" kb/s        " + (bytesDownloaded / (double)(1024 * 1024)).ToString("0.00") +
                                            "/" +
                                            (byteBuffer.Length / (double)(1024 * 1024)).ToString("0.00") + " MB";
                                        lblSpeed.Text = _speed;
                                        lblFileName.Text = _fileName;
                                        pbValue.Value = (int)(100 * bytesDownloaded / (float)fileSize);

                                    };

                                    try
                                    {
                                        lblSpeed.Invoke(updateDelegate);
                                    }
                                    catch(InvalidOperationException)
                                    {
                                        // do nothing, at times this can happen before the view is visible. It's okay, just carry on...
                                    }


                                }
                            }
                        }
                    }

                    using (var writer = new StreamWriter(fileData.TimestampFile))
                    {
                        writer.Write(fileTimeStamp.ToString());
                    }

                    _downloadedFile = true;
                }
            }
        }

        private bool AlreadyDownloaded(FileData saveFile, DateTime fileTimestamp)
        {
            //Check to see if our on disk zip is up to date
            if (File.Exists(saveFile.TimestampFile) && File.Exists(saveFile.DiskFile))
            {
                string timestamp;

                using (var reader = new StreamReader(saveFile.TimestampFile))
                {
                    timestamp = reader.ReadToEnd();
                }

                DateTime lastAccess;

                if (DateTime.TryParse(timestamp, out lastAccess))
                {
                    if (lastAccess == fileTimestamp)
                        return true;
                }

                return false;
            }

            //Create directory since it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(saveFile.DiskFile));

            return false;
        }

        private void DoAllDownloadLogic()
        {

            try
            {
                DoDownloadAndSaveAllFiles(mFoundFiles, mDownloadedFiles);
            }
            catch (InvalidOperationException ioe)
            {
                if (_currentFile != null)
                {
                    MessageBox.Show($@"Download for the following file has failed:\n{_currentFile.ServerFile}\n{ioe}");
                }
                return;
            }

            try
            {
                //Copy files
                foreach (var fileData in mDownloadedFiles)
                {
                    GlueCommands.Self.TryMultipleTimes(() =>
                    {
                        File.Copy(fileData.DiskFile, fileData.ProjectFile, true);
                    });

                }
            }
            catch (Exception exception)
            {
                MessageBox.Show($@"Copy failed.\n\nException Info:\n" + exception);
                return;
            }
        }


        delegate void ActionDelegate();

        private void UpdateWorkerThreadRunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled)
            {
                if (_downloadedFile)
                {
                    ActionDelegate toInvoke = () => MessageBox.Show(@"Successfully downloaded and updated files!");
                    pbValue.Invoke(toInvoke);
                }
            }

            Close();
        }

        private void UpdateWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
        }
    }
}
