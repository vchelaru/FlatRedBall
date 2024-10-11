using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using ToolsUtilities;
using System.Net.Http;
using System.Threading.Tasks;

namespace FRBDKUpdater
{
    public class DownloaderErrorEventArgs
    {
        public DownloaderErrorEventArgs(string error)
        {
            Error = error;
        }

        public string Error { get; set; }
    }

 
    public delegate void DownloaderErrorEventHandler(
        object sender, DownloaderErrorEventArgs e);

    public class Downloader
    {
        private readonly Settings mSettings;
        private DateTime _remoteFileTimeStamp;

        public event Action<long?, long> ReportProgress;
        //public event DownloaderCompleteEventHandler DownloadComplete;
        public event DownloaderErrorEventHandler ErrorOccured;

        public Downloader(Settings settings)
        {
            mSettings = settings;
        }

        public async Task<GeneralResponse> PerformDownload(CancellationToken token = default)
        {
            Logger.Log("Downloading from " + mSettings.Url);

            //HttpWebResponse response = null;
            //long fileSize = 0;

            //// If file size is <= 0, it may stil be a valid response
            //// so base it on OK code...
            //if(response.StatusCode != HttpStatusCode.OK)
            //{
            //    mHasErrorOccured = true;
            //    if (ErrorOccured != null)
            //    {
            //        ErrorOccured(this, new DownloaderErrorEventArgs(
            //                    $"File size for .zip was not greater than 0. Attempted to download the file {maxFailures} times."));
            //    }
            //}
            //if (fileSize <= 0)
            //{
            //}

            //if (!mHasErrorOccured)
            //{
            //Logger.Log("File size: " + fileSize);

            var networkManager = ToolsUtilitiesStandard.Network.NetworkManager.Self;

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromDays(1), };

            string saveFile;
            GetSaveInformation(out saveFile);

            void UpdateProgress(long? totalSize, long downloadedSoFar)
            {
                ReportProgress?.Invoke(totalSize, downloadedSoFar);
            }

            var downloadResponse = await networkManager.DownloadWithProgress(httpClient, mSettings.Url, 
                saveFile, UpdateProgress, token);

            //DownloadComplete(null, new DownloaderCompleteEventArgs(!mHasErrorOccured));
            //}
            return downloadResponse;

        }

        //private int ReadAllBytes(Stream webStream, FileStream fileStream, byte[] byteBuffer)
        //{
        //    var start = DateTime.Now;

        //    var bytesDownloaded = 0;
        //    int bytesRead;
        //    while ((bytesRead = webStream.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
        //    {
        //        fileStream.Write(byteBuffer, 0, bytesRead);
        //        bytesDownloaded += bytesRead;

        //        var speed = (int)((bytesDownloaded / 1000f) / ((DateTime.Now - start).TotalMilliseconds / 1000f)) +
        //                 @" kb/s        " + (bytesDownloaded / (double)(1024 * 1024)).ToString("0.00") + "/" +
        //                 (byteBuffer.Length / (double)(1024 * 1024)).ToString("0.00") + " MB";

        //        if (ReportProgress != null)
        //            ReportProgress(this,
        //                            new DownloaderProgressEventArgs(
        //                                (int)(((double)bytesDownloaded / byteBuffer.Length) * 100), speed));
        //    }
        //    return bytesDownloaded;
        //}

        private void GetSaveInformation(out string saveFile)
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
