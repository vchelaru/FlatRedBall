using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
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
            var newRfs = rfs.Clone();

            var container = rfs.GetContainer();

            while(NameVerifier.IsReferencedFileNameValid(newRfs.Name,
                newRfs.GetAssetTypeInfo(), rfs, rfs.GetContainer(), out string throwaway))
            {
                newRfs.Name = StringFunctions.IncrementNumberAtEnd(newRfs.Name);
            }

            if(container != null)
            {
                throw new NotImplementedException("Bug Vic to implement this");
            }
            else
            {

            }
        }
    }
}
