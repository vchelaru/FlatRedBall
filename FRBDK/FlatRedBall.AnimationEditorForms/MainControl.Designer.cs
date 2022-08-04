namespace FlatRedBall.AnimationEditorForms
{
    partial class MainControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainControl));
            this.TreeViewAndEverythingElse = new System.Windows.Forms.SplitContainer();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.AnimationsTab = new System.Windows.Forms.TabPage();
            this.TreeViewRightClickMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.TexturesPage = new System.Windows.Forms.TabPage();
            this.TexturesTreeView = new System.Windows.Forms.TreeView();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.SelectedItemPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.UnitTypeComboBox = new System.Windows.Forms.ComboBox();
            this.tabControl2 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.PreviewSplitContainer = new System.Windows.Forms.SplitContainer();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.MenuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resizeTextureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newAnimationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.frameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.multipleframesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.CursorStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.AnimationTreeView = new FlatRedBall.AnimationEditorForms.Controls.CustomNodeColorTreeView();
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.animationsListToolBar1 = new FlatRedBall.AnimationEditorForms.Controls.AnimationsListToolBar();
            this.tileMapInfoWindow1 = new FlatRedBall.AnimationEditorForms.Controls.TileMapInfoWindow();
            this.WireframeTopUiControl = new FlatRedBall.AnimationEditorForms.Controls.WireframeEditControls();
            this.PreviewGraphicsControl = new XnaAndWinforms.GraphicsDeviceControl();
            this.previewControls1 = new FlatRedBall.AnimationEditorForms.Controls.PreviewControls();
            ((System.ComponentModel.ISupportInitialize)(this.TreeViewAndEverythingElse)).BeginInit();
            this.TreeViewAndEverythingElse.Panel1.SuspendLayout();
            this.TreeViewAndEverythingElse.Panel2.SuspendLayout();
            this.TreeViewAndEverythingElse.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.AnimationsTab.SuspendLayout();
            this.TexturesPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabControl2.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PreviewSplitContainer)).BeginInit();
            this.PreviewSplitContainer.Panel1.SuspendLayout();
            this.PreviewSplitContainer.Panel2.SuspendLayout();
            this.PreviewSplitContainer.SuspendLayout();
            this.MenuStrip.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // TreeViewAndEverythingElse
            // 
            this.TreeViewAndEverythingElse.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.TreeViewAndEverythingElse.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TreeViewAndEverythingElse.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.TreeViewAndEverythingElse.Location = new System.Drawing.Point(0, 24);
            this.TreeViewAndEverythingElse.Name = "TreeViewAndEverythingElse";
            // 
            // TreeViewAndEverythingElse.Panel1
            // 
            this.TreeViewAndEverythingElse.Panel1.Controls.Add(this.tabControl1);
            // 
            // TreeViewAndEverythingElse.Panel2
            // 
            this.TreeViewAndEverythingElse.Panel2.Controls.Add(this.splitContainer1);
            this.TreeViewAndEverythingElse.Size = new System.Drawing.Size(1028, 393);
            this.TreeViewAndEverythingElse.SplitterDistance = 165;
            this.TreeViewAndEverythingElse.TabIndex = 0;
            this.TreeViewAndEverythingElse.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.TreeViewAndEverythingElse_SplitterMoved);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.AnimationsTab);
            this.tabControl1.Controls.Add(this.TexturesPage);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(161, 389);
            this.tabControl1.TabIndex = 1;
            // 
            // AnimationsTab
            // 
            this.AnimationsTab.Controls.Add(this.AnimationTreeView);
            this.AnimationsTab.Controls.Add(this.elementHost1);
            this.AnimationsTab.Location = new System.Drawing.Point(4, 22);
            this.AnimationsTab.Name = "AnimationsTab";
            this.AnimationsTab.Padding = new System.Windows.Forms.Padding(3);
            this.AnimationsTab.Size = new System.Drawing.Size(153, 363);
            this.AnimationsTab.TabIndex = 0;
            this.AnimationsTab.Text = "Animations";
            this.AnimationsTab.UseVisualStyleBackColor = true;
            // 
            // TreeViewRightClickMenu
            // 
            this.TreeViewRightClickMenu.Name = "TreeViewRightClickMenu";
            this.TreeViewRightClickMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // TexturesPage
            // 
            this.TexturesPage.Controls.Add(this.TexturesTreeView);
            this.TexturesPage.Location = new System.Drawing.Point(4, 22);
            this.TexturesPage.Name = "TexturesPage";
            this.TexturesPage.Padding = new System.Windows.Forms.Padding(3);
            this.TexturesPage.Size = new System.Drawing.Size(153, 363);
            this.TexturesPage.TabIndex = 1;
            this.TexturesPage.Text = "Textures";
            this.TexturesPage.UseVisualStyleBackColor = true;
            // 
            // TexturesTreeView
            // 
            this.TexturesTreeView.AllowDrop = true;
            this.TexturesTreeView.ContextMenuStrip = this.TreeViewRightClickMenu;
            this.TexturesTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TexturesTreeView.HideSelection = false;
            this.TexturesTreeView.Location = new System.Drawing.Point(3, 3);
            this.TexturesTreeView.Name = "TexturesTreeView";
            this.TexturesTreeView.Size = new System.Drawing.Size(147, 357);
            this.TexturesTreeView.TabIndex = 1;
            // 
            // splitContainer1
            // 
            this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.SelectedItemPropertyGrid);
            this.splitContainer1.Panel1.Controls.Add(this.tileMapInfoWindow1);
            this.splitContainer1.Panel1.Controls.Add(this.UnitTypeComboBox);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tabControl2);
            this.splitContainer1.Size = new System.Drawing.Size(859, 393);
            this.splitContainer1.SplitterDistance = 151;
            this.splitContainer1.TabIndex = 0;
            // 
            // SelectedItemPropertyGrid
            // 
            this.SelectedItemPropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SelectedItemPropertyGrid.HelpVisible = false;
            this.SelectedItemPropertyGrid.Location = new System.Drawing.Point(0, 21);
            this.SelectedItemPropertyGrid.Name = "SelectedItemPropertyGrid";
            this.SelectedItemPropertyGrid.Size = new System.Drawing.Size(147, 257);
            this.SelectedItemPropertyGrid.TabIndex = 0;
            this.SelectedItemPropertyGrid.ToolbarVisible = false;
            // 
            // UnitTypeComboBox
            // 
            this.UnitTypeComboBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.UnitTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.UnitTypeComboBox.FormattingEnabled = true;
            this.UnitTypeComboBox.Location = new System.Drawing.Point(0, 0);
            this.UnitTypeComboBox.Name = "UnitTypeComboBox";
            this.UnitTypeComboBox.Size = new System.Drawing.Size(147, 21);
            this.UnitTypeComboBox.TabIndex = 1;
            this.UnitTypeComboBox.SelectedIndexChanged += new System.EventHandler(this.UnitTypeComboBox_SelectedIndexChanged);
            // 
            // tabControl2
            // 
            this.tabControl2.Controls.Add(this.tabPage1);
            this.tabControl2.Controls.Add(this.tabPage2);
            this.tabControl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl2.Location = new System.Drawing.Point(0, 0);
            this.tabControl2.Name = "tabControl2";
            this.tabControl2.SelectedIndex = 0;
            this.tabControl2.Size = new System.Drawing.Size(700, 389);
            this.tabControl2.TabIndex = 2;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.PreviewSplitContainer);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(692, 363);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "EditWindow";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // PreviewSplitContainer
            // 
            this.PreviewSplitContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.PreviewSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PreviewSplitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.PreviewSplitContainer.Location = new System.Drawing.Point(3, 3);
            this.PreviewSplitContainer.Name = "PreviewSplitContainer";
            this.PreviewSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // PreviewSplitContainer.Panel1
            // 
            this.PreviewSplitContainer.Panel1.Controls.Add(this.WireframeTopUiControl);
            this.PreviewSplitContainer.Panel1.Click += new System.EventHandler(this.PreviewSplitContainer_Panel1_Click);
            // 
            // PreviewSplitContainer.Panel2
            // 
            this.PreviewSplitContainer.Panel2.Controls.Add(this.PreviewGraphicsControl);
            this.PreviewSplitContainer.Panel2.Controls.Add(this.previewControls1);
            this.PreviewSplitContainer.Size = new System.Drawing.Size(686, 357);
            this.PreviewSplitContainer.SplitterDistance = 175;
            this.PreviewSplitContainer.TabIndex = 1;
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(692, 363);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Edit Window (new)";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // MenuStrip
            // 
            this.MenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.addToolStripMenuItem});
            this.MenuStrip.Location = new System.Drawing.Point(0, 0);
            this.MenuStrip.Name = "MenuStrip";
            this.MenuStrip.Size = new System.Drawing.Size(1028, 24);
            this.MenuStrip.TabIndex = 1;
            this.MenuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem,
            this.resizeTextureToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.copyToolStripMenuItem.Text = "Copy";
            this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
            // 
            // pasteToolStripMenuItem
            // 
            this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
            this.pasteToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.pasteToolStripMenuItem.Text = "Paste";
            this.pasteToolStripMenuItem.Click += new System.EventHandler(this.pasteToolStripMenuItem_Click);
            // 
            // resizeTextureToolStripMenuItem
            // 
            this.resizeTextureToolStripMenuItem.Name = "resizeTextureToolStripMenuItem";
            this.resizeTextureToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.resizeTextureToolStripMenuItem.Text = "Resize Texture...";
            this.resizeTextureToolStripMenuItem.Click += new System.EventHandler(this.resizeTextureToolStripMenuItem_Click);
            // 
            // addToolStripMenuItem
            // 
            this.addToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newAnimationToolStripMenuItem,
            this.frameToolStripMenuItem,
            this.multipleframesToolStripMenuItem});
            this.addToolStripMenuItem.Name = "addToolStripMenuItem";
            this.addToolStripMenuItem.Size = new System.Drawing.Size(41, 20);
            this.addToolStripMenuItem.Text = "Add";
            // 
            // newAnimationToolStripMenuItem
            // 
            this.newAnimationToolStripMenuItem.Name = "newAnimationToolStripMenuItem";
            this.newAnimationToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.newAnimationToolStripMenuItem.Text = "Animation";
            this.newAnimationToolStripMenuItem.Click += new System.EventHandler(this.AddAnimationToolStripMenuItem_Click);
            // 
            // frameToolStripMenuItem
            // 
            this.frameToolStripMenuItem.Name = "frameToolStripMenuItem";
            this.frameToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+plus";
            this.frameToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Oemplus)));
            this.frameToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.frameToolStripMenuItem.Text = "Frame";
            this.frameToolStripMenuItem.Click += new System.EventHandler(this.frameToolStripMenuItem_Click);
            // 
            // multipleframesToolStripMenuItem
            // 
            this.multipleframesToolStripMenuItem.Name = "multipleframesToolStripMenuItem";
            this.multipleframesToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.multipleframesToolStripMenuItem.Text = "Multiple Frames";
            this.multipleframesToolStripMenuItem.Click += new System.EventHandler(this.multipleframesToolStripMenuItem_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CursorStatusLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 417);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1028, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // CursorStatusLabel
            // 
            this.CursorStatusLabel.Name = "CursorStatusLabel";
            this.CursorStatusLabel.Size = new System.Drawing.Size(76, 17);
            this.CursorStatusLabel.Text = "Cursor: (X, Y)";
            // 
            // AnimationTreeView
            // 
            this.AnimationTreeView.AllowDrop = true;
            this.AnimationTreeView.AlwaysHaveOneNodeSelected = false;
            this.AnimationTreeView.ContextMenuStrip = this.TreeViewRightClickMenu;
            this.AnimationTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AnimationTreeView.HideSelection = false;
            this.AnimationTreeView.Location = new System.Drawing.Point(3, 33);
            this.AnimationTreeView.MultiSelectBehavior = CommonFormsAndControls.MultiSelectBehavior.CtrlDown;
            this.AnimationTreeView.Name = "AnimationTreeView";
            this.AnimationTreeView.SelectedNodes = ((System.Collections.Generic.List<System.Windows.Forms.TreeNode>)(resources.GetObject("AnimationTreeView.SelectedNodes")));
            this.AnimationTreeView.Size = new System.Drawing.Size(147, 327);
            this.AnimationTreeView.TabIndex = 0;
            this.AnimationTreeView.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.AnimationTreeView_ItemDrag);
            this.AnimationTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.AnimationTreeView_AfterSelect);
            this.AnimationTreeView.DragDrop += new System.Windows.Forms.DragEventHandler(this.AnimationTreeView_DragDrop);
            this.AnimationTreeView.DragEnter += new System.Windows.Forms.DragEventHandler(this.AnimationTreeView_DragEnter);
            this.AnimationTreeView.DragOver += new System.Windows.Forms.DragEventHandler(this.AnimationTreeView_DragOver);
            this.AnimationTreeView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.AnimationTreeView_KeyDown);
            this.AnimationTreeView.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.AnimationTreeView_KeyPress);
            this.AnimationTreeView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.AnimationTreeView_MouseClick);
            this.AnimationTreeView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.AnimationTreeView_MouseDown);
            // 
            // elementHost1
            // 
            this.elementHost1.Dock = System.Windows.Forms.DockStyle.Top;
            this.elementHost1.Location = new System.Drawing.Point(3, 3);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Size = new System.Drawing.Size(147, 30);
            this.elementHost1.TabIndex = 1;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = this.animationsListToolBar1;
            // 
            // tileMapInfoWindow1
            // 
            this.tileMapInfoWindow1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tileMapInfoWindow1.Location = new System.Drawing.Point(0, 278);
            this.tileMapInfoWindow1.Name = "tileMapInfoWindow1";
            this.tileMapInfoWindow1.Size = new System.Drawing.Size(147, 111);
            this.tileMapInfoWindow1.TabIndex = 2;
            this.tileMapInfoWindow1.TileMapInformation = null;
            // 
            // WireframeTopUiControl
            // 
            this.WireframeTopUiControl.DataContext = null;
            this.WireframeTopUiControl.Dock = System.Windows.Forms.DockStyle.Top;
            this.WireframeTopUiControl.Location = new System.Drawing.Point(0, 0);
            this.WireframeTopUiControl.Name = "WireframeTopUiControl";
            this.WireframeTopUiControl.PercentageValue = 100;
            this.WireframeTopUiControl.Size = new System.Drawing.Size(682, 23);
            this.WireframeTopUiControl.TabIndex = 1;
            this.WireframeTopUiControl.ZoomChanged += new System.EventHandler(this.zoomControl1_ZoomChanged);
            this.WireframeTopUiControl.Load += new System.EventHandler(this.WireframeTopUiControl_Load);
            // 
            // PreviewGraphicsControl
            // 
            this.PreviewGraphicsControl.DesiredFramesPerSecond = 30F;
            this.PreviewGraphicsControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PreviewGraphicsControl.Location = new System.Drawing.Point(0, 22);
            this.PreviewGraphicsControl.Name = "PreviewGraphicsControl";
            this.PreviewGraphicsControl.Size = new System.Drawing.Size(682, 152);
            this.PreviewGraphicsControl.TabIndex = 0;
            this.PreviewGraphicsControl.Text = "graphicsDeviceControl1";
            // 
            // previewControls1
            // 
            this.previewControls1.Dock = System.Windows.Forms.DockStyle.Top;
            this.previewControls1.IsOnionSkinVisible = false;
            this.previewControls1.Location = new System.Drawing.Point(0, 0);
            this.previewControls1.Name = "previewControls1";
            this.previewControls1.OffsetMultiplier = 1F;
            this.previewControls1.PercentageValue = 100;
            this.previewControls1.Size = new System.Drawing.Size(682, 22);
            this.previewControls1.SpriteAlignment = FlatRedBall.AnimationEditorForms.Data.SpriteAlignment.Center;
            this.previewControls1.TabIndex = 1;
            this.previewControls1.ZoomChanged += new System.EventHandler(this.previewControls1_ZoomChanged);
            // 
            // MainControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.TreeViewAndEverythingElse);
            this.Controls.Add(this.MenuStrip);
            this.Controls.Add(this.statusStrip1);
            this.Name = "MainControl";
            this.Size = new System.Drawing.Size(1028, 439);
            this.TreeViewAndEverythingElse.Panel1.ResumeLayout(false);
            this.TreeViewAndEverythingElse.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.TreeViewAndEverythingElse)).EndInit();
            this.TreeViewAndEverythingElse.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.AnimationsTab.ResumeLayout(false);
            this.TexturesPage.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.tabControl2.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.PreviewSplitContainer.Panel1.ResumeLayout(false);
            this.PreviewSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PreviewSplitContainer)).EndInit();
            this.PreviewSplitContainer.ResumeLayout(false);
            this.MenuStrip.ResumeLayout(false);
            this.MenuStrip.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer TreeViewAndEverythingElse;
        private FlatRedBall.AnimationEditorForms.Controls.CustomNodeColorTreeView AnimationTreeView;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.PropertyGrid SelectedItemPropertyGrid;
        private Controls.TileMapInfoWindow tileMapInfoWindow1;
        private System.Windows.Forms.ContextMenuStrip TreeViewRightClickMenu;
        private System.Windows.Forms.SplitContainer PreviewSplitContainer;
        private Controls.WireframeEditControls WireframeTopUiControl;
        private XnaAndWinforms.GraphicsDeviceControl PreviewGraphicsControl;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem resizeTextureToolStripMenuItem;
        private Controls.PreviewControls previewControls1;
        private System.Windows.Forms.ToolStripMenuItem addToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newAnimationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem frameToolStripMenuItem;
        public System.Windows.Forms.MenuStrip MenuStrip;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel CursorStatusLabel;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage AnimationsTab;
        private System.Windows.Forms.TabPage TexturesPage;
        private System.Windows.Forms.TreeView TexturesTreeView;
        public System.Windows.Forms.ComboBox UnitTypeComboBox;
        private System.Windows.Forms.Integration.ElementHost elementHost1;
        private Controls.AnimationsListToolBar animationsListToolBar1;
        private System.Windows.Forms.TabControl tabControl2;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.ToolStripMenuItem multipleframesToolStripMenuItem;
    }
}
