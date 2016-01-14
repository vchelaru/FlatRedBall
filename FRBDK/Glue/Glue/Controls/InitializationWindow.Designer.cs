namespace FlatRedBall.Glue.Controls
{
    partial class InitializationWindow
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
            this.TopLevelLabel = new System.Windows.Forms.Label();
            this.SubLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // TopLevelLabel
            // 
            this.TopLevelLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TopLevelLabel.Location = new System.Drawing.Point(18, 14);
            this.TopLevelLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.TopLevelLabel.Name = "TopLevelLabel";
            this.TopLevelLabel.Size = new System.Drawing.Size(642, 114);
            this.TopLevelLabel.TabIndex = 0;
            this.TopLevelLabel.Text = "Loading Glue...";
            // 
            // SubLabel
            // 
            this.SubLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SubLabel.Location = new System.Drawing.Point(20, 128);
            this.SubLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.SubLabel.Name = "SubLabel";
            this.SubLabel.Size = new System.Drawing.Size(640, 74);
            this.SubLabel.TabIndex = 1;
            // 
            // InitializationWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(678, 215);
            this.Controls.Add(this.SubLabel);
            this.Controls.Add(this.TopLevelLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "InitializationWindow";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Loading Glue";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label TopLevelLabel;
        private System.Windows.Forms.Label SubLabel;
    }
}