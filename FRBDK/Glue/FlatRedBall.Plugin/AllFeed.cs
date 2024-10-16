using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace FlatRedBall.Glue.Plugins.Rss
{
    [XmlRoot("channel")]
    public class AllFeed
    {
        [XmlElement("item")]
        public List<RssItem> Items { get; set; }

        public static void StartDownloadingInformation(string url, Action<AllFeed, DownloadState> callback)
        {
            Thread thread = new Thread(() =>
            {
                AllFeed toReturn = null;
                DownloadState result = DownloadState.Error;
                try
                {
                    // Create a request for the URL. 		
                    WebRequest request = WebRequest.Create(url);
                    // If required by the server, set the credentials.
                    request.Credentials = CredentialCache.DefaultCredentials;
                    // Get the response.
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (Stream dataStream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(dataStream))
                    {
                        // Read the content. 
                        string responseFromServer = reader.ReadToEnd();
                        toReturn = new AllFeed();
                        //toReturn = FileManager.XmlDeserializeFromString<AllFeed>(responseFromServer);
                        XDocument doc = XDocument.Parse(responseFromServer);

                        foreach (var xElement in doc.Element("rss").Element("channel").Elements("item"))
                        {
                            RssItem newItem = RssItem.FromXElement(xElement);
                            toReturn.Items.Add(newItem);
                        }
                    }

                    result = DownloadState.InformationDownloaded;
                }
                catch (Exception)
                {
                    result = DownloadState.Error;
                }
                callback(toReturn, result);
            });

            thread.Start();
        }

        public AllFeed()
        {
            Items = new List<RssItem>();
        }

    }
}
