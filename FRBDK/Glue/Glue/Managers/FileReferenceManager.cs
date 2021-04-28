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

namespace FlatRedBall.Glue.Managers
{
    public class FileReferenceManager : Singleton<FileReferenceManager>
    {
        #region FileReferenceInformation

        class FileReferenceInformation
        {
            public DateTime LastWriteTime;
            public List<FilePath> References = new List<FilePath>();

            public override string ToString()
            {
                return References.Count.ToString();
            }
        }

        #endregion

        Dictionary<FilePath, FileReferenceInformation> fileReferences = new Dictionary<FilePath, FileReferenceInformation>();

        Dictionary<FilePath, FileReferenceInformation> filesNeededOnDisk = new Dictionary<FilePath, FileReferenceInformation>();

        public Dictionary<FilePath, GeneralResponse> FilesWithFailedGetReferenceCalls { get; private set; } = new Dictionary<FilePath, GeneralResponse>();

        List<string> getFileReferenceCalls = new List<string>();

        internal void ClearFileCache(FilePath absoluteName)
        {
            if (fileReferences.ContainsKey(absoluteName))
            {
                fileReferences.Remove(absoluteName);
            }

            if(filesNeededOnDisk.ContainsKey(absoluteName))
            {
                filesNeededOnDisk.Remove(absoluteName);
            }
        }

        public List<FilePath> GetFilesReferencedBy(FilePath absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive = TopLevelOrRecursive.Recursive)
        {
            var toReturn = new List<FilePath>();

            GetFilesReferencedBy(absoluteName, topLevelOrRecursive, listToFill: toReturn);

            return toReturn;
        }

        public void GetFilesReferencedBy(FilePath absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive, List<FilePath> listToFill)
        { 
            List<FilePath> topLevelOnly = null;
            bool handledByCache = false;

            if(fileReferences.ContainsKey(absoluteName))
            {
                // compare dates:
                bool isOutOfDate = absoluteName.Exists() && 
                    //File.GetLastWriteTime(standardized) > fileReferences[standardized].LastWriteTime;
                    // Do a != in case the user reverts a file
                    File.GetLastWriteTime(absoluteName.FullPath) != fileReferences[absoluteName].LastWriteTime;

                if(!isOutOfDate)
                {
                    handledByCache = true;
                    topLevelOnly = fileReferences[absoluteName].References;
                }
            }

            if (!handledByCache)
            {
                bool succeeded = false;
                try
                {
                    topLevelOnly = ContentParser.GetFilesReferencedByAsset(absoluteName, TopLevelOrRecursive.TopLevel);
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
                            LastWriteTime = absoluteName.Exists() ? File.GetLastWriteTime(absoluteName.FullPath) : DateTime.MinValue,
                            References = topLevelOnly
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

                        // todo - need to raise an event here on parse error:
                        PluginManager.HandleFileReadError(absoluteName, response);
                    }

                }
            }
            
            // topLevelOnly could be null if a file wasn't found
            if(topLevelOnly != null)
            {
                var newFiles = topLevelOnly.Except(listToFill).ToList();
                listToFill.AddRange(newFiles);

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

        public List<FilePath> GetFilesNeededOnDiskBy(FilePath absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive)
        {
            List<FilePath> topLevelOnly = null;
            bool handledByCache = false;

            if (filesNeededOnDisk.ContainsKey(absoluteName))
            {
                // compare dates:
                bool isOutOfDate = absoluteName.Exists() &&
                    //File.GetLastWriteTime(standardized) > filesNeededOnDisk[standardized].LastWriteTime;
                    // Do a != in case the user reverts a file
                    File.GetLastWriteTime(absoluteName.FullPath) != filesNeededOnDisk[absoluteName].LastWriteTime;

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
                    References = topLevelOnly
                };
            }


            var toReturn = new List<FilePath>();
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
}
