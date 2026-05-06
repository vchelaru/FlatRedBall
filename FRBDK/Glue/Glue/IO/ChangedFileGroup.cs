using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.IO;

#region ChangeInformation class

public enum FileChangeType
{
    Modified,
    Deleted,
    Created,
    Renamed
    // todo - expand here
}

public class FileChange
{
    public FilePath FilePath { get; set; }
    public FileChangeType ChangeType { get; set; }

    public override string ToString() => FilePath?.ToString();
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

    /// <summary>
    /// Attempts to add the change to the internal list of changed files. This only adds if the file is not already added
    /// </summary>
    /// <param name="fileName">The file to add</param>
    /// <param name="changeType">The file change</param>
    /// <returns>Whether the file was added</returns>
    public bool Add(FilePath filePath, FileChangeType changeType)
    {
        var wasAdded = false;
        lock (mChangedFiles)
        {
            var contains = mChangedFiles.Any(item => item.FilePath == filePath);

            if (!contains)
            {

                mChangedFiles.Add(new FileChange
                {
                    FilePath = filePath,
                    ChangeType = changeType
                });
                wasAdded = true;
            }

            LastAdd = DateTime.Now;
        }
        return wasAdded;
    }

    public void Clear()
    {
        lock (mChangedFiles)
        {
            mChangedFiles.Clear();
        }
    }

    public void Sort(Comparison<FileChange> comparison)
    {
        lock (mChangedFiles)
        {
            mChangedFiles.Sort(comparison);
        }

    }
}

#endregion

/// <summary>
/// Watches a directory tree for file changes and queues them for Glue to react to.
///
/// Two ways to suppress watcher reactions to a file Glue is about to write itself:
///
/// * <see cref="IgnoreChangeOnFileUntil(FilePath, DateTimeOffset)"/> -- PREFERRED
///   for any OS-level file write. Suppresses any change to the file until the
///   supplied timestamp passes. Robust to OS event-count variance: a single
///   logical save may produce 1 event (in-place write) or 2-3 events (atomic
///   save: write-temp + rename + delete-original) depending on the writer.
///   Time-based suppression doesn't care how many events fire; the whole window
///   is silent. This is the model the Gum project uses (see Gum's
///   FileWatchIgnoreList) and the one to default to for self-saves.
///
/// * <see cref="IgnoreNextChangeOn"/> -- SPECIAL-PURPOSE counter-based ignore.
///   Drops exactly the next event matching the file. Only correct when the
///   caller knows EXACTLY how many events a single save will produce. That
///   guarantee is hard to make for any OS file write -- different editors,
///   different drives, network shares, antivirus, atomic-save vs. in-place
///   writes all change the count. Keep this API for the rare cases where you
///   genuinely want event-count-based suppression (e.g., expecting exactly
///   one synthetic notification you raised yourself). Do not use for OS-level
///   file writes -- use <see cref="IgnoreChangeOnFileUntil(FilePath, DateTimeOffset)"/>
///   or its convenience overload instead.
///
/// Historical note: <see cref="extensionsToIgnoreDeletes"/> originated as a
/// fallback when only the counter API existed. Glue's own saves could produce
/// extra Created/Renamed events that the counter couldn't catch (because the
/// counter only consumed one event), so the simplest fix was to drop those
/// events entirely for Glue-owned file types. With timed suppression in place
/// that fallback is unnecessary for Created/Renamed -- atomic-save tools (VS
/// Code, Claude Code, and others that write-temp + rename) must be allowed
/// to update these files. The list is retained for Deleted only: a .glej etc.
/// disappearing from disk should go through a deliberate "the project lost a
/// file" code path, not the auto-reload path that Modified/Created/Renamed
/// share. If you ever revert to a writer that produces multiple events for
/// a single self-save and the counter API can't catch them all, the right
/// answer is to switch the writer to use timed suppression -- not to widen
/// this skip-list.
/// </summary>
public class ChangedFileGroup
{
    #region Fields

    // Files like 
    // the .glux and
    // .csproj files are
    // saved by Glue, but
    // when they change on
    // disk Glue needs to react
    // to the change.  To react to
    // the change, Glue keeps a file
    // watch on these files.  However
    // when Glue saves these files it kicks
    // of a file change.  Therefore, any time
    // Glue changes one of these files it needs
    // to know to ignore the next file change since
    // it came from itself.  Furthermore, multiple plugins
    // and parts of Glue may kick off multiple saves.  Therefore
    // we can't just keep track of a bool on whether to ignore the
    // next change or not - instead we have to keep track of an int
    // to mark how many changes Glue should ignore.
    ConcurrentDictionary<FilePath, int> mChangesToIgnore;

    ConcurrentDictionary<FilePath, DateTimeOffset> timedChangesToIgnore;

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

