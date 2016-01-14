namespace WinformCustomControls
{
    partial class Form1
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
            this.collapsibleContainerStrip1 = new FlatRedBall.Winforms.Container.CollapsibleContainerStrip();
            this.SuspendLayout();
            // 
            // collapsibleContainerStrip1
            // 
            this.collapsibleContainerStrip1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.collapsibleContainerStrip1.Location = new System.Drawing.Point(0, 0);
            this.collapsibleContainerStrip1.Name = "collapsibleContainerStrip1";
            this.collapsibleContainerStrip1.Size = new System.Drawing.Size(503, 456);
            this.collapsibleContainerStrip1.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(503, 456);
            this.Controls.Add(this.collapsibleContainerStrip1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private FlatRedBall.Winforms.Container.CollapsibleContainerStrip collapsibleContainerStrip1;


    }
}

