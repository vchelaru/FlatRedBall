using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.IO
{
    #region ChangeInformation class

    public enum FileChangeType
    {
        Modified,
        Deleted,
        Added,
        Renamed
        // todo - expand here
    }

    class FileChange
    {
        public FilePath FilePath { get; set; }
        public FileChangeType ChangeType { get; set; }
    }

    class ChangeInformation
    {
        static TimeSpan mMinimumTimeAfterChangeToReact = new TimeSpan(0, 0, 0, 0, 500);

        List<FileChange> mChangedFiles = new List<FileChange>();

        public bool CanFlush
        {
            get
            {
                return DateTime.Now - LastAdd > mMinimumTimeAfterChangeToReact;
            }
        }

        public DateTime LastAdd
        {
            get;
            private set;
        }

        public ReadOnlyCollection<FileChange> Changes
        {
            get;
            private set;
        }

        public ChangeInformation()
        {
            Changes = new ReadOnlyCollection<FileChange>(mChangedFiles);
        }

        public void Add(string fileName, FileChangeType changeType)
        {
            fileName = FileManager.Standardize(fileName);

            var contains = mChangedFiles.Any(item => item.FilePath == fileName);

            if (!contains)
            {

                mChangedFiles.Add(new FileChange
                {
                    FilePath = fileName,
                    ChangeType = changeType
                });
            }

            LastAdd = DateTime.Now;
        }

        public void Clear()
        {
            mChangedFiles.Clear();
        }

        public void Sort(Comparison<FileChange> comparison)
        {
            mChangedFiles.Sort(comparison);
        }
    }

    #endregion

    public class ChangedFileGroup
    {
        #region Fields

        Dictionary<string, int> mChangesToIgnore;

        FileSystemWatcher mFileSystemWatcher;


        static TimeSpan mMinimumTimeAfterChangeToReact = new TimeSpan(0, 0, 1);

        ChangeInformation mChangedFiles;
        ChangeInformation mDeletedFiles;

        DateTime mLastModification;

        #endregion

        #region Properties

        public bool CanFlush
        {
            get
            {
                return mChangedFiles.CanFlush && mDeletedFiles.CanFlush;
            }
        }

        public object LockObject
        {
            get;
            private set;
        }

        public bool Enabled
        {
            get { return mFileSystemWatcher.EnableRaisingEvents; }
            set { mFileSystemWatcher.EnableRaisingEvents = value; }
        }

        public string Path
        {
            get
            {
                return mFileSystemWatcher.Path;
            }
            set
            {
                if(value?.EndsWith("/") == true)
                {
                    mFileSystemWatcher.Path = value.Substring(0, value.Length-1);
                }
                else
                {
                    mFileSystemWatcher.Path = value;
                }
            }
        }

        public ReadOnlyCollection<FileChange> DeletedFiles => mDeletedFiles.Changes; 

        public ReadOnlyCollection<FileChange> ChangedFiles
        {
            get { return mChangedFiles.Changes; }
        }

        public IEnumerable<FileChange> AllFiles
        {
            get
            {
                List<FileChange> toReturn = new List<FileChange>();
                lock (LockObject)
                {
                    toReturn.AddRange(mDeletedFiles.Changes);
                    toReturn.AddRange(mChangedFiles.Changes);
                }
                return toReturn;
            }
        }

        #endregion

        public Comparison<FileChange> SortDelegate;

        public ChangedFileGroup()
        {
            mChangesToIgnore = new Dictionary<string, int>();
            LockObject = new object();
            mFileSystemWatcher = new FileSystemWatcher();

            mChangedFiles = new ChangeInformation();
            mDeletedFiles = new ChangeInformation();

            mFileSystemWatcher.Filter = "*.*";
            mFileSystemWatcher.IncludeSubdirectories = true;
            mFileSystemWatcher.NotifyFilter =
                NotifyFilters.LastWrite |
                // tiled seems to save the file with a temp name like
                // MyFile.tmx.D1234
                // then it renames it to
                // MyFile.tmx
                // We need to handle the filename changing or else Glue isn't notified of the change.
                // Update - only do this for TMX (see below).
                // Update 2 - We need to handle renames incase
                //            the user renamed files to solve missing
                //            file errors. Just don't handle the .glux
                //            so we don't get double-loads.
                NotifyFilters.FileName |
                NotifyFilters.DirectoryName;


            mFileSystemWatcher.Deleted += new FileSystemEventHandler(HandleFileSystemDelete);
            mFileSystemWatcher.Changed += new FileSystemEventHandler(HandleFileSystemChange);
            // May 6, 2022
            mFileSystemWatcher.Created += HandleFileSystemCreated;
            mFileSystemWatcher.Renamed += HandleRename;
        }

        

        string[] extensionsToIgnoreRenamesAndDeletes = new string[]
        {
            "glux",
            "gluj",
            SaveClasses.GlueProjectSave.ScreenExtension,
            SaveClasses.GlueProjectSave.EntityExtension
        };

        private void HandleRename(object sender, RenamedEventArgs e)
        {
            var extension = FileManager.GetExtension(e.Name);


            var shouldProcess = extensionsToIgnoreRenamesAndDeletes.Contains(extension) == false;

            if(shouldProcess)
            {
                // Process both the old and the new just in case someone depended on the old
                AddChangedFileTo(e.OldFullPath, FileChangeType.Renamed, mChangedFiles);
                AddChangedFileTo(e.FullPath, FileChangeType.Renamed, mChangedFiles);
            }

        }

        public void ClearIgnores()
        {
            mChangesToIgnore.Clear();
        }

        public int NumberOfTimesToIgnore(string file)
        {

            if (FileManager.IsRelative(file))
            {
                throw new Exception("File name should be absolute");
            }
            string standardized = FileManager.Standardize(file, null, false).ToLower();

            if (mChangesToIgnore.ContainsKey(standardized))
            {
                return mChangesToIgnore[standardized];
            }
            else
            {
                return 0;
            }
        }

        public void SetIgnoreDictionary(Dictionary<string, int> sharedInstance)
        {
            mChangesToIgnore = sharedInstance;
        }

        void HandleFileSystemDelete(object sender, FileSystemEventArgs e)
        {
            string fileName = e.FullPath;
            ChangeInformation toAddTo = mDeletedFiles;

            bool shouldIgnoreDelete = GetIfShouldIgnoreDelete(fileName);
            if(!shouldIgnoreDelete)
            {
                AddChangedFileTo(fileName, toAddTo);
            }

        }

        private void HandleFileSystemCreated(object sender, FileSystemEventArgs e)
        {
            string fileName = e.FullPath;

            bool shouldProcess = true;

            if (shouldProcess)
            {
                ChangeInformation toAddTo = mChangedFiles;

                bool wasAdded = AddChangedFileTo(fileName, toAddTo);
            }
        }

        private bool GetIfShouldIgnoreDelete(string fileName)
        {
            var extension = FileManager.GetExtension(fileName);
            return extensionsToIgnoreRenamesAndDeletes.Contains(extension);
        }

        void HandleFileSystemChange(object sender, FileSystemEventArgs e)
        {
            string fileName = e.FullPath;

            bool shouldProcess = true;

            if(e.ChangeType == WatcherChangeTypes.Renamed)
            {
                var extension = FileManager.GetExtension(fileName);
                shouldProcess = !extensionsToIgnoreRenamesAndDeletes.Contains(extension);
            }
            
            if(shouldProcess)
            {
                ChangeInformation toAddTo = mChangedFiles;

                bool wasAdded = AddChangedFileTo(fileName, toAddTo);
            }
        }

        private bool AddChangedFileTo(string fileName, FileChangeType fileChangeType, ChangeInformation toAddTo)
        {
            bool wasAdded = false;

            lock (LockObject)
            {
                bool wasIgnored = TryIgnoreFileChange(fileName);
                if (!wasIgnored)
                {
                    toAddTo.Add(fileName, fileChangeType);
                    if (SortDelegate != null)
                    {
                        toAddTo.Sort(SortDelegate);
                    }
                    wasAdded = true;
                }
                mLastModification = DateTime.Now;
            }
            return wasAdded;
        }

        bool TryIgnoreFileChange(string fileName)
        {
            fileName = fileName.ToLower();
            int changesToIgnore = 0;

            fileName = FileManager.Standardize(fileName, null, false);

            if (mChangesToIgnore.ContainsKey(fileName))
            {
                changesToIgnore = mChangesToIgnore[fileName];

                mChangesToIgnore[fileName] = System.Math.Max(0, changesToIgnore - 1);
            }

            return changesToIgnore > 0;
        }

        public void IgnoreNextChangeOn(string fileName)
        {
            lock (LockObject)
            {

                if (FileManager.IsRelative(fileName))
                {
                    throw new Exception("File name should be absolute");
                }
                string standardized = FileManager.Standardize(fileName, null, false).ToLower() ;
                if (mChangesToIgnore.ContainsKey(standardized))
                {
                    mChangesToIgnore[standardized] = 1 + mChangesToIgnore[standardized];
                }
                else
                {
                    mChangesToIgnore[standardized] = 1;
                }
            }
        }

        public void Clear()
        {
            mDeletedFiles.Clear();
            mChangedFiles.Clear();
        }
    }
}
