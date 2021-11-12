using FlatRedBall.Glue;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Windows.Forms;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.CodePreviewPlugin
{
    // notes about this plugin:
    // Initially this was supposed to be a plugin that supported both viewing and editing code.
    // Eventually Vic determined that maintaining the code editor here was a lot of effort, and even
    // with all this effort the editor didn't get even close to Visual Studio.
    // So the editing support was removed and it became a readonly view.
    // That has since broken. On March 7, 2021 it was moved out of the main window into its own plugin
    // as part of the effort to clean the main window. At some point maybe we want this to work again? Until
    // then, this is a non-functional plugin.
    // To make it work, we just need to uncomment the item select, and fill the rich text box with the contents of the selected ifle
    [Export(typeof(PluginBase))]
    class MainCodePreviewPlugin : EmbeddedPlugin
    {
        internal PluginTab CodeTab;
        public System.Windows.Forms.RichTextBox CodePreviewTextBox;

        public override void StartUp()
        {
            this.CodePreviewTextBox = new System.Windows.Forms.RichTextBox();

            CodeTab = CreateAndAddTab(CodePreviewTextBox, "Code");
            // 
            // CodePreviewTextBox
            // 
            this.CodePreviewTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CodePreviewTextBox.Name = "CodePreviewTextBox";
            this.CodePreviewTextBox.ReadOnly = true;
            this.CodePreviewTextBox.WordWrap = false;


            CodeTab.Hide();

        }

        string CurrentCodeFile
        {
            get
            {
                TreeNode treeNode = GlueState.Self.CurrentTreeNode;
                {
                    if (treeNode != null && treeNode.Text.EndsWith(".cs"))
                    {
                        return treeNode.Text;
                    }
                    else
                    {
                        return "";
                    }
                }
            }
        }
    }
}
