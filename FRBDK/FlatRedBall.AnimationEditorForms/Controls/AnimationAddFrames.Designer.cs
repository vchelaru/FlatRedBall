
namespace FlatRedBall.AnimationEditorForms.Controls
{
    partial class AnimationAddFrames
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
            this.FrameAddCount = new System.Windows.Forms.NumericUpDown();
            this.FrameIncrement = new System.Windows.Forms.CheckBox();
            this.labelHeader = new System.Windows.Forms.Label();
            this.buttonOk = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.FrameIncrementError = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.FrameAddCount)).BeginInit();
            this.SuspendLayout();
            // 
            // FrameAddCount
            // 
            this.FrameAddCount.Location = new System.Drawing.Point(12, 99);
            this.FrameAddCount.Name = "FrameAddCount";
            this.FrameAddCount.Size = new System.Drawing.Size(81, 20);
            this.FrameAddCount.TabIndex = 0;
            this.FrameAddCount.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // FrameIncrement
            // 
            this.FrameIncrement.AutoSize = true;
            this.FrameIncrement.Checked = true;
            this.FrameIncrement.CheckState = System.Windows.Forms.CheckState.Checked;
            this.FrameIncrement.Location = new System.Drawing.Point(12, 125);
            this.FrameIncrement.Name = "FrameIncrement";
            this.FrameIncrement.Size = new System.Drawing.Size(147, 17);
            this.FrameIncrement.TabIndex = 2;
            this.FrameIncrement.Text = "Increment frame position?";
            this.FrameIncrement.UseVisualStyleBackColor = true;
            // 
            // labelHeader
            // 
            this.labelHeader.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelHeader.Location = new System.Drawing.Point(12, 9);
            this.labelHeader.Name = "labelHeader";
            this.labelHeader.Size = new System.Drawing.Size(311, 73);
            this.labelHeader.TabIndex = 3;
            this.labelHeader.Text = "Select number of frames to add.\r\n\r\nIncrementing frame position will adjust each n" +
    "ew frame calculated off the last frame in the animation.  Otherwise frame 0 is d" +
    "uplicated.";
            // 
            // buttonOk
            // 
            this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOk.Location = new System.Drawing.Point(167, 182);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(75, 23);
            this.buttonOk.TabIndex = 4;
            this.buttonOk.Text = "Ok";
            this.buttonOk.UseVisualStyleBackColor = true;
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(248, 182);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 5;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // FrameIncrementError
            // 
            this.FrameIncrementError.AutoSize = true;
            this.FrameIncrementError.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FrameIncrementError.ForeColor = System.Drawing.Color.IndianRed;
            this.FrameIncrementError.Location = new System.Drawing.Point(12, 145);
            this.FrameIncrementError.Name = "FrameIncrementError";
            this.FrameIncrementError.Size = new System.Drawing.Size(48, 13);
            this.FrameIncrementError.TabIndex = 6;
            this.FrameIncrementError.Text = "error text";
            // 
            // AnimationAddFrames
            // 
            this.AcceptButton = this.buttonOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(335, 217);
            this.Controls.Add(this.FrameIncrementError);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOk);
            this.Controls.Add(this.labelHeader);
            this.Controls.Add(this.FrameIncrement);
            this.Controls.Add(this.FrameAddCount);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "AnimationAddFrames";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add Multiple Frames";
            ((System.ComponentModel.ISupportInitialize)(this.FrameAddCount)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NumericUpDown FrameAddCount;
        private System.Windows.Forms.CheckBox FrameIncrement;
        private System.Windows.Forms.Label labelHeader;
        private System.Windows.Forms.Button buttonOk;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Label FrameIncrementError;
    }
}