namespace FlatRedBall.AnimationEditorForms.Controls
{
    partial class PreviewControls
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
            this.OnionSkinCheckBox = new System.Windows.Forms.CheckBox();
            this.SpriteAlignmentComboBox = new System.Windows.Forms.ComboBox();
            this.ZoomComboBox = new System.Windows.Forms.ComboBox();
            this.OffsetMultiplierLabel = new System.Windows.Forms.Label();
            this.OffsetMultTextBox = new System.Windows.Forms.TextBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // OnionSkinCheckBox
            // 
            this.OnionSkinCheckBox.AutoSize = true;
            this.OnionSkinCheckBox.Location = new System.Drawing.Point(136, 3);
            this.OnionSkinCheckBox.Name = "OnionSkinCheckBox";
            this.OnionSkinCheckBox.Size = new System.Drawing.Size(78, 17);
            this.OnionSkinCheckBox.TabIndex = 0;
            this.OnionSkinCheckBox.Text = "Onion Skin";
            this.OnionSkinCheckBox.UseVisualStyleBackColor = true;
            this.OnionSkinCheckBox.CheckedChanged += new System.EventHandler(this.OnionSkinCheckBox_CheckedChanged);
            // 
            // SpriteAlignmentComboBox
            // 
            this.SpriteAlignmentComboBox.FormattingEnabled = true;
            this.SpriteAlignmentComboBox.Location = new System.Drawing.Point(66, 1);
            this.SpriteAlignmentComboBox.Name = "SpriteAlignmentComboBox";
            this.SpriteAlignmentComboBox.Size = new System.Drawing.Size(65, 21);
            this.SpriteAlignmentComboBox.TabIndex = 1;
            this.SpriteAlignmentComboBox.Text = "Center";
            this.SpriteAlignmentComboBox.SelectedIndexChanged += new System.EventHandler(this.SpriteAlignmentComboBox_SelectedIndexChanged);
            // 
            // ZoomComboBox
            // 
            this.ZoomComboBox.FormattingEnabled = true;
            this.ZoomComboBox.Location = new System.Drawing.Point(4, 1);
            this.ZoomComboBox.Name = "ZoomComboBox";
            this.ZoomComboBox.Size = new System.Drawing.Size(59, 21);
            this.ZoomComboBox.TabIndex = 2;
            this.ZoomComboBox.Text = "100%";
            this.ZoomComboBox.SelectedIndexChanged += new System.EventHandler(this.ZoomComboBox_SelectedIndexChanged);
            // 
            // OffsetMultiplierLabel
            // 
            this.OffsetMultiplierLabel.AutoSize = true;
            this.OffsetMultiplierLabel.Location = new System.Drawing.Point(321, 4);
            this.OffsetMultiplierLabel.Name = "OffsetMultiplierLabel";
            this.OffsetMultiplierLabel.Size = new System.Drawing.Size(60, 13);
            this.OffsetMultiplierLabel.TabIndex = 3;
            this.OffsetMultiplierLabel.Text = "Offset mult:";
            // 
            // OffsetMultTextBox
            // 
            this.OffsetMultTextBox.Location = new System.Drawing.Point(381, 1);
            this.OffsetMultTextBox.Name = "OffsetMultTextBox";
            this.OffsetMultTextBox.Size = new System.Drawing.Size(39, 20);
            this.OffsetMultTextBox.TabIndex = 4;
            this.OffsetMultTextBox.Text = "1";
            this.OffsetMultTextBox.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(227, 3);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(89, 17);
            this.checkBox1.TabIndex = 5;
            this.checkBox1.Text = "Show Guides";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // PreviewControls
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.OffsetMultTextBox);
            this.Controls.Add(this.OffsetMultiplierLabel);
            this.Controls.Add(this.ZoomComboBox);
            this.Controls.Add(this.SpriteAlignmentComboBox);
            this.Controls.Add(this.OnionSkinCheckBox);
            this.Name = "PreviewControls";
            this.Size = new System.Drawing.Size(533, 22);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox OnionSkinCheckBox;
        private System.Windows.Forms.ComboBox SpriteAlignmentComboBox;
        private System.Windows.Forms.ComboBox ZoomComboBox;
        private System.Windows.Forms.Label OffsetMultiplierLabel;
        private System.Windows.Forms.TextBox OffsetMultTextBox;
        private System.Windows.Forms.CheckBox checkBox1;
    }
}
