


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !WINDOWS_8
using System.IO.IsolatedStorage;
#else
using Windows.Storage;
using Windows.Foundation;
#endif
using System.IO;
using System.Threading;


namespace FlatRedBall.IO
{
	public static partial class FileManager
	{
#if !WINDOWS_8
        static IsolatedStorageFile mIsolatedStorageFile;
#endif

        const string IsolatedStoragePrefix = "$ISOLATEDSTORAGE";

#if XBOX360
        static string mAssemblyName;
#endif
        static string mLastUserName;


#if WINDOWS_8
        // All FRB calls are expected to be synchronous.  File IO that is to be done async is usually
        // handled in LoadStaticContents or between Screens.  So we have this:
        public static TResult Await<TResult>(this IAsyncOperation<TResult> operation)
        {
            return operation.AsTask().Result;

            //try
            //{
            //    return operation.GetResults();
            //}
            //finally
            //{
            //    operation.Close();
            //}
        }

        public static void Await(this IAsyncAction operation)
        {
            ManualResetEvent mre = new ManualResetEvent(false);

            operation.Completed = (IAsyncAction asyncInfo, AsyncStatus asyncStatus) =>
                {
                    mre.Set();
                };

            if (operation.Status == AsyncStatus.Completed)
            {
                return;
            }
            else
            {
                mre.WaitOne();
            }
            //operation.AsTask().RunSynchronously();
        }
#endif



        static void DeleteFileFromIsolatedStorage(string fileName)
        {
            string original = fileName;
            if (!fileName.Contains(IsolatedStoragePrefix))
            {
                throw new ArgumentException("You must use isolated storage.  Use FileManager.GetUserFolder.");
            }
            fileName = FileManager.GetIsolatedStorageFileName(fileName);

#if XBOX_360
            throw new NotImplementedException();
            //if (!string.IsNullOrEmpty(FileManager.GetDirectory(fileName)) &&
            //    !Directory.Exists(FileManager.GetDirectory(fileName)))
            //{
            //    Directory.CreateDirectory(FileManager.GetDirectory(fileName));
            //}
            //FileInfo fileInfo = new FileInfo(fileName);
            //if (System.IO.File.Exists(fileName))
            //{
            //    System.IO.File.Delete(fileName);
            //}
            //writer = fileInfo.CreateText();
#elif WINDOWS_8

            // Why did we do the original name?  We're in iso storage so we should use the modified name:
            //var storageFile = ApplicationData.Current.LocalFolder.GetFileAsync(original).Await();
            var storageFile = ApplicationData.Current.LocalFolder.GetFileAsync(fileName).Await();
            storageFile.DeleteAsync().Await();
#else
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
            storage.DeleteFile(fileName);

            //IsolatedStorageFileStream isfs = null;

            //isfs = new IsolatedStorageFileStream(
            //    fileName, FileMode.Create, mIsolatedStorageFile);

            //writer = new StreamWriter(isfs);
#endif
        }

        public static bool IsInIsolatedStorage(string fileName)
        {
            bool isInIsolatedStorage = fileName.Contains(IsolatedStoragePrefix);
            return isInIsolatedStorage;
        }

        static bool FileExistsInIsolatedStorage(string fileName)
        {
            string original = fileName;

            #if XBOX360
            if (!mHasUserFolderBeenInitialized)
            {
                throw new InvalidOperationException("The user folder has not been initialized yet");
            }

            fileName = GetIsolatedStorageFileName(fileName);

            var sc = GetStorageContainer();
            bool returnValue = sc.FileExists(fileName);
            DisposeLastStorageContainer();
            return returnValue;
#else
            if (!mHasUserFolderBeenInitialized)
            {
                throw new InvalidOperationException("The user folder has not been initialized yet");
            }

            fileName = GetIsolatedStorageFileName(fileName);



#if WINDOWS_8

	        try
	        {
                var items = ApplicationData.Current.LocalFolder.GetFilesAsync().Await();
                bool found = false;
                foreach (var entry in items)
                {
                    if (entry.Name.ToLowerInvariant() == fileName.ToLowerInvariant())
                    {
                        found = true;
                        break;
                    }
                }

                return found;
                // I don't think we need to do a GetFileAsync because we can just loop through the files above
                // Also, this seems to fail while the for loop above doesn't.
                //var file = ApplicationData.Current.LocalFolder.GetFileAsync( original ).Await();
	        }
	        catch( Exception )
	        {
		        return false;
	        }

	        return true;
            //var folder = ApplicationData.Current.LocalFolder;
            //var items = folder.CreateItemQuery().GetItemsAsync().Await();
            //foreach (var item in items)
            //{
                
            //    if (item.Name == fileName)
            //    {
            //        return true;
            //    }
            //}
            //return false;
#else
            return mIsolatedStorageFile.FileExists(fileName);

#endif
#endif
        }

