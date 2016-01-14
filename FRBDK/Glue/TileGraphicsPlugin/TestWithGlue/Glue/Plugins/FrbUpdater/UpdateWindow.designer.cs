namespace OfficialPlugins.FrbUpdater
{
    partial class UpdateWindow
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
            this.pbValue = new System.Windows.Forms.ProgressBar();
            this.updateWorkerThread = new System.ComponentModel.BackgroundWorker();
            this.lblSpeed = new System.Windows.Forms.Label();
            this.lblFileName = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // pbValue
            // 
            this.pbValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pbValue.Location = new System.Drawing.Point(12, 27);
            this.pbValue.Name = "pbValue";
            this.pbValue.Size = new System.Drawing.Size(449, 23);
            this.pbValue.TabIndex = 0;
            // 
            // updateWorkerThread
            // 
            this.updateWorkerThread.WorkerReportsProgress = true;
            this.updateWorkerThread.WorkerSupportsCancellation = true;
            this.updateWorkerThread.DoWork += new System.ComponentModel.DoWorkEventHandler(this.UpdateWorkerThreadDoWork);
            this.updateWorkerThread.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.UpdateWorkerThreadProgressChanged);
            this.updateWorkerThread.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.UpdateWorkerThreadRunWorkerCompleted);
            // 
            // lblSpeed
            // 
            this.lblSpeed.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblSpeed.AutoSize = true;
            this.lblSpeed.Location = new System.Drawing.Point(12, 55);
            this.lblSpeed.Name = "lblSpeed";
            this.lblSpeed.Size = new System.Drawing.Size(0, 13);
            this.lblSpeed.TabIndex = 1;
            // 
            // lblFileName
            // 
            this.lblFileName.AutoSize = true;
            this.lblFileName.Location = new System.Drawing.Point(12, 9);
            this.lblFileName.Name = "lblFileName";
            this.lblFileName.Size = new System.Drawing.Size(0, 13);
            this.lblFileName.TabIndex = 2;
            // 
            // UpdateWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(473, 72);
            this.Controls.Add(this.lblFileName);
            this.Controls.Add(this.lblSpeed);
            this.Controls.Add(this.pbValue);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UpdateWindow";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Updating FRBDK";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.UpdateWindow_FormClosing);
            this.Load += new System.EventHandler(this.FrmMainLoad);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar pbValue;
        private System.ComponentModel.BackgroundWorker updateWorkerThread;
        private System.Windows.Forms.Label lblSpeed;
        private System.Windows.Forms.Label lblFileName;
    }
}

