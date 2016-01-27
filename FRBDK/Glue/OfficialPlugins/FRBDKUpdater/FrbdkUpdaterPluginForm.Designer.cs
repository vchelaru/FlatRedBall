namespace OfficialPlugins.FrbdkUpdater
{
    partial class FrbdkUpdaterPluginForm
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
            this.tbPath = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.fbdFRBDK = new System.Windows.Forms.FolderBrowserDialog();
            this.btnSelectDirectory = new System.Windows.Forms.Button();
            this.btnUpdater = new System.Windows.Forms.Button();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnReset = new System.Windows.Forms.Button();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.cbCleanFolder = new System.Windows.Forms.CheckBox();
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel4 = new System.Windows.Forms.FlowLayoutPanel();
            this.cbForceDownload = new System.Windows.Forms.CheckBox();
            this.ExtractUpdaterCheckBox = new System.Windows.Forms.CheckBox();
            this.ttMain = new System.Windows.Forms.ToolTip(this.components);
            this.flowLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel3.SuspendLayout();
            this.flowLayoutPanel4.SuspendLayout();
            this.SuspendLayout();
            // 
            // tbPath
            // 
            this.tbPath.Location = new System.Drawing.Point(97, 3);
            this.tbPath.Name = "tbPath";
            this.tbPath.Size = new System.Drawing.Size(230, 20);
            this.tbPath.TabIndex = 0;
            this.ttMain.SetToolTip(this.tbPath, "Path to FRBDK directory.");
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 5);
            this.label1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(88, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "FRBDK Directory";
            // 
            // fbdFRBDK
            // 
            this.fbdFRBDK.RootFolder = System.Environment.SpecialFolder.MyComputer;
            // 
            // btnSelectDirectory
            // 
            this.btnSelectDirectory.Location = new System.Drawing.Point(333, 3);
            this.btnSelectDirectory.Name = "btnSelectDirectory";
            this.btnSelectDirectory.Size = new System.Drawing.Size(24, 20);
            this.btnSelectDirectory.TabIndex = 2;
            this.btnSelectDirectory.Text = "...";
            this.ttMain.SetToolTip(this.btnSelectDirectory, "Open dialog to select directory.");
            this.btnSelectDirectory.UseVisualStyleBackColor = true;
            this.btnSelectDirectory.Click += new System.EventHandler(this.BtnSelectDirectoryClick);
            // 
            // btnUpdater
            // 
            this.btnUpdater.Location = new System.Drawing.Point(363, 3);
            this.btnUpdater.Name = "btnUpdater";
            this.btnUpdater.Size = new System.Drawing.Size(75, 23);
            this.btnUpdater.TabIndex = 4;
            this.btnUpdater.Text = "&Update";
            this.ttMain.SetToolTip(this.btnUpdater, "Click here to start the update process.");
            this.btnUpdater.UseVisualStyleBackColor = true;
            this.btnUpdater.Click += new System.EventHandler(this.BtnSyncClick);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.Controls.Add(this.label1);
            this.flowLayoutPanel1.Controls.Add(this.tbPath);
            this.flowLayoutPanel1.Controls.Add(this.btnSelectDirectory);
            this.flowLayoutPanel1.Controls.Add(this.btnReset);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(441, 26);
            this.flowLayoutPanel1.TabIndex = 7;
            // 
            // btnReset
            // 
            this.btnReset.Location = new System.Drawing.Point(363, 3);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(75, 20);
            this.btnReset.TabIndex = 3;
            this.btnReset.Text = "&Reset";
            this.ttMain.SetToolTip(this.btnReset, "Reset directory to the default value.");
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.AutoSize = true;
            this.flowLayoutPanel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(450, 3);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(0, 0);
            this.flowLayoutPanel2.TabIndex = 8;
            // 
            // cbCleanFolder
            // 
            this.cbCleanFolder.AutoSize = true;
            this.cbCleanFolder.Location = new System.Drawing.Point(162, 6);
            this.cbCleanFolder.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.cbCleanFolder.Name = "cbCleanFolder";
            this.cbCleanFolder.Size = new System.Drawing.Size(85, 17);
            this.cbCleanFolder.TabIndex = 5;
            this.cbCleanFolder.Text = "Clean Folder";
            this.ttMain.SetToolTip(this.cbCleanFolder, "When updating, all files and folders will be deleted in the FRBDK folder.");
            this.cbCleanFolder.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel3
            // 
            this.flowLayoutPanel3.AutoSize = true;
            this.flowLayoutPanel3.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel3.Controls.Add(this.flowLayoutPanel1);
            this.flowLayoutPanel3.Controls.Add(this.flowLayoutPanel4);
            this.flowLayoutPanel3.Controls.Add(this.flowLayoutPanel2);
            this.flowLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel3.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel3.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.flowLayoutPanel3.Size = new System.Drawing.Size(450, 68);
            this.flowLayoutPanel3.TabIndex = 9;
            // 
            // flowLayoutPanel4
            // 
            this.flowLayoutPanel4.Controls.Add(this.btnUpdater);
            this.flowLayoutPanel4.Controls.Add(this.cbForceDownload);
            this.flowLayoutPanel4.Controls.Add(this.cbCleanFolder);
            this.flowLayoutPanel4.Controls.Add(this.ExtractUpdaterCheckBox);
            this.flowLayoutPanel4.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel4.Location = new System.Drawing.Point(3, 35);
            this.flowLayoutPanel4.Name = "flowLayoutPanel4";
            this.flowLayoutPanel4.Size = new System.Drawing.Size(441, 28);
            this.flowLayoutPanel4.TabIndex = 9;
            // 
            // cbForceDownload
            // 
            this.cbForceDownload.AutoSize = true;
            this.cbForceDownload.Checked = true;
            this.cbForceDownload.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbForceDownload.Location = new System.Drawing.Point(253, 6);
            this.cbForceDownload.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.cbForceDownload.Name = "cbForceDownload";
            this.cbForceDownload.Size = new System.Drawing.Size(104, 17);
            this.cbForceDownload.TabIndex = 6;
            this.cbForceDownload.Text = "Force Download";
            this.ttMain.SetToolTip(this.cbForceDownload, "When updating, all files and folders will be deleted in the FRBDK folder.");
            this.cbForceDownload.UseVisualStyleBackColor = true;
            // 
            // ExtractUpdaterCheckBox
            // 
            this.ExtractUpdaterCheckBox.AutoSize = true;
            this.ExtractUpdaterCheckBox.Checked = true;
            this.ExtractUpdaterCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ExtractUpdaterCheckBox.Location = new System.Drawing.Point(8, 6);
            this.ExtractUpdaterCheckBox.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.ExtractUpdaterCheckBox.Name = "ExtractUpdaterCheckBox";
            this.ExtractUpdaterCheckBox.Size = new System.Drawing.Size(148, 17);
            this.ExtractUpdaterCheckBox.TabIndex = 7;
            this.ExtractUpdaterCheckBox.Text = "Extract Updater from Glue";
            this.ttMain.SetToolTip(this.ExtractUpdaterCheckBox, "When updating, all files and folders will be deleted in the FRBDK folder.");
            this.ExtractUpdaterCheckBox.UseVisualStyleBackColor = true;
            // 
            // FrbdkUpdaterPluginForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(450, 68);
            this.Controls.Add(this.flowLayoutPanel3);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrbdkUpdaterPluginForm";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FRBDK Updater";
            this.Load += new System.EventHandler(this.SyncFormLoad);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.flowLayoutPanel3.ResumeLayout(false);
            this.flowLayoutPanel3.PerformLayout();
            this.flowLayoutPanel4.ResumeLayout(false);
            this.flowLayoutPanel4.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbPath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.FolderBrowserDialog fbdFRBDK;
        private System.Windows.Forms.Button btnSelectDirectory;
        private System.Windows.Forms.Button btnUpdater;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
        private System.Windows.Forms.CheckBox cbCleanFolder;
        private System.Windows.Forms.ToolTip ttMain;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel4;
        private System.Windows.Forms.CheckBox cbForceDownload;
        private System.Windows.Forms.CheckBox ExtractUpdaterCheckBox;
    }
}