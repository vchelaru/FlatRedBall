#if WINDOWS_8 || MONODROID || UWP
#define USE_ISOLATED_STORAGE
#endif

#if  MONODROID || IOS || UWP
#define USES_DOT_SLASH_ABOLUTE_FILES
#endif

#region Using Statements
using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

using System.Xml;
using System.Xml.Serialization;

using System.Text;
using System.Xml.Linq;
using System.Linq;

// FileManager is used by FRBDK Updater, so the #if is needed
#if FRB_XNA
using Microsoft.Xna.Framework;
#endif

#if USE_ISOLATED_STORAGE
using System.IO.IsolatedStorage;
#endif

using File = System.IO.File;
using System.Collections;

using System.Reflection;

#if !UWP
using System.Runtime.Serialization.Formatters.Binary;
#endif


#if !FRB_RAW
using FlatRedBall.Instructions.Reflection;

using System.Runtime.Serialization;
using System.Threading;
using System.Diagnostics;

#endif

#if !MONOGAME
using FlatRedBall.IO.Remote;
#endif
#endregion

namespace FlatRedBall.IO
{
    public enum RelativeType
    {
        Relative,
        Absolute
    }

    public static partial class FileManager
    {
        #region Fields

        static bool mHasUserFolderBeenInitialized = false;

#if FRB_RAW || DESKTOP_GL
        public static string DefaultRelativeDirectory = 
            System.IO.Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) + "/";
#elif MONOGAME
        public static string DefaultRelativeDirectory = "./";

#else
        // Vic says - this used to be:
        //static string mRelativeDirectory = (System.IO.Directory.GetCurrentDirectory() + "/").Replace("\\", "/");
        // But the current directory is the directory that launched the application, not the directory of the .exe.
        // We want to make sure that we use the .exe so that the game/tool can reference the proper path when loading
        // content.
        // Update: Made this per-thread so we can do multi-threaded loading.
        // static string mRelativeDirectory = (System.Windows.Forms.Application.StartupPath + "/").Replace("\\", "/");
        // Update October 22, 2012 - Projects like Glue may be multi-threaded, but they want the default directory to be preset to
        // something specific.  But I think we only want this for tools (on the PC).
        public static string DefaultRelativeDirectory = (System.Windows.Forms.Application.StartupPath + "/").Replace("\\", "/");

#endif

        static Dictionary<int, string> mRelativeDirectoryDictionary = new Dictionary<int, string>();



        static Dictionary<string, object> mFileCache = new Dictionary<string, object>();

        static Dictionary<Type, XmlSerializer> mXmlSerializers = new Dictionary<Type, XmlSerializer>();

        static XmlReaderSettings mXmlReaderSettings = new XmlReaderSettings();

#endregion

        #region Properties

        public static string CurrentDirectory
        {
            get
            {
                return (System.IO.Directory.GetCurrentDirectory() + "/").Replace("\\", "/");

            }
            set
            {
                System.IO.Directory.SetCurrentDirectory(value);
            }
        }

        /// <summary>
        /// Whether file paths should be preserved as mixed case. If false 
        /// all file paths will be made to-lower.
        /// </summary>
        public static bool PreserveCase
        {
            get;
            set;
        }

        /// <summary>
        /// The directory that FlatRedBall will use when loading assets.  Defaults to the application's directory.
        /// </summary>
        static public string RelativeDirectory
        {
            get
            {
#if UWP
                int threadID = Environment.CurrentManagedThreadId;
#else
                int threadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
#endif
                if (mRelativeDirectoryDictionary.ContainsKey(threadID))
                {
                    // VERY rare, but possible:
                    try
                    {
                        // the thread ID could go away inbetween the if.
                        return mRelativeDirectoryDictionary[threadID];
                    }
                    catch
                    {
                        return DefaultRelativeDirectory;
                    }
                }
                else
                {
                    return DefaultRelativeDirectory;
                }

            }
            set
            {

                if (FileManager.IsRelative(value))
                {
                    throw new InvalidOperationException("Relative Directory must be an absolute path");
                }

                string valueToSet = value;

#if USES_DOT_SLASH_ABOLUTE_FILES
                // On the Xbox 360 the way to specify absolute is to put a '/' before
                // a file name.
                if (value.Length > 1 && (value[0] != '.' || value[1] != '/'))
                {
                    valueToSet = Standardize("./" + value.Replace("\\", "/"));
                }
                else if (value.Length == 0)
                {
                    valueToSet = "./";
                }
                else
                {
                    valueToSet = Standardize(value.Replace("\\", "/"));
                }

#else
                ReplaceSlashes(ref valueToSet);
                valueToSet = Standardize(valueToSet, "", false);

#endif

                if (!string.IsNullOrEmpty(valueToSet) && !valueToSet.EndsWith("/"))
                    valueToSet += "/";



#if WINDOWS_8 || UWP
                int threadID = Environment.CurrentManagedThreadId;
#else

                int threadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
#endif

                lock (mRelativeDirectoryDictionary)
                {
                    if (valueToSet == DefaultRelativeDirectory)
                    {
                        if (mRelativeDirectoryDictionary.ContainsKey(threadID))
                        {
                            mRelativeDirectoryDictionary.Remove(threadID);
                        }
                    }
                    else
                    {
                        if (mRelativeDirectoryDictionary.ContainsKey(threadID))
                        {
                            mRelativeDirectoryDictionary[threadID] = valueToSet;
                        }
                        else
                        {
                            mRelativeDirectoryDictionary.Add(threadID, valueToSet);
                        }
                    }
                }
            }
        }

#if !UWP && !MONODROID && !WINDOWS_8


        public static string StartupPath
        {
            get
            {
#if IOS
				return "./";
#elif FRB_RAW || DESKTOP_GL || STANDARD
                return System.IO.Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) + "/";

#else


                return (System.Windows.Forms.Application.StartupPath + "/");
#endif
			}
        }

        public static string MyDocuments
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\"; }
        }

        /// <summary>
        /// Gets the path to the user specific application data directory.
        /// </summary>
        /// <remarks>If your game/application will be writing anything to the file system, you will want 
        /// to do so somewhere in this directory.  The reason for this is because you cannot anticipate
        /// whether the user will have the needed permissions to write to the directory where the 
        /// executable lives.</remarks>
        /// <example>C:\Documents and Settings\&lt;username&gt;\Application Data</example> 
        public static string UserApplicationData
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\"; }
        }

        public static string UserApplicationDataForThisApplication
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();

                string applicationDataName = assembly == null ? "" : Assembly.GetEntryAssembly().FullName;
				if(string.IsNullOrEmpty (applicationDataName))
				{
					applicationDataName = @"FRBDefault";
				}
				else
				{
					applicationDataName = applicationDataName.Substring(0, applicationDataName.IndexOf(','));
				}

#if IOS
                
                var documents = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
                string folder = Path.Combine (documents, "..", "Library");

                folder = FileManager.RemoveDotDotSlash(folder);

                //// Make it absolute:
                // actually, leading / is now absolute:
                //folder = "." + folder;
#else
				string folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
#endif
				folder =  folder + @"\" + applicationDataName + @"\";

#if IOS
                folder = folder.Replace('\\', '/');
#endif
                return folder;

            }
        }

#endif
#endregion

        #region Methods

        #region Constructor

        static FileManager()
        {
            // this is for linux:
            PreserveCase = true;
        }

