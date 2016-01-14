namespace EffectEditor.Controls
{
    partial class ComponentsList
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
            this.componentListBox = new System.Windows.Forms.ListBox();
            this.componentDescriptionBox = new System.Windows.Forms.TextBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.selectComponentsButton = new System.Windows.Forms.Button();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // componentListBox
            // 
            this.componentListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.componentListBox.FormattingEnabled = true;
            this.componentListBox.Location = new System.Drawing.Point(0, 0);
            this.componentListBox.Name = "componentListBox";
            this.componentListBox.Size = new System.Drawing.Size(206, 173);
            this.componentListBox.TabIndex = 0;
            // 
            // componentDescriptionBox
            // 
            this.componentDescriptionBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.componentDescriptionBox.Location = new System.Drawing.Point(0, 0);
            this.componentDescriptionBox.Multiline = true;
            this.componentDescriptionBox.Name = "componentDescriptionBox";
            this.componentDescriptionBox.ReadOnly = true;
            this.componentDescriptionBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.componentDescriptionBox.Size = new System.Drawing.Size(206, 85);
            this.componentDescriptionBox.TabIndex = 1;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.selectComponentsButton);
            this.splitContainer1.Panel1.Controls.Add(this.componentListBox);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.componentDescriptionBox);
            this.splitContainer1.Size = new System.Drawing.Size(206, 288);
            this.splitContainer1.SplitterDistance = 199;
            this.splitContainer1.TabIndex = 2;
            // 
            // selectComponentsButton
            // 
            this.selectComponentsButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.selectComponentsButton.Location = new System.Drawing.Point(0, 173);
            this.selectComponentsButton.Name = "selectComponentsButton";
            this.selectComponentsButton.Size = new System.Drawing.Size(206, 23);
            this.selectComponentsButton.TabIndex = 1;
            this.selectComponentsButton.Text = "Select Components";
            this.selectComponentsButton.UseVisualStyleBackColor = true;
            // 
            // ComponentsList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "ComponentsList";
            this.Size = new System.Drawing.Size(206, 288);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox componentListBox;
        private System.Windows.Forms.TextBox componentDescriptionBox;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button selectComponentsButton;
    }
}