        static List<string> GetAllFilesInDirectoryIsolatedStorage(string directory)
        {
            if (directory.Contains(IsolatedStoragePrefix))
            {
#if WINDOWS_8
                List<string> toReturn = new List<string>();

                StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

                var query = localFolder.CreateItemQuery();

                var result = query.GetItemsAsync().Await();

                foreach (var item in result)
                {
                    toReturn.Add(item.Name);
                }

                return toReturn;
#else
                // This is in isolated storage
                string[] files = mIsolatedStorageFile.GetFileNames();

                for (int i = 0; i < files.Length; i++)
                {
                    files[i] = IsolatedStorageToCommonFileName(files[i]);
                }

                return new List<string>(files);
#endif
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

#if WINDOWS_8
            throw new NotImplementedException();

#else
            BinaryWriter writer = null;
            fileName = FileManager.GetIsolatedStorageFileName(fileName);

            IsolatedStorageFileStream isfs = null;

            using (isfs = new IsolatedStorageFileStream(fileName, FileMode.Create, mIsolatedStorageFile))
            using (writer = new BinaryWriter(isfs))
            {
                writer.Write(garbageBytes);
                Close(writer);
            }
#endif

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

#if WINDOWS_8
        private static void XmlSerializeWindows8(Type type, object objectToSerialize, string fileName)
        {
            string asString;
            FileManager.XmlSerialize(type, objectToSerialize, out asString);

            // Now we save this to disk:
            var folder = ApplicationData.Current.LocalFolder;
            var storageFile = folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting).Await();
            FileIO.WriteTextAsync(storageFile, asString).Await();

            var items = folder.GetFilesAsync().Await();

            foreach (var entry in items)
            {
                int m = 3;
            }

        }

        public static async void XmlSerializeAsync(object objectToSerialize, string fileName)
        {
            string asString;
            Type type = objectToSerialize.GetType();
            FileManager.XmlSerialize(type, objectToSerialize, out asString);

            // Now we save this to disk:
            var folder = ApplicationData.Current.LocalFolder;
            var storageFile = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(storageFile, asString);
        }

        private static bool XmlDeserializeWindows8IfIsolatedStorage<T>(string fileName, out T objectToReturn)
        {
            bool handled = false;

            if (fileName.Length > 1 && fileName[0] == '.' && fileName[1] == '/')
            {
                fileName = fileName.Substring(2);
            }

            objectToReturn = default(T);

            if (fileName.Contains(IsolatedStoragePrefix))
            {
                fileName = GetIsolatedStorageFileName(fileName);
                handled = true;

                var storageFile = GetStorageFile( fileName );

               
                string asString = FileIO.ReadTextAsync(storageFile).Await();

                objectToReturn = FileManager.XmlDeserializeFromString<T>(asString);
            }
            
            return handled;
        }

        private static StorageFile GetStorageFile(string fileName)
        {
            // Not sure why but GetFileAsync fails, while looping doesn't:
            //var result = ApplicationData.Current.LocalFolder.GetFileAsync(filename).Await();
            StorageFile result = null;
            foreach (var file in ApplicationData.Current.LocalFolder.GetFilesAsync().Await())
            {
                if (file.Name.ToLowerInvariant() == fileName.ToLowerInvariant())
                {
                    result = file;
                    break;
                }
            }

            return result;
        }
#endif
	}
}