#endregion

#region Public Methods

        #region Caching Methods
        public static void CacheObject(object objectToCache, string fileName)
        {
            mFileCache.Add(Standardize(fileName), objectToCache);
        }


        public static object GetCachedObject(string fileName)
        {
            return mFileCache[Standardize(fileName)];
        }


        public static bool IsCached(string fileName)
        {
            return mFileCache.ContainsKey(Standardize(fileName));
        }




#endregion

        public static T CloneObject<T>(T objectToClone)
        {
            string container;

            XmlSerialize(objectToClone, out container);

            XmlSerializer serializer = GetXmlSerializer(typeof(T));// new XmlSerializer(type);

            return (T)serializer.Deserialize(new StringReader(container));
        }

        public static void CopyDirectory(string sourceDirectory, string destDirectory, bool deletePrevious, List<string> excludeFiles, List<string> excludeDirectories)
        {
            if (excludeDirectories != null)
            {
                string currentDir;

                for (int i = 0; i < excludeDirectories.Count; ++i)
                {
                    currentDir = excludeDirectories[i];

                    currentDir = RemovePath(currentDir);
                    currentDir = currentDir.TrimEnd(new char[] { '\\', '/' });
                    excludeDirectories[i] = currentDir;
                }
            }

            if (excludeFiles != null)
            {
                string currentFile;

                for (int i = 0; i < excludeFiles.Count; ++i)
                {
                    currentFile = excludeFiles[i];

                    currentFile = RemovePath(currentFile);
                    currentFile = Standardize(currentFile);
                    excludeFiles[i] = currentFile;
                }
            }

            CopyDirectoryHelper(sourceDirectory, destDirectory, deletePrevious, excludeFiles, excludeDirectories);
        }

        public static void CopyDirectory(string sourceDirectory, string destDirectory, bool deletePrevious)
        {
            CopyDirectoryHelper(sourceDirectory, destDirectory, deletePrevious, null, null);
        }


        public static void DeleteDirectory(string directory)
        {

            string[] dirList = Directory.GetDirectories(directory);
            foreach (string dir in dirList)
            {
                DeleteDirectory(dir);
            }

            string[] fileList = Directory.GetFiles(directory);
            foreach (string file in fileList)
            {
                File.Delete(file);
            }

            Directory.Delete(directory);

        }

        public static void DeleteFile(string fileName)
        {
#if USE_ISOLATED_STORAGE
            DeleteFileFromIsolatedStorage(fileName);

#else
            System.IO.File.Delete(fileName);
#endif
        }


#region XML Docs
        /// <summary>
        /// Returns whether the file exists considering the relative directory.
        /// </summary>
        /// <param name="fileName">The file to search for.</param>
        /// <returns>Whether the argument file exists.</returns>
        /// <remarks>
        /// The PC platform can use the File.Exists check, but other platforms like Android and iOS do not provide direct access to
        /// a project's content. Therefore this internally will use the preferred way of checking for files per platform.
        /// iOS and Android use the TitleContainer.OpenStream method.
        /// </remarks>
#endregion
        public static bool FileExists(string fileName)
        {
            if (IsRelative(fileName))
            {
                return FileExists(MakeAbsolute(fileName));
            }
            else
            {
#if  USE_ISOLATED_STORAGE
                bool isIsolatedStorageFile = IsInIsolatedStorage(fileName);

                if (isIsolatedStorageFile)
                {
                    return FileExistsInIsolatedStorage(fileName);
                }
                else
                {

                    if (fileName.Length > 1 && fileName[0] == '.' && fileName[1] == '/')
                        fileName = fileName.Substring(2);
                    fileName = fileName.Replace("\\", "/");


                    // I think we can make this to-lower on iOS and Android so we don't have to spread to-lowers everywhere else:
                    fileName = fileName.ToLowerInvariant();

#if ANDROID
                    // We may be checking for a file outside of the title container
                    if (System.IO.File.Exists(fileName))
                    {
                        return true;
                    }
#endif


                    Stream stream = null;
                    // This method tells us if a file exists.  I hate that we have 
                    // to do it this way - the TitleContainer should have a FileExists
                    // property to avoid having to do logic off of exceptions.  <sigh>
                    try
                    {
                        stream = TitleContainer.OpenStream(fileName);
                    }
#if MONODROID
                    catch (Java.IO.FileNotFoundException fnfe)
                    {
                        return false;
                    }
#else
                    catch (FileNotFoundException fnfe)
                    {
                        return false;
                    }
#endif

                    if (stream != null)
                    {
                        stream.Dispose();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
#else
                
				if (fileName.Length > 1 && fileName[0] == '.' && fileName[1] == '/')
					fileName = fileName.Substring(2);

                return System.IO.File.Exists(fileName);
#endif
            }
        }
        

#if !WINDOWS_8
        /// <summary>
        /// Searches the passed directory and all subdirectories for the passed file.
        /// </summary>
        /// <param name="fileToFind">The name of the file including extension.</param>
        /// <param name="directory">The directory to search in, including all subdirectories.</param>
        /// <returns>The full path of the first file found matching the name, or an empty string if none is found.</returns>
        public static string FindFileInDirectory(string fileToFind, string directory)
        {

            string[] files = System.IO.Directory.GetFiles(directory);
            string[] directories = System.IO.Directory.GetDirectories(directory);

            fileToFind = RemovePath(fileToFind);

            foreach (string file in files)
            {
                if (RemovePath(file).ToLowerInvariant() == fileToFind.ToLowerInvariant())
                    return directory + "/" + fileToFind;
            }

            foreach (string directoryChecking in directories)
            {
                string fileFound = FindFileInDirectory(fileToFind, directoryChecking);
                if (fileFound != "")
                    return fileFound;

            }

            return "";
        }

#region XML Docs
        /// <summary>
        /// Searches the executable's director and all subdirectories for the passed file.
        /// </summary>
        /// <param name="fileToFind">The name of the file which may or may not include an extension.</param>
        /// <returns>The full path of the first file found matching the name, or an empty string if none is found</returns>
#endregion
        public static string FindFileInDirectory(string fileToFind)
        {
            return FindFileInDirectory(FileManager.RelativeDirectory);
        }
#endif


        

        public static string FromFileText(string fileName)
        {
#if SILVERLIGHT
            string containedText;

            Uri uri = new Uri(fileName, UriKind.Relative);

            StreamResourceInfo sri = Application.GetResourceStream(uri);
            Stream stream = sri.Stream;
            StreamReader reader = new StreamReader(stream);

            containedText = reader.ReadToEnd();

            stream.Close();
            reader.Close();
            
            return containedText;

#else

            if (IsRelative(fileName))
            {
                fileName = MakeAbsolute(fileName);
            }
            //NM: 14/08/11
            //I changed this to do a standardize as my tilemap files had backwards slashes in them,
            //the RemoveDotDotSlash method on it's own was not working correctly with these filepaths.
            //Standardize on the other hand repalces the slashes and then calls RemoveDotDotSlash.
            //fileName = FileManager.RemoveDotDotSlash(fileName);
            fileName = FileManager.Standardize(fileName);

            string containedText = "";

            // Creating a filestream then using that enables us to open files that are open by other apps.

            Stream fileStream = null;
            try
            {
                // We used to do it this way because it got around the file already being open...but this causes
                // problems on WP7.  Maybe we'll need to branch if the GetStream doesn't work for us.
                //using (FileStream fileStream = new FileStream( new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                //{
                // Update June , 2011
                // We do need to branch
                // because opening a CSV
                // that is already open in 
                // Excel causes a crash otherwise
#if WINDOWS
                fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
#else
                fileStream = GetStreamForFile(fileName);

#endif
                using (System.IO.StreamReader sr = new StreamReader(fileStream))
                {
                    containedText = sr.ReadToEnd();
                    Close(sr);
                }

            }
            finally
            {
                if(fileStream != null)
                {
                    Close(fileStream);
                }

            }


            return containedText;
#endif
        }


        public static byte[] GetByteArrayFromEmbeddedResource(Assembly assemblyContainingResource, string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                throw new NullReferenceException("ResourceName must not be null - can't get the byte array for resource");
            }

            if (assemblyContainingResource == null)
            {
                throw new NullReferenceException("Assembly is null, so can't find the resource\n" + resourceName);
            }

            Stream resourceStream = assemblyContainingResource.GetManifestResourceStream(resourceName);

            if (resourceStream == null)
            {
                string message = "Could not find a resource stream for\n" + resourceName + "\n but found " +
                    "the following names:\n\n";

                var existingNames = assemblyContainingResource.GetManifestResourceNames();

                foreach (string containedResource in existingNames)
                {
                    message += containedResource + "\n";
                }


                throw new NullReferenceException(message);
            }

            byte[] buffer = new byte[resourceStream.Length];
            resourceStream.Read(buffer, 0, buffer.Length);
            Close(resourceStream);

            resourceStream.Dispose();
            return buffer;
        }

#region XML Docs
        /// <summary>
        /// Returns the extension in a filename.
        /// </summary>
        /// <remarks>
        /// The extension returned will not contain a period.
        /// 
        /// <para>
        /// <code>
        /// // this code will return a string containing "png", not ".png"
        /// FileManager.GetExtension(@"FolderName/myImage.png");
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="fileName">The filename.</param>
        /// <returns>Returns the extension or an empty string if no period is found in the filename.</returns>
#endregion
        public static string GetExtension(string fileName)
        {
            if (fileName == null)
            {
                return "";
            }


            int i = fileName.LastIndexOf('.');
            if (i != -1)
            {
                bool hasDotSlash = i < fileName.Length - 1 && (fileName[i + 1] == '/' || fileName[i + 1] == '\\');

                // Justin Johnson, 09/28/2017:
                // Folders in the path might have a period in them. We need to make sure
                // the period is after the last slash or we could end up with an "extension"
                // that is a large chunk of the path
                bool hasSlashAfterDot = i < fileName.LastIndexOf("/", StringComparison.OrdinalIgnoreCase) 
                                        || i < fileName.LastIndexOf(@"\", StringComparison.OrdinalIgnoreCase);

                if (hasDotSlash || hasSlashAfterDot)
                {
                    return "";
                }
                else
                {
                    return fileName.Substring(i + 1, fileName.Length - (i + 1)).ToLowerInvariant();
                }
            }
            else
            {
                return ""; // This returns "" because calling the method with a string like "redball" should return no extension
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

            string directoryToReturn = "";

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
                bool isFtp = false;

#if !MONOGAME
                isFtp = FtpManager.IsFtp(fileName);
#endif

                if (FileManager.IsUrl(fileName) || isFtp)
                {
                    // don't standardize URLs - they're case sensitive!!!
                    directoryToReturn = fileName.Substring(0, lastIndex + 1);

                }
                else
                {
                    if (relativeType == RelativeType.Absolute)
                    {
                        directoryToReturn = FileManager.Standardize(fileName.Substring(0, lastIndex + 1));
                    }
                    else
                    {
                        directoryToReturn = FileManager.Standardize(fileName.Substring(0, lastIndex + 1), "", false);
                    }
                }
            }
            else
            {
                directoryToReturn = ""; // there was no directory found.
            }

            return directoryToReturn;
        }

        public static string GetDirectoryKeepRelative(string fileName)
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
                return fileName.Substring(0, lastIndex + 1);
            }
            else
            {
                return "";
            }
        }


