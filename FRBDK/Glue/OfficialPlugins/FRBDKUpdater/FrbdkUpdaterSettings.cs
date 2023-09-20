using System;
using System.Xml.Serialization;

using Microsoft.Win32;
using System.IO;
using System.Xml;
using System.Collections.Generic;

namespace OfficialPlugins.FrbdkUpdater
{
    #region RelativeType enum

    public enum RelativeType
    {
        Relative,
        Absolute
    }

    #endregion

    [XmlRoot("FRBDKUpdaterSave")]
    public class FrbdkUpdaterSettings
    {
        #region FileManager methods
        // We embed these here so the installer can be used in situations where FRB isn't installed yet.



        static Dictionary<Type, XmlSerializer> mXmlSerializers = new Dictionary<Type, XmlSerializer>();


        public static string UserApplicationData
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\"; }
        }

        public static string DefaultSaveLocation
        {
            get
            {
                return UserApplicationData + @"FRBDK/" + Filename;

            }
        }


        public static string GetDirectory(string fileName)
        {
            return GetDirectory(fileName, RelativeType.Absolute);
        }

        public static string GetDirectory(string fileName, RelativeType relativeType)
        {
            int lastIndex = System.Math.Max(
                fileName.LastIndexOf('/'), fileName.LastIndexOf('\\'));

            if (lastIndex == fileName.Length - 1)
            {
                // If this happens then fileName is actually a directory.
                // So we should return the parent directory of the argument.



                lastIndex = System.Math.Max(
                    fileName.LastIndexOf('/', fileName.Length - 2),
                    fileName.LastIndexOf('\\', fileName.Length - 2));
            }

            if (lastIndex != -1)
            {
                //bool isFtp = false;


                return fileName.Substring(0, lastIndex + 1);
                //if (FileManager.IsUrl(fileName) || isFtp)
                //{
                //    // don't standardize URLs - they're case sensitive!!!
                //    return fileName.Substring(0, lastIndex + 1);

                //}
                //else
                {
                    //if (relativeType == RelativeType.Absolute)
                    //{
                    //    return FileManager.Standardize(fileName.Substring(0, lastIndex + 1));
                    //}
                    //else
                    //{
                    //    return FileManager.Standardize(fileName.Substring(0, lastIndex + 1), "", false);
                    //}
                }
            }
            else
                return ""; // there was no directory found.

        }

        public static string GetExtension(string fileName)
        {
            if (fileName == null)
            {
                return "";
            }


            int i = fileName.LastIndexOf('.');
            if (i != -1)
            {
                bool hasDotSlash = i < fileName.Length + 1 && (fileName[i + 1] == '/' || fileName[i + 1] == '\\');

                if (hasDotSlash)
                {
                    return "";
                }

                return fileName.Substring(i + 1, fileName.Length - (i + 1)).ToLower();
            }

            return ""; // This returns "" because calling the method with a string like "redball" should return no extension
        }

        #region XML Docs
        /// <summary>
        /// Modifies the fileName by removing its path, or makes no changes if the fileName has no path.
        /// </summary>
        /// <param name="fileName">The file name to change</param>
        #endregion
        public static void RemovePath(ref string fileName)
        {
            int indexOf1 = fileName.LastIndexOf('/', fileName.Length - 1, fileName.Length);
            if (indexOf1 == fileName.Length - 1 && fileName.Length > 1)
            {
                indexOf1 = fileName.LastIndexOf('/', fileName.Length - 2, fileName.Length - 1);
            }

            int indexOf2 = fileName.LastIndexOf('\\', fileName.Length - 1, fileName.Length);
            if (indexOf2 == fileName.Length - 1 && fileName.Length > 1)
            {
                indexOf2 = fileName.LastIndexOf('\\', fileName.Length - 2, fileName.Length - 1);
            }


            if (indexOf1 > indexOf2)
                fileName = fileName.Remove(0, indexOf1 + 1);
            else if (indexOf2 != -1)
                fileName = fileName.Remove(0, indexOf2 + 1);
        }

