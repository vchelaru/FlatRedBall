namespace FlatRedBall.AnimationEditorForms.Controls
{
    partial class AdjustOffsetControl
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
            this.AdjustmentGroupBox = new System.Windows.Forms.GroupBox();
            this.AdjustAllOffsetsRadioButton = new System.Windows.Forms.RadioButton();
            this.JustifyRadioButton = new System.Windows.Forms.RadioButton();
            this.JustifyPanel = new System.Windows.Forms.Panel();
            this.InformationLabel = new System.Windows.Forms.Label();
            this.JustificationComboBox = new System.Windows.Forms.ComboBox();
            this.JustificationLabel = new System.Windows.Forms.Label();
            this.OkButton = new System.Windows.Forms.Button();
            this.CancelButton = new System.Windows.Forms.Button();
            this.AdjustAllPanel = new System.Windows.Forms.Panel();
            this.AdjustmentTypeLabel = new System.Windows.Forms.Label();
            this.OffsetYTextBox = new System.Windows.Forms.TextBox();
            this.OffsetXTextbox = new System.Windows.Forms.TextBox();
            this.AdjustYLabel = new System.Windows.Forms.Label();
            this.AdjustXLabel = new System.Windows.Forms.Label();
            this.AdjustTypeGroupBox = new System.Windows.Forms.GroupBox();
            this.AdjustAbsoluteRadioButton = new System.Windows.Forms.RadioButton();
            this.AdjustRelativeRadioButton = new System.Windows.Forms.RadioButton();
            this.AdjustmentGroupBox.SuspendLayout();
            this.JustifyPanel.SuspendLayout();
            this.AdjustAllPanel.SuspendLayout();
            this.AdjustTypeGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // AdjustmentGroupBox
            // 
            this.AdjustmentGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AdjustmentGroupBox.Controls.Add(this.AdjustAllOffsetsRadioButton);
            this.AdjustmentGroupBox.Controls.Add(this.JustifyRadioButton);
            this.AdjustmentGroupBox.Location = new System.Drawing.Point(3, 3);
            this.AdjustmentGroupBox.Name = "AdjustmentGroupBox";
            this.AdjustmentGroupBox.Size = new System.Drawing.Size(265, 44);
            this.AdjustmentGroupBox.TabIndex = 0;
            this.AdjustmentGroupBox.TabStop = false;
            this.AdjustmentGroupBox.Text = "Options";
            // 
            // AdjustAllOffsetsRadioButton
            // 
            this.AdjustAllOffsetsRadioButton.AutoSize = true;
            this.AdjustAllOffsetsRadioButton.Location = new System.Drawing.Point(109, 20);
            this.AdjustAllOffsetsRadioButton.Name = "AdjustAllOffsetsRadioButton";
            this.AdjustAllOffsetsRadioButton.Size = new System.Drawing.Size(68, 17);
            this.AdjustAllOffsetsRadioButton.TabIndex = 1;
            this.AdjustAllOffsetsRadioButton.Text = "Adjust All";
            this.AdjustAllOffsetsRadioButton.UseVisualStyleBackColor = true;
            this.AdjustAllOffsetsRadioButton.CheckedChanged += new System.EventHandler(this.AdjustAllOffsetsRadioButton_CheckedChanged);
            // 
            // JustifyRadioButton
            // 
            this.JustifyRadioButton.AutoSize = true;
            this.JustifyRadioButton.Checked = true;
            this.JustifyRadioButton.Location = new System.Drawing.Point(6, 19);
            this.JustifyRadioButton.Name = "JustifyRadioButton";
            this.JustifyRadioButton.Size = new System.Drawing.Size(54, 17);
            this.JustifyRadioButton.TabIndex = 0;
            this.JustifyRadioButton.TabStop = true;
            this.JustifyRadioButton.Text = "Justify";
            this.JustifyRadioButton.UseVisualStyleBackColor = true;
            this.JustifyRadioButton.CheckedChanged += new System.EventHandler(this.JustifyRadioButton_CheckedChanged);
            // 
            // JustifyPanel
            // 
            this.JustifyPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.JustifyPanel.Controls.Add(this.InformationLabel);
            this.JustifyPanel.Controls.Add(this.JustificationComboBox);
            this.JustifyPanel.Controls.Add(this.JustificationLabel);
            this.JustifyPanel.Location = new System.Drawing.Point(3, 45);
            this.JustifyPanel.Name = "JustifyPanel";
            this.JustifyPanel.Size = new System.Drawing.Size(265, 164);
            this.JustifyPanel.TabIndex = 1;
            // 
            // InformationLabel
            // 
            this.InformationLabel.Location = new System.Drawing.Point(3, 29);
            this.InformationLabel.Name = "InformationLabel";
            this.InformationLabel.Size = new System.Drawing.Size(262, 71);
            this.InformationLabel.TabIndex = 2;
            this.InformationLabel.Text = "label2 but I\'m making this really long so that it can text wrap okay?";
            // 
            // JustificationComboBox
            // 
            this.JustificationComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.JustificationComboBox.FormattingEnabled = true;
            this.JustificationComboBox.Location = new System.Drawing.Point(88, 1);
            this.JustificationComboBox.Name = "JustificationComboBox";
            this.JustificationComboBox.Size = new System.Drawing.Size(122, 21);
            this.JustificationComboBox.TabIndex = 1;
            this.JustificationComboBox.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // JustificationLabel
            // 
            this.JustificationLabel.AutoSize = true;
            this.JustificationLabel.Location = new System.Drawing.Point(3, 4);
            this.JustificationLabel.Name = "JustificationLabel";
            this.JustificationLabel.Size = new System.Drawing.Size(65, 13);
            this.JustificationLabel.TabIndex = 0;
            this.JustificationLabel.Text = "Justification:";
            // 
            // OkButton
            // 
            this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OkButton.Location = new System.Drawing.Point(112, 215);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(75, 23);
            this.OkButton.TabIndex = 2;
            this.OkButton.Text = "OK";
            this.OkButton.UseVisualStyleBackColor = true;
            this.OkButton.Click += new System.EventHandler(this.HandleOkClick);
            // 
            // CancelButton
            // 
            this.CancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CancelButton.Location = new System.Drawing.Point(193, 215);
            this.CancelButton.Name = "CancelButton";
            this.CancelButton.Size = new System.Drawing.Size(75, 23);
            this.CancelButton.TabIndex = 3;
            this.CancelButton.Text = "Cancel";
            this.CancelButton.UseVisualStyleBackColor = true;
            this.CancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // AdjustAllPanel
            // 
            this.AdjustAllPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AdjustAllPanel.Controls.Add(this.AdjustmentTypeLabel);
            this.AdjustAllPanel.Controls.Add(this.OffsetYTextBox);
            this.AdjustAllPanel.Controls.Add(this.OffsetXTextbox);
            this.AdjustAllPanel.Controls.Add(this.AdjustYLabel);
            this.AdjustAllPanel.Controls.Add(this.AdjustXLabel);
            this.AdjustAllPanel.Controls.Add(this.AdjustTypeGroupBox);
            this.AdjustAllPanel.Location = new System.Drawing.Point(3, 40);
            this.AdjustAllPanel.Name = "AdjustAllPanel";
            this.AdjustAllPanel.Size = new System.Drawing.Size(265, 164);
            this.AdjustAllPanel.TabIndex = 4;
            this.AdjustAllPanel.Visible = false;
            // 
            // AdjustmentTypeLabel
            // 
            this.AdjustmentTypeLabel.Location = new System.Drawing.Point(3, 121);
            this.AdjustmentTypeLabel.Name = "AdjustmentTypeLabel";
            this.AdjustmentTypeLabel.Size = new System.Drawing.Size(259, 45);
            this.AdjustmentTypeLabel.TabIndex = 8;
            this.AdjustmentTypeLabel.Text = "label2 but I\'m making this really long so that it can text wrap okay?";
            // 
            // OffsetYTextBox
            // 
            this.OffsetYTextBox.Location = new System.Drawing.Point(29, 83);
            this.OffsetYTextBox.Name = "OffsetYTextBox";
            this.OffsetYTextBox.Size = new System.Drawing.Size(152, 20);
            this.OffsetYTextBox.TabIndex = 7;
            // 
            // OffsetXTextbox
            // 
            this.OffsetXTextbox.Location = new System.Drawing.Point(29, 51);
            this.OffsetXTextbox.Name = "OffsetXTextbox";
            this.OffsetXTextbox.Size = new System.Drawing.Size(152, 20);
            this.OffsetXTextbox.TabIndex = 6;
            // 
            // AdjustYLabel
            // 
            this.AdjustYLabel.AutoSize = true;
            this.AdjustYLabel.Location = new System.Drawing.Point(6, 86);
            this.AdjustYLabel.Name = "AdjustYLabel";
            this.AdjustYLabel.Size = new System.Drawing.Size(17, 13);
            this.AdjustYLabel.TabIndex = 5;
            this.AdjustYLabel.Text = "Y:";
            // 
            // AdjustXLabel
            // 
            this.AdjustXLabel.AutoSize = true;
            this.AdjustXLabel.Location = new System.Drawing.Point(6, 54);
            this.AdjustXLabel.Name = "AdjustXLabel";
            this.AdjustXLabel.Size = new System.Drawing.Size(17, 13);
            this.AdjustXLabel.TabIndex = 4;
            this.AdjustXLabel.Text = "X:";
            // 
            // AdjustTypeGroupBox
            // 
            this.AdjustTypeGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AdjustTypeGroupBox.Controls.Add(this.AdjustAbsoluteRadioButton);
            this.AdjustTypeGroupBox.Controls.Add(this.AdjustRelativeRadioButton);
            this.AdjustTypeGroupBox.Location = new System.Drawing.Point(0, 3);
            this.AdjustTypeGroupBox.Name = "AdjustTypeGroupBox";
            this.AdjustTypeGroupBox.Size = new System.Drawing.Size(265, 44);
            this.AdjustTypeGroupBox.TabIndex = 3;
            this.AdjustTypeGroupBox.TabStop = false;
            this.AdjustTypeGroupBox.Text = "Adjustment Type";
            // 
            // AdjustAbsoluteRadioButton
            // 
            this.AdjustAbsoluteRadioButton.AutoSize = true;
            this.AdjustAbsoluteRadioButton.Location = new System.Drawing.Point(156, 20);
            this.AdjustAbsoluteRadioButton.Name = "AdjustAbsoluteRadioButton";
            this.AdjustAbsoluteRadioButton.Size = new System.Drawing.Size(66, 17);
            this.AdjustAbsoluteRadioButton.TabIndex = 1;
            this.AdjustAbsoluteRadioButton.Text = "Absolute";
            this.AdjustAbsoluteRadioButton.UseVisualStyleBackColor = true;
            this.AdjustAbsoluteRadioButton.CheckedChanged += new System.EventHandler(this.AdjustAbsoluteRadioButton_CheckedChanged);
            // 
            // AdjustRelativeRadioButton
            // 
            this.AdjustRelativeRadioButton.AutoSize = true;
            this.AdjustRelativeRadioButton.Checked = true;
            this.AdjustRelativeRadioButton.Location = new System.Drawing.Point(53, 19);
            this.AdjustRelativeRadioButton.Name = "AdjustRelativeRadioButton";
            this.AdjustRelativeRadioButton.Size = new System.Drawing.Size(64, 17);
            this.AdjustRelativeRadioButton.TabIndex = 0;
            this.AdjustRelativeRadioButton.TabStop = true;
            this.AdjustRelativeRadioButton.Text = "Relative";
            this.AdjustRelativeRadioButton.UseVisualStyleBackColor = true;
            this.AdjustRelativeRadioButton.CheckedChanged += new System.EventHandler(this.AdjustRelativeRadioButton_CheckedChanged);
            // 
            // AdjustOffsetControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.AdjustAllPanel);
            this.Controls.Add(this.CancelButton);
            this.Controls.Add(this.OkButton);
            this.Controls.Add(this.JustifyPanel);
            this.Controls.Add(this.AdjustmentGroupBox);
            this.Name = "AdjustOffsetControl";
            this.Size = new System.Drawing.Size(271, 244);
            this.AdjustmentGroupBox.ResumeLayout(false);
            this.AdjustmentGroupBox.PerformLayout();
            this.JustifyPanel.ResumeLayout(false);
            this.JustifyPanel.PerformLayout();
            this.AdjustAllPanel.ResumeLayout(false);
            this.AdjustAllPanel.PerformLayout();
            this.AdjustTypeGroupBox.ResumeLayout(false);
            this.AdjustTypeGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox AdjustmentGroupBox;
        private System.Windows.Forms.RadioButton JustifyRadioButton;
        private System.Windows.Forms.Panel JustifyPanel;
        private System.Windows.Forms.ComboBox JustificationComboBox;
        private System.Windows.Forms.Label JustificationLabel;
        private System.Windows.Forms.Button OkButton;
        private System.Windows.Forms.Button CancelButton;
        private System.Windows.Forms.Label InformationLabel;
        private System.Windows.Forms.RadioButton AdjustAllOffsetsRadioButton;
        private System.Windows.Forms.Panel AdjustAllPanel;
        private System.Windows.Forms.Label AdjustmentTypeLabel;
        private System.Windows.Forms.TextBox OffsetYTextBox;
        private System.Windows.Forms.TextBox OffsetXTextbox;
        private System.Windows.Forms.Label AdjustYLabel;
        private System.Windows.Forms.Label AdjustXLabel;
        private System.Windows.Forms.GroupBox AdjustTypeGroupBox;
        private System.Windows.Forms.RadioButton AdjustAbsoluteRadioButton;
        private System.Windows.Forms.RadioButton AdjustRelativeRadioButton;
    }
}
