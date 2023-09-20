using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers;
using EditorObjects.Parsing;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using System.IO;
using System.Windows.Forms;
using Glue;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Microsoft.Build.Evaluation;

namespace FlatRedBall.Glue.ContentPipeline
{
    public class ContentPipelineHelper
    {
       
        public static void ReactToUseContentPipelineChange(ReferencedFileSave rfs)
        {
            TaskManager.Self.Add(() =>
            {
                var filesInModifiedRfs = new List<FilePath>();
                bool shouldRemoveAndAdd = false;
     
     
                var projectBase = ProjectManager.ContentProject;
                if (projectBase == null)
                {
                    projectBase = ProjectManager.ProjectBase;
                }
     
     
                AddOrRemoveIndividualRfs(rfs, filesInModifiedRfs, ref shouldRemoveAndAdd, projectBase);
     
                if (shouldRemoveAndAdd)
                {
                    List<ReferencedFileSave> rfses = new List<ReferencedFileSave>();
                    rfses.Add(rfs);
                    bool usesContentPipeline = rfs.UseContentPipeline || rfs.GetAssetTypeInfo() != null && rfs.GetAssetTypeInfo().MustBeAddedToContentPipeline;
                    AddAndRemoveModifiedRfsFiles(rfses, filesInModifiedRfs, projectBase, usesContentPipeline);
     
                    GlueCommands.Self.ProjectCommands.SaveProjects();
                }
            }, "Reacting to changing UseContentPipeline");
        }
        
        private static void AddAndRemoveModifiedRfsFiles(List<ReferencedFileSave> rfses, List<FilePath> filesInModifiedRfs, VisualStudioProject projectBase, bool usesContentPipeline)
        {

            if (filesInModifiedRfs.Count != 0)
            {

                for (int i = 0; i < filesInModifiedRfs.Count; i++)
                {
                    filesInModifiedRfs[i] = ProjectManager.MakeRelativeContent(filesInModifiedRfs[i].FullPath).ToLowerInvariant();
                }

                List<ReferencedFileSave> allReferencedFiles = ObjectFinder.Self.GetAllReferencedFiles();
                foreach (ReferencedFileSave rfsToRemove in rfses)
                {
                    allReferencedFiles.Remove(rfsToRemove);
                }

                RemoveFilesFromListReferencedByRfses(filesInModifiedRfs, allReferencedFiles);

                #region Loop through all files to add/remove

                foreach (var fileToAddOrRemove in filesInModifiedRfs)
                {

                    List<ProjectBase> projectsAlreadyModified = new List<ProjectBase>();

                    // There are files referenced by the RFS that aren't referenced by others

                    // If moving to content pipeline, remove the files from the project.
                    // If moving to copy if newer, add the files back to the project.
                    string absoluteFileName = GlueCommands.Self.GetAbsoluteFileName(fileToAddOrRemove.FullPath, true);

                    projectsAlreadyModified.Add(projectBase);

                    #region Uses the content pipeline, remove the file from all projects
                    if (usesContentPipeline)
                    {
                        // Remove this file - it'll automatically be handled by the content pipeline
                        projectBase.RemoveItem(absoluteFileName);

                        foreach (ProjectBase syncedProject in ProjectManager.SyncedProjects)
                        {

                            ProjectBase syncedContentProjectBase = syncedProject;
                            if (syncedProject.ContentProject != null)
                            {
                                syncedContentProjectBase = syncedProject.ContentProject;
                            }

                            if (!projectsAlreadyModified.Contains(syncedContentProjectBase))
                            {
                                projectsAlreadyModified.Add(syncedContentProjectBase);

                                syncedContentProjectBase.RemoveItem(absoluteFileName);
                            }
                        }
                    }
                    #endregion

                    #region Does not use the content pipeline - add the file if necessary

                    else
                    {

                        // This file may have alraedy been part of the project for whatever reason, so we
                        // want to make sure it's not already part of it when we try to add it
                        if (!projectBase.IsFilePartOfProject(absoluteFileName, BuildItemMembershipType.CopyIfNewer))
                        {
                            projectBase.AddContentBuildItem(absoluteFileName, SyncedProjectRelativeType.Contained, false);
                        }
                        foreach (VisualStudioProject syncedProject in ProjectManager.SyncedProjects)
                        {
                            VisualStudioProject syncedContentProjectBase = syncedProject;
                            if (syncedProject.ContentProject != null)
                            {
                                syncedContentProjectBase = (VisualStudioProject) syncedProject.ContentProject;
                            }

                            if (!projectsAlreadyModified.Contains(syncedContentProjectBase))
                            {
                                projectsAlreadyModified.Add(syncedContentProjectBase);

                                if (syncedContentProjectBase.SaveAsAbsoluteSyncedProject)
                                {
                                    syncedContentProjectBase.AddContentBuildItem(absoluteFileName, SyncedProjectRelativeType.Contained, false);
                                }
                                else
                                {
                                    if (!projectBase.IsFilePartOfProject(absoluteFileName, BuildItemMembershipType.CopyIfNewer))
                                    {
                                        syncedContentProjectBase.AddContentBuildItem(absoluteFileName, SyncedProjectRelativeType.Linked, false);
                                    }
                                }
                            }
                        }
                    }

                    #endregion
                }
                #endregion
            }

        }

