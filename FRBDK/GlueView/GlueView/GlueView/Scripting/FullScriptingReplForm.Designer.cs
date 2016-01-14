namespace GlueView.Scripting
{
    partial class FullScriptingReplForm
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
            this.Apply = new System.Windows.Forms.Button();
            this.InitializeTextBox = new System.Windows.Forms.RichTextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.ResetOnApplyCheckBox = new System.Windows.Forms.CheckBox();
            this.button1 = new System.Windows.Forms.Button();
            this.InitAndUpdateSplitContainer = new System.Windows.Forms.SplitContainer();
            this.label2 = new System.Windows.Forms.Label();
            this.UpdateTextBox = new System.Windows.Forms.RichTextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.ClassScopeAndOthersSplitContainer = new System.Windows.Forms.SplitContainer();
            this.ClassScopeTextBox = new System.Windows.Forms.RichTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.InitAndUpdateSplitContainer)).BeginInit();
            this.InitAndUpdateSplitContainer.Panel1.SuspendLayout();
            this.InitAndUpdateSplitContainer.Panel2.SuspendLayout();
            this.InitAndUpdateSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ClassScopeAndOthersSplitContainer)).BeginInit();
            this.ClassScopeAndOthersSplitContainer.Panel1.SuspendLayout();
            this.ClassScopeAndOthersSplitContainer.Panel2.SuspendLayout();
            this.ClassScopeAndOthersSplitContainer.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // Apply
            // 
            this.Apply.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Apply.Location = new System.Drawing.Point(0, 0);
            this.Apply.Name = "Apply";
            this.Apply.Size = new System.Drawing.Size(397, 23);
            this.Apply.TabIndex = 0;
            this.Apply.Text = "Apply Script";
            this.Apply.UseVisualStyleBackColor = true;
            this.Apply.Click += new System.EventHandler(this.Apply_Click);
            // 
            // InitializeTextBox
            // 
            this.InitializeTextBox.AcceptsTab = true;
            this.InitializeTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InitializeTextBox.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InitializeTextBox.Location = new System.Drawing.Point(0, 13);
            this.InitializeTextBox.Name = "InitializeTextBox";
            this.InitializeTextBox.Size = new System.Drawing.Size(507, 126);
            this.InitializeTextBox.TabIndex = 1;
            this.InitializeTextBox.Text = "";
            this.InitializeTextBox.TextChanged += new System.EventHandler(this.ScriptTextBox_TextChanged);
            this.InitializeTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.InitializeTextBox_KeyDown);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.ResetOnApplyCheckBox);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Controls.Add(this.Apply);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 381);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(511, 48);
            this.panel1.TabIndex = 2;
            // 
            // ResetOnApplyCheckBox
            // 
            this.ResetOnApplyCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ResetOnApplyCheckBox.AutoSize = true;
            this.ResetOnApplyCheckBox.Checked = true;
            this.ResetOnApplyCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ResetOnApplyCheckBox.Location = new System.Drawing.Point(403, 4);
            this.ResetOnApplyCheckBox.Name = "ResetOnApplyCheckBox";
            this.ResetOnApplyCheckBox.Size = new System.Drawing.Size(98, 17);
            this.ResetOnApplyCheckBox.TabIndex = 2;
            this.ResetOnApplyCheckBox.Text = "Reset on Apply";
            this.ResetOnApplyCheckBox.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(0, 25);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(508, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "Reset";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.ResetClick);
            // 
            // InitAndUpdateSplitContainer
            // 
            this.InitAndUpdateSplitContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.InitAndUpdateSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InitAndUpdateSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.InitAndUpdateSplitContainer.Name = "InitAndUpdateSplitContainer";
            this.InitAndUpdateSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // InitAndUpdateSplitContainer.Panel1
            // 
            this.InitAndUpdateSplitContainer.Panel1.Controls.Add(this.InitializeTextBox);
            this.InitAndUpdateSplitContainer.Panel1.Controls.Add(this.label2);
            // 
            // InitAndUpdateSplitContainer.Panel2
            // 
            this.InitAndUpdateSplitContainer.Panel2.Controls.Add(this.UpdateTextBox);
            this.InitAndUpdateSplitContainer.Panel2.Controls.Add(this.label3);
            this.InitAndUpdateSplitContainer.Size = new System.Drawing.Size(511, 287);
            this.InitAndUpdateSplitContainer.SplitterDistance = 143;
            this.InitAndUpdateSplitContainer.TabIndex = 3;
            this.InitAndUpdateSplitContainer.TabStop = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Top;
            this.label2.Location = new System.Drawing.Point(0, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(47, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Initialize:";
            // 
            // UpdateTextBox
            // 
            this.UpdateTextBox.AcceptsTab = true;
            this.UpdateTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UpdateTextBox.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UpdateTextBox.Location = new System.Drawing.Point(0, 13);
            this.UpdateTextBox.Name = "UpdateTextBox";
            this.UpdateTextBox.Size = new System.Drawing.Size(507, 123);
            this.UpdateTextBox.TabIndex = 2;
            this.UpdateTextBox.Text = "";
            this.UpdateTextBox.TextChanged += new System.EventHandler(this.UpdateTextBox_TextChanged);
            this.UpdateTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.UpdateTextBox_KeyDown);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Top;
            this.label3.Location = new System.Drawing.Point(0, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(45, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Update:";
            // 
            // ClassScopeAndOthersSplitContainer
            // 
            this.ClassScopeAndOthersSplitContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.ClassScopeAndOthersSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ClassScopeAndOthersSplitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.ClassScopeAndOthersSplitContainer.Location = new System.Drawing.Point(0, 24);
            this.ClassScopeAndOthersSplitContainer.Name = "ClassScopeAndOthersSplitContainer";
            this.ClassScopeAndOthersSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // ClassScopeAndOthersSplitContainer.Panel1
            // 
            this.ClassScopeAndOthersSplitContainer.Panel1.Controls.Add(this.ClassScopeTextBox);
            this.ClassScopeAndOthersSplitContainer.Panel1.Controls.Add(this.label1);
            // 
            // ClassScopeAndOthersSplitContainer.Panel2
            // 
            this.ClassScopeAndOthersSplitContainer.Panel2.Controls.Add(this.InitAndUpdateSplitContainer);
            this.ClassScopeAndOthersSplitContainer.Size = new System.Drawing.Size(511, 357);
            this.ClassScopeAndOthersSplitContainer.SplitterDistance = 66;
            this.ClassScopeAndOthersSplitContainer.TabIndex = 4;
            this.ClassScopeAndOthersSplitContainer.TabStop = false;
            // 
            // ClassScopeTextBox
            // 
            this.ClassScopeTextBox.AcceptsTab = true;
            this.ClassScopeTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ClassScopeTextBox.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ClassScopeTextBox.Location = new System.Drawing.Point(0, 13);
            this.ClassScopeTextBox.Name = "ClassScopeTextBox";
            this.ClassScopeTextBox.Size = new System.Drawing.Size(507, 49);
            this.ClassScopeTextBox.TabIndex = 2;
            this.ClassScopeTextBox.Text = "";
            this.ClassScopeTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ClassScopeTextBox_KeyDown);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Class Scope:";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(511, 24);
            this.menuStrip1.TabIndex = 5;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadToolStripMenuItem,
            this.saveAsToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // loadToolStripMenuItem
            // 
            this.loadToolStripMenuItem.Name = "loadToolStripMenuItem";
            this.loadToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
            this.loadToolStripMenuItem.Text = "Load...";
            this.loadToolStripMenuItem.Click += new System.EventHandler(this.loadToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
            this.saveAsToolStripMenuItem.Text = "Save As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // FullScriptingReplForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(511, 429);
            this.Controls.Add(this.ClassScopeAndOthersSplitContainer);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FullScriptingReplForm";
            this.ShowIcon = false;
            this.Text = "Scripting";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.InitAndUpdateSplitContainer.Panel1.ResumeLayout(false);
            this.InitAndUpdateSplitContainer.Panel1.PerformLayout();
            this.InitAndUpdateSplitContainer.Panel2.ResumeLayout(false);
            this.InitAndUpdateSplitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.InitAndUpdateSplitContainer)).EndInit();
            this.InitAndUpdateSplitContainer.ResumeLayout(false);
            this.ClassScopeAndOthersSplitContainer.Panel1.ResumeLayout(false);
            this.ClassScopeAndOthersSplitContainer.Panel1.PerformLayout();
            this.ClassScopeAndOthersSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ClassScopeAndOthersSplitContainer)).EndInit();
            this.ClassScopeAndOthersSplitContainer.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button Apply;
        private System.Windows.Forms.RichTextBox InitializeTextBox;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox ResetOnApplyCheckBox;
        private System.Windows.Forms.SplitContainer InitAndUpdateSplitContainer;
        private System.Windows.Forms.RichTextBox UpdateTextBox;
        private System.Windows.Forms.SplitContainer ClassScopeAndOthersSplitContainer;
        private System.Windows.Forms.RichTextBox ClassScopeTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
    }
}