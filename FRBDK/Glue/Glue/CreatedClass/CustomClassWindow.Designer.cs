namespace FlatRedBall.Glue.Controls
{
    partial class CustomClassWindow
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
            this.TreeView = new System.Windows.Forms.TreeView();
            this.NewClassButton = new System.Windows.Forms.Button();
            this.UseThisClassButton = new System.Windows.Forms.Button();
            this.DoneButton = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.CustomNamespaceTextbox = new System.Windows.Forms.TextBox();
            this.CustomNamespaceLabel = new System.Windows.Forms.Label();
            this.GenerateDataClassComboBox = new System.Windows.Forms.CheckBox();
            this.ClassNameTextBox = new System.Windows.Forms.TextBox();
            this.NameLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // TreeView
            // 
            this.TreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TreeView.HideSelection = false;
            this.TreeView.Location = new System.Drawing.Point(0, 25);
            this.TreeView.Name = "TreeView";
            this.TreeView.Size = new System.Drawing.Size(175, 540);
            this.TreeView.TabIndex = 0;
            this.TreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.TreeView_AfterSelect);
            this.TreeView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TreeView_KeyDown);
            // 
            // NewClassButton
            // 
            this.NewClassButton.Dock = System.Windows.Forms.DockStyle.Top;
            this.NewClassButton.Location = new System.Drawing.Point(0, 0);
            this.NewClassButton.Name = "NewClassButton";
            this.NewClassButton.Size = new System.Drawing.Size(175, 25);
            this.NewClassButton.TabIndex = 1;
            this.NewClassButton.Text = "New Class";
            this.NewClassButton.UseVisualStyleBackColor = true;
            this.NewClassButton.Click += new System.EventHandler(this.NewClassButton_Click);
            // 
            // UseThisClassButton
            // 
            this.UseThisClassButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.UseThisClassButton.Location = new System.Drawing.Point(2, 537);
            this.UseThisClassButton.Name = "UseThisClassButton";
            this.UseThisClassButton.Size = new System.Drawing.Size(95, 25);
            this.UseThisClassButton.TabIndex = 2;
            this.UseThisClassButton.Text = "UseThisClass";
            this.UseThisClassButton.UseVisualStyleBackColor = true;
            this.UseThisClassButton.Click += new System.EventHandler(this.UseThisClassButton_Click);
            // 
            // DoneButton
            // 
            this.DoneButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DoneButton.Location = new System.Drawing.Point(103, 537);
            this.DoneButton.Name = "DoneButton";
            this.DoneButton.Size = new System.Drawing.Size(240, 25);
            this.DoneButton.TabIndex = 3;
            this.DoneButton.Text = "Done";
            this.DoneButton.UseVisualStyleBackColor = true;
            this.DoneButton.Click += new System.EventHandler(this.DoneButton_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.TreeView);
            this.splitContainer1.Panel1.Controls.Add(this.NewClassButton);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.CustomNamespaceTextbox);
            this.splitContainer1.Panel2.Controls.Add(this.CustomNamespaceLabel);
            this.splitContainer1.Panel2.Controls.Add(this.GenerateDataClassComboBox);
            this.splitContainer1.Panel2.Controls.Add(this.ClassNameTextBox);
            this.splitContainer1.Panel2.Controls.Add(this.NameLabel);
            this.splitContainer1.Panel2.Controls.Add(this.DoneButton);
            this.splitContainer1.Panel2.Controls.Add(this.UseThisClassButton);
            this.splitContainer1.Size = new System.Drawing.Size(525, 565);
            this.splitContainer1.SplitterDistance = 175;
            this.splitContainer1.TabIndex = 4;
            // 
            // CustomNamespaceTextbox
            // 
            this.CustomNamespaceTextbox.AcceptsReturn = true;
            this.CustomNamespaceTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.CustomNamespaceTextbox.Location = new System.Drawing.Point(124, 55);
            this.CustomNamespaceTextbox.Name = "CustomNamespaceTextbox";
            this.CustomNamespaceTextbox.Size = new System.Drawing.Size(210, 20);
            this.CustomNamespaceTextbox.TabIndex = 8;
            this.CustomNamespaceTextbox.TextChanged += new System.EventHandler(this.CustomNamespaceTextbox_TextChanged);
            this.CustomNamespaceTextbox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CustomNamespaceTextbox_KeyDown);
            // 
            // CustomNamespaceLabel
            // 
            this.CustomNamespaceLabel.AutoSize = true;
            this.CustomNamespaceLabel.Location = new System.Drawing.Point(13, 58);
            this.CustomNamespaceLabel.Name = "CustomNamespaceLabel";
            this.CustomNamespaceLabel.Size = new System.Drawing.Size(105, 13);
            this.CustomNamespaceLabel.TabIndex = 7;
            this.CustomNamespaceLabel.Text = "Custom Namespace:";
            // 
            // GenerateDataClassComboBox
            // 
            this.GenerateDataClassComboBox.AutoSize = true;
            this.GenerateDataClassComboBox.Location = new System.Drawing.Point(16, 32);
            this.GenerateDataClassComboBox.Name = "GenerateDataClassComboBox";
            this.GenerateDataClassComboBox.Size = new System.Drawing.Size(124, 17);
            this.GenerateDataClassComboBox.TabIndex = 6;
            this.GenerateDataClassComboBox.Text = "Generate Data Class";
            this.GenerateDataClassComboBox.UseVisualStyleBackColor = true;
            this.GenerateDataClassComboBox.CheckedChanged += new System.EventHandler(this.GenerateDataClassComboBox_CheckedChanged);
            // 
            // ClassNameTextBox
            // 
            this.ClassNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ClassNameTextBox.Location = new System.Drawing.Point(57, 6);
            this.ClassNameTextBox.Name = "ClassNameTextBox";
            this.ClassNameTextBox.ReadOnly = true;
            this.ClassNameTextBox.Size = new System.Drawing.Size(277, 20);
            this.ClassNameTextBox.TabIndex = 5;
            // 
            // NameLabel
            // 
            this.NameLabel.AutoSize = true;
            this.NameLabel.Location = new System.Drawing.Point(13, 9);
            this.NameLabel.Name = "NameLabel";
            this.NameLabel.Size = new System.Drawing.Size(38, 13);
            this.NameLabel.TabIndex = 4;
            this.NameLabel.Text = "Name:";
            // 
            // CustomClassWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(525, 565);
            this.Controls.Add(this.splitContainer1);
            this.Name = "CustomClassWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "CreatedClassWindow";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.CustomClassWindow_FormClosing);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView TreeView;
        private System.Windows.Forms.Button NewClassButton;
        private System.Windows.Forms.Button UseThisClassButton;
        private System.Windows.Forms.Button DoneButton;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.CheckBox GenerateDataClassComboBox;
        private System.Windows.Forms.TextBox ClassNameTextBox;
        private System.Windows.Forms.Label NameLabel;
        private System.Windows.Forms.TextBox CustomNamespaceTextbox;
        private System.Windows.Forms.Label CustomNamespaceLabel;
    }
}