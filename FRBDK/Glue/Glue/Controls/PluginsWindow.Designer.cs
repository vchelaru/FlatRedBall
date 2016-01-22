namespace FlatRedBall.Glue.Controls
{
    partial class PluginsWindow
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
            this.ListBox = new System.Windows.Forms.CheckedListBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.RightSideSplitContainer = new System.Windows.Forms.SplitContainer();
            this.RemoteActionButton = new System.Windows.Forms.Button();
            this.RemoteActionButton2 = new System.Windows.Forms.Button();
            this.LastUpdatedValueLabel = new System.Windows.Forms.Label();
            this.LastUpdatedTitleLabel = new System.Windows.Forms.Label();
            this.DetailsTextBox = new System.Windows.Forms.RichTextBox();
            this.LoadOnStartupCheckbox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.RightSideSplitContainer)).BeginInit();
            this.RightSideSplitContainer.Panel1.SuspendLayout();
            this.RightSideSplitContainer.Panel2.SuspendLayout();
            this.RightSideSplitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // ListBox
            // 
            this.ListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ListBox.FormattingEnabled = true;
            this.ListBox.Location = new System.Drawing.Point(0, 0);
            this.ListBox.Name = "ListBox";
            this.ListBox.Size = new System.Drawing.Size(133, 273);
            this.ListBox.TabIndex = 0;
            this.ListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.checkedListBox1_ItemCheck);
            this.ListBox.SelectedIndexChanged += new System.EventHandler(this.checkedListBox1_SelectedIndexChanged);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.ListBox);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.RightSideSplitContainer);
            this.splitContainer1.Size = new System.Drawing.Size(425, 273);
            this.splitContainer1.SplitterDistance = 133;
            this.splitContainer1.TabIndex = 1;
            // 
            // RightSideSplitContainer
            // 
            this.RightSideSplitContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.RightSideSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RightSideSplitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.RightSideSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.RightSideSplitContainer.Name = "RightSideSplitContainer";
            this.RightSideSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // RightSideSplitContainer.Panel1
            // 
            this.RightSideSplitContainer.Panel1.Controls.Add(this.RemoteActionButton);
            this.RightSideSplitContainer.Panel1.Controls.Add(this.RemoteActionButton2);
            this.RightSideSplitContainer.Panel1.Controls.Add(this.LastUpdatedValueLabel);
            this.RightSideSplitContainer.Panel1.Controls.Add(this.LastUpdatedTitleLabel);
            // 
            // RightSideSplitContainer.Panel2
            // 
            this.RightSideSplitContainer.Panel2.Controls.Add(this.DetailsTextBox);
            this.RightSideSplitContainer.Panel2.Controls.Add(this.LoadOnStartupCheckbox);
            this.RightSideSplitContainer.Size = new System.Drawing.Size(288, 273);
            this.RightSideSplitContainer.SplitterDistance = 79;
            this.RightSideSplitContainer.TabIndex = 1;
            this.RightSideSplitContainer.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.RightSideSplitContainer_SplitterMoved);
            // 
            // RemoteActionButton
            // 
            this.RemoteActionButton.Location = new System.Drawing.Point(6, 23);
            this.RemoteActionButton.Name = "RemoteActionButton";
            this.RemoteActionButton.Size = new System.Drawing.Size(275, 23);
            this.RemoteActionButton.TabIndex = 2;
            this.RemoteActionButton.Text = "View on GlueVault";
            this.RemoteActionButton.UseVisualStyleBackColor = true;
            this.RemoteActionButton.Click += new System.EventHandler(this.RemoteActionButton_Click);
            // 
            // RemoteActionButton2
            // 
            this.RemoteActionButton2.Location = new System.Drawing.Point(5, 50);
            this.RemoteActionButton2.Name = "RemoteActionButton2";
            this.RemoteActionButton2.Size = new System.Drawing.Size(275, 23);
            this.RemoteActionButton2.TabIndex = 3;
            this.RemoteActionButton2.Text = "Install Latest Version";
            this.RemoteActionButton2.UseVisualStyleBackColor = true;
            this.RemoteActionButton2.Click += new System.EventHandler(this.RemoteActionButton2_Click);
            // 
            // LastUpdatedValueLabel
            // 
            this.LastUpdatedValueLabel.AutoSize = true;
            this.LastUpdatedValueLabel.Location = new System.Drawing.Point(83, 7);
            this.LastUpdatedValueLabel.Name = "LastUpdatedValueLabel";
            this.LastUpdatedValueLabel.Size = new System.Drawing.Size(82, 13);
            this.LastUpdatedValueLabel.TabIndex = 1;
            this.LastUpdatedValueLabel.Text = "March 21, 1981";
            // 
            // LastUpdatedTitleLabel
            // 
            this.LastUpdatedTitleLabel.AutoSize = true;
            this.LastUpdatedTitleLabel.Location = new System.Drawing.Point(3, 7);
            this.LastUpdatedTitleLabel.Name = "LastUpdatedTitleLabel";
            this.LastUpdatedTitleLabel.Size = new System.Drawing.Size(74, 13);
            this.LastUpdatedTitleLabel.TabIndex = 0;
            this.LastUpdatedTitleLabel.Text = "Last Updated:";
            // 
            // DetailsTextBox
            // 
            this.DetailsTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DetailsTextBox.Location = new System.Drawing.Point(0, 17);
            this.DetailsTextBox.Name = "DetailsTextBox";
            this.DetailsTextBox.ReadOnly = true;
            this.DetailsTextBox.Size = new System.Drawing.Size(284, 169);
            this.DetailsTextBox.TabIndex = 0;
            this.DetailsTextBox.Text = "";
            // 
            // LoadOnStartupCheckbox
            // 
            this.LoadOnStartupCheckbox.AutoSize = true;
            this.LoadOnStartupCheckbox.Checked = true;
            this.LoadOnStartupCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.LoadOnStartupCheckbox.Dock = System.Windows.Forms.DockStyle.Top;
            this.LoadOnStartupCheckbox.Location = new System.Drawing.Point(0, 0);
            this.LoadOnStartupCheckbox.Name = "LoadOnStartupCheckbox";
            this.LoadOnStartupCheckbox.Size = new System.Drawing.Size(284, 17);
            this.LoadOnStartupCheckbox.TabIndex = 1;
            this.LoadOnStartupCheckbox.Text = "Load on startup";
            this.LoadOnStartupCheckbox.UseVisualStyleBackColor = true;
            this.LoadOnStartupCheckbox.CheckedChanged += new System.EventHandler(this.LoadOnStartupCheckbox_CheckedChanged);
            // 
            // PluginsWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(425, 273);
            this.Controls.Add(this.splitContainer1);
            this.Name = "PluginsWindow";
            this.Text = "Plugins";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.RightSideSplitContainer.Panel1.ResumeLayout(false);
            this.RightSideSplitContainer.Panel1.PerformLayout();
            this.RightSideSplitContainer.Panel2.ResumeLayout(false);
            this.RightSideSplitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.RightSideSplitContainer)).EndInit();
            this.RightSideSplitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckedListBox ListBox;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.RichTextBox DetailsTextBox;
        private System.Windows.Forms.SplitContainer RightSideSplitContainer;
        private System.Windows.Forms.Button RemoteActionButton;
        private System.Windows.Forms.Label LastUpdatedValueLabel;
        private System.Windows.Forms.Label LastUpdatedTitleLabel;
        private System.Windows.Forms.CheckBox LoadOnStartupCheckbox;
        private System.Windows.Forms.Button RemoteActionButton2;
    }
}