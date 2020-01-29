using System;
using System.Collections.Generic;
using FlatRedBall.IO;
#if GLUE
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using EditorObjects.Parsing;
using FlatRedBall.Glue.Controls;
using System.Windows.Forms;
using System.Diagnostics;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Utilities;
#endif

using System.Text;
using System.Linq;
using FlatRedBall.Glue.Managers;
using Microsoft.Build.Evaluation;
using Container = EditorObjects.IoC.Container;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;

namespace FlatRedBall.Glue.VSHelpers.Projects
{

    public abstract class VisualStudioProject : ProjectBase
    {
        #region Fields

        Project mProject;
        private string mName;
        string mRootNamespace;

        List<string> mExtensionsToIgnore = new List<string>();

        #endregion

        #region Properties

        public override IEnumerable<ProjectItem> EvaluatedItems
        {
            get 
            {
                // This makes it thread safe:
                var clone = new List<ProjectItem>();
                clone.AddRange(mProject.AllEvaluatedItems);
                return clone;
            }
        }

        public override string FullFileName
        {
            get
            {
                return 
                    mProject.ProjectFileLocation.File;
            }
        }

        public override bool IsDirty
        {
            get { return mProject.IsDirty; }
            set
            {
                if (value)
                {
                    mProject.MarkDirty();
                }
            }
        }

        public Project Project
        {
            get { return mProject; }
        }

        public override string Name
        {
            get
            {
                if (string.IsNullOrEmpty(mName))
                {
                    mName = FileManager.RemoveExtension(FileManager.RemovePath(Project.ProjectFileLocation.File));
                }

                return mName;
            }
        }

        public override string RootNamespace
        {
            get
            {
                return mRootNamespace;

            }
        }

        public virtual bool AllowContentCompile { get { return true; } }

        public virtual string DefaultContentAction { get { return "None"; } }

        public virtual BuildItemMembershipType DefaultContentBuildType 
        { get { return BuildItemMembershipType.CopyIfNewer; } }

        protected virtual bool NeedCopyToOutput { get { return true; } }

        public abstract string NeededVisualStudioVersion { get; }

        #endregion

        #region Methods

        public override string ProcessInclude(string path)
        {
            return path.Replace("/", "\\");
        }

        #region Constructor

        protected VisualStudioProject(Project project)
        {
            mProject = project;

            CodeProject = this;

            FindRootNamespace();
        }

        #endregion

