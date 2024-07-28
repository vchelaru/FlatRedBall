using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
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
        public void SyncContentFromTo(VisualStudioProject from, VisualStudioProject to)
        {
            var evaluatedItems = ((VisualStudioProject)from.ContentProject).EvaluatedItems;
            var contentItemsToSync =
                evaluatedItems
                .Where(item => IsContentFile(item, from.ContentProject, to))
                .ToList();

            foreach (var bi in contentItemsToSync)
            {
                FilePath absoluteFileName = from.ContentProject.MakeAbsolute(bi.UnevaluatedInclude);

                bool forceToContent = false;
                if (from.ContentCopiedToOutput)
                {
                    forceToContent = !bi.HasMetadata("CopyToOutputDirectory") &&
                        !(to is CombinedEmbeddedContentProject);
                }

                GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(to, absoluteFileName, useContentPipeline: forceToContent, shouldLink: true, recursive:false);
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

            // Unevaluated could be something like *.*...
            //var rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(bi.UnevaluatedInclude);
            var rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(bi.UnevaluatedInclude);

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
                // If we got to this point, then the file has not been explicitly excluded as content. However, we still may want to skip it if it's not in the content directory.
                string containingProjectContent = FileManager.Standardize(buildItemOwner.ContentDirectory).ToLowerInvariant();
                string standardUnevaluatedInclude = FileManager.Standardize(bi.UnevaluatedInclude).ToLowerInvariant();
                var isRelativeToContentFolder = standardUnevaluatedInclude.StartsWith(containingProjectContent);

                // This could still be content if it's an XNB, but XNBs are platform-specific. We don't want to sync an XNB from one platform to another. We'll let the 
                // XNB builder handle this. See the ContentPipelinePlugin for more information.

                shouldSkipContent = !isRelativeToContentFolder;
            }

            return !shouldSkipContent;
        }

        public bool GetIfHandledByContentPipelinePlugin(VisualStudioProject targetProject, string extension, ReferencedFileSave rfs)
        {
            // this depends on the type of project:
            if(targetProject is AndroidProject or AndroidMonoGameNet8Project)
            {
                return extension == "wav" ||
                    extension == "fbx" ||
                    (extension == "png" && rfs != null && rfs.UseContentPipeline);
            }
            else if(targetProject is DesktopGlLinuxProject || targetProject is MonoGameDesktopGlBaseProject)
            {
                // DesktopGL can support other audio engines like NAudio. I don't know if other
                // platforms will get this, but we may want to expand this at some point in the future...
                if (extension == "fbx") return true;

                return 
                    (extension == "wav" || extension == "mp3" || extension == "png") && 
                    rfs?.UseContentPipeline == true;
            }
            else if(targetProject is IosMonogameProject or IosMonoGameNet8Project)
            {
                // Turns out we don't want to ignore MP3s on iOS.
                // We just need to make an additional XNB which is
                // going to be handled by a plugin
                //this.ExtensionsToIgnore.Add("mp3");
                return extension == "fbx" || extension == "wav";
            }
            else if(targetProject is UwpProject)
            {
                return extension == "fbx" || extension == "wav" || extension == "mp3";
            }
            else if(targetProject is Windows8MonoGameProject)
            {
                return extension == "fbx" || extension == "wav" || extension == "mp3";
            }
            else
            {
                return rfs?.UseContentPipeline == true &&
                    (extension == "wav" || extension == "mp3");
            }
            //return targetProject.ExtensionsToIgnore.Contains(extension);
        }
    }
}
