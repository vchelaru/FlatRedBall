namespace FlatRedBall.AnimationEditorForms.Controls
{
    partial class AdjustOffsetForm
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
            this.adjustOffsetControl1 = new FlatRedBall.AnimationEditorForms.Controls.AdjustOffsetControl();
            this.SuspendLayout();
            // 
            // adjustOffsetControl1
            // 
            this.adjustOffsetControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.adjustOffsetControl1.Location = new System.Drawing.Point(0, 0);
            this.adjustOffsetControl1.Name = "adjustOffsetControl1";
            this.adjustOffsetControl1.Size = new System.Drawing.Size(276, 244);
            this.adjustOffsetControl1.TabIndex = 0;
            this.adjustOffsetControl1.OkClick += new System.EventHandler(this.adjustOffsetControl1_OkClick);
            this.adjustOffsetControl1.CancelClick += new System.EventHandler(this.adjustOffsetControl1_CancelClick);
            this.adjustOffsetControl1.Load += new System.EventHandler(this.adjustOffsetControl1_Load);
            // 
            // AdjustOffsetForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(276, 244);
            this.Controls.Add(this.adjustOffsetControl1);
            this.Name = "AdjustOffsetForm";
            this.Text = "AdjustOffsetForm";
            this.ResumeLayout(false);

        }

        #endregion

        private AdjustOffsetControl adjustOffsetControl1;
    }
}