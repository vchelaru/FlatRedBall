namespace MasterInstaller.Components.InstallableComponents
{
    partial class DefaultInstallControl
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
            this.lblDescription = new System.Windows.Forms.Label();
            this.lblName = new System.Windows.Forms.Label();
            this.installationTopBar1 = new MasterInstaller.Controls.InstallationTopBar();
            this.SuspendLayout();
            // 
            // lblDescription
            // 
            this.lblDescription.Location = new System.Drawing.Point(29, 93);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(460, 198);
            this.lblDescription.TabIndex = 14;
            this.lblDescription.Text = "Install Description";
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblName.Location = new System.Drawing.Point(14, 73);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(87, 16);
            this.lblName.TabIndex = 13;
            this.lblName.Text = "Install Name";
            // 
            // installationTopBar1
            // 
            this.installationTopBar1.Location = new System.Drawing.Point(0, 2);
            this.installationTopBar1.Name = "installationTopBar1";
            this.installationTopBar1.Size = new System.Drawing.Size(528, 100);
            this.installationTopBar1.TabIndex = 15;
            // 
            // DefaultInstallControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.installationTopBar1);
            this.Name = "DefaultInstallControl";
            this.Size = new System.Drawing.Size(530, 300);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.Label lblName;
        private Controls.InstallationTopBar installationTopBar1;
    }
}
