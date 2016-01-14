using System.Collections.Generic;
using Microsoft.Build.BuildEngine;
using FlatRedBall.IO;
using System.IO;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    #region ProjectType Enum

    //public enum ProjectType
    //{
    //    Xna = 1,
    //    Mdx,
    //    Fsb,
    //    Xna360,
    //    XnaContent, 
    //    Android,
    //    WindowsPhone,
    //    Xna4,
    //    Xna4_360,
    //    MonoDroid

    //}

    #endregion

    public enum BuildItemMembershipType
    {
        CopyIfNewer,
        CompileOrContentPipeline,
        Content,
        Any,
        BundleResource,
        AndroidAsset
    }

    public delegate void SaveDelegate(string fileName);

    public abstract class ProjectBase : IEnumerable<BuildItem>
    {
        public static string AccessContentDirectory = "content/";

        #region VersionedFile
        public struct VersionedFile
        {
            public string Url;
            public string FileName;
            public string Name;
            public bool UpdateProject;
            public bool UpdateContentProject;
        }
        #endregion

        #region Fields

        protected Dictionary<string, BuildItem> mBuildItemDictionaries =
            new Dictionary<string, BuildItem>();

        #endregion

        #region Properties

        public virtual bool ContentCopiedToOutput { get { return true; } }


        public ProjectBase ContentProject
        {
            get;
            set;
        }

        public ProjectBase OriginalProjectBaseIfSynced
        {
            get;
            set;
        }

        public ProjectBase MasterProjectBase
        {
            get;
            set;
        }

        public abstract string FullFileName
        {
            get;
        }

        public string Directory
        {
            get
            {
                return FileManager.GetDirectory(FullFileName);
            }
        }

        public bool IsContentProject
        {
            get;
            set;
        }

        public abstract string Name
        {
            get;
        }

        public abstract bool IsDirty
        {
            get;
            set;
        }

        public abstract BuildItemGroup EvaluatedItems
        {
            get;
        }

        public virtual string RootNamespace
        {
            get
            {
                return this.Name;
            }
        }

        /// <summary>
        /// This is only used for code gen
        /// </summary>
        public string ContainedFilePrefix
        {
            get;
            set;
        }

        public virtual string ContentDirectory
        {
            get { return ""; }
        }

        public string FullContentPath
        {
            get { return Directory + ContentDirectory; }
        }

        public abstract List<string> LibraryDlls { get; } 

        #endregion

        #region Events

        public event SaveDelegate Saving;

        #endregion

#if GLUE
        public bool SaveAsRelativeSyncedProject;
        public bool SaveAsAbsoluteSyncedProject;
        /// <summary>
        /// Adds the argument absoluteFile to the project. This method will not first check
        /// if the file is already part of the project or not. See IsFilePartOfProject for
        /// checking if the file is already part of the project.
        /// </summary>
        /// <param name="absoluteFile">The absolute file name to add.</param>
        /// <returns>The BuildItem which was created and added to the project.</returns>
        public abstract BuildItem AddContentBuildItem(string absoluteFile);

        public abstract BuildItem AddContentBuildItem(string absoluteFile, SyncedProjectRelativeType relativityType, bool forceToContentPipeline);
        public abstract void UpdateContentFile(string sourceFileName);
#endif
        public abstract string FolderName { get; }

        public bool IsFilePartOfProject(string fileToUpdate)
        {
            return IsFilePartOfProject(fileToUpdate, BuildItemMembershipType.Any);
        }
        public abstract bool IsFilePartOfProject(string fileToUpdate, BuildItemMembershipType membershipType);
        public abstract bool IsFilePartOfProject(string fileToUpdate, BuildItemMembershipType membershipType, bool relativeItem);

        public virtual List<string> GetErrors()
        {
            return new List<string>();
        }

        public void Save()
        {
            Save(FullFileName);
        }
        public void Save(string fileName)
        {
            if (IsDirty)
            {
                if (Saving != null)
                {
                    Saving(fileName);
                }
                ForceSave(fileName);
            }
        }

        protected abstract void ForceSave(string fileName);

        public abstract void Load(string fileName);
        public abstract void MakeBuildItemNested(BuildItem item, string parent);
        public abstract void SyncTo(ProjectBase projectBase, bool performTranslation);
        protected abstract BuildItem AddCodeBuildItem(string fileName, bool isSyncedProject, string directoryToCreate);
        /// <summary>
        /// A string which is intended to uniquely identify the project type.
        /// For example, XNA 4 projets might return "Xna4".  This is not tied to
        /// any preprocessor defines.
        /// </summary>
        public abstract string ProjectId { get; }
        public abstract string PrecompilerDirective { get; }
        

        public BuildItem GetItem(string itemName)
        {
            return GetItem(itemName, true);
        }

        public BuildItem GetItem(string itemName, bool standardizeItemName)
        {
            if (standardizeItemName)
            {
                itemName = StandardizeItemName(itemName);
            }
            else
            {
                itemName = itemName.Replace("/", "\\").ToLower();
            }
            if (!mBuildItemDictionaries.ContainsKey(itemName))
            {
                foreach (var item in mBuildItemDictionaries.Values)
                {
                    // This may be a link
                    var foundLink = (string)item.GetEvaluatedMetadata("Link");
                    if (item.Include.Contains("Anim"))
                    {
                        int m = 3;
                    }
                    if (!string.IsNullOrEmpty(foundLink) && foundLink.Equals(itemName, System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        return item;
                    }
                }
                return null;// mBuildItemDictionaries[itemName];
            }
            else
            {
                return mBuildItemDictionaries[itemName];
            }
        }

        public virtual BuildItem AddCodeBuildItem(string fileName)
        {
            return AddCodeBuildItem(fileName, false, "");
        }

        public string MakeAbsolute(string relativePath)
        {
            if (FileManager.IsRelative(relativePath))
            {
                string lowerCase = relativePath.ToLower();

                if (this.IsContentProject &&
                    (lowerCase.StartsWith("content/") || lowerCase.StartsWith(@"content\")))
                {
                    relativePath = relativePath.Substring("content/".Length, relativePath.Length - "content/".Length);
                }

                string returnValue = this.Directory + relativePath;

                if (System.IO.Directory.Exists(returnValue) &&
                    !returnValue.EndsWith("/") &&
                    !returnValue.EndsWith("\\"))
                {
                    returnValue += "/";
                }

                returnValue = FileManager.Standardize(returnValue);

                return returnValue;
            }
            else
            {
                return relativePath;
            }
        }

        public bool RemoveItem(string itemName)
        {
            itemName = StandardizeItemName(itemName);

            BuildItem itemToRemove = null;

            itemName = itemName.Replace("/", "\\").ToLower();
            bool removed = false;
            if (mBuildItemDictionaries.ContainsKey(itemName))
            {
                itemToRemove = mBuildItemDictionaries[itemName];
                removed = true;
            }

            RemoveItem(itemName, itemToRemove);
            return removed;
        }

        public bool RemoveItem(BuildItem buildItem)
        {
            string itemName = buildItem.Include.Replace("/", "\\").ToLower();

            return RemoveItem(itemName);
        }

        protected abstract void RemoveItem(string itemName, BuildItem item);

        public void RenameItem(string oldName, string newName)
        {
            string unmodifiedNewName = newName;

            oldName = StandardizeItemName(oldName);
            newName = StandardizeItemName(newName);

            BuildItem item = GetItem(oldName);

            mBuildItemDictionaries.Remove(oldName);

            item.Include = unmodifiedNewName.Replace("/", "\\");

            mBuildItemDictionaries.Add(newName, item);

            if (newName.Contains(".generated.cs"))
            {
                MakeBuildItemNested(item, FileManager.RemovePath(FileManager.RemoveExtension(newName)).Replace(".generated", "") + ".cs");
            }

        }

        public void RenameInDictionary(string oldName, string newName, BuildItem item)
        {
            mBuildItemDictionaries.Remove(oldName.Replace("/", "\\").ToLower());

            if (!mBuildItemDictionaries.ContainsKey(newName.Replace("/", "\\").ToLower()))
            {
                mBuildItemDictionaries.Add(newName.Replace("/", "\\").ToLower(), item);
            }
        }

        public string StandardizeItemName(string itemName)
        {
            itemName = itemName.ToLower().Replace("/", "\\");

            if (!FileManager.IsRelative(itemName))
            {
                itemName = FileManager.MakeRelative(itemName, this.Directory);
                itemName = itemName.Replace("/", "\\");
            }

            if (this.IsContentProject && itemName.StartsWith("content\\"))
            {
                itemName = itemName.Substring("content\\".Length);
            }

            return itemName;

        }

        protected void CopyFileToProjectRelativeLocation(string sourceFileName, string relativeFileName)
        {

            string fullName = FileManager.GetDirectory(FullFileName) + relativeFileName;

            if (FileManager.Standardize(fullName).ToLower() != FileManager.Standardize(sourceFileName).ToLower())
            {
                try
                {
                    File.Copy(sourceFileName, fullName, true);
                }
                catch (DirectoryNotFoundException)
                {
                    string newDir = FileManager.GetDirectory(fullName);
                    System.IO.Directory.CreateDirectory(newDir);
                    File.Copy(sourceFileName, fullName, true);
                }
            }
        }

        protected virtual bool ShouldIgnoreFile(string fileName)
        {
            return false;

        }

        protected virtual string ContentProjectDirectory
        {
            get { return Directory; }
        }

        protected virtual void AddCodeBuildItems(ProjectBase sourceProjectBase)
        {
#if GLUE
            foreach (BuildItem bi in sourceProjectBase.EvaluatedItems)
            {
                if (bi.Include.EndsWith(".cs"))
                {
                    string fileName;

                    if (SaveAsAbsoluteSyncedProject)
                    {
                        fileName = FileManager.GetDirectory(sourceProjectBase.FullFileName) + bi.Include;
                    }
                    else if (SaveAsRelativeSyncedProject)
                    {
                        fileName = FileManager.MakeRelative(FileManager.GetDirectory(sourceProjectBase.FullFileName), FileManager.GetDirectory(FullFileName)) + bi.Include;
                    }
                    else
                    {
                        fileName = bi.Include;
                    }


                    if (!IsFilePartOfProject(fileName, BuildItemMembershipType.CompileOrContentPipeline) && 
                        !IsFilePartOfProject(bi.Include, BuildItemMembershipType.CompileOrContentPipeline) &&
                        !ShouldIgnoreFile(bi.Include))
                    {
                        if (SaveAsAbsoluteSyncedProject)
                        {
                            AddCodeBuildItem(fileName, true, bi.Include);
                        }
                        else if (SaveAsRelativeSyncedProject)
                        {
                            AddCodeBuildItem(fileName, true, bi.Include);
                        }
                        else
                        {
                            AddCodeBuildItem(bi.Include);
                        }
                    }
                }
            }
#endif
        }

        protected virtual void LoadContentProject()
        {
            ContentProject = this;
        }

        public virtual void PerformPendingTranslations()
        {
        }

        public virtual void ClearPendingTranslations()
        {
        }

        public virtual string ProcessInclude(string path)
        {
            return path;
        }

        public virtual string ProcessLink(string path)
        {
            return path;
        }

        #region IEnumerable<BuildItem> Members

        public IEnumerator<BuildItem> GetEnumerator()
        {
            foreach (BuildItem buildItem in mBuildItemDictionaries.Values)
            {
                yield return buildItem;
            }
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return mBuildItemDictionaries.Values.GetEnumerator();
        }

        #endregion
    }


    public static class BuildItemExtensionMethods
    {
        public static string GetLink(this BuildItem buildItem)
        {
            return (string)buildItem.GetEvaluatedMetadata("Link");
        }
        public static void SetLink(this BuildItem buildItem, string value)
        {
            buildItem.SetMetadata("Link",value);
        }
    }







}