        private static void AddOrRemoveIndividualRfs(ReferencedFileSave rfs, List<FilePath> filesInModifiedRfs, ref bool shouldRemoveAndAdd, VisualStudioProject projectBase)
        {

            List<ProjectBase> projectsAlreadyModified = new List<ProjectBase>();

            bool usesContentPipeline = rfs.UseContentPipeline || rfs.GetAssetTypeInfo() != null && rfs.GetAssetTypeInfo().MustBeAddedToContentPipeline;

            if (rfs.GetAssetTypeInfo() != null && rfs.GetAssetTypeInfo().MustBeAddedToContentPipeline && rfs.UseContentPipeline == false)
            {
                rfs.UseContentPipeline = true;
                MessageBox.Show("The file " + rfs.Name + " must use the content pipeline");

            }
            else
            {
                string absoluteName = GlueCommands.Self.GetAbsoluteFileName(rfs);

                shouldRemoveAndAdd = usesContentPipeline && !projectBase.IsFilePartOfProject(absoluteName, BuildItemMembershipType.CompileOrContentPipeline) ||
                    !usesContentPipeline && !projectBase.IsFilePartOfProject(absoluteName, BuildItemMembershipType.CopyIfNewer);

                if (shouldRemoveAndAdd)
                {
                    projectBase.RemoveItem(absoluteName);
                    projectBase.AddContentBuildItem(absoluteName, SyncedProjectRelativeType.Contained, usesContentPipeline);
                    projectsAlreadyModified.Add(projectBase);

                    #region Loop through all synced projects and add or remove the file referenced by the RFS

                    foreach (VisualStudioProject syncedProject in ProjectManager.SyncedProjects)
                    {

                        VisualStudioProject syncedContentProjectBase = syncedProject;
                        if (syncedProject.ContentProject != null)
                        {
                            syncedContentProjectBase = (VisualStudioProject) syncedProject.ContentProject;
                        }

                        if (!projectsAlreadyModified.Contains(syncedContentProjectBase))
                        {
                            projectsAlreadyModified.Add(syncedContentProjectBase);
                            syncedContentProjectBase.RemoveItem(absoluteName);

                            if (syncedContentProjectBase.SaveAsAbsoluteSyncedProject)
                            {
                                syncedContentProjectBase.AddContentBuildItem(absoluteName, SyncedProjectRelativeType.Contained, usesContentPipeline);

                            }
                            else
                            {
                                syncedContentProjectBase.AddContentBuildItem(absoluteName, SyncedProjectRelativeType.Linked, usesContentPipeline);
                            }
                        }
                    }
                    #endregion

                    var filesReferencedByAsset = 
                        FileReferenceManager.Self.GetFilesReferencedBy(absoluteName, EditorObjects.Parsing.TopLevelOrRecursive.Recursive);

                    for (int i = 0; i < filesReferencedByAsset.Count; i++)
                    {
                        if (!filesInModifiedRfs.Contains(filesReferencedByAsset[i]))
                        {
                            filesInModifiedRfs.Add(filesReferencedByAsset[i]);
                        }
                    }
                }
            }
        }

        //public static void UpdateTextureFormatFor(ReferencedFileSave rfs)
        //{
        //    string absoluteName = ProjectManager.MakeAbsolute(rfs.Name, true);

        //    bool usesContentPipeline = rfs.UseContentPipeline || rfs.GetAssetTypeInfo() != null && rfs.GetAssetTypeInfo().MustBeAddedToContentPipeline;

        //    string parameterTag = GetTextureFormatTag(rfs);
        //    string valueToSet = rfs.TextureFormat.ToString();

        //    SetParameterOnBuildItems(absoluteName, parameterTag, valueToSet);
        //}

        private static void SetParameterOnBuildItems(string absoluteName, string parameterTag, string valueToSet)
        {
            #region Get the project



            var projectBase = ProjectManager.ContentProject;

            if (projectBase == null)
            {
                projectBase = ProjectManager.ProjectBase;
            }

            #endregion

            ProjectItem item = projectBase.GetItem(absoluteName);


            // The item may not be here.  Why wouldn't it be here?  Who knows,
            // could be someone screwed with the .csproj.  Anyway, Glue should
            // be able to survive this, and I don't think this is the place to report
            // a missing file.  We should do that somewhere else - like when we first load
            // the project.
            if (item != null)
            {
                item.SetMetadataValue(parameterTag, valueToSet);
            }

            foreach (VisualStudioProject syncedProject in ProjectManager.SyncedProjects)
            {
                // Since this is a content file, we want the content project.
                var syncedProjectBaseToUse = syncedProject.ContentProject as VisualStudioProject;

                if (syncedProjectBaseToUse == null)
                {
                    syncedProjectBaseToUse = syncedProject;
                }

                item = syncedProjectBaseToUse.GetItem(absoluteName);
                if (item != null)
                {
                    item.SetMetadataValue(parameterTag, valueToSet);
                }
            }
        }

        private static void RemoveFilesFromListReferencedByRfses(List<FilePath> filesInModifiedRfs, List<ReferencedFileSave> allReferencedFiles)
        {
            foreach (var possibleReferencer in allReferencedFiles)
            {
                if (!possibleReferencer.UseContentPipeline)
                {
                    string modifiedPossibleReferencerFile = possibleReferencer.Name.ToLowerInvariant();
                    if (filesInModifiedRfs.Contains(modifiedPossibleReferencerFile))
                    {
                        // There is a RFS referencing this guy
                        filesInModifiedRfs.Remove(modifiedPossibleReferencerFile);
                    }

                    string absoluteFileName = GlueCommands.Self.GetAbsoluteFileName(possibleReferencer);

                    if (File.Exists(absoluteFileName))
                    {
                        var filesInPossibleReferencer =
                            FileReferenceManager.Self.GetFilesReferencedBy(absoluteFileName, TopLevelOrRecursive.Recursive);

                        foreach (var containedFile in filesInPossibleReferencer)
                        {
                            if (filesInModifiedRfs.Contains(containedFile))
                            {
                                filesInModifiedRfs.Remove(containedFile);
                            }
                        }
                    }
                }
            }
        }
    }
}
