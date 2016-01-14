namespace TileGraphicsPlugin.Controls
{
    partial class ControlForAddingCollision
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
            this.HasCollisionCheckBox = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.CircleRadioButton = new System.Windows.Forms.RadioButton();
            this.RectangleRadioButton = new System.Windows.Forms.RadioButton();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // HasCollisionCheckBox
            // 
            this.HasCollisionCheckBox.AutoSize = true;
            this.HasCollisionCheckBox.Checked = true;
            this.HasCollisionCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.HasCollisionCheckBox.Location = new System.Drawing.Point(3, 3);
            this.HasCollisionCheckBox.Name = "HasCollisionCheckBox";
            this.HasCollisionCheckBox.Size = new System.Drawing.Size(86, 17);
            this.HasCollisionCheckBox.TabIndex = 0;
            this.HasCollisionCheckBox.Text = "Has Collision";
            this.HasCollisionCheckBox.UseVisualStyleBackColor = true;
            this.HasCollisionCheckBox.CheckedChanged += new System.EventHandler(this.HasCollisionCheckBox_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.CircleRadioButton);
            this.groupBox1.Controls.Add(this.RectangleRadioButton);
            this.groupBox1.Location = new System.Drawing.Point(0, 26);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(147, 37);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Collision Type";
            // 
            // CircleRadioButton
            // 
            this.CircleRadioButton.AutoSize = true;
            this.CircleRadioButton.Location = new System.Drawing.Point(86, 17);
            this.CircleRadioButton.Name = "CircleRadioButton";
            this.CircleRadioButton.Size = new System.Drawing.Size(51, 17);
            this.CircleRadioButton.TabIndex = 1;
            this.CircleRadioButton.TabStop = true;
            this.CircleRadioButton.Text = "Circle";
            this.CircleRadioButton.UseVisualStyleBackColor = true;
            // 
            // RectangleRadioButton
            // 
            this.RectangleRadioButton.AutoSize = true;
            this.RectangleRadioButton.Checked = true;
            this.RectangleRadioButton.Location = new System.Drawing.Point(6, 17);
            this.RectangleRadioButton.Name = "RectangleRadioButton";
            this.RectangleRadioButton.Size = new System.Drawing.Size(74, 17);
            this.RectangleRadioButton.TabIndex = 0;
            this.RectangleRadioButton.TabStop = true;
            this.RectangleRadioButton.Text = "Rectangle";
            this.RectangleRadioButton.UseVisualStyleBackColor = true;
            // 
            // ControlForAddingCollision
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.HasCollisionCheckBox);
            this.Name = "ControlForAddingCollision";
            this.Size = new System.Drawing.Size(147, 64);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox HasCollisionCheckBox;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton CircleRadioButton;
        private System.Windows.Forms.RadioButton RectangleRadioButton;
    }
}
