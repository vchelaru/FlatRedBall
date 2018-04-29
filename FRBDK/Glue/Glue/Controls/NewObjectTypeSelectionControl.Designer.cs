namespace FlatRedBall.Glue.Controls
{
    partial class NewObjectTypeSelectionControl
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
            this.FlatRedBallTypesTreeView = new System.Windows.Forms.TreeView();
            this.EntitiesTreeView = new System.Windows.Forms.TreeView();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.FromFileRadioButton = new System.Windows.Forms.RadioButton();
            this.EntityRadioButton = new System.Windows.Forms.RadioButton();
            this.FlatRedBallTypeRadioButton = new System.Windows.Forms.RadioButton();
            this.FilesTreeView = new System.Windows.Forms.TreeView();
            this.SearchTextBox = new System.Windows.Forms.TextBox();
            this.GenericTypeComboBox = new System.Windows.Forms.ComboBox();
            this.ListTypeLabel = new System.Windows.Forms.Label();
            this.SourceNameLabel = new System.Windows.Forms.Label();
            this.SourceNameComboBox = new System.Windows.Forms.ComboBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // FlatRedBallTypesTreeView
            // 
            this.FlatRedBallTypesTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.FlatRedBallTypesTreeView.FullRowSelect = true;
            this.FlatRedBallTypesTreeView.HideSelection = false;
            this.FlatRedBallTypesTreeView.Location = new System.Drawing.Point(3, 117);
            this.FlatRedBallTypesTreeView.Name = "FlatRedBallTypesTreeView";
            this.FlatRedBallTypesTreeView.ShowRootLines = false;
            this.FlatRedBallTypesTreeView.Size = new System.Drawing.Size(208, 198);
            this.FlatRedBallTypesTreeView.TabIndex = 0;
            this.FlatRedBallTypesTreeView.Visible = false;
            this.FlatRedBallTypesTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.FlatRedBallTypesTreeView_AfterSelect);
            this.FlatRedBallTypesTreeView.DoubleClick += new System.EventHandler(this.FlatRedBallTypesTreeView_DoubleClick);
            // 
            // EntitiesTreeView
            // 
            this.EntitiesTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.EntitiesTreeView.FullRowSelect = true;
            this.EntitiesTreeView.HideSelection = false;
            this.EntitiesTreeView.Location = new System.Drawing.Point(3, 117);
            this.EntitiesTreeView.Name = "EntitiesTreeView";
            this.EntitiesTreeView.ShowRootLines = false;
            this.EntitiesTreeView.Size = new System.Drawing.Size(208, 225);
            this.EntitiesTreeView.TabIndex = 0;
            this.EntitiesTreeView.Visible = false;
            this.EntitiesTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.EntitiesTreeView_AfterSelect);
            this.EntitiesTreeView.DoubleClick += new System.EventHandler(this.EntitiesTreeView_DoubleClick);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.FromFileRadioButton);
            this.groupBox1.Controls.Add(this.EntityRadioButton);
            this.groupBox1.Controls.Add(this.FlatRedBallTypeRadioButton);
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(205, 86);
            this.groupBox1.TabIndex = 30;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Select Object Type:";
            // 
            // FromFileRadioButton
            // 
            this.FromFileRadioButton.AutoSize = true;
            this.FromFileRadioButton.Location = new System.Drawing.Point(7, 65);
            this.FromFileRadioButton.Name = "FromFileRadioButton";
            this.FromFileRadioButton.Size = new System.Drawing.Size(67, 17);
            this.FromFileRadioButton.TabIndex = 2;
            this.FromFileRadioButton.TabStop = true;
            this.FromFileRadioButton.Text = "From File";
            this.FromFileRadioButton.UseVisualStyleBackColor = true;
            this.FromFileRadioButton.CheckedChanged += new System.EventHandler(this.FromFileRadioButton_CheckedChanged);
            // 
            // EntityRadioButton
            // 
            this.EntityRadioButton.AutoSize = true;
            this.EntityRadioButton.Location = new System.Drawing.Point(7, 42);
            this.EntityRadioButton.Name = "EntityRadioButton";
            this.EntityRadioButton.Size = new System.Drawing.Size(51, 17);
            this.EntityRadioButton.TabIndex = 1;
            this.EntityRadioButton.TabStop = true;
            this.EntityRadioButton.Text = "Entity";
            this.EntityRadioButton.UseVisualStyleBackColor = true;
            this.EntityRadioButton.CheckedChanged += new System.EventHandler(this.EntityRadioButton_CheckedChanged);
            // 
            // FlatRedBallTypeRadioButton
            // 
            this.FlatRedBallTypeRadioButton.AutoSize = true;
            this.FlatRedBallTypeRadioButton.Location = new System.Drawing.Point(7, 19);
            this.FlatRedBallTypeRadioButton.Name = "FlatRedBallTypeRadioButton";
            this.FlatRedBallTypeRadioButton.Size = new System.Drawing.Size(156, 17);
            this.FlatRedBallTypeRadioButton.TabIndex = 0;
            this.FlatRedBallTypeRadioButton.TabStop = true;
            this.FlatRedBallTypeRadioButton.Text = "FlatRedBall or Custom Type";
            this.FlatRedBallTypeRadioButton.UseVisualStyleBackColor = true;
            this.FlatRedBallTypeRadioButton.CheckedChanged += new System.EventHandler(this.FlatRedBallTypeRadioButton_CheckedChanged);
            // 
            // FilesTreeView
            // 
            this.FilesTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.FilesTreeView.FullRowSelect = true;
            this.FilesTreeView.HideSelection = false;
            this.FilesTreeView.Location = new System.Drawing.Point(3, 117);
            this.FilesTreeView.Name = "FilesTreeView";
            this.FilesTreeView.ShowRootLines = false;
            this.FilesTreeView.Size = new System.Drawing.Size(208, 198);
            this.FilesTreeView.TabIndex = 31;
            this.FilesTreeView.Visible = false;
            this.FilesTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.FilesTreeView_AfterSelect);
            this.FilesTreeView.DoubleClick += new System.EventHandler(this.FilesTreeView_DoubleClick);
            // 
            // SearchTextBox
            // 
            this.SearchTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SearchTextBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.SearchTextBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.SearchTextBox.Location = new System.Drawing.Point(3, 91);
            this.SearchTextBox.Name = "SearchTextBox";
            this.SearchTextBox.Size = new System.Drawing.Size(205, 20);
            this.SearchTextBox.TabIndex = 32;
            this.SearchTextBox.TextChanged += new System.EventHandler(this.SearchTextBox_TextChanged);
            // 
            // GenericTypeComboBox
            // 
            this.GenericTypeComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GenericTypeComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.GenericTypeComboBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.GenericTypeComboBox.FormattingEnabled = true;
            this.GenericTypeComboBox.Location = new System.Drawing.Point(66, 321);
            this.GenericTypeComboBox.Name = "GenericTypeComboBox";
            this.GenericTypeComboBox.Size = new System.Drawing.Size(145, 21);
            this.GenericTypeComboBox.TabIndex = 33;
            // 
            // ListTypeLabel
            // 
            this.ListTypeLabel.AutoSize = true;
            this.ListTypeLabel.Location = new System.Drawing.Point(7, 324);
            this.ListTypeLabel.Name = "ListTypeLabel";
            this.ListTypeLabel.Size = new System.Drawing.Size(53, 13);
            this.ListTypeLabel.TabIndex = 34;
            this.ListTypeLabel.Text = "List Type:";
            // 
            // SourceNameLabel
            // 
            this.SourceNameLabel.AutoSize = true;
            this.SourceNameLabel.Location = new System.Drawing.Point(8, 324);
            this.SourceNameLabel.Name = "SourceNameLabel";
            this.SourceNameLabel.Size = new System.Drawing.Size(75, 13);
            this.SourceNameLabel.TabIndex = 35;
            this.SourceNameLabel.Text = "Source Name:";
            // 
            // SourceNameComboBox
            // 
            this.SourceNameComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SourceNameComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SourceNameComboBox.DropDownWidth = 200;
            this.SourceNameComboBox.FormattingEnabled = true;
            this.SourceNameComboBox.Location = new System.Drawing.Point(89, 321);
            this.SourceNameComboBox.Name = "SourceNameComboBox";
            this.SourceNameComboBox.Size = new System.Drawing.Size(122, 21);
            this.SourceNameComboBox.TabIndex = 36;
            this.SourceNameComboBox.SelectedIndexChanged += new System.EventHandler(this.SourceNameComboBox_SelectedIndexChanged);
            // 
            // NewObjectTypeSelectionControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.SourceNameComboBox);
            this.Controls.Add(this.SourceNameLabel);
            this.Controls.Add(this.ListTypeLabel);
            this.Controls.Add(this.GenericTypeComboBox);
            this.Controls.Add(this.SearchTextBox);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.FlatRedBallTypesTreeView);
            this.Controls.Add(this.FilesTreeView);
            this.Controls.Add(this.EntitiesTreeView);
            this.Name = "NewObjectTypeSelectionControl";
            this.Size = new System.Drawing.Size(212, 345);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView FlatRedBallTypesTreeView;
        private System.Windows.Forms.TreeView EntitiesTreeView;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton EntityRadioButton;
        private System.Windows.Forms.RadioButton FlatRedBallTypeRadioButton;
        private System.Windows.Forms.RadioButton FromFileRadioButton;
        private System.Windows.Forms.TreeView FilesTreeView;
        private System.Windows.Forms.TextBox SearchTextBox;
        private System.Windows.Forms.ComboBox GenericTypeComboBox;
        private System.Windows.Forms.Label ListTypeLabel;
        private System.Windows.Forms.Label SourceNameLabel;
        private System.Windows.Forms.ComboBox SourceNameComboBox;
    }
}