        public override ProjectItem AddContentBuildItem(string absoluteFile, SyncedProjectRelativeType relativityType = SyncedProjectRelativeType.Contained, bool forceToContentPipeline = false)
        {
            /////////////////////////Early Out////////////////////////////
            string extension = FileManager.GetExtension(absoluteFile);
            var rfs = Container.Get<IGlueCommands>().FileCommands.GetReferencedFile(absoluteFile);


#if GLUE

            bool handledByContentPipelinePlugin = Plugins.EmbeddedPlugins.SyncedProjects.SyncedProjectLogic.Self
                .GetIfHandledByContentPipelinePlugin(this, extension, rfs);
            if (handledByContentPipelinePlugin)
            {
                return null;
            }
            ///////////////////End Early Out//////////////////////////////
            lock (this)
            {
                string relativeFileName = FileManager.MakeRelative(absoluteFile, this.Directory);

                ProjectItem buildItem = null;

                bool addToContentPipeline = false;
                AssetTypeInfo assetTypeInfo = AvailableAssetTypes.Self.GetAssetTypeFromExtension(extension);

                if (assetTypeInfo != null)
                {
                    addToContentPipeline = assetTypeInfo.MustBeAddedToContentPipeline || forceToContentPipeline;
                }


                // August 14, 2011
                // Not sure why this
                // is here - I assume
                // because I thought when
                // I originally wrote this
                // that if something didn't
                // have an AssetTypeInfo, then
                // it couldn't be added to the content
                // pipeline...but I think CSVs can be.
                // So I'm going to remove this for now and
                // see what problems it causes.
                //if (forceToContentPipeline && assetTypeInfo == null)
                //{
                //    return;
                //}
                

                string itemInclude = FileManager.MakeRelative(absoluteFile, this.Directory);

                itemInclude = ProcessInclude(itemInclude);


            #region If added to content pipeline

                if (addToContentPipeline && AllowContentCompile)
                {
                    buildItem = mProject.AddItem("Compile", ProcessInclude(itemInclude)).First();
                    mProject.ReevaluateIfNecessary();

                    if (string.IsNullOrEmpty(assetTypeInfo.ContentImporter) ||
                        string.IsNullOrEmpty(assetTypeInfo.ContentProcessor))
                    {
                        throw new InvalidOperationException("There is missing import/process data for ." + extension + " files");

                    }

                    buildItem.SetMetadataValue("Importer", assetTypeInfo.ContentImporter);
                    buildItem.SetMetadataValue("Processor", assetTypeInfo.ContentProcessor);

                }

            #endregion

            #region else, just copy the file

                else
                {
                    buildItem = mProject.AddItem(DefaultContentAction, itemInclude).FirstOrDefault();
                    mProject.ReevaluateIfNecessary();
                    if (ContentCopiedToOutput)
                    {
                        try
                        {
                            buildItem.SetMetadataValue("CopyToOutputDirectory", "PreserveNewest");
                        }
                        catch(Exception exception)
                        {
                            throw new Exception("Error trying to add build item " + buildItem + " to project: " + exception.ToString());
                        }

                    }
                }

            #endregion
                try
                {
                    // The items in the dictionary must be to-lower on some
                    // platforms, and ProcessPath takes care of this.
                    mBuildItemDictionaries.Add(itemInclude.ToLower(), buildItem);
                }
                catch
                {
                    int m = 3;

                }
                string name = FileManager.RemovePath(FileManager.RemoveExtension(relativeFileName));

                buildItem.SetMetadataValue("Name", name);

                if (relativityType == SyncedProjectRelativeType.Linked)
                {
                    string linkValue;
                    string path = null;

                    if (MasterProjectBase != null)
                    {
                        // OriginalProjectBaseIfSynced is the master project, but not sure why we check that if we already have
                        // the MasterProjectBase. It was causing a NullReferenceException so I put an extra check.
                        if (MasterProjectBase.OriginalProjectBaseIfSynced != null)
                        {
                            path = MasterProjectBase.OriginalProjectBaseIfSynced.ContentProject.FullContentPath;
                        }
                        else
                        {
                            path = MasterProjectBase.ContentProject.FullContentPath;
                        }
                    }
                    // This will be null if we're creating a linked content file without a source project - which is the case for MonoGame projects
                    // linking audio files:
                    else if (OriginalProjectBaseIfSynced != null)
                    {
                        path = OriginalProjectBaseIfSynced.ContentProject.FullContentPath;
                    }

                    if(path != null)
                    {
                        linkValue = ContentDirectory + FileManager.MakeRelative(absoluteFile, path);
                        linkValue = FileManager.RemoveDotDotSlash(linkValue);
                        linkValue = ProcessInclude(linkValue);



                        buildItem.SetMetadataValue("Link", linkValue);
                    }
                }
                mProject.ReevaluateIfNecessary();

                return buildItem;
            }

#else

            throw new NotImplementedException();
#endif
        }