#region GetAllFilesInDirectory


        /// <summary>
        /// Returns a List containing all of the files found in a particular directory and its subdirectories.
        /// </summary>
        /// <param name="directory">The directory to search in.</param>
        /// <returns></returns>
        public static List<string> GetAllFilesInDirectory(string directory)
        {
#if USE_ISOLATED_STORAGE

            return GetAllFilesInDirectoryIsolatedStorage(directory);
#else
            List<string> arrayToReturn = new List<string>();

            if (directory == "")
                directory = RelativeDirectory;


            if (directory.EndsWith(@"\") == false && directory.EndsWith("/") == false)
                directory += @"\";


            string[] files = System.IO.Directory.GetFiles(directory, "*", SearchOption.AllDirectories);

            arrayToReturn.AddRange(files);

            
            return arrayToReturn;
#endif
        }

        /// <summary>
        /// Returns a List containing all files which match the fileType argument which are 
        /// in the directory argument or a subfolder.  This recurs, returning all files.
        /// </summary>
        /// <param name="directory">String representing the directory to search.  If an empty
        /// string is passed, the method will search starting in the directory holding the .exe.</param>
        /// <param name="fileType">The file type to search for specified as an extension.  The extension
        /// can either have a period or not.  That is ".jpg" and "jpg" are both valid fileType arguments.  An empty
        /// or null value for this parameter will return all files regardless of file type.</param>
        /// <returns>A list containing all of the files found which match the fileType.</returns>
        public static List<string> GetAllFilesInDirectory(string directory, string fileType)
        {
            return GetAllFilesInDirectory(directory, fileType, int.MaxValue);

        }


        /// <summary>
        /// Returns a List containing all files which match the fileType argument which are within
        /// the depthToSearch folder range relative to the directory argument.
        /// </summary>
        /// <param name="directory">String representing the directory to search.  If an empty
        /// string is passed, the method will search starting in the directory holding the .exe.</param>
        /// <param name="fileType">The file type to search for specified as an extension.  The extension
        /// can either have a period or not.  That is ".jpg" and "jpg" are both valid fileType arguments.  An empty
        /// or null value for this parameter will return all files regardless of file type.</param>
        /// <param name="depthToSearch">The depth to search through.  If the depthToSearch
        /// is 0, only the argument directory will be searched.</param>
        /// <returns>A list containing all of the files found which match the fileType.</returns>
        public static List<string> GetAllFilesInDirectory(string directory, string fileType, int depthToSearch)
        {
            List<string> arrayToReturn = new List<string>();

            GetAllFilesInDirectory(directory, fileType, depthToSearch, arrayToReturn);

            return arrayToReturn;
        }



        public static void GetAllFilesInDirectory(string directory, string fileType, int depthToSearch, List<string> arrayToReturn)
        {
            // EARLY OUT: Bad directory
            if (!Directory.Exists(directory))
            {
                return;
            }

            if (directory == "")
            {
                directory = RelativeDirectory;
            }

            if (directory.EndsWith(@"\") == false && directory.EndsWith("/") == false)
            {
                directory += @"/";
            }


            // if they passed in a fileType which begins with a period (like ".jpg"), then
            // remove the period so only the extension remains.  That is, convert
            // ".jpg" to "jpg"
            if (fileType != null && fileType.Length > 0 && fileType[0] == '.')
            {
                fileType = fileType.Substring(1);
            }

            string[] files = System.IO.Directory.GetFiles(directory);
            string[] directories = System.IO.Directory.GetDirectories(directory);

            if (string.IsNullOrEmpty(fileType))
            {
                arrayToReturn.AddRange(files);
            }
            else
            {
                int fileCount = files.Length;

                for (int i = 0; i < fileCount; i++)
                {
                    string file = files[i];
                    if (GetExtension(file) == fileType)
                    {
                        arrayToReturn.Add(file);
                    }
                }
            }


            if (depthToSearch > 0)
            {
                int directoryCount = directories.Length;
                for (int i = 0; i < directoryCount; i++)
                {
                    string directoryChecking = directories[i];

                    GetAllFilesInDirectory(directoryChecking, fileType, depthToSearch - 1, arrayToReturn);
                }
            }
        }
        #endregion




        public static bool IsCurrentStorageDeviceConnected()
        {
#if XBOX360
            if (mStorageDevice == null)
            {
                return false;
            }
            else if (!mStorageDevice.IsConnected)
            {
                return false;
            }
#endif


            return true;
        }

        /// <summary>
        /// Gets a folder for the user name. This user is a unique key specific to this game.
        /// </summary>
        /// <param name="userName">The user name, which can be anything for a particular game. If multiple profiles are not stored, then a name like "global" can be used.</param>
        /// <returns>The folder for the current user.</returns>
        public static string GetUserFolder(string userName)
        {

            if (!mHasUserFolderBeenInitialized)
            {
                throw new InvalidOperationException("The user folder has not been initialized yet.  Please call FileManager.InitializeUserFolder first");
            }

            string stringToReturn = "";

#if USE_ISOLATED_STORAGE
            stringToReturn = IsolatedStoragePrefix + @"\" + userName + @"\";
#else
            stringToReturn = FileManager.UserApplicationDataForThisApplication + userName + @"\";
#endif

#if IOS
            stringToReturn = stringToReturn.Replace('\\', '/');

            // let's make sure this thing is absolute
			if(!stringToReturn.StartsWith("/"))
            {
                stringToReturn = "/" + stringToReturn;
            }

#endif

            return stringToReturn;
        }

        /// <summary>
        /// Creates a folder for the given user name, which is a unique key for the current app.
        /// This method must be called before GetUserFolder is called.
        /// </summary>
        /// <param name="userName">The user name, which can be anything for a particular game. If multiple profiles are not stored, then a name like "global" can be used.</param>
        public static void InitializeUserFolder(string userName)
        {
#if USE_ISOLATED_STORAGE

    #if IOS || UWP
            // I don't know if we need to get anything here
    #else
            mIsolatedStorageFile = IsolatedStorageFile.GetUserStoreForApplication();
    #endif
            mHasUserFolderBeenInitialized = true;

#else
            string directory = FileManager.UserApplicationDataForThisApplication + userName + @"\";

            // iOS doesn't like backslashes:
            directory = directory.Replace("\\", "/");

            if (!Directory.Exists(directory))
            {
    #if IOS
                if(directory.StartsWith("./"))
                {
                    directory = directory.Substring(1);
                }
    #endif

                Directory.CreateDirectory(directory);
            }
            mHasUserFolderBeenInitialized = true;
#endif
        }
        


#region XML Docs
        /// <summary>
        /// Determines whether a particular file is a graphical file that can be loaded by the FRB Engine.
        /// </summary>
        /// <remarks>
        /// This method does conducts the simple test of looking at the extension of the filename.  If the extension inaccurately
        /// represents the actual format of the file, the method may also inaccurately report whether the file is graphical.
        /// </remarks>
        /// <param name="fileToTest">The file name to test.</param>
        /// <returns>Whether the file is a graphic file.</returns>
#endregion
        public static bool IsGraphicFile(string fileToTest)
        {

            string extension = GetExtension(fileToTest).ToLowerInvariant();

            return (extension == "bmp" || extension == "jpg" || extension == "png" || extension == "tga");
        }


		public static bool IsRelative(string fileName)
		{
			if (fileName == null)
			{
				throw new System.ArgumentException("Cannot check if a null file name is relative.");
			}

#if USES_DOT_SLASH_ABOLUTE_FILES
            if (fileName.Length > 1 && fileName[0] == '.' && fileName[1] == '/')
            {
                return false;
            }
            // let the leading forward slash be treated as absolute:
            else if (fileName.Length > 0 && fileName[0] == '/')
            {
                return false;
            }
            // If it's isolated storage, then it's not relative:
            else if (fileName.Contains(IsolatedStoragePrefix))
            {
                return false;
            }
            else
            {
                return true;
            }
#else
            // .net considers a path relative if it doesn't start with a separator
            // or windows drive identifier
            try
            {
                return !Path.IsPathRooted(fileName);
            }
            catch(ArgumentException e)
            {
                throw new ArgumentException($"Argument exception on {fileName}", e);
            }
#endif
		}


        public static bool IsRelativeTo(string fileName, string directory)
        {
            if(string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if(string.IsNullOrEmpty(directory))
            {
                throw new ArgumentNullException(nameof(directory));
            }

            if (!IsRelative(fileName))
            {
                // the filename is an absolute path

                if (!IsRelative(directory))
                {
                    // just have to make sure that the filename includes the path
                    fileName = fileName.Replace('\\', '/');
                    directory = directory.Replace('\\', '/');

                    if(directory.EndsWith("/", StringComparison.OrdinalIgnoreCase) == false)
                    {
                        // Do this to simplify the code below by allowing a "contains" call
                        directory += "/";
                    }

                    return fileName.IndexOf(directory, StringComparison.OrdinalIgnoreCase) == 0;
                }
            }
            else // fileName is relative
            {
                if (IsRelative(directory))
                {
                    // both are relative, so let's make em full and see what happens
                    string fullFileName = FileManager.MakeAbsolute(fileName);
                    string fullDirectory = FileManager.MakeAbsolute(directory);

                    return IsRelativeTo(fullFileName, fullDirectory);

                }
            }
            return false;
        }


        public static bool IsUrl(string fileName)
        {
            return fileName.IndexOf("http:", StringComparison.OrdinalIgnoreCase) == 0 || fileName.IndexOf("https:", StringComparison.OrdinalIgnoreCase) == 0;
        }


#region Make Absolute/Make Relative

        public static string MakeAbsolute(string pathToMakeAbsolute)
        {
            if (IsRelative(pathToMakeAbsolute) == false)
            {
                throw new ArgumentException("The path is already absolute: " + pathToMakeAbsolute);
            }

            return Standardize(pathToMakeAbsolute);// RelativeDirectory + pathToMakeAbsolute;
        }


        public static string MakeRelative(string pathToMakeRelative)
        {
            return MakeRelative(pathToMakeRelative, RelativeDirectory);
        }


        public static string MakeRelative(string pathToMakeRelative, string pathToMakeRelativeTo)
        {
            if (string.IsNullOrEmpty(pathToMakeRelative) == false)
            {
                pathToMakeRelative = FileManager.Standardize(pathToMakeRelative);
                pathToMakeRelativeTo = FileManager.Standardize(pathToMakeRelativeTo);
                if (!pathToMakeRelativeTo.EndsWith("/"))
                {
                    pathToMakeRelativeTo += "/";
                }

                // Use the old method if we can
                if (pathToMakeRelative.ToLowerInvariant().StartsWith(pathToMakeRelativeTo.ToLowerInvariant()))
                {
                    pathToMakeRelative = pathToMakeRelative.Substring(pathToMakeRelativeTo.Length);
                }
                else
                {
                    // Otherwise, we have to use the new method to identify the common root

                    // Split the path strings
                    string[] path = pathToMakeRelative.Split('/');
                    string[] relpath = pathToMakeRelativeTo.Split('/');

                    string relativepath = string.Empty;

                    // build the new path
                    int start = 0;
                    // November 1, 2011
                    // Do we want to do this:
                    // March 26, 2012
                    // Yes!  Found a bug
                    // while working on wahoo's
                    // tools that we need to check
                    // "start" against the length of
                    // the string arrays.
                    //while (start < path.Length && start < relpath.Length && path[start] == relpath[start])
                    //while (path[start] == relpath[start])
                    while (start < path.Length && start < relpath.Length && String.Equals(path[start],relpath[start], StringComparison.OrdinalIgnoreCase))
                    {
                        start++;
                    }

                    // If start is 0, they aren't on the same drive, so there is no way to make the path relative without it being absolute
                    if (start != 0)
                    {
                        // add .. for every directory left in the relative path, this is the shared root
                        for (int i = start; i < relpath.Length; i++)
                        {
                            if (relpath[i] != string.Empty)
                                relativepath += @"../";
                        }

                        // if the current relative path is still empty, and there are more than one entries left in the path,
                        // the file is in a subdirectory.  Start with ./
                        if (relativepath == string.Empty && path.Length - start > 0)
                        {
                            relativepath += @"./";
                        }

                        // add the rest of the path
                        for (int i = start; i < path.Length; i++)
                        {
                            relativepath += path[i];
                            if (i < path.Length - 1) relativepath += "/";
                        }

                        pathToMakeRelative = relativepath;
                    }
                }
            }
            return pathToMakeRelative;

        }

#endregion


        /// <summary>
        /// Returns the fileName without an extension, or makes no changes if fileName has no extension.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>The file name with extension removed if an extension existed.</returns>
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

        /// <summary>
        /// Modifies the fileName by removing its path, or makes no changes if the fileName has no path.
        /// </summary>
        /// <param name="fileName">The file name to change</param>
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

        /// <summary>
        /// Returns the fileName without a path, or makes no changes if the fileName has no path.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>The modified fileName if a path is found.</returns>
        public static string RemovePath(string fileName)
        {
            RemovePath(ref fileName);

            return fileName;
        }

        /// <summary>
        /// Sets the relative directory to the current directory.
        /// </summary>
        /// <remarks>
        /// The current directory is not necessarily the same as the directory of the .exe.  If the 
        /// .exe is called from a different location (such as the command line in a different folder),
        /// the current directory will differ.
        /// </remarks>
        public static void ResetRelativeToCurrentDirectory()
        {
            RelativeDirectory = (System.IO.Directory.GetCurrentDirectory() + "/").Replace("\\", "/");
        }


        public static void ThrowExceptionIfFileDoesntExist(string fileName)
        {
            // In Silverlight there is no access to System.IO.File.Exists

            
            string fileToCheck = fileName;

            // on iOS, we can do:
            // "./directory/filename.png
            // or
            // "directory/fileName.png"
            // either seems to work fine for content

            if (FileManager.FileExists(fileToCheck) == false)
            {


#if WINDOWS_8
                throw new FileNotFoundException("Could not find the file " + fileName);
#else

                // Help diagnose the problem
                string directory = GetDirectory(fileName);


                if (FileManager.IsRelative(directory))
                {
                    directory = FileManager.MakeAbsolute(directory);
                }

                if (System.IO.Directory.Exists(directory))
                {
                    // See if the file without extension exists
                    string fileNameAsXnb = RemoveExtension(fileName) + ".xnb";

                    if (System.IO.File.Exists(fileNameAsXnb))
                    {
                        throw new FileNotFoundException("Could not find the " +
                            "file \n" + fileName + "\nbut found the XNB file\n" + fileNameAsXnb +
                            ".\nIs the file loaded through the content pipeline?  If so, " +
                            "try loading the file without an extension.");
                    }
                    else
                    {
#if MONODROID

                        FileNotFoundException fnfe = new FileNotFoundException("Could not find the " +
                            "file " + fileName + " but found the directory " + directory +
                            "  Did you type in the name of the file wrong?");
#else

                        FileNotFoundException fnfe = new FileNotFoundException("Could not find the " +
                            "file " + fileName + " but found the directory " + directory +
                            "  Did you type in the name of the file wrong?", fileName);
#endif
                        throw fnfe;
                    }
                }
                else
                {
#if MONODROID

                    throw new FileNotFoundException("Could not find the " +
                        "file " + fileName + " or the directory " + directory);
#else
                    throw new FileNotFoundException("Could not find the " +
                        "file " + fileName + " or the directory " + directory, fileName);
#endif
                }

#endif
            }
        }


        public static void SaveEmbeddedResource(Assembly assemblyContainingResource, string resourceName, string targetFileName)
        {
#if WINDOWS_8
            throw new NotImplementedException();
#else
            System.IO.Directory.CreateDirectory(FileManager.GetDirectory(targetFileName));

            byte[] buffer = GetByteArrayFromEmbeddedResource(assemblyContainingResource, resourceName);

            bool succeeded = true;

            if (File.Exists(targetFileName))
            {
                File.Delete(targetFileName);
            }
            WriteStreamToFile(targetFileName, buffer, succeeded);
#endif
        }

        private static void WriteStreamToFile(string targetFileName, byte[] buffer, bool succeeded)
        {
            if (succeeded)
            {
#if WINDOWS_8 || UWP
                throw new NotImplementedException();
#else
                using (FileStream fs = new FileStream(targetFileName, FileMode.Create))
                {
                    using (BinaryWriter bw = new BinaryWriter(fs))
                    {
                        bw.Write(buffer);
                        bw.Close();
                        fs.Close();
                    }
                }
#endif
            }
        }

#if DEBUG && !FRB_RAW
        public static void SaveGarbage(int numberOfBytes, string fileName)
        {
            byte[] garbageBytes = new byte[numberOfBytes];

            FlatRedBallServices.Random.NextBytes(garbageBytes);

            BinaryWriter writer = null;
            bool handled = false;
#if MONOGAME && !DESKTOP_GL && !STANDARD


            SaveGarbageIsolatedStorage(garbageBytes, fileName);
            handled = true;

#else
            if (!string.IsNullOrEmpty(FileManager.GetDirectory(fileName)) &&
                !Directory.Exists(FileManager.GetDirectory(fileName)))
            {
                Directory.CreateDirectory(FileManager.GetDirectory(fileName));
            }
            FileInfo fileInfo = new FileInfo(fileName);
            writer = new BinaryWriter(fileInfo.Create());
#endif

            if (!handled)
            {
                using (writer)
                {
                    writer.Write(garbageBytes);
                    Close(writer);
                }
            }
        }
#endif

        public static void SaveText(string stringToSave, string fileName)
        {
            SaveText(stringToSave, fileName, System.Text.Encoding.UTF8);

        }


        private static void SaveText(string stringToSave, string fileName, System.Text.Encoding encoding)
        {
            // encoding is currently unused
            fileName = fileName.Replace("/", "\\");
            
            ////////////Early Out///////////////////////
#if WINDOWS
            if (!string.IsNullOrEmpty(FileManager.GetDirectory(fileName)) &&
                !Directory.Exists(FileManager.GetDirectory(fileName)))
            {
                Directory.CreateDirectory(FileManager.GetDirectory(fileName));
            }

            // Note: On Windows, WrietAllText causes 
            // 2 file changes to be raised if the file already exists.
            // This makes Glue always reload the .glux
            // on any file change. This is slow, inconvenient,
            // and can introduce bugs.
            // Therefore, we have to delete the file first to prevent
            // twi file changes:

            if(System.IO.File.Exists(fileName))
            {
                System.IO.File.Delete(fileName);
            }

            System.IO.File.WriteAllText(fileName, stringToSave);
            return;
#endif
            ////////////End Early Out///////////////////////////






            StreamWriter writer = null;

#if MONOGAME && !DESKTOP_GL && !STANDARD


            if (!fileName.Contains(IsolatedStoragePrefix))
            {
                throw new ArgumentException("You must use isolated storage.  Use FileManager.GetUserFolder.");
            }

            fileName = FileManager.GetIsolatedStorageFileName(fileName);

#if WINDOWS_8 || IOS || UWP
            throw new NotImplementedException();
#else
            IsolatedStorageFileStream isfs = null;

            isfs = new IsolatedStorageFileStream(
                fileName, FileMode.Create, mIsolatedStorageFile);

            writer = new StreamWriter(isfs);
#endif

#else
            if (!string.IsNullOrEmpty(FileManager.GetDirectory(fileName)) &&
                !Directory.Exists(FileManager.GetDirectory(fileName)))
            {
                Directory.CreateDirectory(FileManager.GetDirectory(fileName));
            }


            FileInfo fileInfo = new FileInfo(fileName);
            // We used to first delete the file to try to prevent the
            // OS from reporting 2 file accesses.  But I don't think this
            // solved the problem *and* it has the nasty side effect of possibly
            // deleting the entire file , but not being able to save it if there is
            // some weird access issue.  This would result in Glue deleting some files
            // like the user's Game1 or plugins not properly saving files 
            //if (System.IO.File.Exists(fileName))
            //{
            //    System.IO.File.Delete(fileName);
            //}
            writer = fileInfo.CreateText();



#endif

            using (writer)
            {
                writer.Write(stringToSave);

                Close(writer);
            }

#if MONODROID
            isfs.Close();
            isfs.Dispose();
#endif
        }


        public static string Standardize(string fileNameToFix)
        {
            return Standardize(fileNameToFix, RelativeDirectory);
        }


        public static string Standardize(string fileNameToFix, string relativePath)
        {
            return Standardize(fileNameToFix, relativePath, true);
        }

        /// <summary>
        /// Replaces back slashes with forward slashes, but
        /// doesn't break network addresses.
        /// </summary>
        /// <param name="stringToReplace">The string to replace slashes in.</param>
        static void ReplaceSlashes(ref string stringToReplace)
        {
            bool isNetwork = false;
            if (stringToReplace.StartsWith("\\\\"))
            {
                stringToReplace = stringToReplace.Substring(2);
                isNetwork = true;
            }

            stringToReplace = stringToReplace.Replace("\\", "/");

            if (isNetwork)
            {
                stringToReplace = "\\\\" + stringToReplace;
            }
        }

        public static string Standardize(string fileNameToFix, string relativePath, bool makeAbsolute)
        {
            if (fileNameToFix == null)
                return null;

            bool isNetwork = fileNameToFix.StartsWith(@"\\");

            ReplaceSlashes(ref fileNameToFix);

            if (makeAbsolute && !isNetwork)
            {
                // Not sure what this is all about, but everything should be standardized:
                //#if SILVERLIGHT
                //                if (IsRelative(fileNameToFix) && mRelativeDirectory.Length > 1)
                //                    fileNameToFix = mRelativeDirectory + fileNameToFix;

                //#else

                if (IsRelative(fileNameToFix))
                {
                    fileNameToFix = (relativePath + fileNameToFix);
                    ReplaceSlashes(ref fileNameToFix);
                }

                //#endif
            }

#if !XBOX360
            fileNameToFix = RemoveDotDotSlash(fileNameToFix);
 
            if (fileNameToFix.StartsWith("..") && makeAbsolute)
            {
                throw new InvalidOperationException("Tried to remove all ../ but ended up with this: " + fileNameToFix);
            }

#endif
            // It's possible that there will be double forward slashes.
            fileNameToFix = fileNameToFix.Replace("//", "/");

#if !MONODROID && !IOS
            if (!PreserveCase)
            {
                fileNameToFix = fileNameToFix.ToLowerInvariant();
            }
#endif

            return fileNameToFix;
        }

        public static string RemoveDotDotSlash(string fileNameToFix)
        {
#if DEBUG
            if(fileNameToFix == null)
            {
                throw new ArgumentNullException(nameof(fileNameToFix));
            }
#endif
            if (fileNameToFix.Contains(".."))
            {
                // First let's get rid of any ..'s that are in the middle
                // for example:
                //
                // "content/zones/area1/../../background/outdoorsanim/outdoorsanim.achx"
                //
                // would become
                // 
                // "content/background/outdoorsanim/outdoorsanim.achx"

                fileNameToFix = fileNameToFix.Replace("\\", "/");

                //int indexOfNextDotDotSlash = fileNameToFix.IndexOf("../");
                //bool shouldLoop = indexOfNextDotDotSlash > 0;

                //while (shouldLoop)
                //{
                //    int indexOfPreviousDirectory = fileNameToFix.LastIndexOf('/', indexOfNextDotDotSlash - 2, indexOfNextDotDotSlash - 2);

                //    fileNameToFix = fileNameToFix.Remove(indexOfPreviousDirectory + 1, indexOfNextDotDotSlash - indexOfPreviousDirectory + 2);

                //    indexOfNextDotDotSlash = fileNameToFix.IndexOf("../");

                //    shouldLoop = indexOfNextDotDotSlash > 0;


                //}

                int indexOfNextDotDotSlash = GetDotDotSlashIndex(fileNameToFix);


                bool shouldLoop = indexOfNextDotDotSlash > 0;

                while (shouldLoop)
                {
                    // add one to go from "/../" to "../"
                    indexOfNextDotDotSlash++;

                    int indexOfPreviousDirectory = fileNameToFix.LastIndexOf('/', indexOfNextDotDotSlash - 2, indexOfNextDotDotSlash - 2);

                    fileNameToFix = fileNameToFix.Remove(indexOfPreviousDirectory + 1, indexOfNextDotDotSlash - indexOfPreviousDirectory + 2);

                    indexOfNextDotDotSlash = GetDotDotSlashIndex(fileNameToFix);

                    shouldLoop = indexOfNextDotDotSlash > 0;


                }
            }

            if(fileNameToFix.Contains("/./"))
            {
                fileNameToFix = fileNameToFix.Replace("/./", "/");
            }

            if(fileNameToFix.Contains("\\.\\"))
            {
                fileNameToFix = fileNameToFix.Replace("\\.\\", "\\");

            }
            // Let's not force the user to a certain type of slashes
            if (fileNameToFix.Contains("/.\\"))
            {
                fileNameToFix = fileNameToFix.Replace("/.\\", "/");
            }

            if (fileNameToFix.Contains("\\./"))
            {
                fileNameToFix = fileNameToFix.Replace("\\./", "\\");

            }


            return fileNameToFix;
        }

        private static int GetDotDotSlashIndex(string fileNameToFix)
        {
            int indexOfNextDotDotSlash = fileNameToFix.LastIndexOf("/../");

            while (indexOfNextDotDotSlash > 0 && fileNameToFix[indexOfNextDotDotSlash - 1] == '.')
            {
                indexOfNextDotDotSlash = fileNameToFix.LastIndexOf("/../", indexOfNextDotDotSlash);
            }
            return indexOfNextDotDotSlash;
        }


#region XML Methods

        public static T XmlDeserialize<T>(string fileName)
        {
            T objectToReturn = default(T);

#if MONODROID
            if (fileName.Contains(FileManager.IsolatedStoragePrefix) && mHasUserFolderBeenInitialized == false)
            {
                throw new InvalidOperationException("The user folder hasn't been initialized.  Call FileManager.InitializeUserFolder first");
            }
#endif

            if (FileManager.IsRelative(fileName))
                fileName = FileManager.RelativeDirectory + fileName;

            // Do this check before removing the ./ at the end of the file name

            ThrowExceptionIfFileDoesntExist(fileName);


            bool handled = false;

#if WINDOWS_8
            handled = XmlDeserializeWindows8IfIsolatedStorage<T>(fileName, out objectToReturn);
#endif


            if (!handled)
            {
                using (Stream stream = GetStreamForFile(fileName))
                {
                    objectToReturn = XmlDeserialize<T>(stream);
                }
            }

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

                Close(stream);

                return objectToReturn;
            }
        }

        public static T XmlDeserializeFromString<T>(string stringToDeserialize)
        {
            if (string.IsNullOrEmpty(stringToDeserialize))
            {
                return default(T);
            }
            else
            {
                XmlSerializer serializer = GetXmlSerializer<T>();
                TextReader textReader = new StringReader(stringToDeserialize);

                T objectToReturn = (T)serializer.Deserialize(textReader);

                Close(textReader);
                textReader.Dispose();
                return objectToReturn;

            }
        }

        public static void XmlDeserialize<T>(string fileName, out T objectToDeserializeTo)
        {
            objectToDeserializeTo = XmlDeserialize<T>(fileName);
        }

        public static Stream GetStreamForFile(string fileName)
        {
            return GetStreamForFile(fileName, FileMode.Open);
        }

        public static Stream GetStreamForFile(string fileName, FileMode mode)
        {
            // This used to
            // not be here but
            // there is a branch
            // below which was making
            // this absolute if it already
            // wasn't.  I suppose we should
            // always do this...
            if (FileManager.IsRelative(fileName))
            {
                fileName = FileManager.RelativeDirectory + fileName;
            }

#if IOS || ANDROID
            fileName = fileName.ToLowerInvariant();
#endif


            if (fileName.StartsWith("./"))
            {
                fileName = fileName.Substring(2);
            }
            Stream stream = null;
#if USES_DOT_SLASH_ABOLUTE_FILES && !IOS
            // Silverlight and 360 don't like ./ at the start of the file name, but that's what we use to identify an absolute path
            if (fileName.Length > 1 && fileName[0] == '.' && fileName[1] == '/')
                fileName = fileName.Substring(2);



            if (fileName.Contains(IsolatedStoragePrefix) || fileName.Contains(IsolatedStoragePrefix.ToLowerInvariant()))
            {
                fileName = GetIsolatedStorageFileName(fileName);

#if WINDOWS_8 || UWP
                throw new NotImplementedException();
#else
                IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(fileName, mode, mIsolatedStorageFile);

                stream = isfs;
#endif
            }
            else
            {


#if ANDROID || WINDOWS_8 || IOS || UWP
                stream = TitleContainer.OpenStream(fileName);
#else

                fileName = fileName.Replace("\\", "/");
                Uri uri = new Uri(fileName, UriKind.Relative);

                StreamResourceInfo sri = Application.GetResourceStream(uri);

                if (sri == null)
                {

                    throw new Exception("Could not find the file " +
                        fileName + ".  Did you add " + fileName + " to " +
                        "your project and set its 'Build Action' to 'Content'?");
                }

                stream = sri.Stream;
#endif
            }
#else
            // If the file is locked (like by excel) then
            // this will fail. We only want to read it, so it shouldn't...
            //stream = File.OpenRead(fileName);
            // https://stackoverflow.com/questions/12942717/read-log-file-being-used-by-another-process

            //stream = File.OpenRead(fileName);
            stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
#endif

            return stream;
        }

        public static object BinaryDeserialize(Type type, string fileName)
        {
            object objectToReturn = null;

            if (FileManager.IsRelative(fileName))
                fileName = FileManager.RelativeDirectory + fileName;


            ThrowExceptionIfFileDoesntExist(fileName);

#if MONODROID
            // Cute, the 360 doesn't like ./ at the start of the file name.
            if (fileName.Length > 1 && fileName[0] == '.' && fileName[1] == '/')
                fileName = fileName.Substring(2);
#endif


#if WINDOWS_PHONE || XBOX360 || SILVERLIGHT || MONODROID

            var store = mIsolatedStorageFile;

            using (var stream = GetStreamForFile(fileName))
            {
                throw new NotImplementedException();
            }
#elif WINDOWS_8 || UWP
            throw new NotImplementedException();
#else
            using (FileStream stream = System.IO.File.OpenRead(fileName))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                objectToReturn = formatter.Deserialize(stream);
                stream.Close();
            }
#endif
            return objectToReturn;

        }

        public static object XmlDeserialize(Type type, string fileName)
        {
            object objectToReturn = null;

            if (FileManager.IsRelative(fileName))
                fileName = FileManager.RelativeDirectory + fileName;


            ThrowExceptionIfFileDoesntExist(fileName);

#if XBOX360
            // Cute, the 360 doesn't like ./ at the start of the file name.
            if (fileName.Length > 1 && fileName[0] == '.' && fileName[1] == '/')
                fileName = fileName.Substring(2);
            
#endif


            using (Stream stream = GetStreamForFile(fileName))
            {
                XmlSerializer serializer = GetXmlSerializer(type);
                objectToReturn = serializer.Deserialize(stream);
                Close(stream);
            }

            return objectToReturn;
        }







        public static void BinarySerialize<T>(T objectToSerialize, string fileName)
        {
            BinarySerialize(typeof(T), objectToSerialize, fileName);
        }

        public static void BinarySerialize(Type type, object objectToSerialize, string fileName)
        {
            Stream fs = null;

            if (FileManager.IsRelative(fileName))
                fileName = FileManager.RelativeDirectory + fileName;

#if SILVERLIGHT || WINDOWS_PHONE || MONODROID
            var store = mIsolatedStorageFile;

            string directory = FileManager.GetDirectory(fileName);
            if (!string.IsNullOrEmpty(directory) && !store.DirectoryExists(directory))
            {
                if (directory.EndsWith("/"))
                {
                    // the trailing slash causes an exception to be thrown
                    directory = directory.Substring(0, directory.Length - 1);
                }

                store.CreateDirectory(directory);
            }

            fs = GetStreamForFile(fileName, FileMode.OpenOrCreate);

#endif
#if !MONODROID
            string directory = FileManager.GetDirectory(fileName);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(FileManager.GetDirectory(fileName)))
            {
                Directory.CreateDirectory(FileManager.GetDirectory(fileName));
            }

#endif


            try
            {
#if UWP
                throw new NotImplementedException();
#else

                fs = new FileStream(fileName, System.IO.FileMode.Create);
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, objectToSerialize);
#endif
            }
            finally
            {
                if (fs != null) Close(fs);
            }

        }


        public static void XmlSerialize(Type type, object objectToSerialize, string fileName)
        {
            if (FileManager.IsRelative(fileName))
                fileName = FileManager.RelativeDirectory + fileName;

#if USE_ISOLATED_STORAGE
            if (!fileName.Contains(IsolatedStoragePrefix))
            {
                throw new ArgumentException("You must use isolated storage.  Use FileManager.GetUserFolder.");
            }

            fileName = GetIsolatedStorageFileName(fileName);

#endif

#if IOS
            // The "AllOtherPlatforms" method worked on iOS, but
            // only once. After that, files could not be written to - 
            // I'd get an unauthorized exception.
            // 
            XmlSerializeiOS(type, objectToSerialize, fileName);
#else

            XmlSerializeAllOtherPlatforms(type, objectToSerialize, fileName);
#endif
        }

#if IOS
        private static void XmlSerializeiOS(Type type, object objectToSerialize, string fileName)
        {
            fileName = fileName.Replace('\\', '/');
            string directory = FileManager.GetDirectory(fileName);

            if (directory.StartsWith("./"))
            {
                directory = directory.Substring(1);
            }

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string objectAsString;
            XmlSerialize(type, objectToSerialize, out objectAsString);


            if (fileName.StartsWith("./"))
            {
                fileName = fileName.Substring(1);
            }


            File.WriteAllText(fileName, objectAsString);
        }
#endif

        private static void XmlSerializeAllOtherPlatforms(Type type, object objectToSerialize, string fileName)
        {
            string serializedText;
            FileManager.XmlSerialize(type, objectToSerialize, out serializedText);

            FileManager.SaveText(serializedText, fileName);
        }


        public static void XmlSerialize<T>(T objectToSerialize, string fileName)
        {

            XmlSerialize(typeof(T), objectToSerialize, fileName);
        }

        public static void XmlSerialize<T>(T objectToSerialize, out string stringToSerializeTo)
        {
			XmlSerialize(typeof(T), objectToSerialize, out stringToSerializeTo);
		}

		public static void XmlSerialize(Type type, object objectToSerialize, out string stringToSerializeTo)
		{
            using (var memoryStream = new MemoryStream())
            {
                XmlSerializer serializer = GetXmlSerializer(type);
#if UWP
                serializer.Serialize(memoryStream, objectToSerialize);
#else
                Encoding utf8EncodingWithNoByteOrderMark = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                XmlTextWriter xtw = new XmlTextWriter(memoryStream, utf8EncodingWithNoByteOrderMark);
                xtw.Indentation = 2;
                xtw.Formatting = Formatting.Indented;
                serializer.Serialize(xtw, objectToSerialize);

#endif


#if MONOGAME
			    byte[] asBytes = memoryStream.ToArray();
			    stringToSerializeTo = System.Text.Encoding.UTF8.GetString(asBytes, 0, asBytes.Length);
#else
                stringToSerializeTo = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
    #endif
            }

		}

		//Implemented based on interface, not part of algorithm
		public static string RemoveAllNamespaces(string xmlDocument)
		{
			XElement xmlDocumentWithoutNs = RemoveAllNamespaces(XElement.Parse(xmlDocument));

			return xmlDocumentWithoutNs.ToString();
		}

		//Core recursion function
		private static XElement RemoveAllNamespaces(XElement xmlDocument)
		{
			if (!xmlDocument.HasElements)
			{
				XElement xElement = new XElement(xmlDocument.Name.LocalName);
				xElement.Value = xmlDocument.Value;

				foreach (XAttribute attribute in xmlDocument.Attributes())
					xElement.Add(attribute);

				return xElement;
			}
			return new XElement(xmlDocument.Name.LocalName, xmlDocument.Elements().Select(el => RemoveAllNamespaces(el)));
		}

		#endregion

		#endregion

		#region Internal Methods

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

					// For info on this block, see:
					// http://stackoverflow.com/questions/1127431/xmlserializer-giving-filenotfoundexception-at-constructor
#if DEBUG
					XmlSerializer newSerializer = XmlSerializer.FromTypes(new[] { type })[0];
#else
                    XmlSerializer newSerializer = null;

                    newSerializer = new XmlSerializer(type);
#endif


					mXmlSerializers.Add(type, newSerializer);
					return newSerializer;
				}
			}
		}


		#endregion

		#region Private Methods

		private static void CopyDirectoryHelper(string sourceDirectory, string destDirectory, bool clearDestination, List<string> excludeFiles, List<string> excludeDirectories)
		{
            if(!System.IO.Directory.Exists(sourceDirectory))
            {
                throw new ArgumentException($"Could not find source directory {sourceDirectory}");
            }
            destDirectory = FileManager.Standardize(destDirectory);

			if (!destDirectory.EndsWith(@"\") && !destDirectory.EndsWith(@"/"))
			{
				destDirectory += @"\";
			}

			if (Directory.Exists(destDirectory) && clearDestination)
			{
				DeleteDirectory(destDirectory);
			}

			if (!Directory.Exists(destDirectory))
			{
				Directory.CreateDirectory(destDirectory);
			}

			string[] fileList = Directory.GetFiles(sourceDirectory);
			foreach (string file in fileList)
			{
				if (excludeFiles == null || !excludeFiles.Contains(file))
					File.Copy(file, destDirectory + RemovePath(file), true);
			}

			string dirName;
			string[] dirList = Directory.GetDirectories(sourceDirectory);
			foreach (string dir in dirList)
			{
				dirName = RemovePath(dir);

				if (excludeDirectories == null || !excludeDirectories.Contains(dirName))
					CopyDirectoryHelper(dir, destDirectory + dirName, clearDestination, excludeFiles, excludeDirectories);
			}

		}

		public static void Close(Stream stream)
		{
#if UWP
            // Close was removed - no need to do anything
#else
			stream.Close();
#endif
		}

		public static void Close(StreamReader streamReader)
		{
#if UWP
            // Close was removed - no need to do anything
#else
			streamReader.Close();
#endif
		}

		private static void Close(BinaryWriter writer)
		{
#if UWP
            // Close was removed - no need to do anything
#else
			writer.Close();
#endif
		}

		private static void Close(StreamWriter writer)
		{
#if UWP
            // Close was removed - no need to do anything
#else
			writer.Close();
#endif
		}

		public static void Close(TextReader writer)
		{
#if WINDOWS_8 || UWP
            // Close was removed - no need to do anything
#else
			writer.Close();
#endif
        }

#endregion

#endregion
    }
}
