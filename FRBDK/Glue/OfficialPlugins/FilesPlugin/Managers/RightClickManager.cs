using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using GlueFormsCore.FormHelpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace OfficialPlugins.FilesPlugin.Managers
{
    static class RightClickManager
    {
        internal static void HandleRightClick(ITreeNode rightClickedTreeNode, List<GeneralToolStripMenuItem> menuToModify)
        {
            var tag = rightClickedTreeNode.Tag;

            if(tag is ReferencedFileSave rfs)
            {
                menuToModify.Add(
                    $"Duplicate file",
                    async (not, used) =>
                    {
                        await GlueCommands.Self.GluxCommands.DuplicateAsync(rfs);
                    });
            }
        }
    }
}
