namespace TmxEditor
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadTMXToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadTilesetPropertiesFromToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.saveTMXAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveTILBToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(480, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadTMXToolStripMenuItem,
            this.loadTilesetPropertiesFromToolStripMenuItem,
            this.toolStripSeparator1,
            this.saveTMXAsToolStripMenuItem,
            this.saveTILBToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // loadTMXToolStripMenuItem
            // 
            this.loadTMXToolStripMenuItem.Name = "loadTMXToolStripMenuItem";
            this.loadTMXToolStripMenuItem.Size = new System.Drawing.Size(231, 22);
            this.loadTMXToolStripMenuItem.Text = "Load TMX...";
            this.loadTMXToolStripMenuItem.Click += new System.EventHandler(this.loadTMXToolStripMenuItem_Click);
            // 
            // loadTilesetPropertiesFromToolStripMenuItem
            // 
            this.loadTilesetPropertiesFromToolStripMenuItem.Name = "loadTilesetPropertiesFromToolStripMenuItem";
            this.loadTilesetPropertiesFromToolStripMenuItem.Size = new System.Drawing.Size(231, 22);
            this.loadTilesetPropertiesFromToolStripMenuItem.Text = "Load Tileset Properties from...";
            this.loadTilesetPropertiesFromToolStripMenuItem.Click += new System.EventHandler(this.loadTilesetPropertiesFromToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(228, 6);
            // 
            // saveTMXAsToolStripMenuItem
            // 
            this.saveTMXAsToolStripMenuItem.Name = "saveTMXAsToolStripMenuItem";
            this.saveTMXAsToolStripMenuItem.Size = new System.Drawing.Size(231, 22);
            this.saveTMXAsToolStripMenuItem.Text = "Save TMX as...";
            this.saveTMXAsToolStripMenuItem.Click += new System.EventHandler(this.saveTMXAsToolStripMenuItem_Click);
            // 
            // saveTILBToolStripMenuItem
            // 
            this.saveTILBToolStripMenuItem.Name = "saveTILBToolStripMenuItem";
            this.saveTILBToolStripMenuItem.Size = new System.Drawing.Size(231, 22);
            this.saveTILBToolStripMenuItem.Text = "Save TILB";
            this.saveTILBToolStripMenuItem.Click += new System.EventHandler(this.saveTILBToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(480, 460);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "Form1";
            this.Resize += new System.EventHandler(this.Form1_Resize);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadTMXToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadTilesetPropertiesFromToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem saveTMXAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveTILBToolStripMenuItem;
    }
}

