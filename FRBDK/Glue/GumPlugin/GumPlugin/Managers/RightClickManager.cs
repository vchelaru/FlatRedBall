using FlatRedBall.Glue.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Gum.DataTypes;
using System.Windows.Forms;
using FlatRedBall.IO;
using GumPlugin.CodeGeneration;

namespace GumPlugin.Managers
{
    public class RightClickManager : Singleton<RightClickManager>
    {
        
        public void HandleTreeViewRightClick(System.Windows.Forms.TreeNode rightClickedTreeNode, System.Windows.Forms.ContextMenuStrip menuToModify)
        {
            TryAddAddGumScreenItem(rightClickedTreeNode, menuToModify);

            TryAddRegenerateGumElement(rightClickedTreeNode, menuToModify);
        }

        private void TryAddRegenerateGumElement(TreeNode rightClickedTreeNode, ContextMenuStrip menuToModify)
        {
            var file = rightClickedTreeNode.Tag as ReferencedFileSave;

            bool shouldShowRegenerateCodeMenu =
                file != null;

            if(shouldShowRegenerateCodeMenu)
            {
                var extension = FileManager.GetExtension(file.Name);

                shouldShowRegenerateCodeMenu =
                    extension == GumProjectSave.ComponentExtension ||
                    extension == GumProjectSave.ProjectExtension ||
                    extension == GumProjectSave.ScreenExtension ||
                    extension == GumProjectSave.StandardExtension;

            }

            if(file != null && shouldShowRegenerateCodeMenu)
            {

                var newMenu = new ToolStripMenuItem("Regenerate Gum Code");
                menuToModify.Items.Add(newMenu);
                newMenu.Click += delegate
                {
                    var fileName = GlueCommands.Self.GetAbsoluteFileName(file);
                    CodeGeneratorManager.Self.GenerateDueToFileChange(fileName);

                };
            }
        }

        private bool TryAddAddGumScreenItem(TreeNode rightClickedTreeNode, ContextMenuStrip menuToModify)
        {
            bool shouldContinue = true;
            if (!rightClickedTreeNode.IsFilesContainerNode() || !rightClickedTreeNode.Parent.IsScreenNode())
            {
                shouldContinue = false;
            }

            ReferencedFileSave gumxRfs = null;

            if (shouldContinue)
            {
                // Let's get all the available Screens:
                gumxRfs = GumProjectManager.Self.GetRfsForGumProject();
                shouldContinue = gumxRfs != null;

            }

            if (shouldContinue)
            {
                string fullFileName = GlueCommands.Self.GetAbsoluteFileName(gumxRfs);

                if (System.IO.File.Exists(fullFileName))
                {
                    string error;

                    // Calling Load does a deep load.  We only want references, so we're
                    // going to do a shallow load for perf reasons.
                    //GumProjectSave gps = GumProjectSave.Load(fullFileName, out error);
                    GumProjectSave gps = FileManager.XmlDeserialize<GumProjectSave>(fullFileName);

                    if (gps.ScreenReferences.Count != 0)
                    {
                        var menuToAddScreensTo = new ToolStripMenuItem("Add Gum Screen");

                        menuToModify.Items.Add(menuToAddScreensTo);

                        foreach (var screen in gps.ScreenReferences)
                        {
                            var screenMenuItem = new ToolStripMenuItem(screen.Name);
                            screenMenuItem.Click += HandleScreenToAddClick;
                            menuToAddScreensTo.DropDownItems.Add(screenMenuItem);
                        }
                    }
                }
            }

            return shouldContinue;
        }

        private void HandleScreenToAddClick(object sender, EventArgs e)
        {
            string screenName = ((ToolStripMenuItem)sender).Text;

            AddScreenByName(screenName);

        }

        public void AddScreenByName(string screenName)
        {
            string fullFileName = AppState.Self.GumProjectFolder + "Screens/" +
                screenName + "." + GumProjectSave.ScreenExtension;

            if (System.IO.File.Exists(fullFileName))
            {
                bool cancelled = false;

                var newRfs = FlatRedBall.Glue.FormHelpers.RightClickHelper.AddSingleFile(
                    fullFileName, ref cancelled);

                // prior to doing any codegen, need to refresh the project specific ATIs:
                AssetTypeInfoManager.Self.RefreshProjectSpecificAtis();


                var element = CodeGeneratorManager.GetElementFrom(newRfs);

                if(element != null)
                {
                    newRfs.RuntimeType =
                        GueDerivingClassCodeGenerator.Self.GetQualifiedRuntimeTypeFor(element);

                    GlueCommands.Self.GluxCommands.SaveGluxTask();
                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCodeTask();

                }
            }
            else
            {
                MessageBox.Show("Could not find the file for the Gum screen " + screenName);
            }
        }
    }
}
