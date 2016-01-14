using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

using System.Collections;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Threading;

using FlatRedBall.IO;

using System.Net;
#if !WINDOWS_PHONE
//using System.Windows.Forms;
#endif

/*Kevin Simpson : Currently to upload files without the directorly already needing to be made the
 * program has to recive a request from the server to determine if a directorly already exist. When
 * asked though, the way to determine if it exist or not is to read a error from the server, as a 
 * consequence, the flow of the methods is determined by exceptions. While this is a bad practice, there
 * may be no way around it, if a way is discovered in the future, it is recomended to upgrade.
 * */

namespace FlatRedBall.IO.Remote
{
    #region Structs

    public struct FileStruct
    {
        public string Flags;
        public string Owner;
        public string Group;
        public bool IsDirectory;
        public DateTime CreateTime;
        public string Name;

        public override string ToString()
        {
            return Name + " " + CreateTime + " " + Flags + " " + Owner;
        }

    }

#if !SILVERLIGHT && !WINDOWS_PHONE
    public class FtpState
    {
        private ManualResetEvent wait;
        private string fileName;
        private Exception operationException = null;
        string status;

        private FtpWebRequest request;

        public FtpState()
        {
            wait = new ManualResetEvent(false);
        }

        public ManualResetEvent OperationComplete
        {
            get { return wait; }
        }

        public FtpWebRequest Request
        {
            get { return request; }
            set { request = value; }
        }

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        public string TextToUpload
        {
            get;
            set;
        }

        public Exception OperationException
        {
            get { return operationException; }
            set { operationException = value; }
        }
        public string StatusDescription
        {
            get { return status; }
            set { status = value; }
        }


    }
#endif


    #endregion

    #region Enums

    public enum FileListStyle
    {
        UnixStyle,
        WindowsStyle,
        Unknown
    }


    #endregion

    #region Delegate definitions

    public delegate void FileListCompleteDelegate(FileStruct[] fileStructs);
    public delegate void GetStreamCompleteDelegate(Stream stream, object callback);
    public delegate void ObjectReturnCallback(object returnedObject);

    #endregion

    #region Classes

    public class FtpSaveEventArgs : EventArgs
    {
        public bool Succeeded;

        public FtpSaveEventArgs(bool succeeded)
        {
            Succeeded = succeeded;
        }
    }

    #endregion

    public class FtpManager
    {

        #region Fields

        static NetworkCredential mNetworkCredentials = new NetworkCredential();

#if SILVERLIGHT || WINDOWS_PHONE
        // Maybe we want to use this 
        static long mNextRequest = 0;
#endif

        static List<long> mLiveRequests = new List<long>();

        #endregion

        #region Properties

#if SILVERLIGHT || WINDOWS_PHONE
        public static bool CacheReads
        {
            get;
            set;
        }
#endif

        #endregion

        #region Methods

        #region Public Methods

#if SILVERLIGHT || WINDOWS_PHONE

        public static void CancelAllRequests()
        {
            lock (mLiveRequests)
            {
                mLiveRequests.Clear();
            }
        }

        static long GetReadStreamAsync(string ashxFile, string url, string userName, string password, GetStreamCompleteDelegate delegateToUse, object callback)
        {
            long requestID = mNextRequest;

            lock (mLiveRequests)
            {
                mNextRequest++;
                mLiveRequests.Add(requestID);
            }
            string datastring = "";

            string serviceUrl = ashxFile;
            

            UriBuilder ub = new UriBuilder(serviceUrl);

            WebClient c = new WebClient();
            
            string filenameQuery = 
                "uri=" + url;

            string usernameQuery =
                "username=" + userName;

            string passwordQuery = "password=" + password;

            if (CacheReads)
            {
                ub.Query = string.Format("{0}&{1}&{2}", filenameQuery, usernameQuery, passwordQuery);
            }
            else
            {
                ub.Query = string.Format("{0}&{1}&{2}", filenameQuery, usernameQuery, passwordQuery) + "&nocache=" + Environment.TickCount;
            }

            c.OpenReadCompleted += (sender, e) =>
            {
                try
                {
                    if(mLiveRequests.Contains(requestID))
                    {
                        delegateToUse(e.Result, callback);
                    }

                }
                catch (Exception exc)
                {
                    delegateToUse(null, callback);
                }
                finally
                {
                    lock (mLiveRequests)
                    {
                        mLiveRequests.Remove(requestID);
                    }
                }
            };

            c.OpenReadAsync(ub.Uri);
            c.AllowReadStreamBuffering = true;

            return requestID;
        }