        #region XML Docs
        /// <summary>
        /// Returns the fileName without a path, or makes no changes if the fileName has no path.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>The modified fileName if a path is found.</returns>
        #endregion
        public static string RemovePath(string fileName)
        {
            RemovePath(ref fileName);

            return fileName;
        }

        internal static XmlSerializer GetXmlSerializer<T>()
        {
            return GetXmlSerializer(typeof(T));
        }


        internal static XmlSerializer GetXmlSerializer(Type type)
        {
            lock (mXmlSerializers)
            {
                if (mXmlSerializers.ContainsKey(type))
                {
                    return mXmlSerializers[type];
                }
                else
                {
                    XmlSerializer newSerializer = new XmlSerializer(type);


                    mXmlSerializers.Add(type, newSerializer);
                    return newSerializer;
                }
            }
        }

        public static string RemoveExtension(string fileName)
        {
            int extensionLength = GetExtension(fileName).Length;

            if (extensionLength == 0)
                return fileName;

            if (fileName.Length > extensionLength && fileName[fileName.Length - (extensionLength + 1)] == '.')
                return fileName.Substring(0, fileName.Length - (extensionLength + 1));
            else
                return fileName;

        }

        public static T XmlDeserialize<T>(string fileName)
        {
            T objectToReturn = default(T);

#if SILVERLIGHT || WINDOWS_PHONE || (XBOX360 && XNA4) || MONODROID
            if (fileName.Contains(FileManager.IsolatedStoragePrefix) && mHasUserFolderBeenInitialized == false)
            {
                throw new InvalidOperationException("The user folder hasn't been initialized.  Call FileManager.InitializeUserFolder first");
            }
#endif

            //if (FileManager.IsRelative(fileName))
            //    fileName = FileManager.RelativeDirectory + fileName;

            // Do this check before removing the ./ at the end of the file name
//#if !XBOX360 || XNA4
//            ThrowExceptionIfFileDoesntExist(fileName);
//#endif


#if XBOX360 || SILVERLIGHT || WINDOWS_PHONE || MONODROID
            // Silverlight and 360 don't like ./ at the start of the file name, but that's what we use to identify an absolute path
            if (fileName.Length > 1 && fileName[0] == '.' && fileName[1] == '/')
                fileName = fileName.Substring(2);

#endif

#if SILVERLIGHT || WINDOWS_PHONE || XBOX360 || MONODROID
            using (Stream stream = GetStreamForFile(fileName))
#else
            using (FileStream stream = System.IO.File.OpenRead(fileName))
#endif
            {
                objectToReturn = XmlDeserialize<T>(stream);
            }

#if XBOX360 //&& !XNA4
            if (IsFileNameInUserFolder(fileName))
            {
                FileManager.DisposeLastStorageContainer();
            }
#endif

            return objectToReturn;
        }


        public static T XmlDeserialize<T>(Stream stream)
        {

            if (stream == null)
            {
                return default(T); // this happens if the file can't be found
            }
            else
            {
                XmlSerializer serializer = GetXmlSerializer<T>();
                T objectToReturn;
                objectToReturn = (T)serializer.Deserialize(stream);
                stream.Close();
                return objectToReturn;
            }
        }



        public static void XmlSerialize<T>(T objectToSerialize, string fileName)
        {

            XmlSerialize(typeof(T), objectToSerialize, fileName);
        }

        
        public static void XmlSerialize(Type type, object objectToSerialize, string fileName)
        {
            //if (FileManager.IsRelative(fileName))
            //    fileName = FileManager.RelativeDirectory + fileName;
            
            Stream fs = null;
            XmlWriter writer = null;

            string directory = GetDirectory(fileName);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }


