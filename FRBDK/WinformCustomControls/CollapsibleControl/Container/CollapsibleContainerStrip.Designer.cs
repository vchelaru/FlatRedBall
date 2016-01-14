namespace FlatRedBall.Winforms.Container
{
    partial class CollapsibleContainerStrip
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
            this.vScrollBar1 = new System.Windows.Forms.VScrollBar();
            this.SuspendLayout();
            // 
            // vScrollBar1
            // 
            this.vScrollBar1.Dock = System.Windows.Forms.DockStyle.Right;
            this.vScrollBar1.Location = new System.Drawing.Point(406, 0);
            this.vScrollBar1.Name = "vScrollBar1";
            this.vScrollBar1.Size = new System.Drawing.Size(17, 431);
            this.vScrollBar1.TabIndex = 0;
            this.vScrollBar1.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vScrollBar1_Scroll);
            // 
            // CollapsibleContainerStrip
            // 
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.vScrollBar1);
            this.DoubleBuffered = true;
            this.Name = "CollapsibleContainerStrip";
            this.Size = new System.Drawing.Size(423, 431);
            this.SizeChanged += new System.EventHandler(this.CollapsibleContainerStrip_SizeChanged);
            this.ControlRemoved += new System.Windows.Forms.ControlEventHandler(this.CollapsibleContainerStrip_ControlRemoved);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.VScrollBar vScrollBar1;

    }
}
