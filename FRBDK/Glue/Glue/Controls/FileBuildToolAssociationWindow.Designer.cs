namespace FlatRedBall.Glue.Controls
{
    partial class FileBuildToolAssociationWindow
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
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.OkButton = new System.Windows.Forms.Button();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.CreateNewButton = new System.Windows.Forms.Button();
            this.ExternalFileRootTextBox = new System.Windows.Forms.TextBox();
            this.ExternalFileLocation = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // propertyGrid1
            // 
            this.propertyGrid1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.propertyGrid1.HelpVisible = false;
            this.propertyGrid1.Location = new System.Drawing.Point(212, 4);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.Size = new System.Drawing.Size(372, 352);
            this.propertyGrid1.TabIndex = 0;
            this.propertyGrid1.ToolbarVisible = false;
            this.propertyGrid1.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propertyGrid1_PropertyValueChanged);
            // 
            // OkButton
            // 
            this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.OkButton.Location = new System.Drawing.Point(457, 413);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(131, 32);
            this.OkButton.TabIndex = 2;
            this.OkButton.Text = "OK";
            this.OkButton.UseVisualStyleBackColor = true;
            this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // listBox1
            // 
            this.listBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(0, 4);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(206, 394);
            this.listBox1.TabIndex = 3;
            this.listBox1.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
            this.listBox1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.listBox1_KeyDown);
            // 
            // CreateNewButton
            // 
            this.CreateNewButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.CreateNewButton.Location = new System.Drawing.Point(0, 413);
            this.CreateNewButton.Name = "CreateNewButton";
            this.CreateNewButton.Size = new System.Drawing.Size(206, 32);
            this.CreateNewButton.TabIndex = 5;
            this.CreateNewButton.Text = "Add new build tool";
            this.CreateNewButton.UseVisualStyleBackColor = true;
            this.CreateNewButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // ExternalFileRootTextBox
            // 
            this.ExternalFileRootTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ExternalFileRootTextBox.Location = new System.Drawing.Point(212, 387);
            this.ExternalFileRootTextBox.Name = "ExternalFileRootTextBox";
            this.ExternalFileRootTextBox.Size = new System.Drawing.Size(372, 20);
            this.ExternalFileRootTextBox.TabIndex = 6;
            this.ExternalFileRootTextBox.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // ExternalFileLocation
            // 
            this.ExternalFileLocation.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ExternalFileLocation.AutoSize = true;
            this.ExternalFileLocation.Location = new System.Drawing.Point(212, 371);
            this.ExternalFileLocation.Name = "ExternalFileLocation";
            this.ExternalFileLocation.Size = new System.Drawing.Size(276, 13);
            this.ExternalFileLocation.TabIndex = 7;
            this.ExternalFileLocation.Text = "External file root (relative paths are relative to the .csproj):";
            // 
            // FileBuildToolAssociationWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(587, 457);
            this.Controls.Add(this.ExternalFileLocation);
            this.Controls.Add(this.ExternalFileRootTextBox);
            this.Controls.Add(this.CreateNewButton);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.OkButton);
            this.Controls.Add(this.propertyGrid1);
            this.Name = "FileBuildToolAssociationWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FileBuildToolAssociationWindow";
            this.Shown += new System.EventHandler(this.FileBuildToolAssociationWindow_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PropertyGrid propertyGrid1;
        private System.Windows.Forms.Button OkButton;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button CreateNewButton;
        private System.Windows.Forms.TextBox ExternalFileRootTextBox;
        private System.Windows.Forms.Label ExternalFileLocation;
    }
}