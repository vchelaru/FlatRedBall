namespace OfficialPlugins.ProjectCopier
{
    partial class CopyToProjectControl
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
            this.CopyToTextBox = new System.Windows.Forms.TextBox();
            this.BrowseButton = new System.Windows.Forms.Button();
            this.CopyItButton = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.PercentageCompleteLabel = new System.Windows.Forms.Label();
            this.CurrentlyCopyingLabel = new System.Windows.Forms.Label();
            this.CopyFromGroupBox = new System.Windows.Forms.GroupBox();
            this.CopyFromBrowseButton = new System.Windows.Forms.Button();
            this.CopyFromTextBox = new System.Windows.Forms.TextBox();
            this.CopyFromCustomFolderRadioButton = new System.Windows.Forms.RadioButton();
            this.CopyFromSolutionRootRadioButton = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.CopyBackButton = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.SourceVsDestinationDatesRadio = new System.Windows.Forms.RadioButton();
            this.AllSinceLastCopyRadio = new System.Windows.Forms.RadioButton();
            this.CopyFromGroupBox.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // CopyToTextBox
            // 
            this.CopyToTextBox.Location = new System.Drawing.Point(6, 19);
            this.CopyToTextBox.Name = "CopyToTextBox";
            this.CopyToTextBox.Size = new System.Drawing.Size(211, 20);
            this.CopyToTextBox.TabIndex = 0;
            // 
            // BrowseButton
            // 
            this.BrowseButton.Location = new System.Drawing.Point(223, 16);
            this.BrowseButton.Name = "BrowseButton";
            this.BrowseButton.Size = new System.Drawing.Size(27, 23);
            this.BrowseButton.TabIndex = 2;
            this.BrowseButton.Text = "...";
            this.BrowseButton.UseVisualStyleBackColor = true;
            this.BrowseButton.Click += new System.EventHandler(this.CopyToBrowseButton_Click);
            // 
            // CopyItButton
            // 
            this.CopyItButton.Location = new System.Drawing.Point(274, 5);
            this.CopyItButton.Name = "CopyItButton";
            this.CopyItButton.Size = new System.Drawing.Size(99, 91);
            this.CopyItButton.TabIndex = 3;
            this.CopyItButton.Text = "Copy Projects!";
            this.CopyItButton.UseVisualStyleBackColor = true;
            this.CopyItButton.Click += new System.EventHandler(this.CopyItButton_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(6, 193);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(262, 23);
            this.progressBar1.TabIndex = 5;
            // 
            // PercentageCompleteLabel
            // 
            this.PercentageCompleteLabel.AutoSize = true;
            this.PercentageCompleteLabel.BackColor = System.Drawing.Color.Transparent;
            this.PercentageCompleteLabel.Location = new System.Drawing.Point(274, 199);
            this.PercentageCompleteLabel.Name = "PercentageCompleteLabel";
            this.PercentageCompleteLabel.Size = new System.Drawing.Size(106, 13);
            this.PercentageCompleteLabel.TabIndex = 4;
            this.PercentageCompleteLabel.Text = "PercentageComplete";
            // 
            // CurrentlyCopyingLabel
            // 
            this.CurrentlyCopyingLabel.AutoSize = true;
            this.CurrentlyCopyingLabel.Location = new System.Drawing.Point(3, 219);
            this.CurrentlyCopyingLabel.Name = "CurrentlyCopyingLabel";
            this.CurrentlyCopyingLabel.Size = new System.Drawing.Size(112, 13);
            this.CurrentlyCopyingLabel.TabIndex = 6;
            this.CurrentlyCopyingLabel.Text = "CurrentlyCopyingLabel";
            // 
            // CopyFromGroupBox
            // 
            this.CopyFromGroupBox.Controls.Add(this.CopyFromBrowseButton);
            this.CopyFromGroupBox.Controls.Add(this.CopyFromTextBox);
            this.CopyFromGroupBox.Controls.Add(this.CopyFromCustomFolderRadioButton);
            this.CopyFromGroupBox.Controls.Add(this.CopyFromSolutionRootRadioButton);
            this.CopyFromGroupBox.Location = new System.Drawing.Point(6, 5);
            this.CopyFromGroupBox.Name = "CopyFromGroupBox";
            this.CopyFromGroupBox.Size = new System.Drawing.Size(262, 72);
            this.CopyFromGroupBox.TabIndex = 7;
            this.CopyFromGroupBox.TabStop = false;
            this.CopyFromGroupBox.Text = "Copy From";
            // 
            // CopyFromBrowseButton
            // 
            this.CopyFromBrowseButton.Location = new System.Drawing.Point(223, 42);
            this.CopyFromBrowseButton.Name = "CopyFromBrowseButton";
            this.CopyFromBrowseButton.Size = new System.Drawing.Size(27, 23);
            this.CopyFromBrowseButton.TabIndex = 8;
            this.CopyFromBrowseButton.Text = "...";
            this.CopyFromBrowseButton.UseVisualStyleBackColor = true;
            this.CopyFromBrowseButton.Click += new System.EventHandler(this.CopyFromBrowserButtonClick);
            // 
            // CopyFromTextBox
            // 
            this.CopyFromTextBox.Location = new System.Drawing.Point(6, 44);
            this.CopyFromTextBox.Name = "CopyFromTextBox";
            this.CopyFromTextBox.Size = new System.Drawing.Size(211, 20);
            this.CopyFromTextBox.TabIndex = 2;
            // 
            // CopyFromCustomFolderRadioButton
            // 
            this.CopyFromCustomFolderRadioButton.AutoSize = true;
            this.CopyFromCustomFolderRadioButton.Location = new System.Drawing.Point(101, 21);
            this.CopyFromCustomFolderRadioButton.Name = "CopyFromCustomFolderRadioButton";
            this.CopyFromCustomFolderRadioButton.Size = new System.Drawing.Size(92, 17);
            this.CopyFromCustomFolderRadioButton.TabIndex = 1;
            this.CopyFromCustomFolderRadioButton.Text = "Custom Folder";
            this.CopyFromCustomFolderRadioButton.UseVisualStyleBackColor = true;
            this.CopyFromCustomFolderRadioButton.CheckedChanged += new System.EventHandler(this.CopyFromCustomFolderRadioChecked);
            // 
            // CopyFromSolutionRootRadioButton
            // 
            this.CopyFromSolutionRootRadioButton.AutoSize = true;
            this.CopyFromSolutionRootRadioButton.Checked = true;
            this.CopyFromSolutionRootRadioButton.Location = new System.Drawing.Point(6, 21);
            this.CopyFromSolutionRootRadioButton.Name = "CopyFromSolutionRootRadioButton";
            this.CopyFromSolutionRootRadioButton.Size = new System.Drawing.Size(89, 17);
            this.CopyFromSolutionRootRadioButton.TabIndex = 0;
            this.CopyFromSolutionRootRadioButton.TabStop = true;
            this.CopyFromSolutionRootRadioButton.Text = "Solution Root";
            this.CopyFromSolutionRootRadioButton.UseVisualStyleBackColor = true;
            this.CopyFromSolutionRootRadioButton.CheckedChanged += new System.EventHandler(this.CopyFromSolutionRootRadioChecked);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.CopyToTextBox);
            this.groupBox1.Controls.Add(this.BrowseButton);
            this.groupBox1.Location = new System.Drawing.Point(6, 83);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(262, 48);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Copy To";
            // 
            // CopyBackButton
            // 
            this.CopyBackButton.BackColor = System.Drawing.Color.Salmon;
            this.CopyBackButton.Location = new System.Drawing.Point(274, 102);
            this.CopyBackButton.Name = "CopyBackButton";
            this.CopyBackButton.Size = new System.Drawing.Size(99, 29);
            this.CopyBackButton.TabIndex = 9;
            this.CopyBackButton.Text = "Copy Back";
            this.CopyBackButton.UseVisualStyleBackColor = false;
            this.CopyBackButton.Click += new System.EventHandler(this.CopyBackButton_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.AllSinceLastCopyRadio);
            this.groupBox2.Controls.Add(this.SourceVsDestinationDatesRadio);
            this.groupBox2.Location = new System.Drawing.Point(6, 137);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(367, 48);
            this.groupBox2.TabIndex = 9;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Copy Settings";
            // 
            // SourceVsDestinationDatesRadio
            // 
            this.SourceVsDestinationDatesRadio.AutoSize = true;
            this.SourceVsDestinationDatesRadio.Checked = true;
            this.SourceVsDestinationDatesRadio.Location = new System.Drawing.Point(10, 19);
            this.SourceVsDestinationDatesRadio.Name = "SourceVsDestinationDatesRadio";
            this.SourceVsDestinationDatesRadio.Size = new System.Drawing.Size(163, 17);
            this.SourceVsDestinationDatesRadio.TabIndex = 0;
            this.SourceVsDestinationDatesRadio.TabStop = true;
            this.SourceVsDestinationDatesRadio.Text = "Source vs. Destination Dates";
            this.SourceVsDestinationDatesRadio.UseVisualStyleBackColor = true;
            this.SourceVsDestinationDatesRadio.CheckedChanged += new System.EventHandler(this.radioButton1_CheckedChanged);
            // 
            // AllSinceLastCopyRadio
            // 
            this.AllSinceLastCopyRadio.AutoSize = true;
            this.AllSinceLastCopyRadio.Location = new System.Drawing.Point(179, 19);
            this.AllSinceLastCopyRadio.Name = "AllSinceLastCopyRadio";
            this.AllSinceLastCopyRadio.Size = new System.Drawing.Size(116, 17);
            this.AllSinceLastCopyRadio.TabIndex = 1;
            this.AllSinceLastCopyRadio.Text = "All Since Last Copy";
            this.AllSinceLastCopyRadio.UseVisualStyleBackColor = true;
            this.AllSinceLastCopyRadio.CheckedChanged += new System.EventHandler(this.AllSinceLastCopyRadio_CheckedChanged);
            // 
            // CopyToProjectControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.CopyBackButton);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.CopyFromGroupBox);
            this.Controls.Add(this.CurrentlyCopyingLabel);
            this.Controls.Add(this.PercentageCompleteLabel);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.CopyItButton);
            this.Name = "CopyToProjectControl";
            this.Size = new System.Drawing.Size(386, 236);
            this.CopyFromGroupBox.ResumeLayout(false);
            this.CopyFromGroupBox.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox CopyToTextBox;
        private System.Windows.Forms.Button BrowseButton;
        private System.Windows.Forms.Button CopyItButton;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label PercentageCompleteLabel;
        private System.Windows.Forms.Label CurrentlyCopyingLabel;
        private System.Windows.Forms.GroupBox CopyFromGroupBox;
        private System.Windows.Forms.Button CopyFromBrowseButton;
        private System.Windows.Forms.TextBox CopyFromTextBox;
        private System.Windows.Forms.RadioButton CopyFromCustomFolderRadioButton;
        private System.Windows.Forms.RadioButton CopyFromSolutionRootRadioButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button CopyBackButton;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton AllSinceLastCopyRadio;
        private System.Windows.Forms.RadioButton SourceVsDestinationDatesRadio;
    }
}
