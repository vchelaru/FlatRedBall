using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Windows.Forms;
using FlatRedBall.Glue.FormHelpers;
using System.IO;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Elements;
using System.Collections.Generic;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.ContentPipeline;
using FlatRedBall.Glue.Projects;
using EditorObjects.Parsing;
using FlatRedBall.Glue.Errors;
using System.Linq;
using FlatRedBall.Glue.IO;
using Microsoft.Build.Evaluation;
using FlatRedBall.Glue.VSHelpers;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    class ProjectCommands : IProjectCommands
    {
        /// <summary>
        /// Saves the projects immediately if run from a task, or adds the save projects command to a task if not
        /// </summary>
        public void SaveProjects()
        {
            TaskManager.Self.AddOrRunIfTasked(
                SaveProjectsImmediately,
                nameof(SaveProjects),
                TaskExecutionPreference.AddOrMoveToEnd);
        }
        
        public void SaveProjectsImmediately()
        {
            lock (ProjectManager.ProjectBase)
            {
                bool shouldSync = false;
                // IsDirty means that the project has items that haven't
                // been updated to the "evaluated" list, not if it needs to
                // be saved.
                //if (mProjectBase != null && mProjectBase.IsDirty)
                if (ProjectManager.ProjectBase != null)
                {
                    bool succeeded = true;
                    try
                    {
                        ProjectManager.ProjectBase.Save(ProjectManager.ProjectBase.FullFileName);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        MessageBox.Show("Could not save the file because the file is in use");
                        succeeded = false;
                    }

                    if (succeeded)
                    {
                        shouldSync = true;
                    }
                }
                if (ProjectManager.ContentProject != null && ProjectManager.ContentProject != ProjectManager.ProjectBase)
                {
                    ProjectManager.ContentProject.Save(ProjectManager.ContentProject.FullFileName);
                    shouldSync = true;
                }

                //Save projects in case they are dirty
                foreach (var syncedProject in ProjectManager.SyncedProjects)
                {
                    try
                    {
                        syncedProject.Save(syncedProject.FullFileName);
                    }
                    catch (Exception e)
                    {
                        PluginManager.ReceiveError(e.ToString());
                        syncedProject.IsDirty = true;
                    }
                    if (syncedProject.ContentProject != syncedProject)
                    {
                        syncedProject.ContentProject.Save(syncedProject.ContentProject.FullFileName);
                    }
                }

                //Sync all synced projects
                if (shouldSync || ProjectManager.HaveNewProjectsBeenSyncedSinceSave)
                {
                    var syncedProjects = ProjectManager.SyncedProjects.ToArray();
                    foreach (var syncedProject in syncedProjects)
                    {
                        ProjectSyncer.SyncProjects(ProjectManager.ProjectBase, syncedProject, false);
                    }
                }

                // It may be that only the synced projects have changed, so we have to save those:
                foreach (var syncedProject in ProjectManager.SyncedProjects)
                {
                    syncedProject.Save(syncedProject.FullFileName);
                    if (syncedProject != syncedProject.ContentProject)
                    {
                        syncedProject.ContentProject.Save(syncedProject.ContentProject.FullFileName);
                    }
                }

                ProjectManager.HaveNewProjectsBeenSyncedSinceSave = false;
            }
        }


        public void CreateAndAddPartialFile(IElement element, string partialName, string code)
        {
            var fileName = element.Name + ".Generated." + partialName + ".cs";
            var fullFileName = ProjectManager.ProjectBase.Directory + fileName;

            var save = false; // we'll be doing manual saving after it's created
            ProjectManager.CodeProjectHelper.CreateAndAddPartialCodeFile(fileName, save);
            
            // Now we can save it:
            FileManager.SaveText(code, fullFileName);
        }

        public void AddContentFileToProject(string absoluteFileName, bool saveProjects = true)
        {
            string relativeFileName = FileManager.MakeRelative(absoluteFileName, ProjectManager.ProjectBase.ContentProject.Directory);
            GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(ProjectManager.ProjectBase, relativeFileName, false, false, null);
            if (saveProjects)
            {
                SaveProjects();
            }
        }

        /// <summary>
        /// Updates the presence of the RFS in the main project.  If the RFS has project specific files, then those
        /// files are updated in the appropriate synced project.  
        /// </summary>
        /// <remarks>
        /// This method does not update synced projects if the synced projects use the same file.  The reason is because
        /// this is taken care of when the projects are saved later on.
        /// </remarks>
        /// <param name="referencedFileSave">The RFS representing the file to update membership on.</param>
        /// <returns>Whether anything was added to any projects.</returns>
        public bool UpdateFileMembershipInProject(ReferencedFileSave referencedFileSave)
        {
            var assetTypeInfo = referencedFileSave.GetAssetTypeInfo();

            bool shouldSkip = assetTypeInfo != null && assetTypeInfo.ExcludeFromContentProject;

            bool wasAnythingAdded = false;

            if (!shouldSkip)
            {

                bool useContentPipeline = referencedFileSave.UseContentPipeline || (assetTypeInfo != null && assetTypeInfo.MustBeAddedToContentPipeline);

                wasAnythingAdded = UpdateFileMembershipInProject(GlueState.Self.CurrentMainProject, referencedFileSave.GetRelativePath(), useContentPipeline, false);

                foreach (ProjectSpecificFile projectSpecificFile in referencedFileSave.ProjectSpecificFiles)
                {
                    VisualStudioProject foundProject = (VisualStudioProject)ProjectManager.GetProjectByName(projectSpecificFile.ProjectName);
                    wasAnythingAdded |= UpdateFileMembershipInProject(foundProject, projectSpecificFile.FilePath, useContentPipeline, true);
                }
            }
            return wasAnythingAdded;
        }

        /// <summary>
        /// Adds the argument fileRelativeToProject to the argument project if it's not already part of the project. This is a recursive
        /// call so it will also add all referenced files to the project.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="fileName"></param>
        /// <param name="useContentPipeline">Whether this file must be part of the content pipeline. See internal notes on this variable.</param>
        /// <param name="shouldLink"></param>
        /// <param name="parentFile"></param>
        /// <returns>Whether the project was modified.</returns>
        public bool UpdateFileMembershipInProject(VisualStudioProject project, string fileName, bool useContentPipeline, bool shouldLink, string parentFile = null, bool recursive = true, List<string> alreadyReferencedFiles = null)
        {
            bool wasProjectModified = false;
            ///////////////////Early Out/////////////////////
            if (project == null || GlueState.Self.CurrentMainProject == null) return wasProjectModified;

            /////////////////End Early Out//////////////////

            string fileToAddAbsolute = GlueCommands.Self.GetAbsoluteFileName(fileName, isContent:true);

            fileToAddAbsolute = fileToAddAbsolute.Replace("/", "\\");

            bool isFileAlreadyPartOfProject = false;

            bool needsToBeInContentProject = ShouldFileBeInContentProject(fileToAddAbsolute);

            BuildItemMembershipType bimt = BuildItemMembershipType.CopyIfNewer;

            // useContentPipeline can come from the parent file, if it uses content pipeline. But there may be other cases where we want to force content pepeline
            
            if (!useContentPipeline)
            {
                useContentPipeline = GetIfShouldUseContentPipeline(fileToAddAbsolute);
            }

            if (useContentPipeline)
            {
                bimt = BuildItemMembershipType.CompileOrContentPipeline;
            }
            else if (!project.ContentProject.ContentCopiedToOutput)
            {
                bimt = BuildItemMembershipType.Content;
            }

            if (!needsToBeInContentProject)
            {
                isFileAlreadyPartOfProject = project.IsFilePartOfProject(fileName, BuildItemMembershipType.CompileOrContentPipeline);
            }

            string fileRelativeToContent = FileManager.MakeRelative(
                fileToAddAbsolute,
                FileManager.GetDirectory(project.ContentProject.FullFileName));
            fileRelativeToContent = fileRelativeToContent.Replace("/", "\\");

            if (!isFileAlreadyPartOfProject && needsToBeInContentProject)
            {
                // Here we're going to get the absolute file name.
                // We want to get the file name 

                isFileAlreadyPartOfProject = project.ContentProject.IsFilePartOfProject(fileRelativeToContent, bimt);

                if (!isFileAlreadyPartOfProject)
                {
                    var buildItem = ((VisualStudioProject)project.ContentProject).GetItem(fileRelativeToContent);
                    if (buildItem != null)
                    {
                        // The item is here but it's using the wrong build types.  Let's
                        // remove it and readd it so that it gets added with the right options.
                        // Let's remove it and say it's not part of the project so it gets removed and readded
                        project.ContentProject.RemoveItem(fileRelativeToContent);
                    }
                }
            }
            

            bool shouldSkipAdd = useContentPipeline &&
                project.ContentProject is VisualStudioProject &&
                !((VisualStudioProject)project.ContentProject).AllowContentCompile;

            bool shouldRemoveFile = shouldSkipAdd && 
                project.ContentProject.IsFilePartOfProject(fileRelativeToContent, bimt);

            if (shouldRemoveFile)
            {
                // It's using content pipeline, so we use XNBs not PNGs
                var buildItem = ((VisualStudioProject)project.ContentProject).GetItem(fileRelativeToContent);
                if (buildItem != null)
                {
                    // The item is here but it's using the wrong build types.  Let's
                    // remove it and readd it so that it gets added with the right options.
                    // Let's remove it and say it's not part of the project so it gets removed and readded
                    project.ContentProject.RemoveItem(fileRelativeToContent);
                }
            }


            if (!isFileAlreadyPartOfProject && !shouldSkipAdd)
            {
                wasProjectModified = true;

                if (needsToBeInContentProject)
                {
                    AddFileToContentProject(project, useContentPipeline, shouldLink, fileToAddAbsolute);
                }
                else
                {
                    ProjectManager.CodeProjectHelper.AddFileToCodeProject(project, fileToAddAbsolute);
                }
            }

            var listOfReferencedFiles = new List<string>();

            // Glue is going to assume .cs files can't reference content:
            if (!fileToAddAbsolute.EndsWith(".cs"))
            {
                var inner = new List<FilePath>();
                FileReferenceManager.Self.GetFilesReferencedBy(fileToAddAbsolute, TopLevelOrRecursive.TopLevel, inner);
                listOfReferencedFiles.AddRange(inner.Select(item => item.Standardized));
                if(alreadyReferencedFiles != null)
                {
                    listOfReferencedFiles = listOfReferencedFiles.Except(alreadyReferencedFiles).ToList();
                }
            }

            bool shouldAddChildren = true;


            if (fileName.EndsWith(".x") || useContentPipeline)
            {
                shouldAddChildren = false;
            }

            if(alreadyReferencedFiles == null)
            {
                alreadyReferencedFiles = new List<string>();
            }
            alreadyReferencedFiles.AddRange(listOfReferencedFiles);

            if (shouldAddChildren && listOfReferencedFiles != null && recursive)
            {
                for (int i = 0; i < listOfReferencedFiles.Count; i++)
                {
                    string file = listOfReferencedFiles[i];

                    if (file.Contains(@"../"))
                    {
                        string message = "The file\n\n" + fileToAddAbsolute + "\n\nincludes the file\n\n" + file + "\n\n" +
                            "This file should not contain ../ in the path.  This likely happened if you saved the file " +
                            "in a FRBDK tool and didn't select the \"Copy to relative\" option.\n\nYou should probably shut " +
                            "down Glue, fix this problem, then re-open your project.";

                        System.Windows.Forms.MessageBox.Show(message);
                    }
                    else
                    {
                        wasProjectModified |= UpdateFileMembershipInProject(project, file, useContentPipeline, shouldLink, fileToAddAbsolute, recursive:true, alreadyReferencedFiles: alreadyReferencedFiles);
                    }
                }
            }

            return wasProjectModified;
        }

        private static bool GetIfShouldUseContentPipeline(string fileAbsolute)
        {
            // grab the RFS and see if the rfs forces it
            var rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(fileAbsolute);
            bool useContentPipeline = false;
            if (rfs != null && rfs.UseContentPipeline)
            {
                useContentPipeline = true;
            }

            if(!useContentPipeline)
            {
                // let plugins decide:
                var returnedValue = PluginManager.GetIfUsesContentPipeline(fileAbsolute);
                if(returnedValue != null)
                {
                    useContentPipeline = returnedValue.Value;
                }
            }

            return useContentPipeline;
        }

        private static bool ShouldFileBeInContentProject(string fileToAddAbsolute)
        {
            var mainContentProject = GlueState.Self.CurrentMainContentProject;
            var mainProject = GlueState.Self.CurrentMainProject;

            bool toReturn = false;

            if(mainContentProject != null && mainProject != null)
            {
                var contentFolder = mainContentProject.GetAbsoluteContentFolder();

                toReturn = FileManager.IsRelativeTo(fileToAddAbsolute, contentFolder);

                // If this is a .cs file and the content project is the same project as the main project, then it's actually a code file
                if (toReturn && mainContentProject.FullFileName == mainProject.FullFileName && FileManager.GetExtension(fileToAddAbsolute) == "cs")
                {
                    toReturn = false;
                }

            }
            return toReturn;
        }

        private static void AddFileToContentProject(ProjectBase project, bool useContentPipeline, bool shouldLink, string fileToAddAbsolute)
        {
            string relativeFileName = FileManager.MakeRelative(
                fileToAddAbsolute,
                FileManager.GetDirectory(project.ContentProject.FullFileName) + project.ContentProject.ContentDirectory);

            if (relativeFileName.StartsWith(ProjectManager.ContentDirectoryRelative))
            {
                relativeFileName = relativeFileName.Substring(ProjectManager.ContentDirectoryRelative.Length);
            }

            if (!useContentPipeline && project.ContentProject.IsFilePartOfProject(fileToAddAbsolute, BuildItemMembershipType.CompileOrContentPipeline))
            {
                ReferencedFileSave rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(fileToAddAbsolute);

                if (rfs != null)
                {
                    rfs.UseContentPipeline = false;
                    ContentPipelineHelper.ReactToUseContentPipelineChange(rfs);
                }

            }
            else if (useContentPipeline && project.ContentProject.IsFilePartOfProject(fileToAddAbsolute, BuildItemMembershipType.CopyIfNewer))
            {
                ReferencedFileSave rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(fileToAddAbsolute);

                if (rfs != null)
                {
                    rfs.UseContentPipeline = true;
                    ContentPipelineHelper.ReactToUseContentPipelineChange(rfs);
                }
            }
            else
            {
                ((VisualStudioProject)project.ContentProject).AddContentBuildItem(
                    fileToAddAbsolute,
                    shouldLink ? SyncedProjectRelativeType.Linked : SyncedProjectRelativeType.Contained,
                    useContentPipeline);

            }

            PluginManager.ReceiveOutput("Added " + relativeFileName + $" to {project.Name} as content");
        }

        public void CreateAndAddCodeFile(string relativeFileName)
        {
            //////////////Early Out///////////////////
            // Just in case this is called when the project is unloaded:
            if (GlueState.Self.CurrentGlueProject == null)
            {
                return;
            }
            ////////////End Early Out////////////////

            // see if the file exists. If not, create it:
            FilePath filePath = GlueState.Self.CurrentGlueProjectDirectory + relativeFileName;

            CreateAndAddCodeFile(filePath);
        }

        public void CreateAndAddCodeFile(FilePath filePath)
        {
            CreateAndAddCodeFile(filePath, save: true);
        }

        public ProjectItem CreateAndAddCodeFile(FilePath filePath, bool save = true)
        {
            var directory = filePath.GetDirectoryContainingThis();

            System.IO.Directory.CreateDirectory(directory.FullPath);

            if (filePath.Exists() == false)
            {
                // will get back in later
                GlueCommands.Self.TryMultipleTimes(() => System.IO.File.WriteAllText(filePath.FullPath, ""));
            }

            ProjectItem added = null;

            var mainProject = GlueState.Self.CurrentMainProject;

            if(mainProject?.CodeProject == null)
            {
                throw new NullReferenceException("Main Project");
            }

            if (mainProject.CodeProject.IsFilePartOfProject(filePath.FullPath) == false)
            {
                added = ((VisualStudioProject)mainProject.CodeProject).AddCodeBuildItem(filePath.FullPath);

                if(save)
                {
                    GlueCommands.Self.TryMultipleTimes(mainProject.Save, 5);
                }
            }

            return added;
        }

        public void TryAddCodeFileToProject(FilePath codeFilePath, bool saveOnAdd = false)
        {
            TaskManager.Self.AddOrRunIfTasked(() =>
            {
                var mainProject = GlueState.Self.CurrentMainProject;
                if (mainProject.CodeProject.IsFilePartOfProject(codeFilePath.FullPath) == false)
                {
                    ((VisualStudioProject)mainProject.CodeProject).AddCodeBuildItem(codeFilePath.FullPath);

                    SaveProjectsImmediately();
                }
            }, $"Adding {codeFilePath} to project");
        }

        public void CopyToBuildFolder(ReferencedFileSave rfs)
        {
            FilePath source = GlueState.Self.ContentDirectory + rfs.Name;

            CopyToBuildFolder(source);
        }

        public void CopyToBuildFolder(FilePath absoluteSource)
        {
            TaskManager.Self.AddOrRunIfTasked(() =>
            {
                // This is the location when running from Glue
                // Maybe eventually I'll fix glue build to build to the same location as the project...
                string debugPath = "bin/x86/debug/";
                CopyToBuildFolder(absoluteSource.FullPath, debugPath);

                if (GlueState.Self.CurrentMainProject is VisualStudioProject)
                {
                    // This is the location when running from Visual Studio
                    var foundProperty = (GlueState.Self.CurrentMainProject as VisualStudioProject).Project.Properties
                        .ToArray()
                        .FirstOrDefault(item => item.Name == "OutputPath");

                    if (foundProperty != null)
                    {
                        debugPath = foundProperty.EvaluatedValue.Replace('\\', '/');
                        CopyToBuildFolder(absoluteSource.FullPath, debugPath);
                    }

                }

            }, $"{nameof(CopyToBuildFolder)} {absoluteSource}");
        }

        private static void CopyToBuildFolder(FilePath absoluteSource, string debugPath)
        {
            string buildFolder = FileManager.GetDirectory(GlueState.Self.CurrentCodeProjectFileName) + debugPath + "Content/";
            string destination = buildFolder + FileManager.MakeRelative(absoluteSource.FullPath, GlueState.Self.ContentDirectory);

            string destinationFolder = FileManager.GetDirectory(destination);

            // We used to only check the bin folder, but we want to check the specific
            // destination folder. If this is a new entity or a new folder in an entity, 
            // there's no reason to copy this over yet - it means the game hasn't been built
            // with this file:
            // Update July 13, 2021 LIES! Due to the level editor, now we do want to copy it over
            //if (System.IO.Directory.Exists(destinationFolder))
            System.IO.Directory.CreateDirectory(destinationFolder);

            try
            {
                System.IO.File.Copy(absoluteSource.FullPath, destination, true);

                PluginManager.ReceiveOutput("Copied " + absoluteSource + " ==> " + destination);
            }
            catch (Exception e)
            {
                // this could really overwhelm the user with popups, so let's just show output:
                PluginManager.ReceiveOutput("Error copying file:\n\n" + e.ToString());
            }
        }

        public void AddDirectory(string folderName, TreeNode treeNodeToAddTo)
        {

            if (treeNodeToAddTo.IsGlobalContentContainerNode())
            {
                string rootDirectory = FileManager.RelativeDirectory;
                if (ProjectManager.ContentProject != null)
                {
                    rootDirectory = ProjectManager.ContentProject.GetAbsoluteContentFolder();
                }

                string directory = rootDirectory + "GlobalContent/" + folderName;

                Directory.CreateDirectory(directory);
            }
            else if (treeNodeToAddTo.IsRootEntityNode())
            {
                string directory = GlueState.Self.CurrentGlueProjectDirectory + "Entities/" +
                    folderName;

                Directory.CreateDirectory(directory);
            }
            else if (treeNodeToAddTo.IsDirectoryNode())
            {
                // This used to use RelativeDirectory, but
                // I think we want this to be content, so not
                // sure why it uses RelativeDirectory...
                //string directory = FileManager.RelativeDirectory +
                //    currentTreeNode.GetRelativePath() +
                //    tiw.Result;
                // Update October 16, 2011
                // An Enity has both folders
                // in the code folder (representedb
                // by RelativeDirectory) as well as
                // in the Content project.  An Entity
                // may not have files in the Content folder,
                // but it must have code files.  Therefore, we
                // create folders in the code directory tree and
                // we worry about content when NamedObjectSaves are
                // added to a given Entity later.
                //string directory = currentTreeNode.GetRelativePath() +
                //    tiw.Result;
                // Update February 17, 2012
                // But...when we add a new folder
                // to an Entity, we want that folder
                // to show up in the tree view in Glue.
                // Glue only scans the content folder (WRONG! See correction below), so
                // we want to make sure this folder exists
                // so it shows up okay.
                // Update March 11, 2018
                // If we create a folder in the code directory,
                // then Glue will pick that up when constructing
                // the entities tree structure and will add empty
                // folders there. We only want to add to the code whenever
                // an entity is created, or when a user adds a folder to the
                // entities tree node.
                // Therefore, only create a folder in the content folder path,
                // and code folders will be created only when the user explicitly
                // adds a folder to the Entities tree node.
                // Update August 3, 2021
                // Actually, Glue scans the code folders for entities not the content
                // folders for the reasons given above - an entity must have code but may
                // not have content. The RootEntityNode else if check above adds to the code
                // folder, and we should respect that here too. We must tolerate empty folders
                // but unless the .glux were to have explicit folder add/removes like .csproj, this
                // is just something we'll have to deal with.
                string directory;
                
                //directory = GlueState.Self.ContentDirectory +
                directory = GlueState.Self.CurrentGlueProjectDirectory +
                        treeNodeToAddTo.GetRelativePath() +
                        folderName;
                directory = ProjectManager.MakeAbsolute(directory, true);

                Directory.CreateDirectory(directory);

            }
            else if (treeNodeToAddTo.IsFilesContainerNode() || treeNodeToAddTo.IsFolderInFilesContainerNode())
            {
                string directory =
                    treeNodeToAddTo.GetRelativePath() + folderName;

                Directory.CreateDirectory(ProjectManager.MakeAbsolute(directory, true));

                GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();
            }
            else if (treeNodeToAddTo.IsFolderInFilesContainerNode())
            {

                throw new NotImplementedException();
            }

            var containingElementNode = treeNodeToAddTo.GetContainingElementTreeNode();

            GlueElement element = null;
            if (containingElementNode != null)
            {
                element = containingElementNode.Tag as GlueElement;
            }

            if (containingElementNode == null)
            {
                GlueCommands.Self.RefreshCommands.RefreshGlobalContent();
            }
            else
            {
                GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element);
            }

            GlueCommands.Self.RefreshCommands.RefreshDirectoryTreeNodes();
        }

        public string MakeAbsolute(string relativeFileName, bool forceAsContent = false)
        {
            return ProjectManager.MakeAbsolute(relativeFileName, forceAsContent);

        }

        public void MakeGeneratedCodeItemsNested()
        {
            foreach (var bi in ProjectManager.ProjectBase.EvaluatedItems)
            {

                string biInclude = bi.UnevaluatedInclude;

                if (biInclude.EndsWith(".cs") && FileManager.RemovePath(biInclude).IndexOf('.') != FileManager.RemovePath(biInclude).Length - 3)
                {
                    // Don't do it for factories!
                    if (bi.UnevaluatedInclude.StartsWith("Factories\\") || bi.UnevaluatedInclude.StartsWith("Factories/"))
                    {
                        continue;
                    }
                    else
                    {
                        string whatToNestUnder = FileManager.RemovePath(bi.UnevaluatedInclude);
                        whatToNestUnder = whatToNestUnder.Substring(0, whatToNestUnder.IndexOf('.')) + ".cs";

                        // make sure there is an object to nest under in this directory
                        string whatToNextUnderWithPath = FileManager.GetDirectory(bi.UnevaluatedInclude, RelativeType.Relative) + whatToNestUnder;

                        if (ProjectManager.ProjectBase.GetItem(whatToNextUnderWithPath) != null)
                        {
                            ProjectManager.ProjectBase.MakeBuildItemNested(bi, whatToNestUnder);

                            foreach (VisualStudioProject project in ProjectManager.SyncedProjects)
                            {
                                var associatedBuildItem = project.GetItem(ProjectManager.MakeAbsolute(biInclude));
                                if (associatedBuildItem != null)
                                {
                                    project.MakeBuildItemNested(associatedBuildItem, whatToNestUnder);
                                }
                            }
                        }
                        else
                        {
                            // Don't do anything - this guy shouldn't be nested
                        }
                    }
                }
            }
        }

        public void RemoveFromProjectsTask(FilePath filePath, bool save = true)
        {
            TaskManager.Self.Add(() =>
                ProjectManager.RemoveItemFromAllProjects(filePath.FullPath, save),
                $"Removing {filePath.FullPath}");
        }

        public void RemoveFromProjects(FilePath filePath, bool saveAfterRemoving = true)
        {
            ProjectManager.RemoveItemFromAllProjects(filePath.FullPath, saveAfterRemoving);
        }

        public void RemoveFromProjects(string absoluteFileName)
        {
            ProjectManager.RemoveItemFromAllProjects(absoluteFileName, performSave:true);
        }

        public void CreateNewProject()
        {
            NewProjectHelper.CreateNewProject();
        }

        public void AddNugetIfNotAdded(string packageName, string versionNumber)
        {
            //////////////Early Out///////////////////
            // Just in case this is called when the project is unloaded:
            if (GlueState.Self.CurrentGlueProject == null)
            {
                return;
            }
            ////////////End Early Out////////////////

            if(string.IsNullOrWhiteSpace(packageName))
            {
                throw new ArgumentException(nameof(packageName));
            }
            if (string.IsNullOrWhiteSpace(versionNumber))
            {
                throw new ArgumentException(nameof(versionNumber));
            }

            var hasNugetsEmbeddedInCsproj = GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.NugetPackageInCsproj;
            // If not....do we fail silently?
            // So far this is for adding Newtonsoft json, and if we don't have it the user will get a compile error so maybe that's enough to guide them?
            if(hasNugetsEmbeddedInCsproj)
            {
                var mainProject = GlueState.Self.CurrentMainProject;

                if (mainProject?.CodeProject == null)
                {
                    throw new NullReferenceException("Main Project");
                }

                var codeProject = mainProject.CodeProject as VisualStudioProject;

                if(!codeProject.HasPackage(packageName))
                {
                    TaskManager.Self.Add(() =>
                    {
                        codeProject.AddNugetPackage(packageName, versionNumber);
                        GlueCommands.Self.ProjectCommands.SaveProjects();
                    }, $"Adding Nuget Package {packageName}");
                }
            }

        }
    }
}
