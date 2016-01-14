namespace FlatRedBall.AnimationEditorForms
{
    partial class TextureCoordinateSelectionWindow
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
            this.wireframeEditControls1 = new FlatRedBall.AnimationEditorForms.Controls.WireframeEditControls();
            this.SuspendLayout();
            // 
            // wireframeEditControls1
            // 
            this.wireframeEditControls1.Dock = System.Windows.Forms.DockStyle.Top;
            this.wireframeEditControls1.IsMagicWandSelected = false;
            this.wireframeEditControls1.Location = new System.Drawing.Point(0, 0);
            this.wireframeEditControls1.Name = "wireframeEditControls1";
            this.wireframeEditControls1.PercentageValue = 100;
            this.wireframeEditControls1.Size = new System.Drawing.Size(426, 23);
            this.wireframeEditControls1.TabIndex = 0;
            this.wireframeEditControls1.ZoomChanged += new System.EventHandler(this.wireframeEditControls1_ZoomChanged);
            // 
            // TextureCoordinateSelectionWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.wireframeEditControls1);
            this.Name = "TextureCoordinateSelectionWindow";
            this.Size = new System.Drawing.Size(426, 332);
            this.ResumeLayout(false);

        }

        #endregion

        private Controls.WireframeEditControls wireframeEditControls1;
    }
}
