namespace FilteringHaloRemover
{
    partial class FormMain
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.pictureBoxPreview = new System.Windows.Forms.PictureBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.buttonOpenFile = new System.Windows.Forms.Button();
            this.buttonExtract = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.buttonOpenFolder = new System.Windows.Forms.Button();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.checkBoxReplace = new System.Windows.Forms.CheckBox();
            this.FileListBox = new System.Windows.Forms.ListBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBoxPreview
            // 
            this.pictureBoxPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBoxPreview.Location = new System.Drawing.Point(0, 0);
            this.pictureBoxPreview.Name = "pictureBoxPreview";
            this.pictureBoxPreview.Size = new System.Drawing.Size(0, 0);
            this.pictureBoxPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBoxPreview.TabIndex = 0;
            this.pictureBoxPreview.TabStop = false;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.DefaultExt = "png";
            // 
            // buttonOpenFile
            // 
            this.buttonOpenFile.FlatAppearance.BorderSize = 0;
            this.buttonOpenFile.Image = ((System.Drawing.Image)(resources.GetObject("buttonOpenFile.Image")));
            this.buttonOpenFile.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.buttonOpenFile.Location = new System.Drawing.Point(10, 12);
            this.buttonOpenFile.Name = "buttonOpenFile";
            this.buttonOpenFile.Padding = new System.Windows.Forms.Padding(1);
            this.buttonOpenFile.Size = new System.Drawing.Size(103, 34);
            this.buttonOpenFile.TabIndex = 6;
            this.buttonOpenFile.Text = "Open Image";
            this.buttonOpenFile.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonOpenFile.UseVisualStyleBackColor = true;
            this.buttonOpenFile.Click += new System.EventHandler(this.buttonOpenFile_Click);
            // 
            // buttonExtract
            // 
            this.buttonExtract.FlatAppearance.BorderSize = 0;
            this.buttonExtract.Image = ((System.Drawing.Image)(resources.GetObject("buttonExtract.Image")));
            this.buttonExtract.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.buttonExtract.Location = new System.Drawing.Point(10, 115);
            this.buttonExtract.Name = "buttonExtract";
            this.buttonExtract.Padding = new System.Windows.Forms.Padding(1);
            this.buttonExtract.Size = new System.Drawing.Size(103, 34);
            this.buttonExtract.TabIndex = 7;
            this.buttonExtract.Text = "Process";
            this.buttonExtract.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonExtract.UseVisualStyleBackColor = true;
            this.buttonExtract.Click += new System.EventHandler(this.button1_Click);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.AutoScroll = true;
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.pictureBoxPreview);
            this.panel1.Location = new System.Drawing.Point(424, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(295, 267);
            this.panel1.TabIndex = 8;
            // 
            // buttonOpenFolder
            // 
            this.buttonOpenFolder.FlatAppearance.BorderSize = 0;
            this.buttonOpenFolder.Image = ((System.Drawing.Image)(resources.GetObject("buttonOpenFolder.Image")));
            this.buttonOpenFolder.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.buttonOpenFolder.Location = new System.Drawing.Point(10, 52);
            this.buttonOpenFolder.Name = "buttonOpenFolder";
            this.buttonOpenFolder.Padding = new System.Windows.Forms.Padding(1);
            this.buttonOpenFolder.Size = new System.Drawing.Size(103, 34);
            this.buttonOpenFolder.TabIndex = 9;
            this.buttonOpenFolder.Text = "Open Folder";
            this.buttonOpenFolder.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonOpenFolder.UseVisualStyleBackColor = true;
            this.buttonOpenFolder.Click += new System.EventHandler(this.buttonOpenFolder_Click);
            // 
            // folderBrowserDialog1
            // 
            this.folderBrowserDialog1.ShowNewFolderButton = false;
            // 
            // checkBoxReplace
            // 
            this.checkBoxReplace.AutoSize = true;
            this.checkBoxReplace.Location = new System.Drawing.Point(12, 92);
            this.checkBoxReplace.Name = "checkBoxReplace";
            this.checkBoxReplace.Size = new System.Drawing.Size(105, 17);
            this.checkBoxReplace.TabIndex = 10;
            this.checkBoxReplace.Text = "Replace Existing";
            this.checkBoxReplace.UseVisualStyleBackColor = true;
            // 
            // FileListBox
            // 
            this.FileListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.FileListBox.FormattingEnabled = true;
            this.FileListBox.Location = new System.Drawing.Point(123, 12);
            this.FileListBox.Name = "FileListBox";
            this.FileListBox.Size = new System.Drawing.Size(295, 264);
            this.FileListBox.TabIndex = 11;
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(731, 291);
            this.Controls.Add(this.FileListBox);
            this.Controls.Add(this.checkBoxReplace);
            this.Controls.Add(this.buttonOpenFolder);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.buttonExtract);
            this.Controls.Add(this.buttonOpenFile);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormMain";
            this.Text = "Filtering Halo Remover";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBoxPreview;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button buttonOpenFile;
        private System.Windows.Forms.Button buttonExtract;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button buttonOpenFolder;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.CheckBox checkBoxReplace;
        private System.Windows.Forms.ListBox FileListBox;
    }
}

