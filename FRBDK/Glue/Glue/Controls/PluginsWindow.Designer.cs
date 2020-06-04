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
            this.PluginView = new System.Windows.Forms.Integration.ElementHost();
            this.pluginView1 = new FlatRedBall.Glue.Plugins.EmbeddedPlugins.ManagePlugins.PluginView();
            this.panel1 = new System.Windows.Forms.Panel();
            this.RemoteActionButton2 = new System.Windows.Forms.Button();
            this.LastUpdatedValueLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panel1.SuspendLayout();
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
            this.splitContainer1.Panel2.Controls.Add(this.PluginView);
            this.splitContainer1.Panel2.Controls.Add(this.panel1);
            this.splitContainer1.Size = new System.Drawing.Size(425, 273);
            this.splitContainer1.SplitterDistance = 133;
            this.splitContainer1.TabIndex = 1;
            // 
            // PluginView
            // 
            this.PluginView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PluginView.Location = new System.Drawing.Point(0, 85);
            this.PluginView.Name = "PluginView";
            this.PluginView.Size = new System.Drawing.Size(288, 188);
            this.PluginView.TabIndex = 4;
            this.PluginView.Text = "elementHost1";
            this.PluginView.Child = this.pluginView1;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.RemoteActionButton2);
            this.panel1.Controls.Add(this.LastUpdatedValueLabel);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(288, 85);
            this.panel1.TabIndex = 5;
            // 
            // RemoteActionButton2
            // 
            this.RemoteActionButton2.Location = new System.Drawing.Point(5, 52);
            this.RemoteActionButton2.Name = "RemoteActionButton2";
            this.RemoteActionButton2.Size = new System.Drawing.Size(275, 23);
            this.RemoteActionButton2.TabIndex = 7;
            this.RemoteActionButton2.Text = "Install Latest Version";
            this.RemoteActionButton2.UseVisualStyleBackColor = true;
            this.RemoteActionButton2.Click += new System.EventHandler(this.RemoteActionButton2_Click);
            // 
            // LastUpdatedValueLabel
            // 
            this.LastUpdatedValueLabel.AutoSize = true;
            this.LastUpdatedValueLabel.Location = new System.Drawing.Point(83, 9);
            this.LastUpdatedValueLabel.Name = "LastUpdatedValueLabel";
            this.LastUpdatedValueLabel.Size = new System.Drawing.Size(82, 13);
            this.LastUpdatedValueLabel.TabIndex = 5;
            this.LastUpdatedValueLabel.Text = "March 21, 1981";
            // 
            // PluginsWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "PluginsWindow";
            this.Size = new System.Drawing.Size(425, 273);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckedListBox ListBox;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Integration.ElementHost PluginView;
        private Plugins.EmbeddedPlugins.ManagePlugins.PluginView pluginView1;
        private System.Windows.Forms.Panel panel1;
        
        private System.Windows.Forms.Button RemoteActionButton2;
        private System.Windows.Forms.Label LastUpdatedValueLabel;
    }
}