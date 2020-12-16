using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace OfficialPluginsCore.FilesPlugin.Managers
{
    static class RightClickManager
    {
        internal static void HandleRightClick(TreeNode rightClickedTreeNode, ContextMenuStrip menuToModify)
        {
            var tag = rightClickedTreeNode.Tag;

            if(tag is ReferencedFileSave rfs)
            {
                menuToModify.Items.Add(
                    $"Duplicate file",
                    null,
                    (not, used) => HandleDuplicate(rfs));
            }
        }

        private static void HandleDuplicate(ReferencedFileSave rfs)
        {
            var file = GlueCommands.Self.GetAbsoluteFileName(rfs);


            var newRfs = rfs.Clone();

            var stripped = FileManager.RemovePath(FileManager.RemoveExtension(newRfs.Name));

            var container = rfs.GetContainer();

            var directoryOnDisk = FileManager.GetDirectory(file);
            var extension = FileManager.GetExtension(rfs.Name);

            while (!NameVerifier.IsReferencedFileNameValid(stripped,
                newRfs.GetAssetTypeInfo(), newRfs, rfs.GetContainer(), out string throwaway) || 
                System.IO.File.Exists(directoryOnDisk + stripped + "." + extension)
                )
            {
                stripped = StringFunctions.IncrementNumberAtEnd(stripped);
            }

            newRfs.Name = FileManager.GetDirectory(rfs.Name, RelativeType.Relative) + stripped + "." + FileManager.GetExtension(rfs.Name);

            var destinationFile = FileManager.GetDirectory(file) + stripped + "." + FileManager.GetExtension(file);

            System.IO.File.Copy(file, destinationFile);

            if(container != null)
            {
                throw new NotImplementedException("Bug Vic to implement this");
            }
            else
            {
                GlueCommands.Self.GluxCommands.AddReferencedFileToGlobalContent(newRfs);
            }

            var customClass = GlueState.Self.CurrentGlueProject.GetCustomClassReferencingFile(rfs.Name);

            if(customClass != null)
            {
                customClass.CsvFilesUsingThis.Add(newRfs.Name);
            }

            GlueCommands.Self.GluxCommands.SaveGlux();
            GlueCommands.Self.ProjectCommands.SaveProjects();

            GlueState.Self.CurrentReferencedFileSave = newRfs;
        }
    }
}
