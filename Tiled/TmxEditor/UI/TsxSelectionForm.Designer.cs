namespace TmxEditor.UI
{
    partial class TsxSelectionForm
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
            this.FromThisProjectTreeView = new System.Windows.Forms.TreeView();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.FromFileRadioButton = new System.Windows.Forms.RadioButton();
            this.FromThisProjectRadioButton = new System.Windows.Forms.RadioButton();
            this.FromThisProjectPanel = new System.Windows.Forms.Panel();
            this.FromFilePanel = new System.Windows.Forms.Panel();
            this.FromFileButton = new System.Windows.Forms.Button();
            this.FromFileTextBox = new System.Windows.Forms.TextBox();
            this.OkayButton = new System.Windows.Forms.Button();
            this.CancelButton = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.FromThisProjectPanel.SuspendLayout();
            this.FromFilePanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // FromThisProjectTreeView
            // 
            this.FromThisProjectTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FromThisProjectTreeView.Location = new System.Drawing.Point(0, 0);
            this.FromThisProjectTreeView.Name = "FromThisProjectTreeView";
            this.FromThisProjectTreeView.ShowLines = false;
            this.FromThisProjectTreeView.ShowPlusMinus = false;
            this.FromThisProjectTreeView.ShowRootLines = false;
            this.FromThisProjectTreeView.Size = new System.Drawing.Size(657, 144);
            this.FromThisProjectTreeView.TabIndex = 1;
            this.FromThisProjectTreeView.DoubleClick += new System.EventHandler(this.FromThisProjectTreeView_DoubleClick);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.FromFileRadioButton);
            this.groupBox1.Controls.Add(this.FromThisProjectRadioButton);
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(657, 67);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Select TSX Source:";
            // 
            // FromFileRadioButton
            // 
            this.FromFileRadioButton.AutoSize = true;
            this.FromFileRadioButton.Location = new System.Drawing.Point(10, 43);
            this.FromFileRadioButton.Name = "FromFileRadioButton";
            this.FromFileRadioButton.Size = new System.Drawing.Size(67, 17);
            this.FromFileRadioButton.TabIndex = 1;
            this.FromFileRadioButton.Text = "From File";
            this.FromFileRadioButton.UseVisualStyleBackColor = true;
            this.FromFileRadioButton.CheckedChanged += new System.EventHandler(this.radioButton2_CheckedChanged);
            // 
            // FromThisProjectRadioButton
            // 
            this.FromThisProjectRadioButton.AutoSize = true;
            this.FromThisProjectRadioButton.Checked = true;
            this.FromThisProjectRadioButton.Location = new System.Drawing.Point(9, 19);
            this.FromThisProjectRadioButton.Name = "FromThisProjectRadioButton";
            this.FromThisProjectRadioButton.Size = new System.Drawing.Size(102, 17);
            this.FromThisProjectRadioButton.TabIndex = 0;
            this.FromThisProjectRadioButton.TabStop = true;
            this.FromThisProjectRadioButton.Text = "From this project";
            this.FromThisProjectRadioButton.UseVisualStyleBackColor = true;
            this.FromThisProjectRadioButton.CheckedChanged += new System.EventHandler(this.radioButton1_CheckedChanged);
            // 
            // FromThisProjectPanel
            // 
            this.FromThisProjectPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.FromThisProjectPanel.Controls.Add(this.FromThisProjectTreeView);
            this.FromThisProjectPanel.Location = new System.Drawing.Point(3, 76);
            this.FromThisProjectPanel.Name = "FromThisProjectPanel";
            this.FromThisProjectPanel.Size = new System.Drawing.Size(657, 144);
            this.FromThisProjectPanel.TabIndex = 3;
            // 
            // FromFilePanel
            // 
            this.FromFilePanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.FromFilePanel.Controls.Add(this.FromFileButton);
            this.FromFilePanel.Controls.Add(this.FromFileTextBox);
            this.FromFilePanel.Location = new System.Drawing.Point(3, 78);
            this.FromFilePanel.Name = "FromFilePanel";
            this.FromFilePanel.Size = new System.Drawing.Size(657, 29);
            this.FromFilePanel.TabIndex = 4;
            this.FromFilePanel.Visible = false;
            // 
            // FromFileButton
            // 
            this.FromFileButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.FromFileButton.Location = new System.Drawing.Point(627, 1);
            this.FromFileButton.Name = "FromFileButton";
            this.FromFileButton.Size = new System.Drawing.Size(27, 23);
            this.FromFileButton.TabIndex = 1;
            this.FromFileButton.Text = "...";
            this.FromFileButton.UseVisualStyleBackColor = true;
            this.FromFileButton.Click += new System.EventHandler(this.FromFileButton_Click);
            // 
            // FromFileTextBox
            // 
            this.FromFileTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.FromFileTextBox.Location = new System.Drawing.Point(3, 3);
            this.FromFileTextBox.Name = "FromFileTextBox";
            this.FromFileTextBox.Size = new System.Drawing.Size(618, 20);
            this.FromFileTextBox.TabIndex = 0;
            // 
            // OkayButton
            // 
            this.OkayButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OkayButton.Location = new System.Drawing.Point(504, 230);
            this.OkayButton.Name = "OkayButton";
            this.OkayButton.Size = new System.Drawing.Size(75, 23);
            this.OkayButton.TabIndex = 5;
            this.OkayButton.Text = "OK";
            this.OkayButton.UseVisualStyleBackColor = true;
            this.OkayButton.Click += new System.EventHandler(this.OkayButton_Click);
            // 
            // CancelButton
            // 
            this.CancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelButton.Location = new System.Drawing.Point(585, 230);
            this.CancelButton.Name = "CancelButton";
            this.CancelButton.Size = new System.Drawing.Size(75, 23);
            this.CancelButton.TabIndex = 6;
            this.CancelButton.Text = "Cancel";
            this.CancelButton.UseVisualStyleBackColor = true;
            this.CancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // TsxSelectionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(662, 265);
            this.Controls.Add(this.CancelButton);
            this.Controls.Add(this.OkayButton);
            this.Controls.Add(this.FromFilePanel);
            this.Controls.Add(this.FromThisProjectPanel);
            this.Controls.Add(this.groupBox1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TsxSelectionForm";
            this.ShowIcon = false;
            this.Text = "Shared Tileset";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.FromThisProjectPanel.ResumeLayout(false);
            this.FromFilePanel.ResumeLayout(false);
            this.FromFilePanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView FromThisProjectTreeView;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton FromFileRadioButton;
        private System.Windows.Forms.RadioButton FromThisProjectRadioButton;
        private System.Windows.Forms.Panel FromThisProjectPanel;
        private System.Windows.Forms.Panel FromFilePanel;
        private System.Windows.Forms.Button FromFileButton;
        private System.Windows.Forms.TextBox FromFileTextBox;
        private System.Windows.Forms.Button OkayButton;
        private System.Windows.Forms.Button CancelButton;

    }
}