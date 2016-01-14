using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace FlatRedBall.Glue.Authentication
{
    /// <summary>
    /// Submits post data to a url.
    /// </summary>
    /// <remarks>this class originally found here:
    /// http://geekswithblogs.net/rakker/archive/2006/04/21/76044.aspx</remarks>
    internal class PostSubmitter
    {
        /// <summary>
        /// determines what type of post to perform.
        /// </summary>
        public enum PostTypeEnum
        {
            /// <summary>
            /// Does a get against the source.
            /// </summary>
            Get,
            /// <summary>
            /// Does a post against the source.
            /// </summary>
            Post
        }

        private string m_url = string.Empty;
        private NameValueCollection m_values = new NameValueCollection();
        private PostTypeEnum m_type = PostTypeEnum.Get;
        /// <summary>
        /// Default constructor.
        /// </summary>
        public PostSubmitter()
        {
        }

        /// <summary>
        /// Constructor that accepts a url as a parameter
        /// </summary>
        /// <param name="url">The url where the post will be submitted to.</param>
        public PostSubmitter(string url)
            : this()
        {
            m_url = url;
        }

        /// <summary>
        /// Constructor allowing the setting of the url and items to post.
        /// </summary>
        /// <param name="url">the url for the post.</param>
        /// <param name="values">The values for the post.</param>
        public PostSubmitter(string url, NameValueCollection values)
            : this(url)
        {
            m_values = values;
        }

        /// <summary>
        /// Gets or sets the url to submit the post to.
        /// </summary>
        public string Url
        {
            get
            {
                return m_url;
            }
            set
            {
                m_url = value;
            }
        }
        /// <summary>
        /// Gets or sets the name value collection of items to post.
        /// </summary>
        public NameValueCollection PostItems
        {
            get
            {
                return m_values;
            }
            set
            {
                m_values = value;
            }
        }
        /// <summary>
        /// Gets or sets the type of action to perform against the url.
        /// </summary>
        public PostTypeEnum Type
        {
            get
            {
                return m_type;
            }
            set
            {
                m_type = value;
            }
        }
        /// <summary>
        /// Posts the supplied data to specified url.
        /// </summary>
        /// <returns>a string containing the result of the post.</returns>
        public string Post()
        {
            StringBuilder parameters = new StringBuilder();
            for (int i = 0; i < m_values.Count; i++)
            {
                EncodeAndAddItem(ref parameters, m_values.GetKey(i), m_values[i]);
            }
            string result = PostData(m_url, parameters.ToString());
            return result;
        }
        /// <summary>
        /// Posts the supplied data to specified url.
        /// </summary>
        /// <param name="url">The url to post to.</param>
        /// <returns>a string containing the result of the post.</returns>
        public string Post(string url)
        {
            m_url = url;
            return this.Post();
        }
        /// <summary>
        /// Posts the supplied data to specified url.
        /// </summary>
        /// <param name="url">The url to post to.</param>
        /// <param name="values">The values to post.</param>
        /// <returns>a string containing the result of the post.</returns>
        public string Post(string url, NameValueCollection values)
        {
            m_values = values;
            return this.Post(url);
        }
        /// <summary>
        /// Posts data to a specified url. Note that this assumes that you have already url encoded the post data.
        /// </summary>
        /// <param name="postData">The data to post.</param>
        /// <param name="url">the url to post to.</param>
        /// <returns>Returns the result of the post.</returns>
        private string PostData(string url, string postData)
        {
            HttpWebRequest request = null;
            if (m_type == PostTypeEnum.Post)
            {
                Uri uri = new Uri(url);
                request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = postData.Length;
                using (Stream writeStream = request.GetRequestStream())
                {
                    UTF8Encoding encoding = new UTF8Encoding();
                    byte[] bytes = encoding.GetBytes(postData);
                    writeStream.Write(bytes, 0, bytes.Length);
                }
            }
            else
            {
                Uri uri = new Uri(url + "?" + postData);
                request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = "GET";
            }
            string result = string.Empty;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader readStream = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        result = readStream.ReadToEnd();
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// Encodes an item and ads it to the string.
        /// </summary>
        /// <param name="baseRequest">The previously encoded data.</param>
        /// <param name="dataItem">The data to encode.</param>
        /// <returns>A string containing the old data and the previously encoded data.</returns>
        private void EncodeAndAddItem(ref StringBuilder baseRequest, string key, string dataItem)
        {
            if (baseRequest == null)
            {
                baseRequest = new StringBuilder();
            }
            if (baseRequest.Length != 0)
            {
                baseRequest.Append("&");
            }
            baseRequest.Append(key);
            baseRequest.Append("=");
            baseRequest.Append(System.Web.HttpUtility.UrlEncode(dataItem));
        }
    }
}
