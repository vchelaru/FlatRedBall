namespace GlueCsvEditor.Controls
{
    partial class GridView
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
            this.btnShowComplexProperties = new System.Windows.Forms.Button();
            this.cmbCelldata = new System.Windows.Forms.ComboBox();
            this.btnFindNext = new System.Windows.Forms.Button();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.LeftSideSplitContainer = new System.Windows.Forms.SplitContainer();
            this.txtMultilineEditor = new System.Windows.Forms.TextBox();
            this.pgrPropertyEditor = new System.Windows.Forms.PropertyGrid();
            this.dgrEditor = new System.Windows.Forms.DataGridView();
            this.DataGridContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.txtHeaderType = new System.Windows.Forms.TextBox();
            this.chkIsList = new System.Windows.Forms.CheckBox();
            this.chkIsRequired = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txtHeaderName = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.LeftSideSplitContainer)).BeginInit();
            this.LeftSideSplitContainer.Panel1.SuspendLayout();
            this.LeftSideSplitContainer.Panel2.SuspendLayout();
            this.LeftSideSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgrEditor)).BeginInit();
            this.SuspendLayout();
            // 
            // btnShowComplexProperties
            // 
            this.btnShowComplexProperties.Location = new System.Drawing.Point(3, 28);
            this.btnShowComplexProperties.Name = "btnShowComplexProperties";
            this.btnShowComplexProperties.Size = new System.Drawing.Size(25, 23);
            this.btnShowComplexProperties.TabIndex = 0;
            this.btnShowComplexProperties.Text = "...";
            this.btnShowComplexProperties.UseVisualStyleBackColor = true;
            this.btnShowComplexProperties.Click += new System.EventHandler(this.btnShowComplexProperties_Click);
            // 
            // cmbCelldata
            // 
            this.cmbCelldata.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbCelldata.FormattingEnabled = true;
            this.cmbCelldata.Location = new System.Drawing.Point(34, 28);
            this.cmbCelldata.Name = "cmbCelldata";
            this.cmbCelldata.Size = new System.Drawing.Size(335, 21);
            this.cmbCelldata.TabIndex = 1;
            this.cmbCelldata.DropDown += new System.EventHandler(this.cmbCelldata_DropDown);
            this.cmbCelldata.TextChanged += new System.EventHandler(this.cmbCelldata_TextChanged);
            this.cmbCelldata.KeyDown += new System.Windows.Forms.KeyEventHandler(this.cmbCelldata_KeyDown);
            // 
            // btnFindNext
            // 
            this.btnFindNext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFindNext.Location = new System.Drawing.Point(505, 27);
            this.btnFindNext.Name = "btnFindNext";
            this.btnFindNext.Size = new System.Drawing.Size(88, 23);
            this.btnFindNext.TabIndex = 3;
            this.btnFindNext.Text = "Find Next (F3)";
            this.btnFindNext.UseVisualStyleBackColor = true;
            this.btnFindNext.Click += new System.EventHandler(this.btnFindNext_Click);
            // 
            // txtSearch
            // 
            this.txtSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSearch.Location = new System.Drawing.Point(375, 29);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(130, 20);
            this.txtSearch.TabIndex = 2;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            this.txtSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtSearch_KeyDown);
            // 
            // LeftSideSplitContainer
            // 
            this.LeftSideSplitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LeftSideSplitContainer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.LeftSideSplitContainer.Location = new System.Drawing.Point(3, 55);
            this.LeftSideSplitContainer.Name = "LeftSideSplitContainer";
            this.LeftSideSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // LeftSideSplitContainer.Panel1
            // 
            this.LeftSideSplitContainer.Panel1.Controls.Add(this.txtMultilineEditor);
            this.LeftSideSplitContainer.Panel1.Controls.Add(this.pgrPropertyEditor);
            // 
            // LeftSideSplitContainer.Panel2
            // 
            this.LeftSideSplitContainer.Panel2.Controls.Add(this.dgrEditor);
            this.LeftSideSplitContainer.Size = new System.Drawing.Size(594, 310);
            this.LeftSideSplitContainer.SplitterDistance = 122;
            this.LeftSideSplitContainer.TabIndex = 16;
            // 
            // txtMultilineEditor
            // 
            this.txtMultilineEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtMultilineEditor.Location = new System.Drawing.Point(0, 0);
            this.txtMultilineEditor.Multiline = true;
            this.txtMultilineEditor.Name = "txtMultilineEditor";
            this.txtMultilineEditor.Size = new System.Drawing.Size(592, 120);
            this.txtMultilineEditor.TabIndex = 15;
            this.txtMultilineEditor.Visible = false;
            this.txtMultilineEditor.TextChanged += new System.EventHandler(this.txtMultilineEditor_TextChanged);
            this.txtMultilineEditor.Leave += new System.EventHandler(this.txtMultilineEditor_Leave);
            // 
            // pgrPropertyEditor
            // 
            this.pgrPropertyEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pgrPropertyEditor.HelpVisible = false;
            this.pgrPropertyEditor.Location = new System.Drawing.Point(0, 0);
            this.pgrPropertyEditor.Name = "pgrPropertyEditor";
            this.pgrPropertyEditor.Size = new System.Drawing.Size(592, 120);
            this.pgrPropertyEditor.TabIndex = 14;
            this.pgrPropertyEditor.ToolbarVisible = false;
            // 
            // dgrEditor
            // 
            this.dgrEditor.AllowUserToAddRows = false;
            this.dgrEditor.AllowUserToDeleteRows = false;
            this.dgrEditor.ContextMenuStrip = this.DataGridContextMenuStrip;
            this.dgrEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgrEditor.Location = new System.Drawing.Point(0, 0);
            this.dgrEditor.Name = "dgrEditor";
            this.dgrEditor.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dgrEditor.Size = new System.Drawing.Size(592, 182);
            this.dgrEditor.TabIndex = 4;
            this.dgrEditor.VirtualMode = true;
            this.dgrEditor.CellBeginEdit += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.dgrEditor_CellBeginEdit);
            this.dgrEditor.CellEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgrEditor_CellEnter);
            this.dgrEditor.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.dgrEditor_CellValueNeeded);
            this.dgrEditor.CellValuePushed += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.dgrEditor_CellValuePushed);
            this.dgrEditor.ColumnAdded += new System.Windows.Forms.DataGridViewColumnEventHandler(this.dgrEditor_ColumnAdded);
            this.dgrEditor.ColumnRemoved += new System.Windows.Forms.DataGridViewColumnEventHandler(this.dgrEditor_ColumnRemoved);
            this.dgrEditor.Enter += new System.EventHandler(this.dgrEditor_Enter);
            this.dgrEditor.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dgrEditor_KeyDown);
            this.dgrEditor.KeyUp += new System.Windows.Forms.KeyEventHandler(this.dgrEditor_KeyUp);
            this.dgrEditor.Leave += new System.EventHandler(this.dgrEditor_Leave);
            this.dgrEditor.MouseUp += new System.Windows.Forms.MouseEventHandler(this.dgrEditor_MouseUp);
            // 
            // DataGridContextMenuStrip
            // 
            this.DataGridContextMenuStrip.Name = "DataGridContextMenuStrip";
            this.DataGridContextMenuStrip.Size = new System.Drawing.Size(61, 4);
            // 
            // txtHeaderType
            // 
            this.txtHeaderType.Location = new System.Drawing.Point(242, 3);
            this.txtHeaderType.Name = "txtHeaderType";
            this.txtHeaderType.Size = new System.Drawing.Size(223, 20);
            this.txtHeaderType.TabIndex = 11;
            this.txtHeaderType.TextChanged += new System.EventHandler(this.txtHeaderType_TextChanged);
            // 
            // chkIsList
            // 
            this.chkIsList.AutoSize = true;
            this.chkIsList.Location = new System.Drawing.Point(546, 5);
            this.chkIsList.Name = "chkIsList";
            this.chkIsList.Size = new System.Drawing.Size(42, 17);
            this.chkIsList.TabIndex = 14;
            this.chkIsList.Text = "List";
            this.chkIsList.UseVisualStyleBackColor = true;
            this.chkIsList.Click += new System.EventHandler(this.chkIsList_CheckedChanged);
            // 
            // chkIsRequired
            // 
            this.chkIsRequired.AutoSize = true;
            this.chkIsRequired.Location = new System.Drawing.Point(471, 5);
            this.chkIsRequired.Name = "chkIsRequired";
            this.chkIsRequired.Size = new System.Drawing.Size(69, 17);
            this.chkIsRequired.TabIndex = 13;
            this.chkIsRequired.Text = "Required";
            this.chkIsRequired.UseVisualStyleBackColor = true;
            this.chkIsRequired.Click += new System.EventHandler(this.chkIsRequired_CheckedChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(202, 6);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(34, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Type:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(76, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Column Name:";
            // 
            // txtHeaderName
            // 
            this.txtHeaderName.Location = new System.Drawing.Point(85, 3);
            this.txtHeaderName.Name = "txtHeaderName";
            this.txtHeaderName.Size = new System.Drawing.Size(99, 20);
            this.txtHeaderName.TabIndex = 10;
            this.txtHeaderName.TextChanged += new System.EventHandler(this.txtHeaderName_TextChanged);
            // 
            // GridView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chkIsList);
            this.Controls.Add(this.txtHeaderType);
            this.Controls.Add(this.chkIsRequired);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnShowComplexProperties);
            this.Controls.Add(this.LeftSideSplitContainer);
            this.Controls.Add(this.cmbCelldata);
            this.Controls.Add(this.txtHeaderName);
            this.Controls.Add(this.btnFindNext);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.label3);
            this.Name = "GridView";
            this.Size = new System.Drawing.Size(600, 368);
            this.Load += new System.EventHandler(this.GridView_Load);
            this.LeftSideSplitContainer.Panel1.ResumeLayout(false);
            this.LeftSideSplitContainer.Panel1.PerformLayout();
            this.LeftSideSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.LeftSideSplitContainer)).EndInit();
            this.LeftSideSplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgrEditor)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnShowComplexProperties;
        private System.Windows.Forms.PropertyGrid pgrPropertyEditor;
        private System.Windows.Forms.ComboBox cmbCelldata;
        private System.Windows.Forms.Button btnFindNext;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.DataGridView dgrEditor;
        private System.Windows.Forms.TextBox txtHeaderType;
        private System.Windows.Forms.CheckBox chkIsList;
        private System.Windows.Forms.CheckBox chkIsRequired;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtHeaderName;
        private System.Windows.Forms.TextBox txtMultilineEditor;
        private System.Windows.Forms.SplitContainer LeftSideSplitContainer;
        private System.Windows.Forms.ContextMenuStrip DataGridContextMenuStrip;
    }
}
