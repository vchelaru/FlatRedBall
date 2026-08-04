


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.IsolatedStorage;
using System.IO;
using System.Threading;


namespace FlatRedBall.IO
{
	public static partial class FileManager
	{
        static IsolatedStorageFile mIsolatedStorageFile;

        const string IsolatedStoragePrefix = "$ISOLATEDSTORAGE";

        static string mLastUserName;





        static void DeleteFileFromIsolatedStorage(string fileName)
        {
            string original = fileName;
            if (!fileName.Contains(IsolatedStoragePrefix))
            {
                throw new ArgumentException("You must use isolated storage.  Use FileManager.GetUserFolder.");
            }
            fileName = FileManager.GetIsolatedStorageFileName(fileName);

            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
            storage.DeleteFile(fileName);

            //IsolatedStorageFileStream isfs = null;

            //isfs = new IsolatedStorageFileStream(
            //    fileName, FileMode.Create, mIsolatedStorageFile);

            //writer = new StreamWriter(isfs);
        }

        public static bool IsInIsolatedStorage(string fileName)
        {
            bool isInIsolatedStorage = fileName.Contains(IsolatedStoragePrefix);
            return isInIsolatedStorage;
        }

        static bool FileExistsInIsolatedStorage(string fileName)
        {
            string original = fileName;

            if (!mHasUserFolderBeenInitialized)
            {
                throw new InvalidOperationException("The user folder has not been initialized yet");
            }

            fileName = GetIsolatedStorageFileName(fileName);



            return mIsolatedStorageFile.FileExists(fileName);

        }

        static List<string> GetAllFilesInDirectoryIsolatedStorage(string directory)
        {
            if (directory.Contains(IsolatedStoragePrefix))
            {
                // This is in isolated storage
                string[] files = mIsolatedStorageFile.GetFileNames();

                for (int i = 0; i < files.Length; i++)
                {
                    files[i] = IsolatedStorageToCommonFileName(files[i]);
                }

                return new List<string>(files);
            }
            else
            {
                return new List<string>();
            }

        }

        static void SaveGarbageIsolatedStorage(byte[] garbageBytes, string fileName)
        {
            if (!fileName.Contains(IsolatedStoragePrefix))
            {
                throw new ArgumentException("You must use isolated storage.  Use FileManager.GetUserFolder.");
            }

            BinaryWriter writer = null;
            fileName = FileManager.GetIsolatedStorageFileName(fileName);

            IsolatedStorageFileStream isfs = null;

            using (isfs = new IsolatedStorageFileStream(fileName, FileMode.Create, mIsolatedStorageFile))
            using (writer = new BinaryWriter(isfs))
            {
                writer.Write(garbageBytes);
                Close(writer);
            }

        }



        internal static string GetIsolatedStorageFileName(string fileName)
        {
            // Add 1 to include the backslash "\" at the end of the prefix.
            string modifiedFileName = fileName.Substring(IsolatedStoragePrefix.Length + 1);

            // Silverlight doesn't allow subdirectories in the Isolated Storage.
            // Therefore, let's replace the forward and back slashes with 3 underscores.
            // So something like @"Content\MyFile.scnx" would become "Content___MyFile.scnx"
            // Why three you ask?  Well, if it was only one underscore, then something like @"Content\MyFile.scnx" 
            // and "Content_MyFile.scnx" would be the same.  Two underscores makes it less likely, but 3... that's even
            // less likely.  And it reminds me of the three-wolf t-shirt.  

            //But let's make sure the file doesn't already have three underscores in it:
            if (modifiedFileName.Contains("___"))
            {
                throw new ArgumentException("Can't have three underscores in the file name.  This is a reserved character sequence in FlatRedBall on non-PC devices.");
            }
            // Ok, it doesn't, so let's do our replacemnet here to simulate folders.
            if (modifiedFileName.Contains(@"\"))
            {
                modifiedFileName = modifiedFileName.Replace("\\", "___");
            }
            if (modifiedFileName.Contains("/"))
            {
                modifiedFileName = modifiedFileName.Replace("/", "___");
            }


#if IOS || ANDROID
            modifiedFileName = modifiedFileName.ToLowerInvariant();
#endif

            return modifiedFileName;
        }

        private static string IsolatedStorageToCommonFileName(string fileName)
        {
            return IsolatedStoragePrefix + "/" + fileName.Replace("___", "/");
        }


        internal static bool IsFileNameInUserFolder(string fileName)
        {
            return fileName.StartsWith(IsolatedStoragePrefix);
        }

	}
}
