namespace FlatRedBallProfiler
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadSectionTextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadFromClipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadRenderBreaksToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.runGameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.organizeTimingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hierarchyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.totalTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.SectionTreeView = new System.Windows.Forms.TreeView();
            this.DetailsTextBox = new System.Windows.Forms.RichTextBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.SectionsTab = new System.Windows.Forms.TabPage();
            this.PullFromScreenButton = new System.Windows.Forms.Button();
            this.RenderBreakTab = new System.Windows.Forms.TabPage();
            this.RenderBreakHistoryControlHost = new System.Windows.Forms.Integration.ElementHost();
            this.renderBreakHistoryControl1 = new FlatRedBallProfiler.Controls.RenderBreakHistoryControl();
            this.CurrentRenderBreakControl = new System.Windows.Forms.SplitContainer();
            this.RenderBreakTreeView = new System.Windows.Forms.TreeView();
            this.FromEngineButton = new System.Windows.Forms.Button();
            this.tabControl2 = new System.Windows.Forms.TabControl();
            this.TextureTab = new System.Windows.Forms.TabPage();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.PropertiesTab = new System.Windows.Forms.TabPage();
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.HistoryRadioButton = new System.Windows.Forms.RadioButton();
            this.CurrentRenderBreakRadio = new System.Windows.Forms.RadioButton();
            this.ManagedObjectsTab = new System.Windows.Forms.TabPage();
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.managedObjectsControl1 = new FlatRedBallProfiler.Controls.ManagedObjectsControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.elementHost2 = new System.Windows.Forms.Integration.ElementHost();
            this.contentManagerControl1 = new FlatRedBallProfiler.Controls.ContentManagerControl();
            this.menuStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.SectionsTab.SuspendLayout();
            this.RenderBreakTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CurrentRenderBreakControl)).BeginInit();
            this.CurrentRenderBreakControl.Panel1.SuspendLayout();
            this.CurrentRenderBreakControl.Panel2.SuspendLayout();
            this.CurrentRenderBreakControl.SuspendLayout();
            this.tabControl2.SuspendLayout();
            this.TextureTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.PropertiesTab.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.ManagedObjectsTab.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(435, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadSectionTextToolStripMenuItem,
            this.loadFromClipboardToolStripMenuItem,
            this.loadRenderBreaksToolStripMenuItem,
            this.runGameToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // loadSectionTextToolStripMenuItem
            // 
            this.loadSectionTextToolStripMenuItem.Name = "loadSectionTextToolStripMenuItem";
            this.loadSectionTextToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.loadSectionTextToolStripMenuItem.Text = "Load Section Text";
            this.loadSectionTextToolStripMenuItem.Click += new System.EventHandler(this.loadSectionTextToolStripMenuItem_Click);
            // 
            // loadFromClipboardToolStripMenuItem
            // 
            this.loadFromClipboardToolStripMenuItem.Name = "loadFromClipboardToolStripMenuItem";
            this.loadFromClipboardToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.loadFromClipboardToolStripMenuItem.Text = "Load From Clipboard";
            this.loadFromClipboardToolStripMenuItem.Click += new System.EventHandler(this.loadFromClipboardToolStripMenuItem_Click);
            // 
            // loadRenderBreaksToolStripMenuItem
            // 
            this.loadRenderBreaksToolStripMenuItem.Name = "loadRenderBreaksToolStripMenuItem";
            this.loadRenderBreaksToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.loadRenderBreaksToolStripMenuItem.Text = "Load Render Breaks";
            this.loadRenderBreaksToolStripMenuItem.Click += new System.EventHandler(this.loadRenderBreaksToolStripMenuItem_Click);
            // 
            // runGameToolStripMenuItem
            // 
            this.runGameToolStripMenuItem.Name = "runGameToolStripMenuItem";
            this.runGameToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.runGameToolStripMenuItem.Text = "Run Game";
            this.runGameToolStripMenuItem.Click += new System.EventHandler(this.runGameToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.organizeTimingToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "View";
            // 
            // organizeTimingToolStripMenuItem
            // 
            this.organizeTimingToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.hierarchyToolStripMenuItem,
            this.totalTimeToolStripMenuItem});
            this.organizeTimingToolStripMenuItem.Name = "organizeTimingToolStripMenuItem";
            this.organizeTimingToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.organizeTimingToolStripMenuItem.Text = "Organize Timing";
            // 
            // hierarchyToolStripMenuItem
            // 
            this.hierarchyToolStripMenuItem.Name = "hierarchyToolStripMenuItem";
            this.hierarchyToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
            this.hierarchyToolStripMenuItem.Text = "Expanded Times";
            this.hierarchyToolStripMenuItem.Click += new System.EventHandler(this.expanedToolStripMenuItem_Click);
            // 
            // totalTimeToolStripMenuItem
            // 
            this.totalTimeToolStripMenuItem.Name = "totalTimeToolStripMenuItem";
            this.totalTimeToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
            this.totalTimeToolStripMenuItem.Text = "Combined Times";
            this.totalTimeToolStripMenuItem.Click += new System.EventHandler(this.collapsedTimeToolStripMenuItem_Click);
            // 
            // SectionTreeView
            // 
            this.SectionTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SectionTreeView.Location = new System.Drawing.Point(3, 3);
            this.SectionTreeView.Name = "SectionTreeView";
            this.SectionTreeView.Size = new System.Drawing.Size(421, 301);
            this.SectionTreeView.TabIndex = 1;
            this.SectionTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterSelect);
            // 
            // DetailsTextBox
            // 
            this.DetailsTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DetailsTextBox.BackColor = System.Drawing.SystemColors.ControlLight;
            this.DetailsTextBox.Location = new System.Drawing.Point(3, 310);
            this.DetailsTextBox.Name = "DetailsTextBox";
            this.DetailsTextBox.ReadOnly = true;
            this.DetailsTextBox.Size = new System.Drawing.Size(421, 61);
            this.DetailsTextBox.TabIndex = 2;
            this.DetailsTextBox.Text = "";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.SectionsTab);
            this.tabControl1.Controls.Add(this.RenderBreakTab);
            this.tabControl1.Controls.Add(this.ManagedObjectsTab);
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 24);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(435, 428);
            this.tabControl1.TabIndex = 3;
            // 
            // SectionsTab
            // 
            this.SectionsTab.Controls.Add(this.PullFromScreenButton);
            this.SectionsTab.Controls.Add(this.SectionTreeView);
            this.SectionsTab.Controls.Add(this.DetailsTextBox);
            this.SectionsTab.Location = new System.Drawing.Point(4, 22);
            this.SectionsTab.Name = "SectionsTab";
            this.SectionsTab.Padding = new System.Windows.Forms.Padding(3);
            this.SectionsTab.Size = new System.Drawing.Size(427, 402);
            this.SectionsTab.TabIndex = 0;
            this.SectionsTab.Text = "Sections";
            this.SectionsTab.UseVisualStyleBackColor = true;
            // 
            // PullFromScreenButton
            // 
            this.PullFromScreenButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.PullFromScreenButton.Location = new System.Drawing.Point(3, 373);
            this.PullFromScreenButton.Name = "PullFromScreenButton";
            this.PullFromScreenButton.Size = new System.Drawing.Size(138, 23);
            this.PullFromScreenButton.TabIndex = 3;
            this.PullFromScreenButton.Text = "Pull From Screen";
            this.PullFromScreenButton.UseVisualStyleBackColor = true;
            this.PullFromScreenButton.Click += new System.EventHandler(this.PullFromScreenButton_Click);
            // 
            // RenderBreakTab
            // 
            this.RenderBreakTab.Controls.Add(this.RenderBreakHistoryControlHost);
            this.RenderBreakTab.Controls.Add(this.CurrentRenderBreakControl);
            this.RenderBreakTab.Controls.Add(this.groupBox1);
            this.RenderBreakTab.Location = new System.Drawing.Point(4, 22);
            this.RenderBreakTab.Name = "RenderBreakTab";
            this.RenderBreakTab.Padding = new System.Windows.Forms.Padding(3);
            this.RenderBreakTab.Size = new System.Drawing.Size(427, 402);
            this.RenderBreakTab.TabIndex = 1;
            this.RenderBreakTab.Text = "Render Breaks";
            this.RenderBreakTab.UseVisualStyleBackColor = true;
            // 
            // RenderBreakHistoryControlHost
            // 
            this.RenderBreakHistoryControlHost.Location = new System.Drawing.Point(18, 271);
            this.RenderBreakHistoryControlHost.Name = "RenderBreakHistoryControlHost";
            this.RenderBreakHistoryControlHost.Size = new System.Drawing.Size(200, 100);
            this.RenderBreakHistoryControlHost.TabIndex = 3;
            this.RenderBreakHistoryControlHost.Text = "elementHost3";
            this.RenderBreakHistoryControlHost.Child = this.renderBreakHistoryControl1;
            // 
            // CurrentRenderBreakControl
            // 
            this.CurrentRenderBreakControl.Location = new System.Drawing.Point(143, 120);
            this.CurrentRenderBreakControl.Name = "CurrentRenderBreakControl";
            this.CurrentRenderBreakControl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // CurrentRenderBreakControl.Panel1
            // 
            this.CurrentRenderBreakControl.Panel1.Controls.Add(this.RenderBreakTreeView);
            // 
            // CurrentRenderBreakControl.Panel2
            // 
            this.CurrentRenderBreakControl.Panel2.Controls.Add(this.FromEngineButton);
            this.CurrentRenderBreakControl.Panel2.Controls.Add(this.tabControl2);
            this.CurrentRenderBreakControl.Size = new System.Drawing.Size(327, 217);
            this.CurrentRenderBreakControl.SplitterDistance = 107;
            this.CurrentRenderBreakControl.SplitterWidth = 6;
            this.CurrentRenderBreakControl.TabIndex = 1;
            // 
            // RenderBreakTreeView
            // 
            this.RenderBreakTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RenderBreakTreeView.Location = new System.Drawing.Point(0, 0);
            this.RenderBreakTreeView.Name = "RenderBreakTreeView";
            this.RenderBreakTreeView.Size = new System.Drawing.Size(327, 107);
            this.RenderBreakTreeView.TabIndex = 0;
            this.RenderBreakTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.RenderBreakTreeView_AfterSelect);
            // 
            // FromEngineButton
            // 
            this.FromEngineButton.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.FromEngineButton.Location = new System.Drawing.Point(0, 81);
            this.FromEngineButton.Name = "FromEngineButton";
            this.FromEngineButton.Size = new System.Drawing.Size(327, 23);
            this.FromEngineButton.TabIndex = 2;
            this.FromEngineButton.Text = "Update From Engine";
            this.FromEngineButton.UseVisualStyleBackColor = true;
            this.FromEngineButton.Click += new System.EventHandler(this.FromEngineButton_Click);
            // 
            // tabControl2
            // 
            this.tabControl2.Controls.Add(this.TextureTab);
            this.tabControl2.Controls.Add(this.PropertiesTab);
            this.tabControl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl2.Location = new System.Drawing.Point(0, 0);
            this.tabControl2.Name = "tabControl2";
            this.tabControl2.SelectedIndex = 0;
            this.tabControl2.Size = new System.Drawing.Size(327, 104);
            this.tabControl2.TabIndex = 1;
            // 
            // TextureTab
            // 
            this.TextureTab.Controls.Add(this.checkBox1);
            this.TextureTab.Controls.Add(this.pictureBox1);
            this.TextureTab.Location = new System.Drawing.Point(4, 22);
            this.TextureTab.Name = "TextureTab";
            this.TextureTab.Padding = new System.Windows.Forms.Padding(3);
            this.TextureTab.Size = new System.Drawing.Size(319, 78);
            this.TextureTab.TabIndex = 0;
            this.TextureTab.Text = "Texture";
            this.TextureTab.UseVisualStyleBackColor = true;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(0, 6);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(122, 17);
            this.checkBox1.TabIndex = 1;
            this.checkBox1.Text = "Show Entire Texture";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox1.Location = new System.Drawing.Point(-1, 29);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(72, 65);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Visible = false;
            // 
            // PropertiesTab
            // 
            this.PropertiesTab.Controls.Add(this.propertyGrid1);
            this.PropertiesTab.Location = new System.Drawing.Point(4, 22);
            this.PropertiesTab.Name = "PropertiesTab";
            this.PropertiesTab.Padding = new System.Windows.Forms.Padding(3);
            this.PropertiesTab.Size = new System.Drawing.Size(319, 78);
            this.PropertiesTab.TabIndex = 1;
            this.PropertiesTab.Text = "Properties";
            this.PropertiesTab.UseVisualStyleBackColor = true;
            // 
            // propertyGrid1
            // 
            this.propertyGrid1.CategoryForeColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid1.HelpVisible = false;
            this.propertyGrid1.Location = new System.Drawing.Point(3, 3);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.Size = new System.Drawing.Size(313, 72);
            this.propertyGrid1.TabIndex = 0;
            this.propertyGrid1.ToolbarVisible = false;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.HistoryRadioButton);
            this.groupBox1.Controls.Add(this.CurrentRenderBreakRadio);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(421, 50);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "View";
            // 
            // HistoryRadioButton
            // 
            this.HistoryRadioButton.AutoSize = true;
            this.HistoryRadioButton.Checked = true;
            this.HistoryRadioButton.Location = new System.Drawing.Point(15, 19);
            this.HistoryRadioButton.Name = "HistoryRadioButton";
            this.HistoryRadioButton.Size = new System.Drawing.Size(57, 17);
            this.HistoryRadioButton.TabIndex = 1;
            this.HistoryRadioButton.TabStop = true;
            this.HistoryRadioButton.Text = "History";
            this.HistoryRadioButton.UseVisualStyleBackColor = true;
            this.HistoryRadioButton.CheckedChanged += new System.EventHandler(this.HistoryRadioButton_CheckedChanged);
            // 
            // CurrentRenderBreakRadio
            // 
            this.CurrentRenderBreakRadio.AutoSize = true;
            this.CurrentRenderBreakRadio.Location = new System.Drawing.Point(87, 19);
            this.CurrentRenderBreakRadio.Name = "CurrentRenderBreakRadio";
            this.CurrentRenderBreakRadio.Size = new System.Drawing.Size(128, 17);
            this.CurrentRenderBreakRadio.TabIndex = 0;
            this.CurrentRenderBreakRadio.Text = "Current Render Break";
            this.CurrentRenderBreakRadio.UseVisualStyleBackColor = true;
            this.CurrentRenderBreakRadio.CheckedChanged += new System.EventHandler(this.CurrentRenderBreakRadio_CheckedChanged);
            // 
            // ManagedObjectsTab
            // 
            this.ManagedObjectsTab.Controls.Add(this.elementHost1);
            this.ManagedObjectsTab.Location = new System.Drawing.Point(4, 22);
            this.ManagedObjectsTab.Name = "ManagedObjectsTab";
            this.ManagedObjectsTab.Padding = new System.Windows.Forms.Padding(3);
            this.ManagedObjectsTab.Size = new System.Drawing.Size(427, 402);
            this.ManagedObjectsTab.TabIndex = 2;
            this.ManagedObjectsTab.Text = "Managed Objects";
            this.ManagedObjectsTab.UseVisualStyleBackColor = true;
            // 
            // elementHost1
            // 
            this.elementHost1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.elementHost1.Location = new System.Drawing.Point(3, 3);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Size = new System.Drawing.Size(421, 396);
            this.elementHost1.TabIndex = 0;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = this.managedObjectsControl1;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.elementHost2);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(427, 402);
            this.tabPage1.TabIndex = 3;
            this.tabPage1.Text = "Content";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // elementHost2
            // 
            this.elementHost2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.elementHost2.Location = new System.Drawing.Point(3, 3);
            this.elementHost2.Name = "elementHost2";
            this.elementHost2.Size = new System.Drawing.Size(421, 396);
            this.elementHost2.TabIndex = 0;
            this.elementHost2.Text = "elementHost2";
            this.elementHost2.Child = this.contentManagerControl1;
            // 
            // MainControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.menuStrip1);
            this.Name = "MainControl";
            this.Size = new System.Drawing.Size(435, 452);
            this.Load += new System.EventHandler(this.MainControl_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.SectionsTab.ResumeLayout(false);
            this.RenderBreakTab.ResumeLayout(false);
            this.CurrentRenderBreakControl.Panel1.ResumeLayout(false);
            this.CurrentRenderBreakControl.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.CurrentRenderBreakControl)).EndInit();
            this.CurrentRenderBreakControl.ResumeLayout(false);
            this.tabControl2.ResumeLayout(false);
            this.TextureTab.ResumeLayout(false);
            this.TextureTab.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.PropertiesTab.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ManagedObjectsTab.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadSectionTextToolStripMenuItem;
        private System.Windows.Forms.TreeView SectionTreeView;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem organizeTimingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hierarchyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem totalTimeToolStripMenuItem;
        private System.Windows.Forms.RichTextBox DetailsTextBox;
        private System.Windows.Forms.ToolStripMenuItem loadFromClipboardToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadRenderBreaksToolStripMenuItem;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage SectionsTab;
        private System.Windows.Forms.TabPage RenderBreakTab;
        private System.Windows.Forms.TreeView RenderBreakTreeView;
        private System.Windows.Forms.SplitContainer CurrentRenderBreakControl;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TabControl tabControl2;
        private System.Windows.Forms.TabPage TextureTab;
        private System.Windows.Forms.TabPage PropertiesTab;
        private System.Windows.Forms.PropertyGrid propertyGrid1;
        private System.Windows.Forms.ToolStripMenuItem runGameToolStripMenuItem;
        private System.Windows.Forms.Button PullFromScreenButton;
        private System.Windows.Forms.Button FromEngineButton;
        private System.Windows.Forms.TabPage ManagedObjectsTab;
        private System.Windows.Forms.Integration.ElementHost elementHost1;
        private Controls.ManagedObjectsControl managedObjectsControl1;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Integration.ElementHost elementHost2;
        private Controls.ContentManagerControl contentManagerControl1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton HistoryRadioButton;
        private System.Windows.Forms.RadioButton CurrentRenderBreakRadio;
        private System.Windows.Forms.Integration.ElementHost RenderBreakHistoryControlHost;
        private Controls.RenderBreakHistoryControl renderBreakHistoryControl1;
    }
}