        static void GetFileListAsync(Stream stream, object callback)
        {

            using (StreamReader streamReader = new StreamReader(stream, Encoding.UTF8))
            {

                string datastring = streamReader.ReadToEnd();

                stream.Close();
                streamReader.Close();

                ((FileListCompleteDelegate)callback)(GetFileStructFromDataString(datastring));
                //mFileListCompleteDelegate(GetFileStructFromDataString(datastring));
            }
        }

        //static FileListCompleteDelegate mFileListCompleteDelegate;
        public static long GetListAsync(string url, string userName, string password, FileListCompleteDelegate completeDelegate)
        {
            return GetReadStreamAsync("http://avillagelife.com/services/ftpgetfilelist.ashx", url, userName, password, new GetStreamCompleteDelegate( GetFileListAsync), completeDelegate);
        }
#else
        public static FileStruct[] GetList(string url, string userName, string password)
        {
            // url might be something like "ftp://ftp.flatredball.com/flatredball.com/";

            FtpWebRequest request = GetFtpWebRequest(url, userName, password,
                WebRequestMethods.Ftp.ListDirectoryDetails, false);

            FtpWebResponse response = request.GetResponse() as FtpWebResponse;

            StreamReader sr = new StreamReader(response.GetResponseStream(), System.Text.Encoding.ASCII);
            string datastring = sr.ReadToEnd();
            response.Close();
            return GetFileStructFromDataString(datastring);

        }
#endif

        private static FileStruct[] GetFileStructFromDataString(string datastring)
        {
            List<FileStruct> myListArray = new List<FileStruct>();

            string[] dataRecords = datastring.Split('\n');
            FileListStyle _directoryListStyle = GuessFileListStyle(dataRecords);
            foreach (string s in dataRecords)
            {
                if (_directoryListStyle != FileListStyle.Unknown && s != "")
                {
                    FileStruct f = new FileStruct();
                    f.Name = "..";
                    switch (_directoryListStyle)
                    {
                        case FileListStyle.UnixStyle:
                            f = ParseFileStructFromUnixStyleRecord(s);
                            break;
                        case FileListStyle.WindowsStyle:
                            f = ParseFileStructFromWindowsStyleRecord(s);
                            break;
                    }
                    if (!(f.Name == "." || f.Name == ".."))
                    {
                        myListArray.Add(f);
                    }
                }
            }
            return myListArray.ToArray(); ;
        }


        public static bool IsFtp(string url)
        {
            return url.StartsWith("ftp://");
        }

        public static void SetNetworkCredentials(string username, string password)
        {
            mNetworkCredentials.UserName = username;
            mNetworkCredentials.Password = password;
        }

        public static void XmlSerializeFtp<T>(T objectToSerialize, string url, string username, string password)
        {
            XmlSerializeFtp(objectToSerialize, url, username, password, null);
        }

        public static void XmlSerializeFtp<T>(T objectToSerialize, string url, string username, string password, EventHandler<FtpSaveEventArgs> resultEventHandler)
        {
            string outputString = "";

            FileManager.XmlSerialize<T>(objectToSerialize, out outputString);

            SaveText(outputString, url, username, password, resultEventHandler);
        }

        public static void SaveText(string stringToSave, string url, string username, string password)
        {
            SaveText(stringToSave, url, username, password, null);
        }

