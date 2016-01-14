using System;
using System.Collections.Generic;
using FlatRedBall.Glue.AutomatedGlue;
using Microsoft.Build.BuildEngine;
using FlatRedBall.IO;
using CodeTranslator;
using System.Diagnostics;
#if GLUE
using FlatRedBall.Glue.SaveClasses;
using EditorObjects.Parsing;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
#endif

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class EclipseProject : ProjectBase
    {
        #region Fields

        private BuildItemGroup mEvaluatedItems;
        //private TranslationRequestSave mPendingTranslation;
        private string mFileName = "";
        private string mName;

        #endregion

        public override string ProjectId { get { return "Eclipse"; } }

        public override string PrecompilerDirective
        {
            get { throw new NotImplementedException(); }
        }

        public override string FullFileName
        {
            get { return mFileName; }
        }

        public override bool IsDirty
        {
            get { throw new NotImplementedException(); }
            set
            {
                // do nothing
            }
        }

        public override BuildItemGroup EvaluatedItems
        {
            get { return mEvaluatedItems; }
        }

        public override string Name
        {
            get
            {
                if (string.IsNullOrEmpty(mName))
                {
                    mName = FileManager.RemovePath(FileManager.GetDirectory(mFileName));
                    mName = mName.Replace("/", "");
                    mName = mName.Replace("\\", "");
                }

                return mName;
            }
        }

        public override string ContentDirectory
        {
            get { return "Content/"; }
        }

        public override List<string> LibraryDlls
        {
            get { throw new NotImplementedException(); }
        }

        public EclipseProject()
        {
            mEvaluatedItems = new BuildItemGroup();
            mPendingTranslation = new TranslationRequestSave();
        }

#if GLUE
        public override BuildItem AddContentBuildItem(string absoluteFile, SyncedProjectRelativeType relativityType, bool forceToContentPipeline)
        {
            throw new NotImplementedException("You can't sync a eclipse project with a VS project!"); 
        }

        public override BuildItem AddContentBuildItem(string absoluteFile)
        {
            string relativeFileName = FileManager.MakeRelative(absoluteFile, this.Directory);
            BuildItem buildItem = null;

            string extension = FileManager.GetExtension(absoluteFile);

            buildItem = new BuildItem(FileManager.RemovePath(FileManager.RemoveExtension(absoluteFile)) + extension, relativeFileName.Replace("/", "\\"));
         

            string name = FileManager.RemovePath(FileManager.RemoveExtension(relativeFileName));

            buildItem.SetMetadata("Name", name);
                mBuildItemDictionaries.Add(buildItem.Include.ToLower(), buildItem);


            UpdateContentFile(absoluteFile.Replace("/", "\\"));
            return buildItem;
        }
#endif

        public override bool IsFilePartOfProject(string fileToUpdate, BuildItemMembershipType membershipType)
        {
            return IsFilePartOfProject(fileToUpdate, membershipType, true);
        }

        public override bool IsFilePartOfProject(string fileToUpdate, BuildItemMembershipType membershipType, bool relativeItem)
        {
            BuildItem buildItem;
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
                return false;
            }

            return true;
        }
        
#if GLUE
        public override BuildItem AddCodeBuildItem(string fileName)
        {
            return AddCodeBuildItem(fileName, TranslationStyle.PerformTranslate);

        }

        protected override BuildItem AddCodeBuildItem(string fileName, bool isSyncedProject, string directoryToCreate)
        {
            return AddCodeBuildItem(fileName, TranslationStyle.PerformTranslate);
        }

        protected BuildItem AddCodeBuildItem(string fileName, TranslationStyle translationStyle)
        {
            if (translationStyle == TranslationStyle.PerformTranslate && ShouldFileBeTranslated(fileName))
            {
                if (string.IsNullOrEmpty(mPendingTranslation.ProjectFileName))
                {
                    mPendingTranslation.ProjectFileName = ProjectManager.ProjectBase.FullFileName;
                    mPendingTranslation.AssemblyLocation = FileManager.GetDirectory(ProjectManager.ProjectBase.FullFileName) + "bin/x86/Debug/" + ProjectManager.ProjectBase.Name + ".exe";
                    mPendingTranslation.SegmentTag = "Generated In Glue";
                    mPendingTranslation.Namespace = ProjectManager.ProjectBase.Name;
                    mPendingTranslation.FilesToTranslate.Clear();
                    mPendingTranslation.DestinationLanguage = Language.Java;
                }

                //fileName = fileName.Replace("\\", "/");
                string destDirectory = FileManager.GetDirectory(FullFileName) + "src/com/" + ProjectManager.ProjectBase.Name + "/";

                mPendingTranslation.FilesToTranslate.Add(new TranslationRequestSave.RequestedFileInfo(fileName, destDirectory));

                return null;
            }
            else
            {
                return AddCodeBuildItem(fileName, false, "");
            }
        }
#else
        public override BuildItem AddCodeBuildItem(string fileName)
        {
            return null;
        }

        protected override BuildItem AddCodeBuildItem(string fileName, bool isSyncedProject, string directoryToCreate)
        {
            return null;
        }
