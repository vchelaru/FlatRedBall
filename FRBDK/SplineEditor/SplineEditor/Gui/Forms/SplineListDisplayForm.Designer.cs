namespace SplineEditor.Gui.Forms
{
    partial class SplineListDisplayForm
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
            this.loadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadScenescnxToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addSplineToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addSplinePointToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.deleteSplineToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteSplinePointToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.cameraPropertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(385, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadToolStripMenuItem,
            this.loadScenescnxToolStripMenuItem,
            this.toolStripSeparator1,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // loadToolStripMenuItem
            // 
            this.loadToolStripMenuItem.Name = "loadToolStripMenuItem";
            this.loadToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.loadToolStripMenuItem.Text = "Load Splines (.splx)";
            this.loadToolStripMenuItem.Click += new System.EventHandler(this.loadToolStripMenuItem_Click);
            // 
            // loadScenescnxToolStripMenuItem
            // 
            this.loadScenescnxToolStripMenuItem.Name = "loadScenescnxToolStripMenuItem";
            this.loadScenescnxToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.loadScenescnxToolStripMenuItem.Text = "Load Scene (.scnx)";
            this.loadScenescnxToolStripMenuItem.Click += new System.EventHandler(this.loadScenescnxToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(171, 6);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click_1);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.saveAsToolStripMenuItem.Text = "Save As";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addSplineToolStripMenuItem,
            this.addSplinePointToolStripMenuItem,
            this.toolStripSeparator2,
            this.deleteSplineToolStripMenuItem,
            this.deleteSplinePointToolStripMenuItem,
            this.toolStripSeparator3,
            this.cameraPropertiesToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // addSplineToolStripMenuItem
            // 
            this.addSplineToolStripMenuItem.Name = "addSplineToolStripMenuItem";
            this.addSplineToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.addSplineToolStripMenuItem.Text = "Add Spline";
            this.addSplineToolStripMenuItem.Click += new System.EventHandler(this.addSplineToolStripMenuItem_Click);
            // 
            // addSplinePointToolStripMenuItem
            // 
            this.addSplinePointToolStripMenuItem.Name = "addSplinePointToolStripMenuItem";
            this.addSplinePointToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.addSplinePointToolStripMenuItem.Text = "Add Spline Point";
            this.addSplinePointToolStripMenuItem.Click += new System.EventHandler(this.addSplinePointToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(170, 6);
            // 
            // deleteSplineToolStripMenuItem
            // 
            this.deleteSplineToolStripMenuItem.Name = "deleteSplineToolStripMenuItem";
            this.deleteSplineToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.deleteSplineToolStripMenuItem.Text = "Delete Spline";
            this.deleteSplineToolStripMenuItem.Click += new System.EventHandler(this.deleteSplineToolStripMenuItem_Click);
            // 
            // deleteSplinePointToolStripMenuItem
            // 
            this.deleteSplinePointToolStripMenuItem.Name = "deleteSplinePointToolStripMenuItem";
            this.deleteSplinePointToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.deleteSplinePointToolStripMenuItem.Text = "Delete Spline Point";
            this.deleteSplinePointToolStripMenuItem.Click += new System.EventHandler(this.deleteSplinePointToolStripMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(170, 6);
            // 
            // cameraPropertiesToolStripMenuItem
            // 
            this.cameraPropertiesToolStripMenuItem.Name = "cameraPropertiesToolStripMenuItem";
            this.cameraPropertiesToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.cameraPropertiesToolStripMenuItem.Text = "Camera Properties";
            this.cameraPropertiesToolStripMenuItem.Click += new System.EventHandler(this.cameraPropertiesToolStripMenuItem_Click);
            // 
            // SplineListDisplayForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(385, 427);
            this.ControlBox = false;
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SplineListDisplayForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Splines";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addSplinePointToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addSplineToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem deleteSplineToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteSplinePointToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadScenescnxToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem cameraPropertiesToolStripMenuItem;
    }
}