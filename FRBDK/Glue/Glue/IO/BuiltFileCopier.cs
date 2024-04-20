using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using FlatRedBall.IO;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.SaveClasses;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using GlueFormsCore.FormHelpers;
using FlatRedBall.Glue.FormHelpers;

namespace FlatRedBall.Glue.IO
{
    enum ErrorReportingStyle
    {
        MessageBox,
        GlueOutput
    }


    // Why does this exist and also the XnbContentAdder
    /// <summary>
    /// Currently project types like MonoGame require 
    /// some files to be built as XNBs before they can
    /// be loaded (like audio files).  This plugin is used
    /// // to copy built files to projects that need them.
    /// </summary>
    [Export(typeof(Plugins.PluginBase))]
    class BuiltFileCopier : EmbeddedPlugin
    {
        public override void StartUp()
        {
            this.ReactToBuiltFileChangeHandler += HandleBuiltFileChange;
            this.ReactToNewFileHandler += HandleNewFile;

            this.ReactToTreeViewRightClickHandler += HandleTreeViewRightClick;

            this.ReactToLoadedSyncedProject += HandleLoadedSyncedProject;
        }

        private void HandleLoadedSyncedProject(ProjectBase obj)
        {
            if (NeedsCopiedXnbs(obj))
            {
                foreach (var rfs in ObjectFinder.Self.GetAllReferencedFiles())
                {
                    TryCopyingBuiltFile(GlueCommands.Self.GetAbsoluteFileName(rfs), ErrorReportingStyle.GlueOutput);
                }
            }
        }

        void HandleTreeViewRightClick(ITreeNode rightClickedTreeNode, List<GeneralToolStripMenuItem> menuToModify)
        {
            if (rightClickedTreeNode.Tag != null && rightClickedTreeNode.Tag is ReferencedFileSave)
            {
                ReferencedFileSave rfs = rightClickedTreeNode.Tag as ReferencedFileSave;

                var extension = FileManager.GetExtension(rfs.Name);

                if (extension == "wma" || extension == "mp3")
                {
                    menuToModify.Add("Copy XNBs for file", HandleCopyXnbMenuClick);
                }


            }
        }

        private void HandleCopyXnbMenuClick(object sender, EventArgs e)
        {
            ReferencedFileSave rfs = GlueState.Self.CurrentReferencedFileSave;

            if (rfs != null)
            {
                string outputFile = ProjectManager.ProjectBase.Directory + "bin/debug/Content/" + rfs.Name;
                outputFile = FileManager.RemoveExtension(outputFile) + ".xnb";


                if (!System.IO.File.Exists(outputFile))
                {
                    MessageBox.Show("The XNB doesn't exist.  Did you build the file?");
                }
                else
                {
                    HandleBuiltFileChange(outputFile);
                }
            }

        }

        void HandleNewFile(ReferencedFileSave rfs, AssetTypeInfo assetTypeInfo)
        {
            HandleBuiltFileChange(GlueCommands.Self.GetAbsoluteFileName(rfs));
        }

        bool NeedsCopiedXnbs(ProjectBase project)
        {
            return project is IosMonogameProject or IosMonoGameNet8Project
                or Windows8MonoGameProject 
                or AndroidProject or AndroidMonoGameNet8Project
                or UwpProject 
                or MonoGameDesktopGlBaseProject;

        }

        void HandleBuiltFileChange(string fileName)
        {
            TryCopyingBuiltFile(fileName, ErrorReportingStyle.MessageBox);
        }

        void TryCopyingBuiltFile(string fileName, ErrorReportingStyle errorReportingStyle)
        {
            // We only care about XNBs and WMAs
            bool shouldCopy = ProjectManager.SyncedProjects.Any(NeedsCopiedXnbs);

            if(shouldCopy)
            {
                string extension = FileManager.GetExtension(fileName);
                shouldCopy = extension == "xnb" || extension == "wma" || extension == "mp3";
            }

            if(GlueState.Self.CurrentGlueProject != null)
            {

                string projectDirectory = GlueState.Self.CurrentCodeProjectFileName.GetDirectoryContainingThis().FullPath;
                string destinationDirectory = FileManager.GetDirectory(fileName);
                destinationDirectory = FileManager.MakeRelative(destinationDirectory, projectDirectory);
                // look for the first instance of "/content/" which is in the root of the bin folder on PC

                if(shouldCopy)
                {
                    // If the changed file is not a file in the bin folder, we don't worry about copying it
                    int indexOfContent = destinationDirectory.ToLowerInvariant().IndexOf("/content/");

                    if (indexOfContent == -1)
                    {
                        shouldCopy = false;
                    }
                }



                if (shouldCopy)
                {

                    try
                    {

                        int indexOfContent = destinationDirectory.ToLowerInvariant().IndexOf("/content/");
                        if (indexOfContent != -1)
                        {
                            // add 1 to take off the leading slash so the resulting path doesn't have a double forward slash
                            destinationDirectory = destinationDirectory.Substring(indexOfContent + 1);
                        }
                        else
                        {
                            PluginManager.ReceiveError("Could not identify root binary folder for path " + fileName);
                        }


                        destinationDirectory = projectDirectory + "CopiedXnbs/" + destinationDirectory;

                        System.IO.Directory.CreateDirectory(destinationDirectory);

                        string destinationFile = destinationDirectory +
                            FileManager.RemovePath(fileName);


                        System.IO.File.Copy(fileName, destinationFile, true);

                        Plugins.PluginManager.ReceiveOutput("Copied built file " + fileName + " to " + destinationFile);

                    }
                    catch (Exception e)
                    {
                        if (errorReportingStyle == ErrorReportingStyle.MessageBox)
                        {
                            System.Windows.Forms.MessageBox.Show("Error copying built file:\n\n" + e.ToString());
                        }
                        else
                        {
                            FlatRedBall.Glue.Plugins.PluginManager.ReceiveOutput(
                                "Error copying built file: " + e.ToString());
                        }
                    }
                }

            }

        }
    }
}