        public static void SaveText(string stringToSave, string url, string username, string password, EventHandler<FtpSaveEventArgs> resultEventHandler)
        {
            ThrowExceptionIfFtpUrlIsBad(url);

#if SILVERLIGHT || WINDOWS_PHONE

            string serviceUrl = "http://66.116.162.50/Handler.ashx";
            

            UriBuilder ub = new UriBuilder(serviceUrl);

            WebClient c = new WebClient();
            
            string filenameQuery = 
                "filename=" + url;

            string usernameQuery =
                "username=" + username;

            string passwordQuery = "password=" + password;


            ub.Query = string.Format("{0}&{1}&{2}", filenameQuery, usernameQuery, passwordQuery);

            c.OpenWriteCompleted += (sender, e) =>
            {
                try
                {
                    Stream stream = e.Result;

                    byte[] bytes = System.Text.UTF8Encoding.UTF8.GetBytes(stringToSave);

                    stream.Write(bytes, 0, bytes.Length);
                    stream.Close();
                    if (resultEventHandler != null)
                    {
                        resultEventHandler(null, new FtpSaveEventArgs(true));
                    }
                    
                }
                catch (Exception exc)
                {
                    int m = 3;

                    if (resultEventHandler != null)
                    {
                        resultEventHandler(null, new FtpSaveEventArgs(false));
                    }
                }
            };

            c.OpenWriteAsync(ub.Uri);

#else

            FtpWebRequest request = GetFtpWebRequest(url, username, password, WebRequestMethods.Ftp.UploadFile, false);

            Stream requestStream = request.GetRequestStream();

            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(stringToSave);

            requestStream.Write(byteArray, 0, byteArray.Length);

            requestStream.Close();
#endif
        }
#if !SILVERLIGHT && !WINDOWS_PHONE
        public static FtpWebResponse DeleteRemoteFile(string url, string username, string password)
        {
            ThrowExceptionIfFtpUrlIsBad(url);


            FtpWebRequest request = GetFtpWebRequest(url, username, password, WebRequestMethods.Ftp.DeleteFile, false);


            FtpWebResponse responseFileDelete = (FtpWebResponse)request.GetResponse();


            return responseFileDelete;
            


        }
#endif
        private static void ThrowExceptionIfFtpUrlIsBad(string url)
        {
            if (!url.StartsWith("ftp://"))
            {
                throw new ArgumentException("The URL \n" + url + "\ndoes not begin with \n\"ftp://\"  \nThis is a requirement");
            }
        }







#if SILVERLIGHT || WINDOWS_PHONE
        //static ObjectReturnCallback mTemporaryObjectReturnCallback;
        public static long XmlDeserialize<T>(string url, string userName, string password, ObjectReturnCallback callback)
        {
        //    mTemporaryObjectReturnCallback = callback;
            return GetReadStreamAsync("http://avillagelife.com/services/ftpgetfile.ashx", url, userName, password, GetObjectAsync<T>, callback);

        }

        static void GetObjectAsync<T>(Stream stream, object callback)
        {

            T objectToReturn = FileManager.XmlDeserialize<T>(stream);
            if (stream != null)
            {
                stream.Close();
                stream.Dispose();
            }

            ((ObjectReturnCallback)callback)(objectToReturn);
            //mTemporaryObjectReturnCallback(objectToReturn);
        }
#endif

#if !SILVERLIGHT && !WINDOWS_PHONE
        public static T XmlDeserialize<T>(string url, string userName, string password)
        {
            ThrowExceptionIfFtpUrlIsBad(url);
            Uri uri = new Uri(url, UriKind.Absolute);

            FtpWebRequest request = GetFtpWebRequest(
                url, userName, password, WebRequestMethods.Ftp.DownloadFile, false);

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Stream ftpStream = response.GetResponseStream();

            XmlSerializer serializer = FileManager.GetXmlSerializer(typeof(T));

            T objectToReturn = (T)serializer.Deserialize(ftpStream);

            ftpStream.Close();

            response.Close();

            return objectToReturn;


        }

