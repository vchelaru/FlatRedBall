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
            this.TextGroupBox = new System.Windows.Forms.GroupBox();
            this.JustifyRadioButton = new System.Windows.Forms.RadioButton();
            this.panel1 = new System.Windows.Forms.Panel();
            this.JustificationComboBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.OkButton = new System.Windows.Forms.Button();
            this.CancelButton = new System.Windows.Forms.Button();
            this.InformationLabel = new System.Windows.Forms.Label();
            this.TextGroupBox.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // TextGroupBox
            // 
            this.TextGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextGroupBox.Controls.Add(this.JustifyRadioButton);
            this.TextGroupBox.Location = new System.Drawing.Point(3, 3);
            this.TextGroupBox.Name = "TextGroupBox";
            this.TextGroupBox.Size = new System.Drawing.Size(265, 44);
            this.TextGroupBox.TabIndex = 0;
            this.TextGroupBox.TabStop = false;
            this.TextGroupBox.Text = "Options";
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
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.InformationLabel);
            this.panel1.Controls.Add(this.JustificationComboBox);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(3, 45);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(265, 164);
            this.panel1.TabIndex = 1;
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
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 4);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Justification:";
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
            // InformationLabel
            // 
            this.InformationLabel.Location = new System.Drawing.Point(3, 29);
            this.InformationLabel.Name = "InformationLabel";
            this.InformationLabel.Size = new System.Drawing.Size(262, 71);
            this.InformationLabel.TabIndex = 2;
            this.InformationLabel.Text = "label2 but I\'m making this really long so that it can text wrap okay?";
            // 
            // AdjustOffsetControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.CancelButton);
            this.Controls.Add(this.OkButton);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.TextGroupBox);
            this.Name = "AdjustOffsetControl";
            this.Size = new System.Drawing.Size(271, 244);
            this.TextGroupBox.ResumeLayout(false);
            this.TextGroupBox.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox TextGroupBox;
        private System.Windows.Forms.RadioButton JustifyRadioButton;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ComboBox JustificationComboBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button OkButton;
        private System.Windows.Forms.Button CancelButton;
        private System.Windows.Forms.Label InformationLabel;
    }
}
