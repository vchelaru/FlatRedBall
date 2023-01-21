namespace FlatRedBall.AnimationEditorForms.Controls
{
    partial class AnimationChainTimeScaleWindow
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
            this.OkButton = new System.Windows.Forms.Button();
            this.CancelButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.TimeTextBox = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.SetAllFramesRadioButton = new System.Windows.Forms.RadioButton();
            this.KeepFramesProportionalRadio = new System.Windows.Forms.RadioButton();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // OkButton
            // 
            this.OkButton.Location = new System.Drawing.Point(155, 127);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(89, 23);
            this.OkButton.TabIndex = 0;
            this.OkButton.Text = "OK";
            this.OkButton.UseVisualStyleBackColor = true;
            this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // CancelButton
            // 
            this.CancelButton.Location = new System.Drawing.Point(258, 127);
            this.CancelButton.Name = "CancelButton";
            this.CancelButton.Size = new System.Drawing.Size(92, 23);
            this.CancelButton.TabIndex = 1;
            this.CancelButton.Text = "Cancel";
            this.CancelButton.UseVisualStyleBackColor = true;
            this.CancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(203, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Set desired animation length (in seconds):";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // TimeTextBox
            // 
            this.TimeTextBox.Location = new System.Drawing.Point(15, 25);
            this.TimeTextBox.Name = "TimeTextBox";
            this.TimeTextBox.Size = new System.Drawing.Size(335, 20);
            this.TimeTextBox.TabIndex = 3;
            this.TimeTextBox.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            this.TimeTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TimeTextBox_KeyDown);
            this.TimeTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TimeTextBox_KeyPress);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.SetAllFramesRadioButton);
            this.groupBox1.Controls.Add(this.KeepFramesProportionalRadio);
            this.groupBox1.Location = new System.Drawing.Point(15, 51);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(335, 70);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Frame time options";
            // 
            // SetAllFramesRadioButton
            // 
            this.SetAllFramesRadioButton.AutoSize = true;
            this.SetAllFramesRadioButton.Location = new System.Drawing.Point(6, 42);
            this.SetAllFramesRadioButton.Name = "SetAllFramesRadioButton";
            this.SetAllFramesRadioButton.Size = new System.Drawing.Size(175, 17);
            this.SetAllFramesRadioButton.TabIndex = 1;
            this.SetAllFramesRadioButton.TabStop = true;
            this.SetAllFramesRadioButton.Text = "Set all frames to the same value";
            this.SetAllFramesRadioButton.UseVisualStyleBackColor = true;
            // 
            // KeepFramesProportionalRadio
            // 
            this.KeepFramesProportionalRadio.AutoSize = true;
            this.KeepFramesProportionalRadio.Checked = true;
            this.KeepFramesProportionalRadio.Location = new System.Drawing.Point(6, 19);
            this.KeepFramesProportionalRadio.Name = "KeepFramesProportionalRadio";
            this.KeepFramesProportionalRadio.Size = new System.Drawing.Size(164, 17);
            this.KeepFramesProportionalRadio.TabIndex = 0;
            this.KeepFramesProportionalRadio.TabStop = true;
            this.KeepFramesProportionalRadio.Text = "Keep frame times proportional";
            this.KeepFramesProportionalRadio.UseVisualStyleBackColor = true;
            // 
            // AnimationChainTimeScaleWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(362, 157);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.TimeTextBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.CancelButton);
            this.Controls.Add(this.OkButton);
            this.Name = "AnimationChainTimeScaleWindow";
            this.ShowIcon = false;
            this.Shown += new System.EventHandler(this.AnimationChainTimeScaleWindow_Shown);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button OkButton;
        private System.Windows.Forms.Button CancelButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox TimeTextBox;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton SetAllFramesRadioButton;
        private System.Windows.Forms.RadioButton KeepFramesProportionalRadio;
    }
}