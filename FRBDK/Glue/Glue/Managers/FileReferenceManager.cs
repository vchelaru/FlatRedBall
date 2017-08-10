using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EditorObjects.Parsing;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.IO;
using System.IO;

namespace FlatRedBall.Glue.Managers
{
    public class FileReferenceManager : Singleton<FileReferenceManager>
    {
        class FileReferenceInformation
        {
            public DateTime LastWriteTime;
            public List<string> References = new List<string>();

            public override string ToString()
            {
                return References.Count.ToString();
            }
        }


        Dictionary<string, FileReferenceInformation> fileReferences = new Dictionary<string, FileReferenceInformation>();
        Dictionary<string, FileReferenceInformation> filesNeededOnDisk = new Dictionary<string, FileReferenceInformation>();

        internal void ClearFileCache(string absoluteName)
        {
            string standardized = FileManager.Standardize(absoluteName);

            if (fileReferences.ContainsKey(standardized))
            {
                fileReferences.Remove(standardized);
            }

            if(filesNeededOnDisk.ContainsKey(standardized))
            {
                filesNeededOnDisk.Remove(standardized);
            }
        }

        List<string> getFileReferenceCalls = new List<string>();


        public List<string> GetFilesReferencedBy(string absoluteName)
        {
            return GetFilesReferencedBy(absoluteName, TopLevelOrRecursive.Recursive);
        }

        public List<string> GetFilesReferencedBy(string absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive)
        {
            List<string> topLevelOnly = null;
            bool handledByCache = false;

            string standardized = FileManager.Standardize(absoluteName);
            if(fileReferences.ContainsKey(standardized))
            {
                // compare dates:
                bool isOutOfDate = File.Exists(standardized) && 
                    //File.GetLastWriteTime(standardized) > fileReferences[standardized].LastWriteTime;
                    // Do a != in case the user reverts a file
                    File.GetLastWriteTime(standardized) != fileReferences[standardized].LastWriteTime;

                if(!isOutOfDate)
                {
                    handledByCache = true;
                    topLevelOnly = fileReferences[standardized].References;
                }
            }

            if (!handledByCache)
            {
                topLevelOnly = ContentParser.GetFilesReferencedByAsset(absoluteName, TopLevelOrRecursive.TopLevel);
                PluginManager.GetFilesReferencedBy(absoluteName, TopLevelOrRecursive.TopLevel, topLevelOnly);
                // let's remove ../ if we can:
                for (int i = 0; i < topLevelOnly.Count; i++)
                {
                    topLevelOnly[i] = FlatRedBall.IO.FileManager.RemoveDotDotSlash(topLevelOnly[i]);
                }

                fileReferences[standardized] = new FileReferenceInformation
                {
                    LastWriteTime = File.Exists(standardized) ? File.GetLastWriteTime(standardized) : DateTime.MinValue,
                    References = topLevelOnly
                };
            }


            List<string> toReturn = new List<string>();
            toReturn.AddRange(topLevelOnly);

            if (topLevelOrRecursive == TopLevelOrRecursive.Recursive)
            {
                foreach (var item in topLevelOnly)
                {
                    toReturn.AddRange(GetFilesReferencedBy(item, TopLevelOrRecursive.Recursive));
                }
            }

            return toReturn.Distinct().ToList();
        }


        public List<string> GetFilesNeededOnDiskBy(string absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive)
        {
            List<string> topLevelOnly = null;
            bool handledByCache = false;

            string standardized = FileManager.Standardize(absoluteName);
            if (filesNeededOnDisk.ContainsKey(standardized))
            {
                // compare dates:
                bool isOutOfDate = File.Exists(standardized) &&
                    //File.GetLastWriteTime(standardized) > filesNeededOnDisk[standardized].LastWriteTime;
                    // Do a != in case the user reverts a file
                    File.GetLastWriteTime(standardized) != filesNeededOnDisk[standardized].LastWriteTime;

                if (!isOutOfDate)
                {
                    handledByCache = true;
                    topLevelOnly = filesNeededOnDisk[standardized].References;
                }
            }

            if (!handledByCache)
            {
                // todo: do we want to change this to use 
                topLevelOnly = ContentParser.GetFilesReferencedByAsset(absoluteName, TopLevelOrRecursive.TopLevel);
                PluginManager.GetFilesNeededOnDiskBy(absoluteName, TopLevelOrRecursive.TopLevel, topLevelOnly);
                // let's remove ../ if we can:
                for (int i = 0; i < topLevelOnly.Count; i++)
                {
                    topLevelOnly[i] = FlatRedBall.IO.FileManager.RemoveDotDotSlash(topLevelOnly[i]);
                }

                filesNeededOnDisk[standardized] = new FileReferenceInformation
                {
                    LastWriteTime = File.Exists(standardized) ? File.GetLastWriteTime(standardized) : DateTime.MinValue,
                    References = topLevelOnly
                };
            }


            List<string> toReturn = new List<string>();
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
