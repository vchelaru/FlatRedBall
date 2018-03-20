using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using FlatRedBall.IO;
using System.Windows.Forms;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace FRBDKUpdater
{
    public class DownloaderProgressEventArgs
    {
        public DownloaderProgressEventArgs(int progressPercentage, string speed)
        {
            ProgressPercentage = progressPercentage;
            Speed = speed;
        }

        public int ProgressPercentage { get; private set; }
        public string Speed { get; private set; }
    }

    public class DownloaderErrorEventArgs
    {
        public DownloaderErrorEventArgs(string error)
        {
            Error = error;
        }

        public string Error { get; set; }
    }

    public class DownloaderCompleteEventArgs
    {
        public DownloaderCompleteEventArgs(bool successful)
        {
            Successful = successful;
        }

        public bool Successful { get; protected set; }
    }

    public delegate void DownloaderProgressEventHandler(
        object sender, DownloaderProgressEventArgs e);

    public delegate void DownloaderErrorEventHandler(
        object sender, DownloaderErrorEventArgs e);

    public delegate void DownloaderCompleteEventHandler(
        object sender, DownloaderCompleteEventArgs e);

    public class Downloader
    {
        private readonly Settings mSettings;
        private BackgroundWorker mBackgroundWorker;
        private DateTime _remoteFileTimeStamp;
        private bool mHasErrorOccured;

        public event DownloaderProgressEventHandler ReportProgress;
        public event DownloaderCompleteEventHandler DownloadComplete;
        public event DownloaderErrorEventHandler ErrorOccured;

        public Downloader(Settings settings)
        {
            mSettings = settings;
        }

        public void Start()
        {


            ThreadStart threadStart = new ThreadStart(() =>
            {
                PerformDownload();
                BackgroundWorkerOnRunWorkerCompleted();
            });
            Thread thread = new Thread(threadStart);
            thread.Start();

            // September 30, 2017
            // For some reason the
            // BackgroundWorker never
            // raised its RunWorkerComplete
            // event when downloading Anfloga.
            // I couldn't figure out why but...
            // black boxes suck so I just did it
            // with threads instead.
            //mBackgroundWorker = new BackgroundWorker();
            //mBackgroundWorker.DoWork += BackgroundWorkerOnDoWork;
            //mBackgroundWorker.RunWorkerCompleted += BackgroundWorkerOnRunWorkerCompleted;

            //mBackgroundWorker.RunWorkerAsync();
        }

        private void BackgroundWorkerOnRunWorkerCompleted()
        {
            if (DownloadComplete != null)
                DownloadComplete(null, new DownloaderCompleteEventArgs(!mHasErrorOccured));
        }

        private void PerformDownload()
        {
            

            mHasErrorOccured = false;
            try
            {
                Logger.Log("Downloading from " + mSettings.Url);

                // 4 failures @ 400 ms each was not enough
                // for brake neck
                int maxFailures = 5;
                int msToSleepOn0Length = 800;
                int numberOfFailures = 0;

                HttpWebResponse response = null;
                long fileSize = 0;

                while (numberOfFailures < maxFailures)
                {
                    response = GetResponseForRequest();
                    fileSize = response.ContentLength;

                    if (fileSize <= 0)
                    {
                        System.Threading.Thread.Sleep(msToSleepOn0Length);
                        numberOfFailures++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (fileSize <= 0)
                {
                    mHasErrorOccured = true;
                    if (ErrorOccured != null)
                    {
                        ErrorOccured(this, new DownloaderErrorEventArgs(
                            $"File size for .zip was not greater than 0. Attempted to download the file {maxFailures} times."));
                    }
                }

                if (!mHasErrorOccured)
                {
                    Logger.Log("File size: " + fileSize);


                    _remoteFileTimeStamp = response.LastModified;
                    Logger.Log("Remote file modified date: " + _remoteFileTimeStamp);




                    using (var mClient = new WebClient())
                    {
                        if (!AlreadyDownloaded())
                        {
                            var webStream = mClient.OpenRead(new Uri(mSettings.Url));

                            string saveFile;
                            FileStream fileStream;
                            GetSaveInformation(out saveFile, out fileStream);

                            var byteBuffer = new byte[fileSize];

                            if (webStream != null)
                            {
                                Logger.Log("Downloading now...");

                                ReadAllBytes(webStream, fileStream, byteBuffer);

                                if (ReportProgress != null)
                                    ReportProgress(this,
                                                    new DownloaderProgressEventArgs(100, "Saving file..."));

                                Logger.Log("Downloading complete.  File saved to " + saveFile);

                            }

                            fileStream.Close();
                            if (webStream != null) webStream.Close();

                            if (!File.Exists(mSettings.LocalFileForTimeStamp))
                            {
                                try
                                {
                                    var directory = FileManager.GetDirectory(mSettings.LocalFileForTimeStamp);
                                    System.IO.Directory.CreateDirectory(directory);
                                    System.IO.File.WriteAllText(mSettings.LocalFileForTimeStamp,
                                        _remoteFileTimeStamp.ToString());
                                }
                                catch (Exception e)
                                {
                                    // Who cares?  Ignore this error, it just means FRB will act as if it's not downloaded
                                    // next time
                                }
                            }
                        }
                        else
                        {
                            ReportProgress(this, new DownloaderProgressEventArgs(
                                                        100, ""));
                        }
                    }
                }
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.NameResolutionFailure)
                {
                    ErrorOccured(this, new DownloaderErrorEventArgs("Could not access FlatRedBall.com."));
                    mHasErrorOccured = true;
                }
                else
                {
                    HandleErrorDefault(webException);
                }
            }
            catch (Exception ex)
            {
                HandleErrorDefault(ex);
            }
        }

        private int ReadAllBytes(Stream webStream, FileStream fileStream, byte[] byteBuffer)
        {
            var start = DateTime.Now;

            var bytesDownloaded = 0;
            int bytesRead;
            while ((bytesRead = webStream.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
            {
                fileStream.Write(byteBuffer, 0, bytesRead);
                bytesDownloaded += bytesRead;

                var speed = (int)((bytesDownloaded / 1000f) / ((DateTime.Now - start).TotalMilliseconds / 1000f)) +
                         @" kb/s        " + (bytesDownloaded / (double)(1024 * 1024)).ToString("0.00") + "/" +
                         (byteBuffer.Length / (double)(1024 * 1024)).ToString("0.00") + " MB";

                if (ReportProgress != null)
                    ReportProgress(this,
                                    new DownloaderProgressEventArgs(
                                        (int)(((double)bytesDownloaded / byteBuffer.Length) * 100), speed));
            }
            return bytesDownloaded;
        }

        private void GetSaveInformation(out string saveFile, out FileStream fileStream)
        {
            saveFile = mSettings.SaveFile;
            if (string.IsNullOrEmpty(saveFile))
            {
                throw new Exception("Settings SaveFile was null, and it shouldn't be");
            }
            string directory = Path.GetDirectoryName(saveFile);
            if (string.IsNullOrEmpty(directory))
            {
                directory = FileManager.UserApplicationDataForThisApplication;
            }
            Directory.CreateDirectory(directory);

            if (FileManager.IsRelative(saveFile))
            {
                saveFile = FileManager.UserApplicationDataForThisApplication + saveFile;
            }

            fileStream = null;

            try
            {
                fileStream = new FileStream(saveFile, FileMode.Create, FileAccess.Write, FileShare.None);
            }
            catch (UnauthorizedAccessException)
            {
                if (!FileManager.IsRelativeTo(saveFile, FileManager.UserApplicationDataForThisApplication))
                {
                    saveFile = FileManager.UserApplicationDataForThisApplication + FileManager.RemovePath(saveFile);
                }
            }
        }

        private HttpWebResponse GetResponseForRequest()
        {
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(AlwaysGoodCertificate);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                | SecurityProtocolType.Tls11
                | SecurityProtocolType.Tls12
                | SecurityProtocolType.Ssl3;

            var url = new Uri(mSettings.Url);
            var request = (HttpWebRequest)WebRequest.Create(url);
            var response = (HttpWebResponse)request.GetResponse();
            response.Close();
            return response;
        }

        private static bool AlwaysGoodCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            return true;
        }

        private void HandleErrorDefault(Exception ex)
        {
            if (ErrorOccured != null)
            {
                ErrorOccured(this, new DownloaderErrorEventArgs(@"Download for the following file has failed:\n\n" +
                    mSettings.Url + "\n\nException Info:\n" + ex));
            }
            mHasErrorOccured = true;
        }

        private bool AlreadyDownloaded()
        {
            //Check if download is forced
            if (mSettings.ForceDownload)
                return false;

            //Check to see if our on disk zip is up to date
            if (File.Exists(mSettings.LocalFileForTimeStamp) && File.Exists(mSettings.SaveFile))
            {
                Logger.Log("Local time stamp file exists so comparing against that...");
                string timestamp;

                using (var reader = new StreamReader(mSettings.LocalFileForTimeStamp))
                {
                    timestamp = reader.ReadToEnd();
                }

                DateTime lastAccess;

                if (DateTime.TryParse(timestamp, out lastAccess))
                {
                    if (lastAccess == _remoteFileTimeStamp)
                    {
                        Logger.Log("File is already downloaded - both remote and local dates are " + lastAccess);
                        return true;

                    }
                }

                Logger.Log("Time stamps don't match, so the file is not already downloaded");
                return false;
            }
            else
            {
                Logger.Log("There is no local timestamp file, so we're going to download the file");
                //Create directory since it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(mSettings.SaveFile));

                return false;
            }
        }
    }
}