    object LockObject;

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
            if (value?.EndsWith("/") == true)
            {
                mFileSystemWatcher.Path = value.Substring(0, value.Length - 1);
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
        mChangesToIgnore = new ConcurrentDictionary<FilePath, int>();
        timedChangesToIgnore = new ConcurrentDictionary<FilePath, DateTimeOffset>();

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


        mFileSystemWatcher.Deleted += HandleFileSystemDelete;
        mFileSystemWatcher.Changed += HandleFileSystemChange;
        // May 6, 2022
        mFileSystemWatcher.Created += HandleFileSystemCreated;
        mFileSystemWatcher.Renamed += HandleRename;
    }

    // Deletions on these extensions are dropped by the watcher: a Glue-owned
    // JSON file disappearing from disk is destructive and should be handled by
    // a deliberate "the project lost a file" flow, not the auto-reload queue.
    // Created and Renamed events on these files are NOT in this list -- timed
    // self-save suppression (IgnoreChangeOnFileUntil) handles Glue's own writes,
    // and external editors that do atomic saves (write-temp + rename, e.g. VS
    // Code, Claude Code) need their final-rename events delivered or their
    // edits never reach Glue. See the class summary for the full history.
    string[] extensionsToIgnoreDeletes = new string[]
    {
        "glux",
        "gluj",
        SaveClasses.GlueProjectSave.ScreenExtension,
        SaveClasses.GlueProjectSave.EntityExtension
    };



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
        string standardized = FileManager.Standardize(file, null, false).ToLowerInvariant();

