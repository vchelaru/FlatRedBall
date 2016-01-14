using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins.Interfaces;

using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using FlatRedBall.Glue.ContentPipeline;
using FlatRedBall.Glue.Controls;
using System.Windows.Forms;
using Glue;
using FlatRedBall.Glue.Elements;

namespace PluginTestbed.FileOperations
{

    [ Export(typeof(ITreeViewRightClick)), Export(typeof(IMenuStripPlugin)) ]
    public class FileOperationsPlugin : ITreeViewRightClick, IMenuStripPlugin
    {
        enum CurrentElementOrAll
        {
            CurrentElement,
            AllElements
        }

        [Import("GlueCommands")]
        public IGlueCommands GlueCommands
        {
            get;
            set;
        }

        #region ITreeViewRightClick Members

        public void ReactToRightClick(System.Windows.Forms.TreeNode rightClickedTreeNode, System.Windows.Forms.ContextMenuStrip menuToModify)
        {
            if (rightClickedTreeNode.IsFilesContainerNode() && EditorLogic.CurrentElement != null)
            {
                menuToModify.Items.Add("-");
                menuToModify.Items.Add("Make all static/loaded only when referenced").Click += new EventHandler(OnMakeStaticAndLoadedWhenReferenced);
                menuToModify.Items.Add("Make all use Content Pipeline").Click += new EventHandler(OnUseContentPipelineClickCurrentElement);
                menuToModify.Items.Add("Make all use From-File").Click += new EventHandler(OnUseFromFileClickCurrentElement);
                menuToModify.Items.Add("Rebuild all").Click += new EventHandler(RebuildAllClick);
            }
        }

        void OnMakeStaticAndLoadedWhenReferenced(object sencer, EventArgs e)
        {
            IElement element = EditorLogic.CurrentElement;

            foreach (ReferencedFileSave rfs in element.ReferencedFiles)
            {
                rfs.IsSharedStatic = true;
                rfs.LoadedOnlyWhenReferenced = true;
                rfs.HasPublicProperty = true;
            }

            GlueCommands.RefreshCommands.RefreshUiForSelectedElement();
            GlueCommands.GenerateCodeCommands.GenerateCurrentElementCode();

            GlueCommands.GluxCommands.SaveGlux();

        }

        void OnUseContentPipelineClickCurrentElement(object sender, EventArgs e)
        {
            ContentPipelineChange(true, CurrentElementOrAll.CurrentElement);
        }

        void OnUseContentPipelineClickAll(object sender, EventArgs e)
        {
            ContentPipelineChange(true, CurrentElementOrAll.AllElements);
        }

        void OnUseFromFileClickCurrentElement(object sender, EventArgs e)
        {
            ContentPipelineChange(false, CurrentElementOrAll.CurrentElement);
        }

        void OnUseFromFileClickAll(object sender, EventArgs e)
        {
            ContentPipelineChange(false, CurrentElementOrAll.AllElements);
        }

        void RebuildAllClick(object sender, EventArgs e)
        {
            IElement element = EditorLogic.CurrentElement;
            foreach (ReferencedFileSave rfs in element.ReferencedFiles)
            {
                rfs.PerformExternalBuild();
            }

        }

