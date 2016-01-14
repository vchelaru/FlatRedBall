namespace ParticleEditorControls
{
    partial class MainControl
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
            this.components = new System.ComponentModel.Container();
            this.MainSplitContainer = new System.Windows.Forms.SplitContainer();
            this.EmitterTreeView = new System.Windows.Forms.TreeView();
            this.TreeViewRightClickMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.PropertyGridSplitContainer = new System.Windows.Forms.SplitContainer();
            this.EmitAllButton = new System.Windows.Forms.Button();
            this.MainPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.EmissionSettingsPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.EmitCurrentButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.MainSplitContainer)).BeginInit();
            this.MainSplitContainer.Panel1.SuspendLayout();
            this.MainSplitContainer.Panel2.SuspendLayout();
            this.MainSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PropertyGridSplitContainer)).BeginInit();
            this.PropertyGridSplitContainer.Panel1.SuspendLayout();
            this.PropertyGridSplitContainer.Panel2.SuspendLayout();
            this.PropertyGridSplitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainSplitContainer
            // 
            this.MainSplitContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.MainSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainSplitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.MainSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.MainSplitContainer.Name = "MainSplitContainer";
            // 
            // MainSplitContainer.Panel1
            // 
            this.MainSplitContainer.Panel1.Controls.Add(this.EmitterTreeView);
            // 
            // MainSplitContainer.Panel2
            // 
            this.MainSplitContainer.Panel2.Controls.Add(this.PropertyGridSplitContainer);
            this.MainSplitContainer.Size = new System.Drawing.Size(421, 388);
            this.MainSplitContainer.SplitterDistance = 140;
            this.MainSplitContainer.TabIndex = 0;
            // 
            // EmitterTreeView
            // 
            this.EmitterTreeView.ContextMenuStrip = this.TreeViewRightClickMenu;
            this.EmitterTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.EmitterTreeView.HideSelection = false;
            this.EmitterTreeView.Location = new System.Drawing.Point(0, 0);
            this.EmitterTreeView.Name = "EmitterTreeView";
            this.EmitterTreeView.Size = new System.Drawing.Size(136, 384);
            this.EmitterTreeView.TabIndex = 0;
            this.EmitterTreeView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.EmitterTreeView_MouseDown);
            // 
            // TreeViewRightClickMenu
            // 
            this.TreeViewRightClickMenu.Name = "TreeViewRightClickMenu";
            this.TreeViewRightClickMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // PropertyGridSplitContainer
            // 
            this.PropertyGridSplitContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.PropertyGridSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PropertyGridSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.PropertyGridSplitContainer.Name = "PropertyGridSplitContainer";
            this.PropertyGridSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // PropertyGridSplitContainer.Panel1
            // 
            this.PropertyGridSplitContainer.Panel1.Controls.Add(this.EmitCurrentButton);
            this.PropertyGridSplitContainer.Panel1.Controls.Add(this.EmitAllButton);
            this.PropertyGridSplitContainer.Panel1.Controls.Add(this.MainPropertyGrid);
            // 
            // PropertyGridSplitContainer.Panel2
            // 
            this.PropertyGridSplitContainer.Panel2.Controls.Add(this.EmissionSettingsPropertyGrid);
            this.PropertyGridSplitContainer.Size = new System.Drawing.Size(277, 388);
            this.PropertyGridSplitContainer.SplitterDistance = 194;
            this.PropertyGridSplitContainer.TabIndex = 1;
            // 
            // EmitAllButton
            // 
            this.EmitAllButton.Location = new System.Drawing.Point(3, 1);
            this.EmitAllButton.Name = "EmitAllButton";
            this.EmitAllButton.Size = new System.Drawing.Size(78, 23);
            this.EmitAllButton.TabIndex = 1;
            this.EmitAllButton.Text = "Emit All";
            this.EmitAllButton.UseVisualStyleBackColor = true;
            this.EmitAllButton.Click += new System.EventHandler(this.EmitAllButton_Click);
            // 
            // MainPropertyGrid
            // 
            this.MainPropertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MainPropertyGrid.HelpVisible = false;
            this.MainPropertyGrid.Location = new System.Drawing.Point(0, 24);
            this.MainPropertyGrid.Name = "MainPropertyGrid";
            this.MainPropertyGrid.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            this.MainPropertyGrid.Size = new System.Drawing.Size(273, 166);
            this.MainPropertyGrid.TabIndex = 0;
            this.MainPropertyGrid.ToolbarVisible = false;
            // 
            // EmissionSettingsPropertyGrid
            // 
            this.EmissionSettingsPropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.EmissionSettingsPropertyGrid.HelpVisible = false;
            this.EmissionSettingsPropertyGrid.Location = new System.Drawing.Point(0, 0);
            this.EmissionSettingsPropertyGrid.Name = "EmissionSettingsPropertyGrid";
            this.EmissionSettingsPropertyGrid.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            this.EmissionSettingsPropertyGrid.Size = new System.Drawing.Size(273, 186);
            this.EmissionSettingsPropertyGrid.TabIndex = 0;
            this.EmissionSettingsPropertyGrid.ToolbarVisible = false;
            // 
            // EmitCurrentButton
            // 
            this.EmitCurrentButton.Location = new System.Drawing.Point(87, 1);
            this.EmitCurrentButton.Name = "EmitCurrentButton";
            this.EmitCurrentButton.Size = new System.Drawing.Size(75, 23);
            this.EmitCurrentButton.TabIndex = 2;
            this.EmitCurrentButton.Text = "Emit Current";
            this.EmitCurrentButton.UseVisualStyleBackColor = true;
            this.EmitCurrentButton.Click += new System.EventHandler(this.EmitCurrentButton_Click);
            // 
            // MainControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.MainSplitContainer);
            this.Name = "MainControl";
            this.Size = new System.Drawing.Size(421, 388);
            this.MainSplitContainer.Panel1.ResumeLayout(false);
            this.MainSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.MainSplitContainer)).EndInit();
            this.MainSplitContainer.ResumeLayout(false);
            this.PropertyGridSplitContainer.Panel1.ResumeLayout(false);
            this.PropertyGridSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PropertyGridSplitContainer)).EndInit();
            this.PropertyGridSplitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer MainSplitContainer;
        private System.Windows.Forms.TreeView EmitterTreeView;
        private System.Windows.Forms.PropertyGrid MainPropertyGrid;
        private System.Windows.Forms.SplitContainer PropertyGridSplitContainer;
        private System.Windows.Forms.PropertyGrid EmissionSettingsPropertyGrid;
        private System.Windows.Forms.ContextMenuStrip TreeViewRightClickMenu;
        private System.Windows.Forms.Button EmitAllButton;
        private System.Windows.Forms.Button EmitCurrentButton;
    }
}