        if (mChangesToIgnore.TryGetValue(standardized, out var change))
        {
            return change;
        }
        else
        {
            return 0;
        }
    }

    void HandleFileSystemDelete(object sender, FileSystemEventArgs e)
    {
        string fileName = e.FullPath;

        FileReferenceManager.Self.ClearFileCache(fileName);

        ChangeInformation toAddTo = mDeletedFiles;

        var extension = FileManager.GetExtension(fileName);
        bool shouldProcess = !extensionsToIgnoreDeletes.Contains(extension) &&
            !IsSkippedBasedOnTypeOrLocation(e.Name);


        if (shouldProcess)
        {
            TryAddChangedFileTo(fileName, FileChangeType.Deleted, toAddTo);
        }

    }

    void HandleFileSystemChange(object sender, FileSystemEventArgs e)
    {
        string fileName = e.FullPath;

        FileReferenceManager.Self.ClearFileCache(fileName);

        bool shouldProcess = !IsSkippedBasedOnTypeOrLocation(e.Name);

        // Note: previously this dropped Renamed events for .glej/.glsj/.gluj/.glux
        // because the counter-based self-save suppression couldn't catch the
        // extra rename event an atomic-save would produce. With timed self-save
        // suppression (IgnoreChangeOnFileUntil) in place those events flow
        // through; dedup at ChangeInformation.Add and the CanFlush debounce
        // window absorb any duplicate event clusters from a single save.

        if (shouldProcess)
        {
            ChangeInformation toAddTo = mChangedFiles;

            TryAddChangedFileTo(fileName, FileChangeType.Modified, toAddTo);
        }
    }

    private void HandleFileSystemCreated(object sender, FileSystemEventArgs e)
    {
        string fileName = e.FullPath;

        FileReferenceManager.Self.ClearFileCache(fileName);

        // .glej/.glsj/.gluj/.glux Created events used to be filtered here; see
        // class summary. They are now allowed through so external editors that
        // do atomic saves (write-temp + rename, producing a Created on the temp
        // path and then a Renamed to the final extension) can update these files.
        bool shouldProcess = !IsSkippedBasedOnTypeOrLocation(e.Name);


        if (shouldProcess)
        {
            ChangeInformation toAddTo = mChangedFiles;

            TryAddChangedFileTo(fileName, FileChangeType.Created, toAddTo);
        }
    }

    private void HandleRename(object sender, RenamedEventArgs e)
    {
        ///////////////early out////////////////
        if(string.IsNullOrEmpty(e.Name))
        {
            // early out
            return;
        }
        ////////////end early out///////////////

        FileReferenceManager.Self.ClearFileCache(e.OldFullPath);
        FileReferenceManager.Self.ClearFileCache(e.FullPath);

        // .glej/.glsj/.gluj/.glux Renamed events used to be filtered here; see
        // class summary. They are now allowed through. Atomic-save tools end
        // their save with a Renamed event from a temp filename to the final
        // extension, and that event is the one that conveys the new content.
        var shouldProcess = !IsSkippedBasedOnTypeOrLocation(e.Name);

        if (shouldProcess)
        {
            if(!IsSkippedBasedOnTypeOrLocation(e.OldFullPath))
            {
                // Process both the old and the new just in case someone depended on the old
                TryAddChangedFileTo(e.OldFullPath, FileChangeType.Renamed, mChangedFiles);
            }
            TryAddChangedFileTo(e.FullPath, FileChangeType.Renamed, mChangedFiles);
        }
    }

    bool IsSkippedBasedOnTypeOrLocation(FilePath filePath) =>
        filePath.Standardized.Contains("/.vs/")
        || filePath.Standardized.StartsWith(".vs")
        || filePath.Standardized.Contains("/bin/Debug/", StringComparison.InvariantCultureIgnoreCase)
        || filePath.Standardized.EndsWith(".generated.cs", StringComparison.InvariantCultureIgnoreCase)
        // temporary shader built file:
        || filePath.Extension.EndsWith("~")
        || (filePath.Extension == "tmp" && filePath.Standardized.Contains(".fx~"));

    private void TryAddChangedFileTo(FilePath fileName, FileChangeType fileChangeType, ChangeInformation toAddTo)
    {
        bool wasAdded = false;

        bool wasIgnored = false;

        // When a file changes, it's deleted and then added. Therefore, if we make a change (delete + create), but we ignore
        // one change, that means that the delete will get ignored, but the create won't. Therefore, we should not check ignores
        // on deletes:
        if (fileChangeType != FileChangeType.Deleted)
        {
            wasIgnored = TryIgnoreFileChange(fileName);
        }

        if(!wasIgnored)
        {
            if(timedChangesToIgnore.TryGetValue(fileName, out DateTimeOffset expiration))
            {
                wasIgnored = expiration > DateTimeOffset.Now;
            }
        }

        if (wasIgnored)
        {
            if (FileWatchManager.IsPrintingDiagnosticOutput)
            {
                GlueCommands.Self.PrintOutput($"Ignoring {fileChangeType} on {fileName}");
            }
        }
        else // !wasIgnored
        {
            lock (LockObject)
            {
                wasAdded = toAddTo.Add(fileName, fileChangeType);
                if (SortDelegate != null)
                {
                    toAddTo.Sort(SortDelegate);
                }
            }
            if (wasAdded && FileWatchManager.IsPrintingDiagnosticOutput)
            {
                GlueCommands.Self.PrintOutput($"Queueign change {fileChangeType} on {fileName} for flushing");
            }
        }
        mLastModification = DateTime.Now;
    }

    bool TryIgnoreFileChange(FilePath fileName)
    {
        int changesToIgnore;

        if(mChangesToIgnore.TryGetValue(fileName, out changesToIgnore))
        {
            mChangesToIgnore[fileName] = System.Math.Max(0, changesToIgnore - 1);
        }
        else
        {
            changesToIgnore = 0;
        }
        return changesToIgnore > 0;
    }


    /// <summary>
    /// Counter-based ignore: drops exactly the next file event matching
    /// <paramref name="fileName"/>. SPECIAL-PURPOSE only -- correct just when
    /// the caller knows EXACTLY how many events the next save will produce.
    /// For OS-level file writes that count is unreliable (atomic-save tools
    /// fire Created + Deleted + Renamed for a single logical save, in-place
    /// writes fire only Modified, etc.); use
    /// <see cref="IgnoreChangeOnFileUntil(FilePath, DateTimeOffset)"/> or its
    /// convenience overload instead. See the class summary for the full
    /// rationale.
    /// </summary>
    public void IgnoreNextChangeOn(string fileName)
    {
        if (FileManager.IsRelative(fileName))
        {
            throw new Exception("File name should be absolute");
        }
        string standardized = FileManager.Standardize(fileName, null, false).ToLowerInvariant();
        if (mChangesToIgnore.ContainsKey(standardized))
        {
            mChangesToIgnore[standardized] = 1 + mChangesToIgnore[standardized];
        }
        else
        {
            mChangesToIgnore[standardized] = 1;
        }
    }

    /// <summary>
    /// Time-based ignore (preferred for self-saves): suppresses watcher
    /// reactions to <paramref name="filePath"/> until <paramref name="expiration"/>
    /// passes. Survives any number of OS file events fired during the window,
    /// which matters because a single logical save may produce 1, 2, or 3
    /// events depending on the writer (in-place vs. atomic). If the file
    /// already has a pending expiration, the LATER of the two times wins --
    /// a shorter call must never shorten a longer one already in flight.
    /// </summary>
    public void IgnoreChangeOnFileUntil(FilePath filePath, DateTimeOffset expiration)
    {
        if (timedChangesToIgnore.TryGetValue(filePath, out DateTimeOffset existing))
        {
            // Take the later of existing vs. requested -- a shorter window
            // must never shorten a longer one already in flight. (Previously
            // this branch returned `expiration` unconditionally, defeating
            // the merge logic; see git history if you need the bug context.)
            timedChangesToIgnore[filePath] = existing > expiration ? existing : expiration;
        }
        else
        {
            timedChangesToIgnore[filePath] = expiration;
        }
    }

    /// <summary>
    /// Convenience overload: ignores changes to <paramref name="filePath"/>
    /// for <paramref name="duration"/> (default 5 seconds, matching Gum's
    /// FileWatchIgnoreList default). Use this from save sites instead of the
    /// counter-based <see cref="IgnoreNextChangeOn"/> -- it's robust to OS
    /// event-count variance.
    /// </summary>
    public void IgnoreChangeOnFileUntil(FilePath filePath, TimeSpan? duration = null)
    {
        var window = duration ?? TimeSpan.FromSeconds(5);
        IgnoreChangeOnFileUntil(filePath, DateTimeOffset.Now + window);
    }

    public void Clear()
    {
        mDeletedFiles.Clear();
        mChangedFiles.Clear();
    }
}