        /// <summary>
        /// Downloads a file locally
        /// </summary>
        /// <param name="url">The remote file to download.  For example: "ftp://ftp.flatredball.com/flatredball.com/";</param>
        /// <param name="localFile">The local file name to download to</param>
        /// <param name="userName">FTP username</param>
        /// <param name="password">FTP password</param>
        public static void SaveFile(string url, string localFile, string userName, string password)
        {
            ThrowExceptionIfFtpUrlIsBad(url);
            Uri uri = new Uri(url, UriKind.Absolute);

            FtpWebRequest request = GetFtpWebRequest(
                url, userName, password, WebRequestMethods.Ftp.DownloadFile, false, true);

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Stream ftpStream = response.GetResponseStream();

            string directoryToCreate = FileManager.GetDirectory(localFile);
            if (!string.IsNullOrEmpty(directoryToCreate))
            {
                Directory.CreateDirectory(directoryToCreate);
            }

            if (File.Exists(localFile))
            {
                File.Delete(localFile);
            }

            FileStream fileStream = File.OpenWrite(localFile);

            

            byte[] buffer = new byte[200000];

            int amountRead = 0;

            while ((amountRead = ftpStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                fileStream.Write(buffer, 0, amountRead);
            }

            fileStream.Close();
            ftpStream.Close();
            response.Close();
        }
#else



        public static void UploadFile(string localFileToUpload, string targetUrl, string userName, string password)
        {

            if (!targetUrl.StartsWith("ftp://"))
            {
                throw new ArgumentException("The URL \n" + targetUrl + "\ndoes not begin with \n\"ftp://\"  \nThis is a requirement");
            }


            string serviceUrl = "http://66.116.162.50/Handler.ashx";


            UriBuilder ub = new UriBuilder(serviceUrl);

            WebClient c = new WebClient();
            
            string filenameQuery =
                "filename=" + targetUrl;

            string usernameQuery =
                "username=" + userName;

            string passwordQuery = "password=" + password;


            ub.Query = string.Format("{0}&{1}&{2}", filenameQuery, usernameQuery, passwordQuery);

            c.OpenWriteCompleted += (sender, e) =>
            {
                try
                {
                    Stream stream = e.Result;

                    Stream fileStream = FileManager.GetStreamForFile("$ISOLATEDSTORAGE\\" + localFileToUpload);

                    byte[] buffer = new byte[fileStream.Length];

                    stream.Write(buffer, 0, buffer.Length);
                    stream.Close();
                    
                }
                catch (Exception exc)
                {
                    int m = 3;
                }
            };

            c.OpenWriteAsync(ub.Uri);
            
        }

#endif

#if !SILVERLIGHT && !WINDOWS_PHONE

        public static void UploadFile(string localFileToUpload, string targetUrl, string userName, string password, bool keepAlive)
        {

            FtpState state = new FtpState();

            state.FileName = localFileToUpload;


            StartUploadUsingFtpState(targetUrl, userName, password, keepAlive, state);
        }



        private static void StartUploadUsingFtpState(string targetUrl, string userName, string password, bool keepAlive, FtpState state)
        {
            ManualResetEvent waitObject;

            FtpWebRequest request;

            bool fileUploaded = false;
            do
            {
                try
                {
                    fileUploaded = true;

                    request = GetFtpWebRequest(targetUrl, userName, password, WebRequestMethods.Ftp.UploadFile, keepAlive);
                    waitObject = state.OperationComplete;
                    state.Request = request;

                    request.BeginGetRequestStream(
                        new AsyncCallback(EndGetStreamCallback),
                        state
                    );
                    state.Request.GetResponse();
                    waitObject.WaitOne();
                }
                catch (WebException e)
                {
                    if (e.ToString().Contains("550"))
                    {
                        fileUploaded = false;
                        CreateDirectory(userName, password, ReturnBaseDirectory(targetUrl), 2);
                    }
                    else
                    {
                        throw e;
                    }
                }
            } while (!fileUploaded);
        }

        public static void UploadFile(string localFileToUpload, string targetUrl, string userName, string password)
        {
            UploadFile(localFileToUpload, targetUrl, userName, password, false);
        }

        public static void UploadFile(string localFileToUpload, string url, string userName, string password, string ftpFileName)
        {
            UploadFile(localFileToUpload, url, userName, password, ftpFileName, false);
        }

        public static void UploadFile(string localFileToUpload, string url, string userName, string password, string ftpFileName, bool keepAlive)
        {
            UploadFile(localFileToUpload, url + "/" + ftpFileName, userName, password);
        }

        public static void CreateDirectory(string userName, string password, string ftpDirectoryName)
        {
            CreateDirectory(userName, password, ftpDirectoryName, 1);

        }

        public static void CreateDirectory(string userName, string password, string ftpDirectoryName, int numberOfTimesToTry)
        {
            bool DirectoryCreated = true;
            if (ftpDirectoryName.Contains("ftp://"))
            {
                ftpDirectoryName = ftpDirectoryName.Replace("ftp://", "www.");

            }
            do
            {
                try
                {
                    numberOfTimesToTry--;

                    DirectoryCreated = true;
                    WebRequest request = WebRequest.Create("ftp://" + userName + ":" + password + "@" + ftpDirectoryName);
                    request.Proxy = null;
                    request.Method = WebRequestMethods.Ftp.MakeDirectory;
                    using (var resp = (FtpWebResponse)request.GetResponse()) { }
                }
                catch (WebException e)
                {
                    if (e.ToString().Contains("550"))
                    {
#if DEBUG
                        Console.WriteLine("Current Directory " + ftpDirectoryName + "'s parent does not exist, creating now");
#endif
                        CreateDirectory(userName, password, ReturnBaseDirectory(ftpDirectoryName));
                        DirectoryCreated = false;
                    }
                    else
                    {
                        if (numberOfTimesToTry == 0)
                        {
                            throw new Exception("Error", e);
                        }
                        else
                        {
                            System.Threading.Thread.Sleep(10);
                            CreateDirectory(userName, password, ftpDirectoryName, numberOfTimesToTry);
                        }
                    }
                }
            } while (!DirectoryCreated);
        }


        //Method for returning one directory up from the current directory of a URL, don't know if this is the best place for 
        //this method. \(0.o)/ <-- me being punny 
        public static string ReturnBaseDirectory(string url)
        {
            //URL that is returned, so if the url is One/Two/Three, this will return One/Two
            string urlToReturn = "";
            string[] urlContents = new string[url.Length];
            //Assign each index of the array to a letter in the given url
            for (int i = 0; i < url.Length; ++i)
            {
                urlContents[i] = url[i].ToString();
            }
            //Run through every letter starting from the end and find the first occurance of \ or /
            for (int i = url.Length - 1; i > 0; --i)
            {
                if (urlContents[i].Equals("/") || urlContents[i].Equals(@"\"))
                {
                    //Nice, we found it! Now that we have it we can delete all the characters after and including the \ or /
                    for (int j = i; j < url.Length; ++j)
                    {
                        urlContents[j] = "";
                    }

                    break;
                }
            }
            //Now, set the URL that we are goign to return to the new URL contained in the string array. 
            for (int i = 0; i < url.Length; ++i)
            {
                urlToReturn += urlContents[i];
            }
            return urlToReturn;
        }
#endif

        #endregion

        #region Private Methods

        private static string CutSubstringFromStringWithTrim(ref string s, char c, int startIndex)
        {
            int pos1 = s.IndexOf(c, startIndex);
            string retString = s.Substring(0, pos1);
            s = (s.Substring(pos1)).Trim();
            return retString;
        }

#if !SILVERLIGHT && !WINDOWS_PHONE
        private static FtpWebRequest GetFtpWebRequest(string url, string userName, string password, string method, bool keepAlive)
        {
            return GetFtpWebRequest(url, userName, password, method, keepAlive, false);
        }

        private static FtpWebRequest GetFtpWebRequest(string url, string userName, string password, string method, bool keepAlive, bool forceBinary)
        {
            if (!url.StartsWith("ftp://"))
            {
                throw new ArgumentException("The URL \n" + url + "\ndoes not begin with \n\"ftp://\"  \nThis is a requirement");
            }

            Uri uri = new Uri(url, UriKind.Absolute);


            FtpWebRequest ftpclientRequest = WebRequest.Create(uri) as FtpWebRequest;

            ftpclientRequest.KeepAlive = keepAlive;

            ftpclientRequest.Method = method;
            switch (method)
            {
                case WebRequestMethods.Ftp.ListDirectoryDetails:
                    ftpclientRequest.Proxy = null;
                    break;
                case WebRequestMethods.Ftp.DownloadFile:
                    ftpclientRequest.UseBinary = forceBinary;
                    break;
                case WebRequestMethods.Ftp.UploadFile:
                    ftpclientRequest.UsePassive = true;

                    ftpclientRequest.UseBinary = true;
                    break;
            }


            ftpclientRequest.Credentials = new NetworkCredential(userName, password);

            return ftpclientRequest;
        }
#endif


        private static FileListStyle GuessFileListStyle(string[] recordList)
        {
            foreach (string s in recordList)
            {
                if (s.Length > 10
                 && Regex.IsMatch(s.Substring(0, 10), "(-|d)(-|r)(-|w)(-|x)(-|r)(-|w)(-|x)(-|r)(-|w)(-|x)"))
                {
                    return FileListStyle.UnixStyle;
                }
                else if (s.Length > 8
                 && Regex.IsMatch(s.Substring(0, 8), "[0-9][0-9]-[0-9][0-9]-[0-9][0-9]"))
                {
                    return FileListStyle.WindowsStyle;
                }
            }
            return FileListStyle.Unknown;
        }

        private static FileStruct ParseFileStructFromWindowsStyleRecord(string Record)
        {
            //Assuming the record style as
            // 02-03-04  07:46PM       <DIR>          Append
            FileStruct f = new FileStruct();
            string processstr = Record.Trim();
            string dateStr = processstr.Substring(0, 8);
            processstr = (processstr.Substring(8, processstr.Length - 8)).Trim();
            string timeStr = processstr.Substring(0, 7);
            processstr = (processstr.Substring(7, processstr.Length - 7)).Trim();
            f.CreateTime = DateTime.Parse(dateStr + " " + timeStr);
            if (processstr.Substring(0, 5) == "<DIR>")
            {
                f.IsDirectory = true;
                processstr = (processstr.Substring(5, processstr.Length - 5)).Trim();
            }
            else
            {
                string[] strs = processstr.Split(new char[] { ' ' });
                processstr = strs[1].Trim();
                f.IsDirectory = false;
            }
            f.Name = processstr;  //Rest is name   
            return f;
        }

        private static FileStruct ParseFileStructFromUnixStyleRecord(string Record)
        {
            ///Assuming record style as
            /// dr-xr-xr-x   1 owner    group               0 Nov 25  2002 bussys
            FileStruct f = new FileStruct();
            string processstr = Record.Trim();
            f.Flags = processstr.Substring(0, 9);
            f.IsDirectory = (f.Flags[0] == 'd');
            processstr = (processstr.Substring(11)).Trim();
            CutSubstringFromStringWithTrim(ref processstr, ' ', 0);   //skip one part
            f.Owner = CutSubstringFromStringWithTrim(ref processstr, ' ', 0);
            f.Group = CutSubstringFromStringWithTrim(ref processstr, ' ', 0);
            CutSubstringFromStringWithTrim(ref processstr, ' ', 0);   //skip one part

            string cutSubstringForCreateTime =
                CutSubstringFromStringWithTrim(ref processstr, ' ', 8);

            int indexOfSpace = cutSubstringForCreateTime.IndexOf(' ');
            string month = cutSubstringForCreateTime.Substring(0, indexOfSpace + 1);

            if (cutSubstringForCreateTime[month.Length] == ' ')
            {
                month += " ";
            }

            int day = FlatRedBall.Utilities.StringFunctions.GetIntAfter(month, cutSubstringForCreateTime);

            string rearranged = day + " " + cutSubstringForCreateTime.Replace(day + " ", "");

            int lastSpace = rearranged.LastIndexOf(' ');

            string modifiedRearranged = rearranged.Substring(0, lastSpace);

            if (!DateTime.TryParse(modifiedRearranged, out f.CreateTime))
            {
                f.CreateTime = new DateTime();
            }

            f.Name = processstr;   //Rest of the part is name
            return f;
        }

#if !SILVERLIGHT && !WINDOWS_PHONE
        private static void EndGetStreamCallback(IAsyncResult result)
        {
            FtpState state = (FtpState)result.AsyncState;

            Stream requestStream = null;

            Stream uploadStream = null;

            try
            {
                requestStream = state.Request.EndGetRequestStream(result);
                const int bufferLength = 2048;
                byte[] buffer = new byte[bufferLength];
                int count = 0;
                int readBytes = 0;

                if (!string.IsNullOrEmpty(state.TextToUpload))
                {
                    byte[] byteArray = Encoding.ASCII.GetBytes(state.TextToUpload);
                    uploadStream = new MemoryStream(byteArray);
                }
                else
                {
                    uploadStream = File.OpenRead(state.FileName);
                }

                do
                {
                    readBytes = uploadStream.Read(buffer, 0, bufferLength);
                    requestStream.Write(buffer, 0, readBytes);
                    count += readBytes;
                }
                while (readBytes != 0);

                requestStream.Close();

                state.Request.BeginGetResponse(
                    new AsyncCallback(EndGetResponseCallback),
                    state
                );
            }
            catch (WebException)
            {
                state.Request.Abort();
                state.OperationComplete.Set();
                state.OperationException = new Exception();
            }
            finally
            {
                if (uploadStream != null)
                {
                    uploadStream.Close();
                }
            }
        }

        private static void EndGetResponseCallback(IAsyncResult result)
        {
            FtpState state = (FtpState)result.AsyncState;
            FtpWebResponse response = null;
            response = (FtpWebResponse)state.Request.EndGetResponse(result);
            response.Close();
            state.StatusDescription = response.StatusDescription;

            state.OperationComplete.Set();
        }
#endif


        #endregion

        #endregion
    }
}