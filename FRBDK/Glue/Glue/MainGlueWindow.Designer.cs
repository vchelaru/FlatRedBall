using GlueFormsCore.Controls;

namespace Glue
{
	partial class MainGlueWindow
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		public System.ComponentModel.IContainer components = null;

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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainGlueWindow));
            this.mElementContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ElementImages = new System.Windows.Forms.ImageList(this.components);

            this.mElementContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // mElementContextMenu
            // 
            this.mElementContextMenu.Name = "mElementContextMenu";
            this.mElementContextMenu.Size = new System.Drawing.Size(187, 384);
            this.mElementContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
            // 
            // ElementImages
            // 
            this.ElementImages.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.ElementImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ElementImages.ImageStream")));
            this.ElementImages.TransparentColor = System.Drawing.Color.Transparent;
            this.ElementImages.Images.SetKeyName(0, "transparent.png");
            this.ElementImages.Images.SetKeyName(1, "code.png");
            this.ElementImages.Images.SetKeyName(2, "edit_code.png");
            this.ElementImages.Images.SetKeyName(3, "entity.png");
            this.ElementImages.Images.SetKeyName(4, "file.png");
            this.ElementImages.Images.SetKeyName(5, "folder.png");
            this.ElementImages.Images.SetKeyName(6, "master_code.png");
            this.ElementImages.Images.SetKeyName(7, "master_entity.png");
            this.ElementImages.Images.SetKeyName(8, "master_file.png");
            this.ElementImages.Images.SetKeyName(9, "master_object.png");
            this.ElementImages.Images.SetKeyName(10, "master_screen.png");
            this.ElementImages.Images.SetKeyName(11, "master_states.png");
            this.ElementImages.Images.SetKeyName(12, "master_variables.png");
            this.ElementImages.Images.SetKeyName(13, "object.png");
            this.ElementImages.Images.SetKeyName(14, "screen.png");
            this.ElementImages.Images.SetKeyName(15, "states.png");
            this.ElementImages.Images.SetKeyName(16, "variable.png");
            this.ElementImages.Images.SetKeyName(17, "layerList.png");
            this.ElementImages.Images.SetKeyName(18, "collisionRelationshipList.png");

            // 
            // MainGlueWindow
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(764, 633);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainGlueWindow";
            this.Text = "FlatRedBall Glue";

            this.mElementContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
		}

		#endregion

        public System.Windows.Forms.ImageList ElementImages;
        internal System.Windows.Forms.ContextMenuStrip mElementContextMenu;
    }
}

