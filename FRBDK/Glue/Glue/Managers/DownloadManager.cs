using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;

namespace FlatRedBall.Glue.Managers
{
    public class DownloadManager : Singleton<DownloadManager>
    {
        public void DownloadFile(string source, string destination, 
            AsyncCompletedEventHandler completeHandler = null, DownloadProgressChangedEventHandler progressHandler = null)
        {
            WebClient webClient = new WebClient();
            if (completeHandler != null)
            {
                webClient.DownloadFileCompleted += completeHandler;
            }
            if (progressHandler != null)
            {
                webClient.DownloadProgressChanged += progressHandler;
            }
            webClient.DownloadFileAsync(new Uri(source), destination);
        }

        //private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        //{
        //    progressBar.Value = e.ProgressPercentage;
        //}

        //private void Completed(object sender, AsyncCompletedEventArgs e)
        //{
        //    MessageBox.Show("Download completed!");
        //}
    }
}
