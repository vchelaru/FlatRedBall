using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Navigation;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Glue;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;

using Point = System.Drawing.Point;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.ExplorerTabPlugin
{
    //[Export(typeof(PluginBase))]
    class MainExplorerPlugin : EmbeddedPlugin
    {
        public static MainExplorerPlugin Self
        {
            get; private set;
        }
        PluginTabPage ExplorerTab;

        private System.Windows.Forms.Panel searchControlPanel;
        public System.Windows.Forms.ListBox SearchListBox;
        public System.Windows.Forms.TreeView ElementTreeView;

        private System.Windows.Forms.Button NavigateForwardButton;
        private System.Windows.Forms.Button NavigateBackButton;

        private System.Windows.Forms.ToolTip ElementViewWindowToolTip;
        public System.Windows.Forms.TextBox SearchTextbox;



        public override void StartUp()
        {

        }

        public void Initialize()
        {
            Self = this;
            this.ElementTreeView = new System.Windows.Forms.TreeView();
            this.SearchListBox = new System.Windows.Forms.ListBox();
            this.searchControlPanel = new System.Windows.Forms.Panel();
            this.NavigateForwardButton = new System.Windows.Forms.Button();
            this.NavigateBackButton = new System.Windows.Forms.Button();
            this.ElementViewWindowToolTip = new System.Windows.Forms.ToolTip(MainGlueWindow.Self.components);
            this.SearchTextbox = new System.Windows.Forms.TextBox();

            // 
            // NavigateForwardButton
            // 
            this.NavigateForwardButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.NavigateForwardButton.Location = new System.Drawing.Point(181, 0);
            this.NavigateForwardButton.Name = "NavigateForwardButton";
            this.NavigateForwardButton.Size = new System.Drawing.Size(22, 23);
            this.NavigateForwardButton.TabIndex = 7;
            this.NavigateForwardButton.Text = ">";
            this.ElementViewWindowToolTip.SetToolTip(this.NavigateForwardButton, "Navigate Forward ( ALT + -> )");
            this.NavigateForwardButton.UseVisualStyleBackColor = true;
            this.NavigateForwardButton.Click += new System.EventHandler(this.NavigateForwardButton_Click);
            // 
            // NavigateBackButton
            // 
            this.NavigateBackButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.NavigateBackButton.Location = new System.Drawing.Point(160, 0);
            this.NavigateBackButton.Name = "NavigateBackButton";
            this.NavigateBackButton.Size = new System.Drawing.Size(22, 23);
            this.NavigateBackButton.TabIndex = 6;
            this.NavigateBackButton.Text = "<";
            this.ElementViewWindowToolTip.SetToolTip(this.NavigateBackButton, "Navigate Back ( ALT + <- )");
            this.NavigateBackButton.UseVisualStyleBackColor = true;
            this.NavigateBackButton.Click += new System.EventHandler(this.NavigateBackButton_Click);

            // 
            // SearchTextbox
            // 
            this.SearchTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SearchTextbox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.SearchTextbox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.SearchTextbox.Location = new System.Drawing.Point(0, 2);
            this.SearchTextbox.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.SearchTextbox.Name = "SearchTextbox";
            this.SearchTextbox.Size = new System.Drawing.Size(160, 20);
            this.SearchTextbox.TabIndex = 5;
            this.SearchTextbox.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            this.SearchTextbox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SearchTextbox_KeyDown);
            this.SearchTextbox.Leave += new System.EventHandler(this.SearchTextbox_Leave);

            // 
            // ElementTreeView
            // 
            this.ElementTreeView.AllowDrop = true;
            this.ElementTreeView.BackColor = System.Drawing.Color.Black;
            this.ElementTreeView.ContextMenuStrip = MainGlueWindow.Self.mElementContextMenu;
            this.ElementTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ElementTreeView.ForeColor = System.Drawing.Color.White;
            this.ElementTreeView.HideSelection = false;
            this.ElementTreeView.ImageIndex = 0;
            this.ElementTreeView.ImageList = MainGlueWindow.Self.ElementImages;
            //this.ElementTreeView.Location = new System.Drawing.Point(0, 23);
            this.ElementTreeView.Name = "ElementTreeView";
            //this.ElementTreeView.SelectedImageIndex = 0;
            //this.ElementTreeView.TabIndex = 0;
            this.ElementTreeView.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.ElementTreeView_ItemDrag);
            this.ElementTreeView.BeforeSelect += new System.Windows.Forms.TreeViewCancelEventHandler(this.ElementTreeView_BeforeSelect);
            this.ElementTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.ElementTreeView_AfterSelect);
            this.ElementTreeView.DragDrop += new System.Windows.Forms.DragEventHandler(this.ElementTreeView_DragDrop);
            this.ElementTreeView.DragEnter += new System.Windows.Forms.DragEventHandler(this.ElementTreeView_DragEnter);
            this.ElementTreeView.DragOver += new System.Windows.Forms.DragEventHandler(this.ElementTreeView_DragOver);
            this.ElementTreeView.DoubleClick += new System.EventHandler(this.mElementTreeView_DoubleClick);
            this.ElementTreeView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ElementTreeView_KeyDown);
            this.ElementTreeView.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.ElementTreeView_KeyPress);
            this.ElementTreeView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.mElementTreeView_MouseClick);
            // 
            // SearchListBox
            // 
            this.SearchListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SearchListBox.FormattingEnabled = true;
            this.SearchListBox.Location = new System.Drawing.Point(0, 23);
            this.SearchListBox.Name = "SearchListBox";
            this.SearchListBox.Size = new System.Drawing.Size(202, 525);
            this.SearchListBox.TabIndex = 1;
            this.SearchListBox.Visible = false;
            this.SearchListBox.Click += new System.EventHandler(this.SearchListBox_Click);
            // 
            // panel1
            // 
            this.searchControlPanel.Controls.Add(this.NavigateForwardButton);
            this.searchControlPanel.Controls.Add(this.NavigateBackButton);
            this.searchControlPanel.Controls.Add(this.SearchTextbox);
            this.searchControlPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.searchControlPanel.Location = new System.Drawing.Point(0, 0);
            this.searchControlPanel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.searchControlPanel.Name = "panel1";
            this.searchControlPanel.Size = new System.Drawing.Size(202, 23);
            this.searchControlPanel.TabIndex = 6;


            var mainPanel = new Panel();
            mainPanel.Dock = DockStyle.Fill;
            ExplorerTab = this.AddToTab(PluginManager.RightTab,
                mainPanel, "Explorer");

            ExplorerTab.DrawX = false;


            mainPanel.Controls.Add(this.ElementTreeView);
            mainPanel.Controls.Add(this.SearchListBox);
            mainPanel.Controls.Add(this.searchControlPanel);


            TreeNodeStackManager.Self.Initialize(NavigateBackButton, NavigateForwardButton);

            SearchBarHelper.Initialize(SearchTextbox);

            InitializeElementViewWindow();
        }

        private void InitializeElementViewWindow()
        {
            TreeNode entityNode = new TreeNode("Entities");
            TreeNode screenNode = new TreeNode("Screens");
            TreeNode globalContentNode = new TreeNode("Global Content Files");

            ElementTreeView.Nodes.Add(entityNode);
            ElementTreeView.Nodes.Add(screenNode);
            ElementTreeView.Nodes.Add(globalContentNode);

            ElementViewWindow.Initialize(ElementTreeView, entityNode, screenNode, globalContentNode);
        }

        private void mElementTreeView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                RightClickHelper.PopulateRightClickItems(ElementTreeView.GetNodeAt(e.X, e.Y));
            }
        }

        private void mElementTreeView_DoubleClick(object sender, EventArgs e)
        {
            Point point = new Point(Cursor.Position.X, Cursor.Position.Y);

            Point topLeft = ElementTreeView.PointToScreen(new Point(0, 0));

            Point relative = new Point(point.X - topLeft.X, point.Y - topLeft.Y);

            var node = this.ElementTreeView.GetNodeAt(relative);
            var hitTestResult = ElementTreeView.HitTest(relative);
            if (node != null &&
                (hitTestResult.Location == TreeViewHitTestLocations.Image ||
                hitTestResult.Location == TreeViewHitTestLocations.Label))
            {
                ElementViewWindow.ElementDoubleClicked();
            }
        }

        public void HandleProjectClose(bool isExiting)
        {
            // Select null so plugins deselect:
            ElementTreeView.SelectedNode = null;

            // This only matters if we're not exiting the app:
            if (isExiting == false)
            {

                ElementViewWindow.AfterSelect();

                ElementTreeView.Nodes.Clear();

                InitializeElementViewWindow();
            }
        }

        private void ElementTreeView_KeyPress(object sender, KeyPressEventArgs e)
        {
            // copy, paste, ctrl c, ctrl v, ctrl + c, ctrl + v, ctrl+c, ctrl+v
            #region Copy ( (char)3 )

            if (e.KeyChar == (char)3)
            {
                e.Handled = true;

                if (GlueState.Self.CurrentNamedObjectSave != null)
                {
                    GlueState.Self.Clipboard.CopiedObject = GlueState.Self.CurrentNamedObjectSave;
                }
                else if (GlueState.Self.CurrentEntitySave != null && ElementTreeView.SelectedNode is EntityTreeNode)
                {
                    // copy ElementSave
                    GlueState.Self.Clipboard.CopiedObject = GlueState.Self.CurrentEntitySave;
                }
                else if (GlueState.Self.CurrentScreenSave != null && ElementTreeView.SelectedNode is ScreenTreeNode)
                {
                    // copy ScreenSave
                    GlueState.Self.Clipboard.CopiedObject = GlueState.Self.CurrentScreenSave;
                }


            }

            #endregion

            #region Paste ( (char)22 )

            else if (e.KeyChar == (char)22)
            {
                e.Handled = true;

                // Vic says: Currently pasting does NOT bring over any non-generated code.  This will
                // need to be fixed eventually

                // Paste CTRL+V stuff



                if (GlueState.Self.Clipboard.CopiedEntity != null)
                {
                    MessageBox.Show("Pasted Entities will not copy any code that you have written in custom functions.");

                    EntitySave newEntitySave = GlueState.Self.Clipboard.CopiedEntity.Clone();

                    newEntitySave.Name += "Copy";

                    string oldFile = newEntitySave.Name + ".cs";
                    string oldGeneratedFile = newEntitySave.Name + ".Generated.cs";
                    string newFile = newEntitySave.Name + "Copy.cs";
                    string newGeneratedFile = newEntitySave.Name + "Copy.Generated.cs";

                    // Not sure why we are adding here - the ProjectManager.AddEntity takes care of it.
                    //ProjectManager.GlueProjectSave.Entities.Add(newEntitySave);
                    GlueCommands.Self.GluxCommands.EntityCommands.AddEntity(newEntitySave);

                    GlueState.Self.Find.EntityTreeNode(newEntitySave).RefreshTreeNodes();
                }
                else if (GlueState.Self.Clipboard.CopiedScreen != null)
                {
                    MessageBox.Show("Pasted Screens will not copy any code that you have written in custom functions.");

                    ScreenSave newScreenSave = GlueState.Self.Clipboard.CopiedScreen.Clone();

                    newScreenSave.Name += "Copy";

                    string oldFile = newScreenSave.Name + ".cs";
                    string oldGeneratedFile = newScreenSave.Name + ".Generated.cs";
                    string newFile = newScreenSave.Name + "Copy.cs";
                    string newGeneratedFile = newScreenSave.Name + "Copy.Generated.cs";

                    // Not sure why we are adding here - AddScreen takes care of it.

                    GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen(newScreenSave);

                    GlueState.Self.Find.ScreenTreeNode(newScreenSave).RefreshTreeNodes();
                }
                else if (GlueState.Self.Clipboard.CopiedNamedObject != null)
                {
                    // todo: implement this, using duplicate
                }
            }

            #endregion

            else if (e.KeyChar == '\r')
            {
                // treat it like a double-click
                e.Handled = true;
            }

        }

        private void ElementTreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            // Get the tree.
            TreeView tree = (TreeView)sender;

            // Get the node underneath the mouse.
            TreeNode node = e.Item as TreeNode;
            tree.SelectedNode = node;

            // Start the drag-and-drop operation with a cloned copy of the node.
            if (node != null)
            {
                ElementViewWindow.TreeNodeDraggedOff = node;

                TreeNode targetNode = null;
                targetNode = ElementTreeView.SelectedNode;
                ElementViewWindow.ButtonUsed = e.Button;

                //ElementTreeView_DragDrop(node, DragDropEffects.Move | DragDropEffects.Copy);
                tree.DoDragDrop(node, DragDropEffects.Move | DragDropEffects.Copy);
            }
        }

        private void ElementTreeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (this.ElementTreeView.SelectedNode != null

                // August 31 2019
                // If the user drag+dropped off a tree node, then as they move over
                // other nodes they will get selected. We don't want to record that as
                // a movement.
                // Actually this value doesn't get nulled out when dropping a node, so 
                // can't use this now. Oh well, I won't bother with fixing this for now, 
                // I thought it would be a quick fix...
                // && ElementViewWindow.TreeNodeDraggedOff == null
                )
            {
                TreeNodeStackManager.Self.Push(ElementTreeView.SelectedNode);
            }
        }

        private void NavigateBackButton_Click(object sender, EventArgs e)
        {
            TreeNodeStackManager.Self.GoBack();
        }

        private void NavigateForwardButton_Click(object sender, EventArgs e)
        {
            TreeNodeStackManager.Self.GoForward();
        }

        private void ElementTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            ElementViewWindow.AfterSelect();
        }

        private void ElementTreeView_DragDrop(object sender, DragEventArgs e)
        {
            ElementViewWindow.DragDrop(sender, e);
        }

        private void ElementTreeView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void ElementTreeView_DragOver(object sender, DragEventArgs e)
        {
            ElementViewWindow.DragOver(sender, e);
        }

        private void ElementTreeView_KeyDown(object sender, KeyEventArgs e)
        {

            #region Delete key

            if (e.KeyCode == Keys.Delete)
            {
                RightClickHelper.RemoveFromProjectToolStripMenuItem();
            }
            #endregion

            else if (e.KeyCode == Keys.Enter)
            {
                ElementViewWindow.ElementDoubleClicked();
                e.Handled = true;
            }
        }

        private void SearchListBox_Click(object sender, EventArgs e)
        {
            SearchBarHelper.SearchListBoxIndexChanged();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            SearchBarHelper.SearchBarTextChange();
        }

        private void SearchTextbox_Leave(object sender, EventArgs e)
        {
            SearchBarHelper.TextBoxLeave(SearchTextbox);
        }

        private void SearchTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            SearchBarHelper.TextBoxKeyDown(e);
        }
    }
}
