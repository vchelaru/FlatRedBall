using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using FlatRedBall.Glue.FormHelpers;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using System.IO;
using Microsoft.Build.Evaluation;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using L = Localization;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.Windows8ContentAdd
{
    public enum FileStateConsideration
    {
        IfOutOfDate,
        Always
    }

    // Why does this exist and also the BuiltFileCopier?
    [Export(typeof(PluginBase))]
    public class XnbContentAdder : EmbeddedPlugin
    {
        public override void StartUp()
        {
            this.InitializeMenuHandler += HandleInitializeMenuHandler;

            this.ReactToLoadedSyncedProject += HandleLoadedSyncedProject;
        }

        private void HandleLoadedSyncedProject(ProjectBase project)
        {

            if (NeedsXnbs(project))
            {
                AddXnbsToProject((VisualStudioProject)project);
            }
        }

        void HandleInitializeMenuHandler(MenuStrip menuStrip)
        {
            ToolStripMenuItem item = ToolStripHelper.Self.GetItem(menuStrip, L.MenuIds.ContentId);
            item.DropDownItems.Add(L.Texts.AddXnbToMonogame, null, HandleAddXnbsClick);
        }

        bool NeedsXnbs(ProjectBase project)
        {
            return project is IosMonogameProject or IosMonoGameNet8Project
                or Windows8MonoGameProject 
                or AndroidProject or AndroidMonoGameNet8Project
                or UwpProject 
                ;

        }

        void HandleAddXnbsClick(object sender, EventArgs args)
        {
            bool wereAnyProjectsFound = false;

            foreach (VisualStudioProject project in ProjectManager.SyncedProjects.Where(NeedsXnbs))
            {
                AddXnbsToProject(project);
                wereAnyProjectsFound = true;
            }

            if (!wereAnyProjectsFound)
            {
                PluginManager.ReceiveOutput("No synced projects need to have XNBs added.");
            }
        }

        private void AddXnbsToProject(VisualStudioProject project)
        {
            bool wasAnythingChanged = false;

            // We need to loop through all of the items in the 
            // base project, see if they are audio files (this is all
            // we look at for now) then add them.
            IEnumerable<ProjectItem> items = ((VisualStudioProject) ProjectManager.ProjectBase.ContentProject).EvaluatedItems;
            foreach (var buildItem in items.Where((item)=>
                ShouldAssociatedXnbBeCopied(item.UnevaluatedInclude, project)))
            {
                wasAnythingChanged |= AddAudioBuildItemToProject(project, buildItem);

            }

            if (wasAnythingChanged)
            {
                // We don't need to save the ContentProject
                // because there isn't one for W8
                project.Save(project.FullFileName.FullPath);
            }
        }

        public bool ShouldAssociatedXnbBeCopied(string fileName, ProjectBase project)
        {
            // On Android we (currently) only copy WAV's. MP3s work fine without XNB:
            if (project is AndroidProject or AndroidMonoGameNet8Project)
            {
                return FileManager.GetExtension(fileName) == "wav";

            }
            else
            {

                return FileManager.GetExtension(fileName) == "wav" ||
                    // Why don't we include WMA?
                    FileManager.GetExtension(fileName) == "mp3";
            }
        }

        private static bool AddAudioBuildItemToProject(VisualStudioProject project, ProjectItem buildItem)
        {
            bool wasAnythingChanged = false;
            // This item needs an associated entry in the project
            // The item will be relative to the main project as opposed
            // to the content project, inside the CopiedXnbs directory:
            string copiedXnb = ProjectManager.ProjectBase.Directory + "CopiedXnbs\\content\\" +
                buildItem.UnevaluatedInclude;

            var link = buildItem.GetLink();
            if(!string.IsNullOrEmpty( link ))
            {
                copiedXnb = ProjectManager.ProjectBase.Directory + "CopiedXnbs\\content\\" +
                    link;
            }

            copiedXnb = FileManager.RemoveDotDotSlash(copiedXnb);

            string extension = FileManager.GetExtension(buildItem.UnevaluatedInclude);

            bool isIos = project is IosMonogameProject or IosMonoGameNet8Project;
            bool isAndroid = project is AndroidProject or AndroidMonoGameNet8Project;
            
            string whatToAddToProject = null;

            // 
            bool copyOriginalFile = (isIos || isAndroid) && FileManager.GetExtension(buildItem.UnevaluatedInclude) != "wav";

            if (copyOriginalFile)
            {
                // Jan 1, 2014
                // Not sure why
                // we were making
                // this file absolute
                // using the synced project's
                // directory.  The file will be
                // shared by synced and original
                // projects so the file needs to be
                // made absolute according to that project.
                //whatToAddToProject = project.MakeAbsolute("content/" + buildItem.Include);
                whatToAddToProject = GlueCommands.Self.GetAbsoluteFileName("content/" + buildItem.UnevaluatedInclude, true);
            }
            else
            {
                whatToAddToProject = copiedXnb;
            }
            // Both sound and music files have XNBs associated with them so let's add that:
            whatToAddToProject = FileManager.RemoveExtension(whatToAddToProject) + ".xnb";
            copiedXnb = FileManager.RemoveExtension(copiedXnb) + ".xnb";


            var item = project.GetItem(whatToAddToProject, true);
            if (item == null)
            {
                item = project.AddContentBuildItem(whatToAddToProject, SyncedProjectRelativeType.Linked, false);

                string linkToSet = null;

                if (!string.IsNullOrEmpty(buildItem.GetLink()))
                {
                    linkToSet = "Content\\" + FileManager.RemoveExtension(buildItem.GetLink()) + ".xnb";
                }
                else
                {
                    linkToSet = "Content\\" + FileManager.RemoveExtension(buildItem.UnevaluatedInclude) + ".xnb";
                }

                // not needed for .NET 8 (not sure why...)
                if(project is AndroidProject)
                {
                    linkToSet = "Assets\\" + linkToSet;
                }
                
                item.SetMetadataValue("Link", linkToSet);


                PluginManager.ReceiveOutput("Added " + buildItem.EvaluatedInclude + " through the file " + whatToAddToProject);
                wasAnythingChanged = true;
            }

            wasAnythingChanged |= FixLink(item, project);


            if (isIos && extension == "mp3")
            {
                if (FileManager.FileExists(copiedXnb))
                {
                    ReplaceWmaReferenceToMp3ReferenceInXnb(copiedXnb, whatToAddToProject);
                }
                
            }

            // I think we want to tell the user that the XNB is missing so they know to build the PC project
            if(!FileManager.FileExists(copiedXnb))
            {

                PluginManager.ReceiveError("XNB file is missing - try rebuilding PC project: " + copiedXnb);
            }

            // Music files also have a wma file:
            if ((extension == "mp3" || extension == "wma") && 
                // iOS doesn't ignore MP3, so it's already there.
                !isIos)
            {
                if (isIos)
                {
                    whatToAddToProject = "Content\\" + buildItem.UnevaluatedInclude;
                }
                else
                {
                    whatToAddToProject = FileManager.RemoveExtension(whatToAddToProject) + ".wma";
                }

                var item2 = project.GetItem(whatToAddToProject, true);

                if (item2 == null)
                {
                    item2 = project.AddContentBuildItem(whatToAddToProject, SyncedProjectRelativeType.Linked, false);
                    item2.SetMetadataValue("Link", "Content\\" + FileManager.RemoveExtension(buildItem.UnevaluatedInclude) + "." + FileManager.GetExtension(whatToAddToProject));

                    PluginManager.ReceiveOutput("Added " + buildItem.EvaluatedInclude + " through the file " + whatToAddToProject);
                    wasAnythingChanged = true;
                }

                wasAnythingChanged |= FixLink(item2, project);
            }
            return wasAnythingChanged;
        }

        private static bool FixLink(ProjectItem item, ProjectBase project)
        {
            bool didFix = false;

            string oldLink = item.GetLink();
            string newLink = project.ProcessLink(oldLink);

            if (oldLink != newLink)
            {
                item.SetLink(newLink);
                didFix = true;
            }


            return didFix;
        }

        private static void ReplaceWmaReferenceToMp3ReferenceInXnb(string copiedXnb, string whatToAddToProject)
        {
            // The iOS uses the XNB to find the file name that it should load
            // then simply loads it.  iOS doesn't support WMAs, but it does support
            // MP3s.  We're going to be adding MP3s so let's load change, and save the
            // XNB:
            using (var stream = File.OpenRead(copiedXnb))
            {
                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, (int)stream.Length);

                char[] chars = new char[bytes.Length];


                for (int i = 0; i < bytes.Length; i++)
                {
                    chars[i] = (char)bytes[i];
                }

                string contentsAsString = new string(chars);

                string whatToLookFor = FileManager.RemovePath(whatToAddToProject).ToLowerInvariant() ;

                int index = GetIndexOfWhatToLookForIn(whatToLookFor, chars);

                if (index == -1)
                {
                    // this should never happen
                    int m = 3;
                }
                string whatToApply = FileManager.RemoveExtension(whatToLookFor) + ".mp3";

                for (int i = 0; i < whatToApply.Length; i++)
                {
                    bytes[i + index] = (byte)whatToApply[i];

                }



                if (File.Exists(whatToAddToProject))
                {
                    File.Delete(whatToAddToProject);
                }







                using (var writeStream = File.Create(whatToAddToProject, bytes.Length, FileOptions.None))
                {
                    writeStream.Write(bytes, 0, (int)bytes.Length);
                    writeStream.Close();
                }

            }
            PluginManager.ReceiveOutput("Saved  " + whatToAddToProject + " for iOS project.");
        }

        private static int GetIndexOfWhatToLookForIn(string whatToLookFor, char[] chars)
        {
            for (int i = 0; i < chars.Length - (whatToLookFor.Length - 1); i++)
            {
                bool foundIndex = true;
                for (int inString = 0; inString < whatToLookFor.Length; inString++)
                {
                    char currentChar = chars[i + inString];
                    bool isCurrentDot = currentChar == '.';

                    if (isCurrentDot && whatToLookFor[inString] == '.')
                    {
                        // The extensions aren't going to match, but that's okay, let's break.
                        break;
                    }

                    if (whatToLookFor[inString] != char.ToLower(chars[i + inString]))
                    {
                        foundIndex = false;
                        break;
                    }

                }

                if (foundIndex)
                {
                    return i;
                }
            }

            return -1;
        }





    }
}