        void ContentPipelineChange(bool useContentPipeline, CurrentElementOrAll currentOrAll)
        {
            TextureProcessorOutputFormat? textureFormat = null;

            if (useContentPipeline)
            {
                MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                mbmb.MessageText = "How should the texture formats be set?";
                mbmb.AddButton("Set to DXT Compression", DialogResult.Yes);
                mbmb.AddButton("Set to Color", DialogResult.OK);
                mbmb.AddButton("Do nothing", DialogResult.No);

                DialogResult result = mbmb.ShowDialog(Form1.Self);

                switch (result)
                {
                    case DialogResult.Yes:
                        textureFormat = TextureProcessorOutputFormat.DxtCompressed;
                        break;
                    case DialogResult.OK:
                        textureFormat = TextureProcessorOutputFormat.Color;
                        break;
                    case DialogResult.No:
                        // do nothing, leave it to null
                        break;


                }
            }

            InitializationWindow initWindow = new InitializationWindow();
            initWindow.Show(Form1.Self);
            initWindow.Message = "Performing Operation...";


            List<ReferencedFileSave> allRfses = new List<ReferencedFileSave>();

            if (currentOrAll == CurrentElementOrAll.CurrentElement)
            {
                IElement element = EditorLogic.CurrentElement;
                allRfses.AddRange(element.ReferencedFiles);
            }
            else
            {
                allRfses.AddRange(ObjectFinder.Self.GetAllReferencedFiles());
            }

            int totalRfs = allRfses.Count;
            //int count = 0;

            initWindow.SubMessage = "Removing/adding files as appropriate";
            Application.DoEvents();

            foreach (ReferencedFileSave rfs in allRfses)
            {
                if (rfs.GetAssetTypeInfo() != null && rfs.GetAssetTypeInfo().MustBeAddedToContentPipeline)
                {
                    rfs.UseContentPipeline = true;
                }
                else
                {
                    rfs.UseContentPipeline = useContentPipeline;
                }
            }

            ContentPipelineHelper.ReactToUseContentPipelineChange(allRfses);

            if (useContentPipeline)
            {

                initWindow.SubMessage = "Updating texture formats";
                Application.DoEvents();
                
                foreach (ReferencedFileSave rfs in allRfses)
                {
                    if (textureFormat.HasValue)
                    {
                        rfs.TextureFormat = textureFormat.Value;
                    }

                    ContentPipelineHelper.UpdateTextureFormatFor(rfs);
                }
            }

            initWindow.SubMessage = "Refreshing UI";
            Application.DoEvents();
            if (EditorLogic.CurrentElement != null)
            {
                GlueCommands.RefreshCommands.RefreshUiForSelectedElement();
            }

            initWindow.SubMessage = "Generating Code";
            Application.DoEvents();

            if (currentOrAll == CurrentElementOrAll.CurrentElement)
            {
                GlueCommands.GenerateCodeCommands.GenerateCurrentElementCode();
            }
            else
            {
                GlueCommands.GenerateCodeCommands.GenerateAllCode();
            }

            GlueCommands.GluxCommands.SaveGlux();

            GlueCommands.ProjectCommands.SaveProjects();


            initWindow.Close();


        }


        #endregion

        #region IPlugin Members

        public string FriendlyName
        {
            get { return "File Operations Plugin"; }
        }

        public Version Version
        {
            get { return new Version(1, 0); }
        }

        public void StartUp()
        {
        }

        public bool ShutDown(PluginShutDownReason shutDownReason)
        {
            ToolStripMenuItem itemToAddTo = GetItem("Project");
            itemToAddTo.DropDownItems.Remove(mUseContentPipeline);
            itemToAddTo.DropDownItems.Remove(mUseFromFile);
            return true;
        }

        #endregion

        #region IMenuStripPlugin Members

        ToolStripMenuItem mUseContentPipeline;
        ToolStripMenuItem mUseFromFile;

        MenuStrip mMenuStrip;

        public void InitializeMenu(MenuStrip menuStrip)
        {
            mMenuStrip = menuStrip;

            mUseContentPipeline = new ToolStripMenuItem("Make all use Content Pipeline");
            mUseFromFile = new ToolStripMenuItem("Make all use From-File");
            ToolStripMenuItem itemToAddTo = GetItem("Project");

            itemToAddTo.DropDownItems.Add(mUseContentPipeline);
            itemToAddTo.DropDownItems.Add(mUseFromFile);
            mUseContentPipeline.Click += new EventHandler(OnUseContentPipelineClickAll);
            mUseFromFile.Click += new EventHandler(OnUseFromFileClickAll);
        }

        ToolStripMenuItem GetItem(string name)
        {
            foreach (ToolStripMenuItem item in mMenuStrip.Items)
            {
                if (item.Text == name)
                {
                    return item;
                }
            }
            return null;
        }


        #endregion
    }
}
