namespace PluginTestbed.PerformanceMeasurement
{
    partial class PerformanceForm
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
            this.MeasureTimesCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // MeasureTimesCheckBox
            // 
            this.MeasureTimesCheckBox.AutoSize = true;
            this.MeasureTimesCheckBox.Location = new System.Drawing.Point(12, 12);
            this.MeasureTimesCheckBox.Name = "MeasureTimesCheckBox";
            this.MeasureTimesCheckBox.Size = new System.Drawing.Size(148, 17);
            this.MeasureTimesCheckBox.TabIndex = 0;
            this.MeasureTimesCheckBox.Text = "Measure Execution Times";
            this.MeasureTimesCheckBox.UseVisualStyleBackColor = true;
            this.MeasureTimesCheckBox.CheckedChanged += new System.EventHandler(this.MeasureTimesCheckBox_CheckedChanged);
            // 
            // PerformanceForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(166, 41);
            this.Controls.Add(this.MeasureTimesCheckBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PerformanceForm";
            this.ShowIcon = false;
            this.Text = "Performance Options";
            this.Load += new System.EventHandler(this.PerformanceForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox MeasureTimesCheckBox;
    }
}