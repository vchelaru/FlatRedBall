namespace SplineEditor.Gui.Controls
{
    partial class SplineListDisplayControl
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
            this.TreeView = new System.Windows.Forms.TreeView();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.AllObjectToolBar = new System.Windows.Forms.Integration.ElementHost();
            this.allObjectsToolbar1 = new SplineEditor.Gui.Controls.AllObjectsToolbar();
            this.PropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.SelectedItemToolBar = new System.Windows.Forms.Integration.ElementHost();
            this.panel1 = new System.Windows.Forms.Panel();
            this.VelocityTypeComboBox = new System.Windows.Forms.ComboBox();
            this.VelocityTextBox = new System.Windows.Forms.TextBox();
            this.PreviewButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // TreeView
            // 
            this.TreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TreeView.HideSelection = false;
            this.TreeView.Location = new System.Drawing.Point(0, 30);
            this.TreeView.Name = "TreeView";
            this.TreeView.Size = new System.Drawing.Size(185, 349);
            this.TreeView.TabIndex = 0;
            this.TreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.TreeView_AfterSelect);
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
            this.splitContainer1.Panel1.Controls.Add(this.AllObjectToolBar);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.PropertyGrid);
            this.splitContainer1.Panel2.Controls.Add(this.SelectedItemToolBar);
            this.splitContainer1.Size = new System.Drawing.Size(556, 379);
            this.splitContainer1.SplitterDistance = 185;
            this.splitContainer1.TabIndex = 1;
            // 
            // AllObjectToolBar
            // 
            this.AllObjectToolBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.AllObjectToolBar.Location = new System.Drawing.Point(0, 0);
            this.AllObjectToolBar.Name = "AllObjectToolBar";
            this.AllObjectToolBar.Size = new System.Drawing.Size(185, 30);
            this.AllObjectToolBar.TabIndex = 2;
            this.AllObjectToolBar.Text = "elementHost1";
            this.AllObjectToolBar.Child = this.allObjectsToolbar1;
            // 
            // PropertyGrid
            // 
            this.PropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PropertyGrid.HelpVisible = false;
            this.PropertyGrid.Location = new System.Drawing.Point(0, 30);
            this.PropertyGrid.Name = "PropertyGrid";
            this.PropertyGrid.Size = new System.Drawing.Size(367, 349);
            this.PropertyGrid.TabIndex = 0;
            this.PropertyGrid.ToolbarVisible = false;
            // 
            // SelectedItemToolBar
            // 
            this.SelectedItemToolBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.SelectedItemToolBar.Location = new System.Drawing.Point(0, 0);
            this.SelectedItemToolBar.Name = "SelectedItemToolBar";
            this.SelectedItemToolBar.Size = new System.Drawing.Size(367, 30);
            this.SelectedItemToolBar.TabIndex = 1;
            this.SelectedItemToolBar.Text = "elementHost1";
            this.SelectedItemToolBar.Child = null;
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.VelocityTypeComboBox);
            this.panel1.Controls.Add(this.VelocityTextBox);
            this.panel1.Controls.Add(this.PreviewButton);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 379);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(556, 31);
            this.panel1.TabIndex = 2;
            // 
            // VelocityTypeComboBox
            // 
            this.VelocityTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.VelocityTypeComboBox.FormattingEnabled = true;
            this.VelocityTypeComboBox.Location = new System.Drawing.Point(84, 4);
            this.VelocityTypeComboBox.Name = "VelocityTypeComboBox";
            this.VelocityTypeComboBox.Size = new System.Drawing.Size(121, 21);
            this.VelocityTypeComboBox.TabIndex = 3;
            this.VelocityTypeComboBox.SelectedIndexChanged += new System.EventHandler(this.VelocityTypeComboBox_SelectedIndexChanged);
            // 
            // VelocityTextBox
            // 
            this.VelocityTextBox.AcceptsReturn = true;
            this.VelocityTextBox.Location = new System.Drawing.Point(211, 4);
            this.VelocityTextBox.Name = "VelocityTextBox";
            this.VelocityTextBox.Size = new System.Drawing.Size(100, 20);
            this.VelocityTextBox.TabIndex = 2;
            this.VelocityTextBox.TextChanged += new System.EventHandler(this.VelocityTextBox_TextChanged);
            this.VelocityTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.VelocityTextBox_KeyDown);
            this.VelocityTextBox.Leave += new System.EventHandler(this.VelocityTextBox_Leave);
            // 
            // PreviewButton
            // 
            this.PreviewButton.Location = new System.Drawing.Point(3, 3);
            this.PreviewButton.Name = "PreviewButton";
            this.PreviewButton.Size = new System.Drawing.Size(75, 23);
            this.PreviewButton.TabIndex = 0;
            this.PreviewButton.Text = "Preview";
            this.PreviewButton.UseVisualStyleBackColor = true;
            this.PreviewButton.Click += new System.EventHandler(this.PreviewButton_Click);
            // 
            // SplineListDisplayControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.panel1);
            this.Name = "SplineListDisplayControl";
            this.Size = new System.Drawing.Size(556, 410);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView TreeView;
        private System.Windows.Forms.SplitContainer splitContainer1;
        public System.Windows.Forms.PropertyGrid PropertyGrid;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ComboBox VelocityTypeComboBox;
        private System.Windows.Forms.TextBox VelocityTextBox;
        private System.Windows.Forms.Button PreviewButton;
        private System.Windows.Forms.Integration.ElementHost AllObjectToolBar;
        private System.Windows.Forms.Integration.ElementHost SelectedItemToolBar;
        private AllObjectsToolbar allObjectsToolbar1;
    }
}