#endif

        public override void ClearPendingTranslations()
        {
            mPendingTranslation.FilesToTranslate.Clear();
        }

        private static bool ShouldFileBeTranslated(string fileName)
        {
#if GLUE
            if (fileName.Contains(".Generated") || fileName.Contains("DataTypes")
                || fileName.StartsWith("Performance\\"))
            {
                TranslatedFileSave tfs =
                    ProjectManager.GlueProjectSave.GetTranslatedFileSave(fileName);

                if (tfs == null)
                {
                    // The user hasn't selected this file yet, so we'll assume we do want translation until the user says not to
                    return true;
                }
                else
                {
                    return tfs.TranslationStyle == TranslationStyle.PerformTranslate;
                }

            }
            return false;
#else
            return false;
#endif
        }

        public override void PerformPendingTranslations()
        {
            string codeTranslatorLocation = @"T:\CodeTranslator\CodeTranslator\" + @"bin\x86\Debug\" + "CodeTranslator.exe";
            string xmlFileName = "tempTranslation.xml";

            try
            {
                if (string.IsNullOrEmpty(mPendingTranslation.ProjectFileName))
                {
                    System.Windows.Forms.MessageBox.Show("There is no project file meaning that there are likely no files to translate.");
                }
                else
                {
                    FileManager.XmlSerialize<TranslationRequestSave>(mPendingTranslation, FileManager.CurrentDirectory + xmlFileName);
                    mPendingTranslation.FilesToTranslate.Clear();

                    Process.Start(codeTranslatorLocation, FileManager.CurrentDirectory + xmlFileName);
                }
            }
            catch (Exception e)
            {
                int m = 3;
            }


        }
        
        protected override void ForceSave(string fileName)
        {
          //  throw new NotImplementedException();
        }

        public override void Load(string fileName)
        {
            mBuildItemDictionaries.Clear();
            mFileName = fileName;

            LoadContentFiles(fileName);
            LoadContentProject();

            //bool wasChanged = false;

            for (int i = EvaluatedItems.Count - 1; i > -1; i--)
            {
                BuildItem buildItem = EvaluatedItems[i];


                if (mBuildItemDictionaries.ContainsKey(buildItem.Include.ToLower()))
                {
                    GlueGui.ShowMessageBox("The item " + buildItem.Include + " is part of " +
                        "the project twice.  Glue does not support double-entries in a project.  Glue will now " +
                        "automatically remove this item");

                    //mProject.EvaluatedItems.RemoveItemAt(i);
                    //mProject.RemoveItem(buildItem);

                    //wasChanged = true;
                }
                else
                {
                    mBuildItemDictionaries.Add(
                        buildItem.Include.ToLower(),
                        buildItem);
                }
            }
        }

        public override void MakeBuildItemNested(BuildItem item, string parent)
        {
            //throw new NotImplementedException();
        }

        public override void SyncTo(ProjectBase projectBase, bool performTranslation)
        {
#if GLUE
            ProjectBase sourceContentProject = projectBase.ContentProject;

            AddCodeBuildItems(projectBase);

            if(performTranslation)
                PerformPendingTranslations();

            string contentDirectory = FileManager.GetDirectory(sourceContentProject.FullFileName);

            foreach (BuildItem bi in sourceContentProject.EvaluatedItems)
            {
                if (!IsFilePartOfProject("Content\\" + bi.Include, BuildItemMembershipType.Content) &&
                    bi.HasMetadata("CopyToOutputDirectory"))
                {
                    string copyAction = bi.GetMetadata("CopyToOutputDirectory");

                    if (copyAction == "PreserveNewest")
                    {

                        AddContentBuildItem(contentDirectory + bi.Include);
                    }

                }
            }

            Save(FullFileName);
#endif
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

                relativeFileName = FileManager.Standardize(relativeFileName, "", false).Replace("/", "\\");

                string relativeDirectory = relativeFileName.Substring(0, relativeFileName.LastIndexOf("\\") + 1);// FileManager.GetDirectory(relativeFileName);
                if (relativeDirectory == "")
                {
                    relativeDirectory = relativeFileName.Substring(0, relativeFileName.LastIndexOf("/") + 1);
                }

                string sourceDirectory = FileManager.GetDirectory(sourceFileName);
                string relativeFile;

                relativeFile = FileManager.MakeRelative(sourceFileName, sourceDirectory);

                string targetLocation = "assets\\" + relativeDirectory + relativeFile;
                targetLocation = targetLocation.ToLower();

                try
                {
                    CopyFileToProjectRelativeLocation(sourceFileName, targetLocation);
                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show("Error trying to copy file to " + targetLocation +
                        "\n\nThis is not a fatal error.");
                }
                foreach (string file in listOfReferencedFiles)
                {
                    UpdateContentFile(file);
                }
            }
        }
#endif

        public override string FolderName
        {
            get { throw new NotImplementedException(); }
        }

        private void LoadContentFiles(string fileName)
        {
            string contentDirectory = FileManager.GetDirectory(fileName) + "assets/content/";

            if (!System.IO.Directory.Exists(contentDirectory))
            {
                System.IO.Directory.CreateDirectory(contentDirectory);
            }


            List<string> filesInDirectory = FileManager.GetAllFilesInDirectory(contentDirectory);

            foreach (string file in filesInDirectory)
            {
                if (!file.Contains(".svn"))
                {
                    string itemName = FileManager.RemovePath(FileManager.RemoveExtension(file)) + FileManager.GetExtension(file);

                    string nameToAdd = FileManager.MakeRelative(file, FileManager.GetDirectory(FullFileName)).Substring("assets/".Length);
                    nameToAdd = nameToAdd.Replace("/", "\\");

                    EvaluatedItems.AddNewItem("Content", nameToAdd);
                }
            }
        }

        protected override void RemoveItem(string itemName, BuildItem item)
        {
            EvaluatedItems.RemoveItem(item);
        }
    }
}
