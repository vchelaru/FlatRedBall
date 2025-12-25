using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EditorObjects.Parsing;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.IO;
using System.IO;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Errors;
using GeneralResponse = ToolsUtilities.GeneralResponse;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System.Collections.Concurrent;

namespace FlatRedBall.Glue.Managers;


public class FileReferenceManager : Singleton<FileReferenceManager>
{
    #region FileReferenceInformation

    class FileReferenceInformation
    {
        public DateTime LastWriteTime;
        public HashSet<FilePath> References = new ();

        public override string ToString()
        {
            return References.Count.ToString();
        }
    }

    #endregion

    ConcurrentDictionary<FilePath, FileReferenceInformation> fileReferences = new ConcurrentDictionary<FilePath, FileReferenceInformation>();

    Dictionary<FilePath, FileReferenceInformation> filesNeededOnDisk = new Dictionary<FilePath, FileReferenceInformation>();

    public Dictionary<FilePath, GeneralResponse> FilesWithFailedGetReferenceCalls { get; private set; } = new Dictionary<FilePath, GeneralResponse>();


    List<string> getFileReferenceCalls = new List<string>();

    internal void ClearFileCache(FilePath absoluteName)
    {
        if (fileReferences.ContainsKey(absoluteName))
        {
            fileReferences.Remove(absoluteName, out _);
        }

        if(filesNeededOnDisk.ContainsKey(absoluteName))
        {
            filesNeededOnDisk.Remove(absoluteName);
        }
    }

    public HashSet<FilePath> GetFilesReferencedBy(FilePath absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive = TopLevelOrRecursive.Recursive)
    {
        var toReturn = new HashSet<FilePath>();

        GetFilesReferencedBy(absoluteName, topLevelOrRecursive, listToFill: toReturn);

        return toReturn;
    }

    public void GetFilesReferencedBy(FilePath absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive, ICollection<FilePath> listToFill)
    { 
        HashSet<FilePath> topLevelOnly = null;
        bool handledByCache = false;

        if(fileReferences.ContainsKey(absoluteName))
        {
            handledByCache = true;
            topLevelOnly = fileReferences[absoluteName].References;
        }

        if (!handledByCache)
        {
            bool succeeded = false;
            try
            {
                topLevelOnly = ContentParser.GetFilesReferencedByAsset(absoluteName, TopLevelOrRecursive.TopLevel).ToHashSet();
                succeeded = true;
            }
            // August 26, 2017
            // I used to have this throw an error when a file was missing, but plugins may not know to handle it, so let's just print
            // output instead
            catch(FileNotFoundException e)
            {
                PluginManager.ReceiveError(e.ToString());
            }
            catch(Exception e)
            {
                PluginManager.ReceiveError(e.ToString());
            }

            if (succeeded)
            {
                var response = PluginManager.GetFilesReferencedBy(absoluteName.FullPath, TopLevelOrRecursive.TopLevel, topLevelOnly);

                if(response.Succeeded)
                {
                    var referenceInfo = new FileReferenceInformation
                    {
                        // Although it might be faster to use DateTime.Now for the last write time, this would only work
                        // if we were to do a > check. However, users can revert files in Git and that should mark the file
                        // as out of date. To support that, we have to do strict equality and use the actual file
                        LastWriteTime = absoluteName.Exists() ? File.GetLastWriteTime(absoluteName.FullPath) : DateTime.MinValue,
                        References = topLevelOnly.ToHashSet()
                    };

                    fileReferences[absoluteName] = referenceInfo;

                    if(FilesWithFailedGetReferenceCalls.ContainsKey(absoluteName))
                    {
                        FilesWithFailedGetReferenceCalls.Remove(absoluteName);
                    }
                }
                else
                {
                    FilesWithFailedGetReferenceCalls[absoluteName] = response;

                    PluginManager.HandleFileReadError(absoluteName, response);
                }
            }
        }
        
        // topLevelOnly could be null if a file wasn't found
        if(topLevelOnly != null)
        {
            var newFiles = topLevelOnly.Except(listToFill).ToList();
            foreach(var item in newFiles)
            {
                listToFill.Add(item);
            }

            if (topLevelOrRecursive == TopLevelOrRecursive.Recursive)
            {
                foreach (var item in newFiles)
                {
                    GetFilesReferencedBy(item, TopLevelOrRecursive.Recursive, listToFill);
                }
            }
        }
    }

    public bool IsFileReferencedRecursively(FilePath filePath)
    {
        var allFilePaths = GlueCommands.Self.FileCommands.GetAllReferencedFilePaths();

        foreach(var filePathInProject in allFilePaths)
        {
            var allReferenced = GetFilesReferencedBy(filePathInProject, TopLevelOrRecursive.Recursive);

            if (allReferenced.Contains(filePath))
            {
                return true;
            }
        }
        return false;
    }

    public HashSet<FilePath> GetFilesNeededOnDiskBy(FilePath absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive)
    {
        HashSet<FilePath> topLevelOnly = null;
        bool handledByCache = false;

        if (filesNeededOnDisk.ContainsKey(absoluteName))
        {
            // compare dates:
            bool isOutOfDate = false;

            if (!isOutOfDate)
            {
                handledByCache = true;
                topLevelOnly = filesNeededOnDisk[absoluteName].References;
            }
        }

        if (!handledByCache)
        {
            // todo: do we want to change this to use 
            topLevelOnly = ContentParser.GetFilesReferencedByAsset(absoluteName, TopLevelOrRecursive.TopLevel);
            PluginManager.GetFilesNeededOnDiskBy(absoluteName.FullPath, TopLevelOrRecursive.TopLevel, topLevelOnly);

            filesNeededOnDisk[absoluteName] = new FileReferenceInformation
            {
                LastWriteTime = absoluteName.Exists() ? File.GetLastWriteTime(absoluteName.FullPath) : DateTime.MinValue,
                References = topLevelOnly.ToHashSet()
            };
        }


        var toReturn = new HashSet<FilePath>();
        toReturn.AddRange(topLevelOnly);

        if (topLevelOrRecursive == TopLevelOrRecursive.Recursive)
        {
            foreach (var item in topLevelOnly)
            {
                toReturn.AddRange(GetFilesNeededOnDiskBy(item, TopLevelOrRecursive.Recursive));
            }
        }

        return toReturn;
    }
}
