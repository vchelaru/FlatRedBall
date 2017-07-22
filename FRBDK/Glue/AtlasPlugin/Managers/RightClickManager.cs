using FlatRedBall.Glue.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;

namespace AtlasPlugin.Managers
{
    public class RightClickManager : Singleton<RightClickManager>
    {
        public void HandleTreeViewRightClick(TreeNode rightClickedTreeNode, ContextMenuStrip menuToModify)
        {
            ReferencedFileSave rfs;

            var getIfShouldShowMenu = GetIfShouldShowRightClickOption(rightClickedTreeNode, out rfs);

            List<string> availableAtlases = null;

            if (getIfShouldShowMenu)
            {
                string fullFileName = GlueCommands.Self.GetAbsoluteFileName(rfs);

                if (System.IO.File.Exists(fullFileName))
                {
                    // This might be slow, we may want to cache it:
                    TpsLoadResult tpsLoadResult;
                    var model = TpsFileSave.Load(fullFileName, out tpsLoadResult);

                    var succeeded = string.IsNullOrEmpty(tpsLoadResult.ErrorMessage) &&
                        string.IsNullOrEmpty(tpsLoadResult.MissingFile);

                    if(succeeded)
                    {
                        availableAtlases = model.AtlasFilters
                            .Select(item=>
                            {
                                if (item.EndsWith("/"))
                                {
                                    return item.Substring(0, item.Length - 1);
                                }
                                else
                                {
                                    return item;
                                }
                            })
                            .ToList();
                    }
                }
            }

            if(availableAtlases != null && availableAtlases.Count > 0)
            {
                var menuToAddTo = new ToolStripMenuItem("Include Atlas");
                menuToModify.Items.Add(menuToAddTo);

                foreach(var item in availableAtlases)
                {

                    var atlasMenuItem = new ToolStripMenuItem(item);
                    atlasMenuItem.Click += HandleScreenToAddClick;
                    menuToAddTo.DropDownItems.Add(atlasMenuItem);
                }
            }
        }

        private void HandleScreenToAddClick(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;

            var atlasName = menuItem.Text;

            if(atlasName.EndsWith("/"))
            {
                atlasName = atlasName.Substring(0, atlasName.Length - 1);
            }

            string appName = FileManager.RemovePath(FileManager.RemoveExtension(
                GlueState.Self.CurrentGlueProjectFileName));

            string fileName = AtlasFileManager.AtlasFolder + appName + "_" + 
                atlasName.Replace("/", "_") + ".atlas";



            if(System.IO.File.Exists(fileName))
            {
                bool cancelled = false;

                var newFile = FlatRedBall.Glue.FormHelpers.RightClickHelper.AddSingleFile(
                    fileName, ref cancelled);

                // After we add it, we want to make sure the atlas is the first item in the list, so that it's loaded before anything that may depend on it:
                List<ReferencedFileSave> filesList = null;
                if(GlueState.Self.CurrentElement != null)
                {
                    filesList = GlueState.Self.CurrentElement.ReferencedFiles;
                    GlueState.Self.CurrentElement.HasChanged = true;
                }
                else
                {
                    filesList = GlueState.Self.CurrentGlueProject.GlobalFiles;
                    GlueState.Self.CurrentGlueProject.GlobalContentHasChanged = true;
                }

                filesList.Remove(newFile);
                filesList.Insert(0, newFile);

                if(GlueState.Self.CurrentElement != null)
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                }
                else
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateGlobalContentCode();
                }
                GlueCommands.Self.GluxCommands.SaveGlux();
            }
            else
            {
                MessageBox.Show($"Could not find the atlas file:\n\n{fileName}\n\nTry rebuilding the atlases from the .tps file.");
            }
        }

        private bool GetIfShouldShowRightClickOption(TreeNode rightClickedTreeNode, out ReferencedFileSave tpsRfs)
        {
            tpsRfs = null;
            bool shouldContinue = true;

            if (!rightClickedTreeNode.IsFilesContainerNode() && !rightClickedTreeNode.IsGlobalContentContainerNode())
            {
                shouldContinue = false;
            }


            if (shouldContinue)
            {
                // Let's get all the available Screens:
                tpsRfs = GlueState.Self.CurrentGlueProject?.GetAllReferencedFiles().FirstOrDefault
                    (item => FileManager.GetExtension(item.Name) == "tps");

                shouldContinue = tpsRfs != null;
            }

            return shouldContinue;
        }

    }
}
