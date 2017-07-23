using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.SyncedProjects
{
    public class SyncedProjectLogic : Singleton<SyncedProjectLogic>
    {
        public void SyncContentFromTo(ProjectBase from, VisualStudioProject to)
        {
            var contentItemsToSync =
                from.ContentProject.EvaluatedItems.Where(item => IsContentFile(item, from.ContentProject, to))
                .ToList();

            foreach (var bi in contentItemsToSync)
            {
                string absoluteFileName = from.ContentProject.MakeAbsolute(bi.UnevaluatedInclude);


                bool forceToContent = false;
                if (from.ContentCopiedToOutput)
                {
                    forceToContent = !bi.HasMetadata("CopyToOutputDirectory") &&
                        !(to is CombinedEmbeddedContentProject);
                }

                BuildItemMembershipType buildItemMembershipType = to.DefaultContentBuildType;
                if (forceToContent)
                {
                    buildItemMembershipType = BuildItemMembershipType.CompileOrContentPipeline;
                }
                else if (to.DefaultContentAction == "Content")
                {
                    buildItemMembershipType = BuildItemMembershipType.Content;
                }

                if (!to.ContentProject.IsFilePartOfProject(absoluteFileName, buildItemMembershipType, true))
                {
                    if (to.ContentProject.GetItem(absoluteFileName) != null)
                    {
                        to.ContentProject.RemoveItem(absoluteFileName);

                    }

                    to.ContentProject.AddContentBuildItem(absoluteFileName, SyncedProjectRelativeType.Linked, forceToContent);

                }


                var biOnThis = to.GetItem(absoluteFileName);
                // Let's process the path to make sure it's matching the latest standards - like
                // if we add additional restrictions at some point in Glue
                if (biOnThis != null)
                {
                    string includeBefore = biOnThis.UnevaluatedInclude;
                    string includeAfter = to.ProcessInclude(biOnThis.UnevaluatedInclude);
                    if (includeBefore != includeAfter)
                    {
                        // simply changing the Include doesn't make a project
                        // dirty, and we only want to make it dirty if we really
                        // did change something so that we don't unnecessarily save
                        // projects.
                        biOnThis.UnevaluatedInclude = includeAfter;
                        to.IsDirty = true;
                    }

                    string linkBefore = biOnThis.GetLink();
                    if (!string.IsNullOrEmpty(linkBefore))
                    {
                        string linkAfter = to.ProcessLink(linkBefore);

                        // If the original project is linking a file outside of
                        // its own file structure, then the Link value assigned on
                        // this BuildItem will include "..\". This is an invalid link
                        // value, so we'll instead try to use the same link as in the original
                        // file:
                        {
                            var linkOnOriginalBuildItem = bi.GetLink();
                            if (string.IsNullOrEmpty(linkOnOriginalBuildItem) == false)
                            {
                                // first let's make the link relative to the main project's content folder
                                var relativeToProject = FileManager.MakeRelative(linkOnOriginalBuildItem, from.ContentProject.ContentDirectory);

                                linkAfter = to.ContentDirectory + relativeToProject;
                                linkAfter = to.ProcessLink(linkAfter);
                            }
                        }


                        if (linkBefore != linkAfter)
                        {
                            // simply changing the Link doesn't make a project
                            // dirty, and we only want to make it dirty if we really
                            // did change something so that we don't unnecessarily save
                            // projects.
                            biOnThis.SetLink(linkAfter);
                            to.IsDirty = true;
                        }
                    }
                }

            }
        }



        private bool IsContentFile(ProjectItem bi, ProjectBase buildItemOwner, VisualStudioProject targetProject)
        {
            bool shouldSkipContent = false;

            if (bi.ItemType == "Folder" || bi.ItemType == "_DebugSymbolsOutputPath" || bi.ItemType == "Reference")
            {
                // Skip trying to add the folder.  We don't need to do this because if it
                // contains anything, then the contained objects will automatically put themselves in a folder
                shouldSkipContent = true;

            }

            if (!shouldSkipContent)
            {
                if (bi.ItemType != "Compile" && bi.ItemType != "None")
                {
                    // but wait, the containing project may embed its content, so if so we need to check that
                    if (buildItemOwner is CombinedEmbeddedContentProject &&
                        ((CombinedEmbeddedContentProject)buildItemOwner).DefaultContentAction == bi.ItemType)
                    {
                        // Looks like it really is content
                        shouldSkipContent = false;
                    }
                    else
                    {
                        shouldSkipContent = true;
                    }
                }
            }

            var rfs = ObjectFinder.Self.GetReferencedFileSaveFromFile(bi.UnevaluatedInclude);

            if (!shouldSkipContent)
            {
                string extension = FileManager.GetExtension(bi.UnevaluatedInclude);
                bool isHandledByContentPipelinePlugin = GetIfHandledByContentPipelinePlugin(targetProject, extension, rfs);
                if (isHandledByContentPipelinePlugin)
                {
                    shouldSkipContent = true;
                }

                if (bi.ItemType == "Compile" && extension == "cs")
                {
                    shouldSkipContent = true;
                }
            }


            // Now that we have checked if we should process this, we want to check if we should exclude it
            if (!shouldSkipContent)
            {

                if (rfs != null && rfs.ProjectsToExcludeFrom.Contains(targetProject.Name))
                {
                    shouldSkipContent = true;
                }
            }

            if (!shouldSkipContent)
            {
                string containingProjectContent = FileManager.Standardize(buildItemOwner.ContentDirectory).ToLowerInvariant();
                string standardUnevaluatedInclude = FileManager.Standardize(bi.UnevaluatedInclude).ToLowerInvariant();

                shouldSkipContent = standardUnevaluatedInclude.StartsWith(containingProjectContent) == false;
            }

            return !shouldSkipContent;
        }

        public bool GetIfHandledByContentPipelinePlugin(VisualStudioProject targetProject, string extension, ReferencedFileSave rfs)
        {
            // this depends on the type of project:
            if(targetProject is AndroidProject)
            {
                return extension == "wav";
            }
            else if(targetProject is DesktopGlLinuxProject || targetProject is DesktopGlProject)
            {
                return extension == "wav" || 
                    extension == "mp3" ||
                    (extension == "png" && rfs != null && rfs.UseContentPipeline);
            }
            else if(targetProject is IosMonogameProject)
            {
                // Turns out we don't want to ignore MP3s on iOS.
                // We just need to make an additional XNB which is
                // going to be handled by a plugin
                //this.ExtensionsToIgnore.Add("mp3");
                return extension == "wav";
            }
            else if(targetProject is UwpProject)
            {
                return extension == "wav" || extension == "mp3";
            }
            else if(targetProject is Windows8MonoGameProject)
            {
                return extension == "wav" || extension == "mp3";
            }
            return false;
            //return targetProject.ExtensionsToIgnore.Contains(extension);
        }
    }
}
