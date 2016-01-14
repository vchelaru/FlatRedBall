namespace PluginTestbed.Bookmark
{
    partial class ucBookmark
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
            this.lbBookmarks = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // lbBookmarks
            // 
            this.lbBookmarks.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbBookmarks.FormattingEnabled = true;
            this.lbBookmarks.Location = new System.Drawing.Point(0, 0);
            this.lbBookmarks.Name = "lbBookmarks";
            this.lbBookmarks.Size = new System.Drawing.Size(150, 150);
            this.lbBookmarks.TabIndex = 1;
            this.lbBookmarks.SelectedIndexChanged += new System.EventHandler(this.lbBookmarks_SelectedIndexChanged);
            this.lbBookmarks.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lbBookmarks_KeyDown);
            // 
            // ucBookmark
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lbBookmarks);
            this.Name = "ucBookmark";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox lbBookmarks;
    }
}
