namespace Glue
{
	partial class MainGlueWindow
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainGlueWindow));
            this.mElementContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ElementImages = new System.Windows.Forms.ImageList(this.components);
            this.mMenu = new System.Windows.Forms.MenuStrip();
            this.ElementViewWindowToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.NavigateForwardButton = new System.Windows.Forms.Button();
            this.NavigateBackButton = new System.Windows.Forms.Button();
            this.rightPanelContainer = new System.Windows.Forms.SplitContainer();
            this.MainTabControl = new FlatRedBall.Glue.Controls.TabControlEx();
            this.PropertiesTab = new FlatRedBall.Glue.Controls.PluginTab();
            this.CodeTab = new System.Windows.Forms.TabPage();
            this.CodePreviewTextBox = new System.Windows.Forms.RichTextBox();
            this.tcRight = new FlatRedBall.Glue.Controls.TabControlEx();
            this.ExplorerTab = new FlatRedBall.Glue.Controls.PluginTab();
            this.ElementTreeView = new System.Windows.Forms.TreeView();
            this.SearchListBox = new System.Windows.Forms.ListBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.SearchTextbox = new System.Windows.Forms.TextBox();
            this.topPanelContainer = new System.Windows.Forms.SplitContainer();
            this.tcTop = new FlatRedBall.Glue.Controls.TabControlEx();
            this.leftPanelContainer = new System.Windows.Forms.SplitContainer();
            this.tcLeft = new FlatRedBall.Glue.Controls.TabControlEx();
            this.bottomPanelContainer = new System.Windows.Forms.SplitContainer();
            this.tcBottom = new FlatRedBall.Glue.Controls.TabControlEx();
            this.msProcesses = new System.Windows.Forms.MenuStrip();
            this.elementHost2 = new System.Windows.Forms.Integration.ElementHost();
            this.toolbarControl1 = new FlatRedBall.Glue.Controls.ToolbarControl();
            this.mElementContextMenu.SuspendLayout();
            this.mMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.rightPanelContainer)).BeginInit();
            this.rightPanelContainer.Panel1.SuspendLayout();
            this.rightPanelContainer.Panel2.SuspendLayout();
            this.rightPanelContainer.SuspendLayout();
            this.MainTabControl.SuspendLayout();
            this.CodeTab.SuspendLayout();
            this.tcRight.SuspendLayout();
            this.ExplorerTab.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.topPanelContainer)).BeginInit();
            this.topPanelContainer.Panel1.SuspendLayout();
            this.topPanelContainer.Panel2.SuspendLayout();
            this.topPanelContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.leftPanelContainer)).BeginInit();
            this.leftPanelContainer.Panel1.SuspendLayout();
            this.leftPanelContainer.Panel2.SuspendLayout();
            this.leftPanelContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bottomPanelContainer)).BeginInit();
            this.bottomPanelContainer.Panel1.SuspendLayout();
            this.bottomPanelContainer.Panel2.SuspendLayout();
            this.bottomPanelContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // mElementContextMenu
            // 
            
            this.mElementContextMenu.Name = "mElementContextMenu";
            this.mElementContextMenu.Size = new System.Drawing.Size(187, 384);
            this.mElementContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
            // 
            // ElementImages
            // 
            this.ElementImages.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.ElementImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ElementImages.ImageStream")));
            this.ElementImages.TransparentColor = System.Drawing.Color.Transparent;
            this.ElementImages.Images.SetKeyName(0, "transparent.png");
            this.ElementImages.Images.SetKeyName(1, "code.png");
            this.ElementImages.Images.SetKeyName(2, "edit_code.png");
            this.ElementImages.Images.SetKeyName(3, "entity.png");
            this.ElementImages.Images.SetKeyName(4, "file.png");
            this.ElementImages.Images.SetKeyName(5, "folder.png");
            this.ElementImages.Images.SetKeyName(6, "master_code.png");
            this.ElementImages.Images.SetKeyName(7, "master_entity.png");
            this.ElementImages.Images.SetKeyName(8, "master_file.png");
            this.ElementImages.Images.SetKeyName(9, "master_object.png");
            this.ElementImages.Images.SetKeyName(10, "master_screen.png");
            this.ElementImages.Images.SetKeyName(11, "master_states.png");
            this.ElementImages.Images.SetKeyName(12, "master_variables.png");
            this.ElementImages.Images.SetKeyName(13, "object.png");
            this.ElementImages.Images.SetKeyName(14, "screen.png");
            this.ElementImages.Images.SetKeyName(15, "states.png");
            this.ElementImages.Images.SetKeyName(16, "variable.png");
            this.ElementImages.Images.SetKeyName(17, "layerList.png");
            this.ElementImages.Images.SetKeyName(18, "collisionRelationshipList.png");
            // 
            // mMenu
            // 
            this.mMenu.Location = new System.Drawing.Point(0, 0);
            this.mMenu.Name = "mMenu";
            this.mMenu.Size = new System.Drawing.Size(764, 24);
            this.mMenu.TabIndex = 1;
            this.mMenu.Text = "menuStrip1";
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
            // rightPanelContainer
            // 
            this.rightPanelContainer.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.rightPanelContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.rightPanelContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightPanelContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.rightPanelContainer.Location = new System.Drawing.Point(0, 0);
            this.rightPanelContainer.Margin = new System.Windows.Forms.Padding(0);
            this.rightPanelContainer.Name = "rightPanelContainer";
            // 
            // rightPanelContainer.Panel1
            // 
            this.rightPanelContainer.Panel1.Controls.Add(this.MainTabControl);
            // 
            // rightPanelContainer.Panel2
            // 
            this.rightPanelContainer.Panel2.Controls.Add(this.tcRight);
            this.rightPanelContainer.Size = new System.Drawing.Size(764, 579);
            this.rightPanelContainer.SplitterDistance = 546;
            this.rightPanelContainer.TabIndex = 4;
            this.rightPanelContainer.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.rightPanelContainer_SplitterMoved);
            // 
            // MainTabControl
            // 
            this.MainTabControl.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.MainTabControl.Controls.Add(this.CodeTab);
            this.MainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainTabControl.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.MainTabControl.IgnoreFirst = false;
            this.MainTabControl.Location = new System.Drawing.Point(0, 0);
            this.MainTabControl.Name = "MainTabControl";
            this.MainTabControl.SelectedIndex = 0;
            this.MainTabControl.Size = new System.Drawing.Size(542, 575);
            this.MainTabControl.TabIndex = 4;
            // 
            // CodeTab
            // 
            this.CodeTab.Controls.Add(this.CodePreviewTextBox);
            this.CodeTab.Location = new System.Drawing.Point(4, 25);
            this.CodeTab.Name = "CodeTab";
            this.CodeTab.Size = new System.Drawing.Size(534, 546);
            this.CodeTab.TabIndex = 1;
            this.CodeTab.Text = "Code";
            this.CodeTab.UseVisualStyleBackColor = true;
            // 
            // CodePreviewTextBox
            // 
            this.CodePreviewTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CodePreviewTextBox.Location = new System.Drawing.Point(0, 0);
            this.CodePreviewTextBox.Name = "CodePreviewTextBox";
            this.CodePreviewTextBox.ReadOnly = true;
            this.CodePreviewTextBox.Size = new System.Drawing.Size(534, 546);
            this.CodePreviewTextBox.TabIndex = 3;
            this.CodePreviewTextBox.Text = "";
            this.CodePreviewTextBox.Visible = false;
            this.CodePreviewTextBox.WordWrap = false;
            this.CodePreviewTextBox.TextChanged += new System.EventHandler(this.richTextBox1_TextChanged);
            // 
            // tcRight
            // 
            this.tcRight.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.tcRight.Controls.Add(this.ExplorerTab);
            this.tcRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcRight.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tcRight.IgnoreFirst = false;
            this.tcRight.Location = new System.Drawing.Point(0, 0);
            this.tcRight.Margin = new System.Windows.Forms.Padding(0);
            this.tcRight.Name = "tcRight";
            this.tcRight.Padding = new System.Drawing.Point(6, 0);
            this.tcRight.SelectedIndex = 0;
            this.tcRight.Size = new System.Drawing.Size(210, 575);
            this.tcRight.TabIndex = 2;
            this.tcRight.ControlAdded += new System.Windows.Forms.ControlEventHandler(this.ControlAddedToRightView);
            this.tcRight.ControlRemoved += new System.Windows.Forms.ControlEventHandler(this.ControlRemovedFromRightView);
            // 
            // ExplorerTab
            // 
            this.ExplorerTab.Controls.Add(this.ElementTreeView);
            this.ExplorerTab.Controls.Add(this.SearchListBox);
            this.ExplorerTab.Controls.Add(this.panel1);
            this.ExplorerTab.DrawX = false;
            this.ExplorerTab.LastTabControl = this.tcRight;
            this.ExplorerTab.LastTimeClicked = new System.DateTime(2016, 4, 23, 12, 13, 55, 981);
            this.ExplorerTab.Location = new System.Drawing.Point(4, 23);
            this.ExplorerTab.Margin = new System.Windows.Forms.Padding(0);
            this.ExplorerTab.Name = "ExplorerTab";
            this.ExplorerTab.Size = new System.Drawing.Size(202, 548);
            this.ExplorerTab.TabIndex = 0;
            this.ExplorerTab.Text = "Explorer";
            this.ExplorerTab.UseVisualStyleBackColor = true;
            // 
            // ElementTreeView
            // 
            this.ElementTreeView.AllowDrop = true;
            this.ElementTreeView.BackColor = System.Drawing.Color.Black;
            this.ElementTreeView.ContextMenuStrip = this.mElementContextMenu;
            this.ElementTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ElementTreeView.ForeColor = System.Drawing.Color.White;
            this.ElementTreeView.HideSelection = false;
            this.ElementTreeView.ImageIndex = 0;
            this.ElementTreeView.ImageList = this.ElementImages;
            this.ElementTreeView.Location = new System.Drawing.Point(0, 23);
            this.ElementTreeView.Name = "ElementTreeView";
            this.ElementTreeView.SelectedImageIndex = 0;
            this.ElementTreeView.Size = new System.Drawing.Size(202, 525);
            this.ElementTreeView.TabIndex = 0;
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
            this.ElementTreeView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ElementTreeView_MouseDown);
            this.ElementTreeView.MouseHover += new System.EventHandler(this.ElementTreeView_MouseHover);
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
            this.SearchListBox.SelectedIndexChanged += new System.EventHandler(this.SearchListBox_SelectedIndexChanged);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.NavigateForwardButton);
            this.panel1.Controls.Add(this.NavigateBackButton);
            this.panel1.Controls.Add(this.SearchTextbox);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(202, 23);
            this.panel1.TabIndex = 6;
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
            // topPanelContainer
            // 
            this.topPanelContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.topPanelContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topPanelContainer.Location = new System.Drawing.Point(0, 0);
            this.topPanelContainer.Name = "topPanelContainer";
            this.topPanelContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // topPanelContainer.Panel1
            // 
            this.topPanelContainer.Panel1.Controls.Add(this.tcTop);
            this.topPanelContainer.Panel1Collapsed = true;
            // 
            // topPanelContainer.Panel2
            // 
            this.topPanelContainer.Panel2.Controls.Add(this.leftPanelContainer);
            this.topPanelContainer.Size = new System.Drawing.Size(764, 579);
            this.topPanelContainer.SplitterDistance = 82;
            this.topPanelContainer.TabIndex = 6;
            // 
            // tcTop
            // 
            this.tcTop.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.tcTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcTop.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tcTop.IgnoreFirst = false;
            this.tcTop.Location = new System.Drawing.Point(0, 0);
            this.tcTop.Name = "tcTop";
            this.tcTop.SelectedIndex = 0;
            this.tcTop.Size = new System.Drawing.Size(146, 78);
            this.tcTop.TabIndex = 0;
            this.tcTop.ControlAdded += new System.Windows.Forms.ControlEventHandler(this.tcPanel1_ControlAdded);
            this.tcTop.ControlRemoved += new System.Windows.Forms.ControlEventHandler(this.tcPanel1_ControlRemoved);
            // 
            // leftPanelContainer
            // 
            this.leftPanelContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.leftPanelContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftPanelContainer.Location = new System.Drawing.Point(0, 0);
            this.leftPanelContainer.Name = "leftPanelContainer";
            // 
            // leftPanelContainer.Panel1
            // 
            this.leftPanelContainer.Panel1.Controls.Add(this.tcLeft);
            this.leftPanelContainer.Panel1Collapsed = true;
            // 
            // leftPanelContainer.Panel2
            // 
            this.leftPanelContainer.Panel2.Controls.Add(this.rightPanelContainer);
            this.leftPanelContainer.Size = new System.Drawing.Size(764, 579);
            this.leftPanelContainer.SplitterDistance = 138;
            this.leftPanelContainer.TabIndex = 9;
            // 
            // tcLeft
            // 
            this.tcLeft.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.tcLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcLeft.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tcLeft.IgnoreFirst = false;
            this.tcLeft.Location = new System.Drawing.Point(0, 0);
            this.tcLeft.Name = "tcLeft";
            this.tcLeft.SelectedIndex = 0;
            this.tcLeft.Size = new System.Drawing.Size(134, 96);
            this.tcLeft.TabIndex = 1;
            this.tcLeft.ControlAdded += new System.Windows.Forms.ControlEventHandler(this.tcPanel1_ControlAdded);
            this.tcLeft.ControlRemoved += new System.Windows.Forms.ControlEventHandler(this.tcPanel1_ControlRemoved);
            // 
            // bottomPanelContainer
            // 
            this.bottomPanelContainer.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.bottomPanelContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.bottomPanelContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bottomPanelContainer.Location = new System.Drawing.Point(0, 54);
            this.bottomPanelContainer.Name = "bottomPanelContainer";
            this.bottomPanelContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // bottomPanelContainer.Panel1
            // 
            this.bottomPanelContainer.Panel1.Controls.Add(this.topPanelContainer);
            // 
            // bottomPanelContainer.Panel2
            // 
            this.bottomPanelContainer.Panel2.Controls.Add(this.tcBottom);
            this.bottomPanelContainer.Panel2Collapsed = true;
            this.bottomPanelContainer.Size = new System.Drawing.Size(764, 579);
            this.bottomPanelContainer.SplitterDistance = 520;
            this.bottomPanelContainer.TabIndex = 7;
            // 
            // tcBottom
            // 
            this.tcBottom.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.tcBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcBottom.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tcBottom.IgnoreFirst = false;
            this.tcBottom.Location = new System.Drawing.Point(0, 0);
            this.tcBottom.Name = "tcBottom";
            this.tcBottom.SelectedIndex = 0;
            this.tcBottom.Size = new System.Drawing.Size(146, 42);
            this.tcBottom.TabIndex = 1;
            this.tcBottom.ControlAdded += new System.Windows.Forms.ControlEventHandler(this.tcPanel2_ControlAdded);
            this.tcBottom.ControlRemoved += new System.Windows.Forms.ControlEventHandler(this.tcPanel2_ControlRemoved);
            // 
            // msProcesses
            // 
            this.msProcesses.AllowItemReorder = true;
            this.msProcesses.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Flow;
            this.msProcesses.Location = new System.Drawing.Point(0, 24);
            this.msProcesses.Name = "msProcesses";
            this.msProcesses.Size = new System.Drawing.Size(953, 23);
            this.msProcesses.TabIndex = 8;
            this.msProcesses.Visible = false;
            this.msProcesses.ItemAdded += new System.Windows.Forms.ToolStripItemEventHandler(this.MsProcessesItemAdded);
            this.msProcesses.ItemRemoved += new System.Windows.Forms.ToolStripItemEventHandler(this.MsProcessesItemRemoved);
            // 
            // elementHost2
            // 
            this.elementHost2.Dock = System.Windows.Forms.DockStyle.Top;
            this.elementHost2.Location = new System.Drawing.Point(0, 24);
            this.elementHost2.Name = "elementHost2";
            this.elementHost2.Size = new System.Drawing.Size(764, 30);
            this.elementHost2.TabIndex = 10;
            this.elementHost2.Text = "elementHost2";
            this.elementHost2.Child = this.toolbarControl1;
            // 
            // MainGlueWindow
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(764, 633);
            this.Controls.Add(this.bottomPanelContainer);
            this.Controls.Add(this.msProcesses);
            this.Controls.Add(this.elementHost2);
            this.Controls.Add(this.mMenu);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.mMenu;
            this.Name = "MainGlueWindow";
            this.Text = "FlatRedBall Glue";
            this.Activated += new System.EventHandler(this.Form1_Activated);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.mElementContextMenu.ResumeLayout(false);
            this.mMenu.ResumeLayout(false);
            this.mMenu.PerformLayout();
            this.rightPanelContainer.Panel1.ResumeLayout(false);
            this.rightPanelContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.rightPanelContainer)).EndInit();
            this.rightPanelContainer.ResumeLayout(false);
            this.MainTabControl.ResumeLayout(false);
            this.PropertiesTab.ResumeLayout(false);
            this.CodeTab.ResumeLayout(false);
            this.tcRight.ResumeLayout(false);
            this.ExplorerTab.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.topPanelContainer.Panel1.ResumeLayout(false);
            this.topPanelContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.topPanelContainer)).EndInit();
            this.topPanelContainer.ResumeLayout(false);
            this.leftPanelContainer.Panel1.ResumeLayout(false);
            this.leftPanelContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.leftPanelContainer)).EndInit();
            this.leftPanelContainer.ResumeLayout(false);
            this.bottomPanelContainer.Panel1.ResumeLayout(false);
            this.bottomPanelContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.bottomPanelContainer)).EndInit();
            this.bottomPanelContainer.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

        private System.Windows.Forms.MenuStrip mMenu;
        internal System.Windows.Forms.TreeView ElementTreeView;
        public System.Windows.Forms.RichTextBox CodePreviewTextBox;
        public System.Windows.Forms.ImageList ElementImages;
        internal System.Windows.Forms.ContextMenuStrip mElementContextMenu;

        private System.Windows.Forms.ToolTip ElementViewWindowToolTip;
        public System.Windows.Forms.SplitContainer rightPanelContainer;
        public System.Windows.Forms.TextBox SearchTextbox;
        public System.Windows.Forms.ListBox SearchListBox;
        private System.Windows.Forms.SplitContainer topPanelContainer;
        private System.Windows.Forms.SplitContainer bottomPanelContainer;
        private System.Windows.Forms.SplitContainer leftPanelContainer;
        private FlatRedBall.Glue.Controls.PluginTab ExplorerTab;
        private FlatRedBall.Glue.Controls.TabControlEx tcRight;
        private FlatRedBall.Glue.Controls.TabControlEx tcTop;
        private FlatRedBall.Glue.Controls.TabControlEx tcLeft;
        private FlatRedBall.Glue.Controls.TabControlEx tcBottom;
        public System.Windows.Forms.MenuStrip msProcesses;
        internal FlatRedBall.Glue.Controls.PluginTab PropertiesTab;
        internal System.Windows.Forms.TabPage CodeTab;
        internal FlatRedBall.Glue.Controls.TabControlEx MainTabControl;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button NavigateForwardButton;
        private System.Windows.Forms.Button NavigateBackButton;
        private System.Windows.Forms.Integration.ElementHost elementHost2;
        private FlatRedBall.Glue.Controls.ToolbarControl toolbarControl1;
    }
}

