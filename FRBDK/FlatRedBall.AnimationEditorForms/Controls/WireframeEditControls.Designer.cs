namespace FlatRedBall.AnimationEditorForms.Controls
{
    partial class WireframeEditControls
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
            this.ComboBox = new System.Windows.Forms.ComboBox();
            this.MagicWandCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // ComboBox
            // 
            this.ComboBox.FormattingEnabled = true;
            this.ComboBox.Location = new System.Drawing.Point(0, 0);
            this.ComboBox.Name = "ComboBox";
            this.ComboBox.Size = new System.Drawing.Size(72, 21);
            this.ComboBox.TabIndex = 0;
            this.ComboBox.SelectedIndexChanged += new System.EventHandler(this.ComboBox_SelectedIndexChanged);
            // 
            // MagicWandCheckBox
            // 
            this.MagicWandCheckBox.Appearance = System.Windows.Forms.Appearance.Button;
            this.MagicWandCheckBox.AutoSize = true;
            this.MagicWandCheckBox.Location = new System.Drawing.Point(77, 0);
            this.MagicWandCheckBox.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this.MagicWandCheckBox.Name = "MagicWandCheckBox";
            this.MagicWandCheckBox.Size = new System.Drawing.Size(78, 23);
            this.MagicWandCheckBox.TabIndex = 1;
            this.MagicWandCheckBox.Text = "Magic Wand";
            this.MagicWandCheckBox.UseVisualStyleBackColor = true;
            this.MagicWandCheckBox.CheckedChanged += new System.EventHandler(this.MagicWandCheckBox_CheckedChanged);
            // 
            // WireframeEditControls
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.MagicWandCheckBox);
            this.Controls.Add(this.ComboBox);
            this.Name = "WireframeEditControls";
            this.Size = new System.Drawing.Size(215, 23);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox ComboBox;
        private System.Windows.Forms.CheckBox MagicWandCheckBox;
    }
}
