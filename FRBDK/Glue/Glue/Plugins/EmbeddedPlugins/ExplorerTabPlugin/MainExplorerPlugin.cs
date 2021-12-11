using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Navigation;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Glue;
using GlueFormsCore.Controls;
using GlueFormsCore.Plugins.EmbeddedPlugins.ExplorerTabPlugin.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;

using Point = System.Drawing.Point;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.ExplorerTabPlugin
{
    [Export(typeof(PluginBase))]
    class MainExplorerPlugin : EmbeddedPlugin
    {
        public static MainExplorerPlugin Self
        {
            get; private set;
        }
        PluginTab ExplorerTab;

        public System.Windows.Controls.ListBox SearchListBox;
        public System.Windows.Forms.TreeView ElementTreeView;
        public System.Windows.FrameworkElement ElementTreeViewContainer;
        public System.Windows.Controls.TextBox SearchTextbox;



        public override void StartUp()
        {
            AssignEvents();
            Initialize();
        }

        private void AssignEvents()
        {
            this.ReactToLoadedGlux += () => ExplorerTab.Show();
            this.ReactToUnloadedGlux += () => HandleProjectClose(MainPanelControl.IsExiting);
        }

        public void Initialize()
        {
            Self = this;
            //InitializeWinforms();

            this.ElementTreeView = new System.Windows.Forms.TreeView();
            // 
            // ElementTreeView
            // 
            this.ElementTreeView.AllowDrop = true;
            this.ElementTreeView.BackColor = System.Drawing.Color.Black;
            this.ElementTreeView.BorderStyle = BorderStyle.None;
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
            //this.ElementTreeView.BeforeSelect += new System.Windows.Forms.TreeViewCancelEventHandler(this.ElementTreeView_BeforeSelect);
            this.ElementTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.ElementTreeView_AfterSelect);
            this.ElementTreeView.DragDrop += new System.Windows.Forms.DragEventHandler(this.ElementTreeView_DragDrop);
            this.ElementTreeView.DragEnter += new System.Windows.Forms.DragEventHandler(this.ElementTreeView_DragEnter);
            this.ElementTreeView.DragOver += new System.Windows.Forms.DragEventHandler(this.ElementTreeView_DragOver);
            this.ElementTreeView.DoubleClick += new System.EventHandler(this.mElementTreeView_DoubleClick);

            this.ElementTreeView.KeyDown += this.ElementTreeView_KeyDown;
            this.ElementTreeView.KeyPress += this.ElementTreeView_KeyPress;

            this.ElementTreeView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.mElementTreeView_MouseClick);

            InitializeWpf();

        }

        private void InitializeWpf()
        {
            var window = new ExplorerView();

            ExplorerTab = this.CreateTab(window, "Explorer (old)", TabLocation.Left);

            ExplorerTab.CanClose = false;
            SearchBarHelper.Initialize(ElementTreeView);
            window.TreeViewHost.Child = this.ElementTreeView;
            ElementTreeViewContainer = window.TreeViewHost;

            SearchTextbox = window.SearchTextBox;

            SearchListBox = window.SearchResultListBox;
            SearchListBox.SelectionChanged += (not, used) =>
            {
                if(!SearchTextbox.IsFocused)
                {
                    SearchBarHelper.SearchListBoxIndexChanged();
                }
            };

            window.BackButton.Click += (not, used) => NavigateBackButton_Click(this, null);
            window.ForwardButton.Click += (not, used) => NavigateForwardButton_Click(this, null);

            var textBox = window.SearchTextBox;
            textBox.TextChanged += (not, used) => SearchBarHelper.SearchBarTextChange(textBox.Text); 
            textBox.PreviewKeyDown += (not, args) => SearchBarHelper.TextBoxKeyDown(args); 
            //textBox.LostFocus += SearchBarHelper.TextBoxLeave(SearchTextbox); 


            TreeNodeStackManager.Self.Initialize(window.BackButton, window.ForwardButton);

            //SearchBarHelper.Initialize(SearchTextbox);

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
                var node = ElementTreeView.GetNodeAt(e.X, e.Y);
                MainExplorerPlugin.Self.ElementTreeView.SelectedNode = node;

                RightClickHelper.PopulateRightClickItems(node);
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

            ExplorerTab.Hide();
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
                else if (GlueState.Self.CurrentEntitySave != null && ElementTreeView.SelectedNode.Tag is EntitySave)
                {
                    // copy ElementSave
                    GlueState.Self.Clipboard.CopiedObject = GlueState.Self.CurrentEntitySave;
                }
                else if (GlueState.Self.CurrentScreenSave != null && ElementTreeView.SelectedNode.Tag is ScreenSave)
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

                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(newEntitySave);
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

                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(newScreenSave);
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
            if (e.KeyCode == Keys.Enter)
            {
                ElementViewWindow.ElementDoubleClicked();
                e.Handled = true;
            }

            if(!e.Handled)
            {
                if(HotkeyManager.Self.TryHandleKeys(e.KeyData))
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
            System.Diagnostics.Debug.WriteLine("-------------------------------" + e.KeyCode);
        }

        private void SearchListBox_Click(object sender, EventArgs e)
        {
            SearchBarHelper.SearchListBoxIndexChanged();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            //SearchBarHelper.SearchBarTextChange();
        }

        private void SearchTextbox_Leave(object sender, EventArgs e)
        {
            //SearchBarHelper.TextBoxLeave(SearchTextbox);
        }

        private void SearchTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            SearchBarHelper.TextBoxKeyDown(e);
        }
    }
}
