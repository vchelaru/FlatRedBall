namespace FlatRedBall.Glue.Controls
{
    partial class ListBoxWindow
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
            this.TreeView = new System.Windows.Forms.TreeView();
            this.DisplayTextLabel = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.SuspendLayout();
            // 
            // TreeView
            // 
            this.TreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TreeView.Location = new System.Drawing.Point(0, 13);
            this.TreeView.Name = "TreeView";
            this.TreeView.ShowLines = false;
            this.TreeView.ShowRootLines = false;
            this.TreeView.Size = new System.Drawing.Size(425, 213);
            this.TreeView.TabIndex = 0;
            // 
            // DisplayTextLabel
            // 
            this.DisplayTextLabel.AutoSize = true;
            this.DisplayTextLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.DisplayTextLabel.Location = new System.Drawing.Point(0, 0);
            this.DisplayTextLabel.MaximumSize = new System.Drawing.Size(423, 0);
            this.DisplayTextLabel.Name = "DisplayTextLabel";
            this.DisplayTextLabel.Size = new System.Drawing.Size(29, 13);
            this.DisplayTextLabel.TabIndex = 2;
            this.DisplayTextLabel.Text = "label";
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 226);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(425, 0);
            this.flowLayoutPanel1.TabIndex = 3;
            this.flowLayoutPanel1.Resize += new System.EventHandler(this.flowLayoutPanel1_Resize);
            // 
            // ListBoxWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(425, 226);
            this.Controls.Add(this.TreeView);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.DisplayTextLabel);
            this.Name = "ListBoxWindow";
            this.ShowIcon = false;
            this.ResizeEnd += new System.EventHandler(this.ListBoxWindow_ResizeEnd);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView TreeView;
        private System.Windows.Forms.Label DisplayTextLabel;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
    }
}