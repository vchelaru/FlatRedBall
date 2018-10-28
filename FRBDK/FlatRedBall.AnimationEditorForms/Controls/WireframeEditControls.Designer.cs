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
            this.ShowFullAlphaCheckBox = new System.Windows.Forms.CheckBox();
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.wireframeEditControlsWpf1 = new FlatRedBall.AnimationEditorForms.Controls.WireframeEditControlsWpf();
            this.SuspendLayout();
            // 
            // ComboBox
            // 
            this.ComboBox.FormattingEnabled = true;
            this.ComboBox.Location = new System.Drawing.Point(0, 1);
            this.ComboBox.Name = "ComboBox";
            this.ComboBox.Size = new System.Drawing.Size(72, 21);
            this.ComboBox.TabIndex = 0;
            this.ComboBox.SelectedIndexChanged += new System.EventHandler(this.ComboBox_SelectedIndexChanged);
            // 
            // ShowFullAlphaCheckBox
            // 
            this.ShowFullAlphaCheckBox.AutoSize = true;
            this.ShowFullAlphaCheckBox.Location = new System.Drawing.Point(565, 4);
            this.ShowFullAlphaCheckBox.Name = "ShowFullAlphaCheckBox";
            this.ShowFullAlphaCheckBox.Size = new System.Drawing.Size(102, 17);
            this.ShowFullAlphaCheckBox.TabIndex = 4;
            this.ShowFullAlphaCheckBox.Text = "Show Full Alpha";
            this.ShowFullAlphaCheckBox.UseVisualStyleBackColor = true;
            this.ShowFullAlphaCheckBox.CheckedChanged += new System.EventHandler(this.ShowFullAlpha_CheckedChanged);
            // 
            // elementHost1
            // 
            this.elementHost1.ForeColor = System.Drawing.SystemColors.Control;
            this.elementHost1.Location = new System.Drawing.Point(79, 0);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Size = new System.Drawing.Size(480, 23);
            this.elementHost1.TabIndex = 5;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = this.wireframeEditControlsWpf1;
            // 
            // WireframeEditControls
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.elementHost1);
            this.Controls.Add(this.ShowFullAlphaCheckBox);
            this.Controls.Add(this.ComboBox);
            this.Name = "WireframeEditControls";
            this.Size = new System.Drawing.Size(710, 23);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox ComboBox;
        private System.Windows.Forms.CheckBox ShowFullAlphaCheckBox;
        private System.Windows.Forms.Integration.ElementHost elementHost1;
        private WireframeEditControlsWpf wireframeEditControlsWpf1;
    }
}
