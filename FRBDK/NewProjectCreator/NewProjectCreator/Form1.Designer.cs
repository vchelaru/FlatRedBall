namespace NewProjectCreator
{
    partial class Form1
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
            this.label1 = new System.Windows.Forms.Label();
            this.ProjectNameTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.ProjectLocationTextBox = new System.Windows.Forms.TextBox();
            this.SelectLocationButton = new System.Windows.Forms.Button();
            this.MakeMyProject = new System.Windows.Forms.Button();
            this.DifferentNamespaceCheckbox = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.FinalDirectoryLabel = new System.Windows.Forms.Label();
            this.CreateProjectDirectoryCheckBox = new System.Windows.Forms.CheckBox();
            this.DifferentNamespaceTextbox = new System.Windows.Forms.TextBox();
            this.CheckForNewVersionCheckBox = new System.Windows.Forms.CheckBox();
            this.InfoBar = new System.Windows.Forms.StatusStrip();
            this.InfoBarLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewTemplateZipFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.WpfHost = new System.Windows.Forms.Integration.ElementHost();
            this.mainControl1 = new NewProjectCreator.Views.MainControl();
            this.InfoBar.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 5);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(74, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Project Name:";
            // 
            // ProjectNameTextBox
            // 
            this.ProjectNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ProjectNameTextBox.Location = new System.Drawing.Point(81, 2);
            this.ProjectNameTextBox.Name = "ProjectNameTextBox";
            this.ProjectNameTextBox.Size = new System.Drawing.Size(196, 20);
            this.ProjectNameTextBox.TabIndex = 2;
            this.ProjectNameTextBox.Text = "NewFlatRedBallProject";
            this.ProjectNameTextBox.TextChanged += new System.EventHandler(this.ProjectNameTextBox_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(24, 80);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(51, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Location:";
            // 
            // ProjectLocationTextBox
            // 
            this.ProjectLocationTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ProjectLocationTextBox.Location = new System.Drawing.Point(81, 77);
            this.ProjectLocationTextBox.Name = "ProjectLocationTextBox";
            this.ProjectLocationTextBox.Size = new System.Drawing.Size(166, 20);
            this.ProjectLocationTextBox.TabIndex = 4;
            this.ProjectLocationTextBox.Text = "C:\\FlatRedBallProjects";
            this.ProjectLocationTextBox.TextChanged += new System.EventHandler(this.ProjectLocationTextBox_TextChanged);
            // 
            // SelectLocationButton
            // 
            this.SelectLocationButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SelectLocationButton.Location = new System.Drawing.Point(253, 76);
            this.SelectLocationButton.Name = "SelectLocationButton";
            this.SelectLocationButton.Size = new System.Drawing.Size(24, 21);
            this.SelectLocationButton.TabIndex = 5;
            this.SelectLocationButton.Text = "...";
            this.SelectLocationButton.UseVisualStyleBackColor = true;
            this.SelectLocationButton.Click += new System.EventHandler(this.HandleSelectLocationClick);
            // 
            // MakeMyProject
            // 
            this.MakeMyProject.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MakeMyProject.Location = new System.Drawing.Point(3, 175);
            this.MakeMyProject.Name = "MakeMyProject";
            this.MakeMyProject.Size = new System.Drawing.Size(274, 30);
            this.MakeMyProject.TabIndex = 6;
            this.MakeMyProject.Text = "Make my project!";
            this.MakeMyProject.UseVisualStyleBackColor = true;
            this.MakeMyProject.Click += new System.EventHandler(this.MakeMyProjectClick);
            // 
            // DifferentNamespaceCheckbox
            // 
            this.DifferentNamespaceCheckbox.AutoSize = true;
            this.DifferentNamespaceCheckbox.Location = new System.Drawing.Point(43, 28);
            this.DifferentNamespaceCheckbox.Name = "DifferentNamespaceCheckbox";
            this.DifferentNamespaceCheckbox.Size = new System.Drawing.Size(148, 17);
            this.DifferentNamespaceCheckbox.TabIndex = 7;
            this.DifferentNamespaceCheckbox.Text = "Use Different Namespace";
            this.DifferentNamespaceCheckbox.UseVisualStyleBackColor = true;
            this.DifferentNamespaceCheckbox.CheckedChanged += new System.EventHandler(this.UseDifferentNamespaceCheckBoxChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 130);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(154, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Solution (.sln) will be located in:";
            // 
            // FinalDirectoryLabel
            // 
            this.FinalDirectoryLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.FinalDirectoryLabel.Location = new System.Drawing.Point(40, 143);
            this.FinalDirectoryLabel.Name = "FinalDirectoryLabel";
            this.FinalDirectoryLabel.Size = new System.Drawing.Size(237, 51);
            this.FinalDirectoryLabel.TabIndex = 9;
            this.FinalDirectoryLabel.Text = "C:\\FlatRedBallProjects\\NewFlatRedBallProject";
            // 
            // CreateProjectDirectoryCheckBox
            // 
            this.CreateProjectDirectoryCheckBox.AutoSize = true;
            this.CreateProjectDirectoryCheckBox.Checked = true;
            this.CreateProjectDirectoryCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CreateProjectDirectoryCheckBox.Location = new System.Drawing.Point(43, 97);
            this.CreateProjectDirectoryCheckBox.Name = "CreateProjectDirectoryCheckBox";
            this.CreateProjectDirectoryCheckBox.Size = new System.Drawing.Size(138, 17);
            this.CreateProjectDirectoryCheckBox.TabIndex = 10;
            this.CreateProjectDirectoryCheckBox.Text = "Create Project Directory";
            this.CreateProjectDirectoryCheckBox.UseVisualStyleBackColor = true;
            this.CreateProjectDirectoryCheckBox.CheckedChanged += new System.EventHandler(this.CreateProjectDirectoryCheckBoxChanged);
            // 
            // DifferentNamespaceTextbox
            // 
            this.DifferentNamespaceTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DifferentNamespaceTextbox.Location = new System.Drawing.Point(81, 45);
            this.DifferentNamespaceTextbox.Name = "DifferentNamespaceTextbox";
            this.DifferentNamespaceTextbox.Size = new System.Drawing.Size(196, 20);
            this.DifferentNamespaceTextbox.TabIndex = 11;
            this.DifferentNamespaceTextbox.Text = "NewFlatRedBallProject";
            // 
            // CheckForNewVersionCheckBox
            // 
            this.CheckForNewVersionCheckBox.AutoSize = true;
            this.CheckForNewVersionCheckBox.Checked = true;
            this.CheckForNewVersionCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CheckForNewVersionCheckBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.CheckForNewVersionCheckBox.Location = new System.Drawing.Point(0, 0);
            this.CheckForNewVersionCheckBox.Name = "CheckForNewVersionCheckBox";
            this.CheckForNewVersionCheckBox.Size = new System.Drawing.Size(444, 17);
            this.CheckForNewVersionCheckBox.TabIndex = 12;
            this.CheckForNewVersionCheckBox.Text = "Check for new version";
            this.CheckForNewVersionCheckBox.UseVisualStyleBackColor = true;
            // 
            // InfoBar
            // 
            this.InfoBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.InfoBarLabel});
            this.InfoBar.Location = new System.Drawing.Point(0, 236);
            this.InfoBar.Name = "InfoBar";
            this.InfoBar.Size = new System.Drawing.Size(736, 22);
            this.InfoBar.TabIndex = 14;
            this.InfoBar.Text = "statusStrip1";
            // 
            // InfoBarLabel
            // 
            this.InfoBarLabel.Name = "InfoBarLabel";
            this.InfoBarLabel.Size = new System.Drawing.Size(113, 17);
            this.InfoBarLabel.Text = "Select a project type";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(736, 24);
            this.menuStrip1.TabIndex = 16;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.viewTemplateZipFolderToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // viewTemplateZipFolderToolStripMenuItem
            // 
            this.viewTemplateZipFolderToolStripMenuItem.Name = "viewTemplateZipFolderToolStripMenuItem";
            this.viewTemplateZipFolderToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.viewTemplateZipFolderToolStripMenuItem.Text = "View Template Zip Folder";
            this.viewTemplateZipFolderToolStripMenuItem.Click += new System.EventHandler(this.viewTemplateZipFolderToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 24);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.WpfHost);
            this.splitContainer1.Panel1.Controls.Add(this.CheckForNewVersionCheckBox);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.MakeMyProject);
            this.splitContainer1.Panel2.Controls.Add(this.label1);
            this.splitContainer1.Panel2.Controls.Add(this.ProjectNameTextBox);
            this.splitContainer1.Panel2.Controls.Add(this.label2);
            this.splitContainer1.Panel2.Controls.Add(this.DifferentNamespaceTextbox);
            this.splitContainer1.Panel2.Controls.Add(this.ProjectLocationTextBox);
            this.splitContainer1.Panel2.Controls.Add(this.CreateProjectDirectoryCheckBox);
            this.splitContainer1.Panel2.Controls.Add(this.SelectLocationButton);
            this.splitContainer1.Panel2.Controls.Add(this.FinalDirectoryLabel);
            this.splitContainer1.Panel2.Controls.Add(this.DifferentNamespaceCheckbox);
            this.splitContainer1.Panel2.Controls.Add(this.label3);
            this.splitContainer1.Size = new System.Drawing.Size(736, 212);
            this.splitContainer1.SplitterDistance = 448;
            this.splitContainer1.TabIndex = 17;
            // 
            // WpfHost
            // 
            this.WpfHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.WpfHost.Location = new System.Drawing.Point(0, 17);
            this.WpfHost.Name = "WpfHost";
            this.WpfHost.Size = new System.Drawing.Size(444, 191);
            this.WpfHost.TabIndex = 13;
            this.WpfHost.Text = "elementHost1";
            this.WpfHost.Child = this.mainControl1;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(736, 258);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.InfoBar);
            this.Controls.Add(this.menuStrip1);
            this.Name = "Form1";
            this.ShowIcon = false;
            this.Text = "FlatRedBall Project Creator";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.InfoBar.ResumeLayout(false);
            this.InfoBar.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button SelectLocationButton;
        private System.Windows.Forms.Button MakeMyProject;
        private System.Windows.Forms.CheckBox DifferentNamespaceCheckbox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label FinalDirectoryLabel;
        public System.Windows.Forms.TextBox ProjectLocationTextBox;
        public System.Windows.Forms.CheckBox CreateProjectDirectoryCheckBox;
        public System.Windows.Forms.TextBox ProjectNameTextBox;
        public System.Windows.Forms.TextBox DifferentNamespaceTextbox;
        private System.Windows.Forms.CheckBox CheckForNewVersionCheckBox;
        public System.Windows.Forms.StatusStrip InfoBar;
        public System.Windows.Forms.ToolStripStatusLabel InfoBarLabel;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewTemplateZipFolderToolStripMenuItem;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.Integration.ElementHost WpfHost;
        private Views.MainControl mainControl1;
    }
}