        public bool IsCodeItem(ProjectItem buildItem)
        {
            if (buildItem.ItemType == "Compile")
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public override bool IsFilePartOfProject(string fileToUpdate, BuildItemMembershipType membershipType)
        {
            return IsFilePartOfProject(fileToUpdate, membershipType, true);
        }

        public override bool IsFilePartOfProject(string fileToUpdate, BuildItemMembershipType membershipType, bool relativeItem)
        {
            ProjectItem buildItem;
            if (relativeItem)
            {
                buildItem = GetItem(fileToUpdate);
            }
            else
            {
                buildItem = GetItem(fileToUpdate, false);
            }

            if (buildItem == null)
            {
                if (membershipType == BuildItemMembershipType.Content)
                {
                    buildItem = GetItem("Content\\" + fileToUpdate);
                    if (buildItem != null && buildItem.ItemType == "Content")
                    {
                        return true;
                    }
                }

                if (buildItem == null)
                {
                    return false;
                }
            }
            else
            {
                if (membershipType == BuildItemMembershipType.CompileOrContentPipeline)
                {
                    if (!AllowContentCompile || buildItem.ItemType == "Compile" || buildItem.ItemType == "Content")
                    {
                        return true;
                    }
                }
                else if (membershipType == BuildItemMembershipType.Content)
                {
                    if (!AllowContentCompile || buildItem.ItemType == "Compile" || buildItem.ItemType == "Content")
                    {
                        return true;
                    }
                    else if (relativeItem == false)
                    {
                        return true;
                    }
                }
                else if (membershipType == BuildItemMembershipType.CopyIfNewer)
                {
                    bool compile = FileManager.GetExtension(fileToUpdate) == "x";
                    if (compile)
                    {
                        if (!AllowContentCompile || buildItem.ItemType == "Compile")
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (!NeedCopyToOutput || buildItem.HasMetadata("CopyToOutputDirectory"))
                        {
                            return true;
                        }
                    }
                }
                else if (membershipType == BuildItemMembershipType.Any)
                {
                    return true;
                }
                else if(membershipType.ToString() == buildItem.ItemType)
                {
                    return true;
                }

            }

            return false;

            /*

            // Even though there's some code duplication, let's do the if statement outside of the loop to speed things up
            if (membershipType == BuildItemMembershipType.CompileOrContentPipeline)
            {
                int count = mProject.EvaluatedItems.Count;
                for (int i = 0; i < count ; i++)
                {
                    BuildItem buildItem = mProject.EvaluatedItems[i];

                    if (buildItem.Include.ToLower().Replace("/", "\\") == fileToUpdate)
                    {
                        if (buildItem.Name == "Compile" || buildItem.Name == "Content")
                        {
                            return true;
                        }
                    }
                }
            }
            else if (membershipType == BuildItemMembershipType.CopyIfNewer)
            {
                bool compile = FileManager.GetExtension(fileToUpdate) == "x";

                int count = mProject.EvaluatedItems.Count;
                if (compile)
                {
                    for (int i = 0; i < count; i++)
                    {
                        BuildItem buildItem = mProject.EvaluatedItems[i];

                        if (buildItem.Include.ToLower().Replace("/", "\\") == fileToUpdate)
                        {
                            if (buildItem.Name == "Compile")
                            {
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        BuildItem buildItem = mProject.EvaluatedItems[i];

                        if (buildItem.Include.ToLower().Replace("/", "\\") == fileToUpdate)
                        {
                            if (buildItem.HasMetadata("CopyToOutputDirectory"))
                            {
                                return true;
                            }
                        }
                    }
                }

            }
            return false;
             */
        }

        public override void Load(string fileName)
        {
            mBuildItemDictionaries.Clear();

            bool wasChanged = false;

#region Build the mBuildItemDictionary to make accessing items faster
            for (int i = mProject.AllEvaluatedItems.Count - 1; i > -1; i--)
            {
                ProjectItem buildItem = mProject.AllEvaluatedItems.ElementAt(i);

                string includeToLower = buildItem.EvaluatedInclude.ToLower();

                if (buildItem.IsImported)
                {
                    //Do Nothing

                    // 04-12-2012
                    // Some computers add a duplicate mscorlib when loading the core Project. Glue would catch this and remove it, 
                    // but for some reason this would remove all instances...causing problems when loading the project. We aren't 
                    // sure why, but if we check for the isIncluded flag it seems to fix it, and isIncluded doesn't seem to be true 
                    // on things added by Glue - and that's ultimately what we want to check duplicates on.
                }
                else if (mBuildItemDictionaries.ContainsKey(includeToLower))
                {
                    wasChanged = ResolveDuplicateProjectEntry(wasChanged, buildItem);
                }
                else
                {
                    mBuildItemDictionaries.Add(
                        buildItem.UnevaluatedInclude.ToLower(),
                        buildItem);
                }
            }
#endregion

            FindRootNamespace();

            // December 20, 2010

            if (wasChanged)
            {
                mProject.Save(mProject.ProjectFileLocation.File);
            }

            LoadContentProject();
        }

        public override void Unload()
        {
            Project.ProjectCollection.UnloadProject(Project);


            if(this.ContentProject != this && this.ContentProject != null && this.ContentProject is VisualStudioProject)
            {
                var contentAsVsProject = this.ContentProject as VisualStudioProject;

                var contentProjectBase = contentAsVsProject.Project;

                contentProjectBase.ProjectCollection.UnloadProject(contentProjectBase);

            }
        }

        private bool ResolveDuplicateProjectEntry(bool wasChanged, ProjectItem buildItem)
        {
#if GLUE

            var mbmb = new MultiButtonMessageBoxWpf();

            mbmb.MessageText = "The item " + buildItem.UnevaluatedInclude + " is part of " +
                "the project twice.  Glue does not support double-entries in a project.  What would you like to do?";

            mbmb.AddButton("Remove the duplicate entry and continue", System.Windows.Forms.DialogResult.OK);
            mbmb.AddButton("Remove the duplicate, but show me a list of all contained objects before removal", System.Windows.Forms.DialogResult.No);
            mbmb.AddButton("Cancel loading the project - this will throw an exception", System.Windows.Forms.DialogResult.Cancel);

            DialogResult result = DialogResult.Cancel;

            if(mbmb.ShowDialog() == true)
            {
                result = (DialogResult)mbmb.ClickedResult;
                switch (result)
                {
                    case DialogResult.OK:
                        mProject.RemoveItem(buildItem);
                        mProject.ReevaluateIfNecessary();
                        break;
                    case DialogResult.No:
                        StringBuilder stringBuilder = new StringBuilder();
                        foreach (var item in mProject.AllEvaluatedItems)
                        {
                            stringBuilder.AppendLine(item.ItemType + " " + item.UnevaluatedInclude);
                        }
                        string whereToSave = FileManager.UserApplicationDataForThisApplication + "ProjectFileOutput.txt";
                        FileManager.SaveText(stringBuilder.ToString(), whereToSave);
                        Process.Start(whereToSave);


                        mProject.RemoveItem(buildItem);
                        mProject.ReevaluateIfNecessary();
                        break;
                    case DialogResult.Cancel:
                        throw new Exception("Duplicate entries found: " + buildItem.ItemType + " " + buildItem.UnevaluatedInclude);
                }

            }
            else
#endif
            {
                //mProject.EvaluatedItems.RemoveItemAt(i);
                mProject.RemoveItem(buildItem);
                mProject.ReevaluateIfNecessary();
            }
            wasChanged = true;
            return wasChanged;
        }

        

#if GLUE
        public override void UpdateContentFile(string sourceFileName)
        {
            string relativeFileName = FileManager.MakeRelative(sourceFileName, this.Directory);
            if (!IsFilePartOfProject(relativeFileName, BuildItemMembershipType.Content))
            {
                AddContentBuildItem(sourceFileName);
            }

            else
            {
                List<string> listOfReferencedFiles;

                if (FileHelper.DoesFileReferenceContent(sourceFileName))
                {
                    listOfReferencedFiles = FileReferenceManager.Self.GetFilesReferencedBy(sourceFileName);
                }
                else
                {
                    listOfReferencedFiles = new List<string>();
                }



                string relativeDirectory = relativeFileName.Substring(0, relativeFileName.LastIndexOf("\\") + 1);
                if (relativeDirectory == "")
                {
                    relativeDirectory = relativeFileName.Substring(0, relativeFileName.LastIndexOf("/") + 1);
                }

                string sourceDirectory = FileManager.GetDirectory(sourceFileName);
                string relativeFile;

                relativeFile = FileManager.MakeRelative(sourceFileName, sourceDirectory);
                CopyFileToProjectRelativeLocation(sourceFileName, relativeDirectory + relativeFile);

                foreach (string file in listOfReferencedFiles)
                {
                    relativeFile = FileManager.MakeRelative(file, sourceDirectory);
                    UpdateContentFile(file);
                }
            }
        }
#endif

        public override void MakeBuildItemNested(ProjectItem item, string parent)
        {
            string fullPathParent = item.EvaluatedInclude;

            // this first index should usually work, but leaving the else in just in case it doesn't.
            if (fullPathParent.Contains("\\"))
            {
                int lastIndex = fullPathParent.LastIndexOf("\\");
                fullPathParent = fullPathParent.Substring(0, lastIndex + 1);
            }
            else
            {
                fullPathParent = FileManager.GetDirectory(fullPathParent);
            }
            if (IsFilePartOfProject(fullPathParent + parent, BuildItemMembershipType.CompileOrContentPipeline))
            {

                if (!item.Metadata.Any(metadata=>metadata.ItemType == "DependentUpon"))
                {
                    item.SetMetadataValue("DependentUpon", parent);
                }
            }
        }



        public override void Save(string fileName)
        {
            // this used to save a backup, but doing so
            // changes the mProject's file name. Since the
            // mProject is used to get this project's file name,
            // which is in turn used by Glue to determine the Glue
            // folder, saving a backup temporarily sets the file (and 
            // folder) to the temporary location. I'm going to remove the
            // backup saving because Glue doesn't handle it well anyway, and
            // it's causing a problem with Camera setup.
            // Update Feb 26, 2017
            // It seems like calling 
            // Save on mProject will actually
            // only write to disk if it's modified...
            // either that or it does always write to disk
            // but the file watch manager doesn't pick up the
            // change, causing accumulated ignores. So instead
            // we're going to check if the file actually changed,
            // and only if it did will we raise the events and write
            // all text.

            bool shouldSave = false;
            
            // April 14, 2017:
            // I used to use a Unicode
            // StringWriter, but it saved
            // the projects with utf-16 encoding
            // (in the XML). Switching to a UTF8...
            //using (var stringWriter = new UnicodeStringWriter())
            using (var stringWriter = new Utf8StringWriter())
            {
                mProject.ReevaluateIfNecessary();

                mProject.Save(stringWriter);

                string newText = stringWriter.ToString();
                var oldText = System.IO.File.ReadAllText(fileName);

                if (oldText != newText)
                {
                    RaiseSaving(FullFileName);

                    System.IO.File.WriteAllText(FullFileName, newText, stringWriter.Encoding);
                }
            }
        }

        public override string ToString()
        {
            return mName;
        }


        protected virtual bool NeedToSaveContentProject { get { return true; } }

        public override void SyncTo(ProjectBase projectBase, bool performTranslation)
        {
#if GLUE
            Load(FullFileName);

            AddCodeBuildItems(projectBase);

            Plugins.EmbeddedPlugins.SyncedProjects.SyncedProjectLogic.Self.SyncContentFromTo(projectBase, this);

            if (NeedToSaveContentProject)
            {
                ContentProject.Save(ContentProject.FullFileName);
            }

            Save(FullFileName);
#endif
        }

        protected override ProjectItem AddCodeBuildItem(string fileName, bool isSyncedProject, string nameRelativeToThisProject)
        {
            lock (this)
            {
                if (!FileManager.IsRelative(fileName))
                {
                    fileName = FileManager.MakeRelative(fileName, this.Directory);
                }

                string fileNameToLower = fileName.Replace('/', '\\').ToLower();



                if (mBuildItemDictionaries.ContainsKey(fileNameToLower))
                {
                    return mBuildItemDictionaries[fileNameToLower];
                }


                if (!FileManager.IsRelative(fileName) && !isSyncedProject)
                {
                    fileName = FileManager.MakeRelative(fileName,
                                                        FileManager.GetDirectory(this.FullFileName));
                }

                fileName = fileName.Replace('/', '\\');
                ProjectItem item;
                if (!isSyncedProject)
                {

                    item = ((VisualStudioProject)this).Project.AddItem("Compile", fileName).First();
                    Project.ReevaluateIfNecessary();
                    item.UnevaluatedInclude = fileName;
                }
                else
                {
                    item = ((VisualStudioProject)this).Project.AddItem("Compile", fileName).First();
                    Project.ReevaluateIfNecessary();
                    item.UnevaluatedInclude = fileName;
                    item.SetMetadataValue("Link", nameRelativeToThisProject);

                    if (nameRelativeToThisProject.Contains("Generated"))
                    {
                        var parent = FileManager.RemovePath(fileName).Replace(".Generated", "");
                        MakeBuildItemNested(item, parent);
                    }
                }


                mBuildItemDictionaries.Add(fileNameToLower, item);

                return item;
            }
        }

        protected override void RemoveItem(string itemName, ProjectItem item)
        {
            lock (this)
            {
                if (item != null)
                {
                    Project.RemoveItem(item);
                    Project.ReevaluateIfNecessary();

                    mBuildItemDictionaries.Remove(itemName);
                }
            }
        }


#region Private Methods

        private void FindRootNamespace()
        {
            mRootNamespace = base.RootNamespace;
            if (mProject != null)
            {
                foreach (var bp in mProject.Properties)
                {
                    if (bp.Name == "RootNamespace")
                    {
                        mRootNamespace = bp.EvaluatedValue;
                        break;
                    }
                }
            }
        }

#endregion


#endregion
    }

    public sealed class UnicodeStringWriter : System.IO.StringWriter
    {
        public override Encoding Encoding => Encoding.Unicode;
    }

    public sealed class Utf8StringWriter : System.IO.StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}