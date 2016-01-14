#region Using Statements

using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;

#endregion

namespace FlatRedBall.IO
{
    /// <summary>This class let's you easily make HTTP requests and, if required,
    /// deserialize the results into local strongly typed objects. Callbacks called
    /// on separate thread.</summary>
    public static class HttpManager
    {
        static int mNextRequestId = 0;

        public static string UserAgent = "FlatRedBall Windows Phone Client";

        /// <summary>Simple HTTP Get, gives you the string result</summary>
        public static int PerformHttpGet(string url, Action<string, int> action, Action<Exception, int> error)
        {
            return PerformHttpGet(new Uri(url, UriKind.Absolute), action, error);
        }

        /// <summary>Simple HTTP Get, gives you the string result</summary>
        public static int PerformHttpGet(Uri uri, Action<string, int> action, Action<Exception, int> error)
        {
            int requestId = mNextRequestId;

            mNextRequestId++;

#if WINDOWS_PHONE
            var request = WebRequest.CreateHttp(uri);
#else
            var request = (HttpWebRequest)WebRequest.Create(uri);
#endif
            request.UserAgent = UserAgent;

            request.BeginGetResponse(i =>
            {
                try
                {
                    var response = request.EndGetResponse(i);
                    var sreader = new StreamReader(response.GetResponseStream());
                    var result = sreader.ReadToEnd();
                    action(result, requestId);
                }
                catch (Exception ex)
                {
                    if (error != null) error(ex, requestId);
                    else throw;
                }
            }, null);

            return requestId;
        }

        /// <summary>Does an HTTP Get, and deserializes the XML result (assumes UTF8) with the File Manager Deserializer.</summary>
        public static int PerformHttpGetAsXml<T>(string url, Action<T, int> action, Action<Exception, int> error)
        {
            return PerformHttpGetAsXml<T>(new Uri(url), action, error);
        }

        /// <summary>Does an HTTP Get, and deserializes the XML result (assumes UTF8) with the File Manager Deserializer.</summary>
        public static int PerformHttpGetAsXml<T>(Uri uri, Action<T, int> action, Action<Exception, int> error)
        {

            int returnValue = PerformHttpGet(uri, (xml, id) =>
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(xml);

                    try
                    {
                        using (var stream = new MemoryStream(bytes))
                        {
                            T deserialized = FileManager.XmlDeserialize<T>(stream);

                            action(deserialized, id);
                        }
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        //string text = ByteArrayToString(bytes);

#endif
                        if (error != null) error(ex, id);
                        else throw;
                    }
                }, error);

            return returnValue;
        }

        public static string ByteArrayToString(byte[] bytes)
        {
            System.Text.Encoding encoding = null;

                    //encoding = new System.Text.ASCIIEncoding();
                    //encoding = new System.Text.UnicodeEncoding();
                    //encoding = new System.Text.UTF7Encoding();
                    encoding = new System.Text.UTF8Encoding();
            return encoding.GetString(bytes, 0, bytes.Length);
        } 

        /// <summary>Does an HTTP Get, and deserializes the JSON result (assumes UTF8) with the .</summary>
        public static int PerformHttpGetAsJson<T>(string url, Action<T, int> action, Action<Exception, int> error)
        {
            return PerformHttpGetAsJson<T>(new Uri(url), action, error);
        }

        /// <summary>Does an HTTP Get, and deserializes the JSON result (assumes UTF8) with the .</summary>
        public static int PerformHttpGetAsJson<T>(Uri uri, Action<T, int> action, Action<Exception, int> error)
        {
            int returnValue = PerformHttpGet(uri, (json, id) =>
            {
                try
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                    byte[] bytes = Encoding.UTF8.GetBytes(json);
                    using (var stream = new MemoryStream(bytes))
                    {
                        var deserialized = serializer.ReadObject(stream);

                        action((T)deserialized, id);
                    }
                }
                catch (Exception ex)
                {
                    if (error != null) error(ex, id);
                    else throw;
                }
            }, error);

            return returnValue;
        }
    }
}