            try
            {
                XmlSerializer serializer = GetXmlSerializer(type);
               
                // I used to call File.Open with the FileMode.Truncate
                // but that caused the file to be modified twice and this
                // was bad in Glue.  So now we delete instead of truncate
                // to prevent file systems from reporting 2 changes when a file
                // has really only changed once.
                if (System.IO.File.Exists(fileName))
                {
                    System.IO.File.Delete(fileName);
                }
                 
                fs = System.IO.File.Open(fileName, FileMode.OpenOrCreate);


                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                writer = XmlWriter.Create(fs, settings);


                serializer.Serialize(writer, objectToSerialize);
            }
            finally
            {
                if (fs != null) fs.Close();

#if SILVERLIGHT || WINDOWS_PHONE || MONODROID
                if (isfs != null)
                {
                    isfs.Close();
                }
#elif XBOX360
                FileManager.DisposeLastStorageContainer();
#endif
            }
        }

        #endregion


        #region Fields

        private string _selectedSource = String.Empty;
        private string _selectedDirectory = String.Empty;
        private string _lastSync = String.Empty;
        private string _glueRunPath = String.Empty;

        const string Filename = "FRBDKUpdaterSettings.xml";

        #endregion

        #region Properties


        public string SelectedSource
        {
            get { return _selectedSource; }
            set { _selectedSource = value; }
        }

        public string SelectedDirectory
        {
            get { return _selectedDirectory; }
            set { _selectedDirectory = value; }
        }

        public string GlueRunPath
        {
            get { return _glueRunPath; }
            set { _glueRunPath = value; }
        }


        public bool CleanFolder { get; set; }

        public bool Passive { get; set; }

        public bool ForceDownload { get; set; }

        #endregion



        public FrbdkUpdaterSettings()
        {
            ForceDownload = true;
            SetDefaultPath();
        }
        
        public static FrbdkUpdaterSettings LoadSettings()
        {
            FrbdkUpdaterSettings toReturn;
            TryLoadSettingsFromuserAppFolder(UserApplicationData, out toReturn);
            return toReturn;
        }

        public static bool TryLoadSettingsFromuserAppFolder(string userAppPath, out FrbdkUpdaterSettings  loadedOrCreated)
        {
            bool wasLoadedFromFile = false;
            var fileName = userAppPath + @"FRBDK/" + Filename;
            if (System.IO.File.Exists(fileName))
            {
                loadedOrCreated = LoadSettings(fileName);
                wasLoadedFromFile = true;
            }
            loadedOrCreated = new FrbdkUpdaterSettings();

            return wasLoadedFromFile;
            
        }

        public static FrbdkUpdaterSettings LoadSettings(string fileName)
        {
            var pS = XmlDeserialize<FrbdkUpdaterSettings>(fileName);
            return pS;
        }

        public void SaveSettings()
        {
            SaveSettings(UserApplicationData);
        }

        public void SaveSettings(string userAppPath)
        {
            var fileName = userAppPath + @"FRBDK/" + Filename;

            XmlSerialize(this, fileName);
        }

        public void SetDefaultPath()
        {
            var rk = Registry.LocalMachine;

            var pathKey = rk.OpenSubKey(@"SOFTWARE\FlatRedBall");   //New install key

            if (pathKey != null)
            {
                try
                {
                    _selectedDirectory = ((string)pathKey.GetValue("FrbdkDir"));
                    return;
                }
                catch (Exception)
                {
                }
            }

            pathKey = rk.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FlatRedBall");    //Native

            if (pathKey != null)
            {
                try
                {
                    _selectedDirectory = ((string)pathKey.GetValue("FrbdkDir")) + @"\FRBDK";
                    return;
                }
                catch (Exception)
                {
                }
            }
            else
            {
                pathKey = rk.OpenSubKey(@"SOFTWARE\WOW6432node\Microsoft\Windows\CurrentVersion\Uninstall\FlatRedBall");    //32 bit app on 64 bit machine

                if (pathKey != null)
                {
                    try
                    {
                        _selectedDirectory = ((string)pathKey.GetValue("FrbdkDir")) + @"\FRBDK";
                        return;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
    }
}
