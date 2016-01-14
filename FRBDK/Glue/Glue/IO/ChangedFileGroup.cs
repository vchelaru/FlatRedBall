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

    class ChangeInformation
    {
        static TimeSpan mMinimumTimeAfterChangeToReact = new TimeSpan(0, 0, 1);

        List<string> mChangedFiles = new List<string>();

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

        public ReadOnlyCollection<string> Changes
        {
            get;
            private set;
        }

        public ChangeInformation()
        {
            Changes = new ReadOnlyCollection<string>(mChangedFiles);
        }

        public void Add(string fileName)
        {
            fileName = FileManager.Standardize(fileName);

            if (!mChangedFiles.Contains(fileName))
            {
                mChangedFiles.Add(fileName);
            }

            LastAdd = DateTime.Now;
        }

        public void Clear()
        {
            mChangedFiles.Clear();
        }

        public void Sort(Comparison<string> comparison)
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
                mFileSystemWatcher.Path = value;
            }
        }

        public ReadOnlyCollection<string> DeletedFiles
        {
            get { return mDeletedFiles.Changes; }
        }

        public ReadOnlyCollection<string> ChangedFiles
        {
            get { return mChangedFiles.Changes; }
        }

        public IEnumerable<string> AllFiles
        {
            get
            {
                List<string> toReturn = new List<string>();
                lock (LockObject)
                {
                    toReturn.AddRange(mDeletedFiles.Changes);
                    toReturn.AddRange(mChangedFiles.Changes);
                }
                return toReturn;
            }
        }

        #endregion

        public Comparison<string> SortDelegate;

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
                NotifyFilters.DirectoryName;


            mFileSystemWatcher.Deleted += new FileSystemEventHandler(HandleFileSystemDelete);
            mFileSystemWatcher.Changed += new FileSystemEventHandler(HandleFileSystemChange);
        }

        public void SetIgnoreDictionary(Dictionary<string, int> sharedInstance)
        {
            mChangesToIgnore = sharedInstance;
        }

        void HandleFileSystemDelete(object sender, FileSystemEventArgs e)
        {
            string fileName = e.FullPath;
            ChangeInformation toAddTo = mDeletedFiles;

            AddChangedFileTo(fileName, toAddTo);
        }

        void HandleFileSystemChange(object sender, FileSystemEventArgs e)
        {
            string fileName = e.FullPath;
            ChangeInformation toAddTo = mChangedFiles;

            AddChangedFileTo(fileName, toAddTo);
        }

        private void AddChangedFileTo(string fileName, ChangeInformation toAddTo)
        {
            lock (LockObject)
            {
                bool wasIgnored = TryIgnoreFileChange(fileName);
                if (!wasIgnored)
                {
                    toAddTo.Add(fileName);
                    if (SortDelegate != null)
                    {
                        toAddTo.Sort(SortDelegate);
                    }
                }
                mLastModification = DateTime.Now;
            }
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
