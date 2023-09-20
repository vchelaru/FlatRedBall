using System.Collections.Generic;
using FlatRedBall.IO;
using System.IO;
using System.Linq;
using System;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    #region Enums

    public enum BuildItemMembershipType
    {
        CopyIfNewer,
        CompileOrContentPipeline,
        Content,
        Any,
        BundleResource,
        AndroidAsset
    }

    public enum SyncedProjectRelativeType
    {
        Contained,
        Linked
    }
    #endregion

    public delegate void SaveDelegate(string fileName);

    public abstract class ProjectBase
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

        #region Properties

        public virtual bool ContentCopiedToOutput { get { return true; } }


        public virtual ProjectBase ContentProject
        {
            get;
            set;
        }

        public virtual ProjectBase CodeProject
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

        public abstract FilePath FullFileName
        {
            get;
        }

        string cachedDirectory;
        public string Directory
        {
            get
            {
                if (cachedDirectory == null)
                {
                    cachedDirectory = FullFileName.GetDirectoryContainingThis().FullPath;
                }
                return cachedDirectory;
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

        public virtual string RootNamespace
        {
            get
            {
                return this.Name;
            }
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

        protected void RaiseSaving(string fileName)
        {
            Saving?.Invoke(fileName);
        }

        #endregion

        public bool SaveAsRelativeSyncedProject;
        public bool SaveAsAbsoluteSyncedProject;
        public abstract void UpdateContentFile(string sourceFileName);
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

        public void Save() { Save(FullFileName.FullPath); }
        public abstract void Save(string fileName);

        public abstract void Load(string fileName);
        public abstract void SyncTo(ProjectBase projectBase, bool performTranslation);
        /// <summary>
        /// A string which is intended to uniquely identify the project type.
        /// For example, XNA 4 projets might return "Xna4".  This is not tied to
        /// any preprocessor defines.
        /// </summary>
        public abstract string ProjectId { get; }
        public abstract string PrecompilerDirective { get; }

        public abstract bool IsFrbSourceLinked();
        //public abstract Version GetFrbDllVersion();


        public string MakeAbsolute(string relativePath)
        {
            if (FileManager.IsRelative(relativePath))
            {
                if (this.IsContentProject && (relativePath.StartsWith("content/", StringComparison.OrdinalIgnoreCase) || relativePath.StartsWith(@"content\", StringComparison.OrdinalIgnoreCase)))
                {
                    relativePath = relativePath.Substring("content/".Length, relativePath.Length - "content/".Length);
                }

                string returnValue = this.Directory + relativePath;

                // If we do a Directory.Exists here, it can be slow (especially since it's called so frequently
                // Let's assume that if it has an extension it is not a directory
                var extension = FileManager.GetExtension(returnValue);
                if(string.IsNullOrEmpty(extension) && 
                    !returnValue.EndsWith("/", StringComparison.OrdinalIgnoreCase) &&
                    !returnValue.EndsWith("\\", StringComparison.OrdinalIgnoreCase))
                {
                    if (// do the slow call last to early out
                        System.IO.Directory.Exists(returnValue)
                        )
                    {
                        returnValue += "/";
                    }

                }

                returnValue = FileManager.Standardize(returnValue);

                return returnValue;
            }
            else
            {
                return relativePath;
            }
        }

        public string GetAbsoluteContentFolder()
        {
            if(this.ContentProject != this)
            {
                return this.ContentProject.Directory;
            }
            else if(!string.IsNullOrEmpty( this.ContentProject.ContentDirectory))
            {
                return this.Directory + this.ContentDirectory;
            }
            else
            {
                return this.Directory;
            }
        }

        public abstract bool RemoveItem(string itemName);

        public string StandardizeItemName(string itemName)
        {
            itemName = itemName.ToLowerInvariant().Replace("/", "\\");

            if (!FileManager.IsRelative(itemName))
            {
                itemName = FileManager.MakeRelative(itemName, this.Directory);
                itemName = itemName.Replace("/", "\\");
            }

            if (this.IsContentProject && itemName.StartsWith("content\\", StringComparison.OrdinalIgnoreCase))
            {
                itemName = itemName.Substring("content\\".Length);
            }

            return itemName;

        }

        protected void CopyFileToProjectRelativeLocation(string sourceFileName, string relativeFileName)
        {

            string fullName = FullFileName.GetDirectoryContainingThis().FullPath + relativeFileName;

            if (!String.Equals(FileManager.Standardize(fullName), FileManager.Standardize(sourceFileName), StringComparison.OrdinalIgnoreCase))
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

        protected virtual string ContentProjectDirectory => Directory;


        public virtual void LoadContentProject()
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

        public abstract void Unload();

    }

}
