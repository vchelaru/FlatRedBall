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
            this.mMenu = new System.Windows.Forms.MenuStrip();
            this.rightPanelContainer = new System.Windows.Forms.SplitContainer();
            this.MainTabControl = new FlatRedBall.Glue.Controls.TabControlEx();
            this.tcRight = new FlatRedBall.Glue.Controls.TabControlEx();
            this.topPanelContainer = new System.Windows.Forms.SplitContainer();
            this.tcTop = new FlatRedBall.Glue.Controls.TabControlEx();
            this.leftPanelContainer = new System.Windows.Forms.SplitContainer();
            this.tcLeft = new FlatRedBall.Glue.Controls.TabControlEx();
            this.bottomPanelContainer = new System.Windows.Forms.SplitContainer();
            this.tcBottom = new FlatRedBall.Glue.Controls.TabControlEx();
            this.elementHost2 = new System.Windows.Forms.Integration.ElementHost();
            this.toolbarControl1 = new FlatRedBall.Glue.Controls.ToolbarControl();
            this.mElementContextMenu.SuspendLayout();
            this.mMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.rightPanelContainer)).BeginInit();
            this.rightPanelContainer.Panel1.SuspendLayout();
            this.rightPanelContainer.Panel2.SuspendLayout();
            this.rightPanelContainer.SuspendLayout();
            this.MainTabControl.SuspendLayout();
            this.tcRight.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.topPanelContainer)).BeginInit();
            this.topPanelContainer.Panel1.SuspendLayout();
            this.topPanelContainer.Panel2.SuspendLayout();
            this.topPanelContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.leftPanelContainer)).BeginInit();
            this.leftPanelContainer.Panel1.SuspendLayout();
            this.leftPanelContainer.Panel2.SuspendLayout();
            this.leftPanelContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bottomPanelContainer)).BeginInit();
            this.bottomPanelContainer.Panel1.SuspendLayout();
            this.bottomPanelContainer.Panel2.SuspendLayout();
            this.bottomPanelContainer.SuspendLayout();
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
            // mMenu
            // 
            this.mMenu.Location = new System.Drawing.Point(0, 0);
            this.mMenu.Name = "mMenu";
            this.mMenu.Size = new System.Drawing.Size(764, 24);
            this.mMenu.TabIndex = 1;
            this.mMenu.Text = "menuStrip1";
            // 
            // rightPanelContainer
            // 
            this.rightPanelContainer.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.rightPanelContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.rightPanelContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightPanelContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.rightPanelContainer.Location = new System.Drawing.Point(0, 0);
            this.rightPanelContainer.Margin = new System.Windows.Forms.Padding(0);
            this.rightPanelContainer.Name = "rightPanelContainer";
            // 
            // rightPanelContainer.Panel1
            // 
            this.rightPanelContainer.Panel1.Controls.Add(this.MainTabControl);
            // 
            // rightPanelContainer.Panel2
            // 
            this.rightPanelContainer.Panel2.Controls.Add(this.tcRight);
            this.rightPanelContainer.Size = new System.Drawing.Size(764, 579);
            this.rightPanelContainer.SplitterDistance = 546;
            this.rightPanelContainer.TabIndex = 4;
            // 
            // MainTabControl
            // 
            this.MainTabControl.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.MainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainTabControl.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.MainTabControl.IgnoreFirst = false;
            this.MainTabControl.Location = new System.Drawing.Point(0, 0);
            this.MainTabControl.Name = "MainTabControl";
            this.MainTabControl.SelectedIndex = 0;
            this.MainTabControl.Size = new System.Drawing.Size(542, 575);
            this.MainTabControl.TabIndex = 4;
            // 
            // tcRight
            // 
            this.tcRight.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.tcRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcRight.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tcRight.IgnoreFirst = false;
            this.tcRight.Location = new System.Drawing.Point(0, 0);
            this.tcRight.Margin = new System.Windows.Forms.Padding(0);
            this.tcRight.Name = "tcRight";
            this.tcRight.Padding = new System.Drawing.Point(6, 0);
            this.tcRight.SelectedIndex = 0;
            this.tcRight.Size = new System.Drawing.Size(210, 575);
            this.tcRight.TabIndex = 2;
            this.tcRight.ControlAdded += new System.Windows.Forms.ControlEventHandler(this.ControlAddedToRightView);
            this.tcRight.ControlRemoved += new System.Windows.Forms.ControlEventHandler(this.ControlRemovedFromRightView);
            // 
            // topPanelContainer
            // 
            this.topPanelContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.topPanelContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topPanelContainer.Location = new System.Drawing.Point(0, 0);
            this.topPanelContainer.Name = "topPanelContainer";
            this.topPanelContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // topPanelContainer.Panel1
            // 
            this.topPanelContainer.Panel1.Controls.Add(this.tcTop);
            this.topPanelContainer.Panel1Collapsed = true;
            // 
            // topPanelContainer.Panel2
            // 
            this.topPanelContainer.Panel2.Controls.Add(this.leftPanelContainer);
            this.topPanelContainer.Size = new System.Drawing.Size(764, 579);
            this.topPanelContainer.SplitterDistance = 82;
            this.topPanelContainer.TabIndex = 6;
            // 
            // tcTop
            // 
            this.tcTop.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.tcTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcTop.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tcTop.IgnoreFirst = false;
            this.tcTop.Location = new System.Drawing.Point(0, 0);
            this.tcTop.Name = "tcTop";
            this.tcTop.SelectedIndex = 0;
            this.tcTop.Size = new System.Drawing.Size(146, 78);
            this.tcTop.TabIndex = 0;
            this.tcTop.ControlAdded += new System.Windows.Forms.ControlEventHandler(this.tcPanel1_ControlAdded);
            this.tcTop.ControlRemoved += new System.Windows.Forms.ControlEventHandler(this.tcPanel1_ControlRemoved);
            // 
            // leftPanelContainer
            // 
            this.leftPanelContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.leftPanelContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftPanelContainer.Location = new System.Drawing.Point(0, 0);
            this.leftPanelContainer.Name = "leftPanelContainer";
            // 
            // leftPanelContainer.Panel1
            // 
            this.leftPanelContainer.Panel1.Controls.Add(this.tcLeft);
            this.leftPanelContainer.Panel1Collapsed = true;
            // 
            // leftPanelContainer.Panel2
            // 
            this.leftPanelContainer.Panel2.Controls.Add(this.rightPanelContainer);
            this.leftPanelContainer.Size = new System.Drawing.Size(764, 579);
            this.leftPanelContainer.SplitterDistance = 138;
            this.leftPanelContainer.TabIndex = 9;
            // 
            // tcLeft
            // 
            this.tcLeft.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.tcLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcLeft.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tcLeft.IgnoreFirst = false;
            this.tcLeft.Location = new System.Drawing.Point(0, 0);
            this.tcLeft.Name = "tcLeft";
            this.tcLeft.SelectedIndex = 0;
            this.tcLeft.Size = new System.Drawing.Size(134, 96);
            this.tcLeft.TabIndex = 1;
            this.tcLeft.ControlAdded += new System.Windows.Forms.ControlEventHandler(this.tcPanel1_ControlAdded);
            this.tcLeft.ControlRemoved += new System.Windows.Forms.ControlEventHandler(this.tcPanel1_ControlRemoved);
            // 
            // bottomPanelContainer
            // 
            this.bottomPanelContainer.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.bottomPanelContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.bottomPanelContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bottomPanelContainer.Location = new System.Drawing.Point(0, 54);
            this.bottomPanelContainer.Name = "bottomPanelContainer";
            this.bottomPanelContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // bottomPanelContainer.Panel1
            // 
            this.bottomPanelContainer.Panel1.Controls.Add(this.topPanelContainer);
            // 
            // bottomPanelContainer.Panel2
            // 
            this.bottomPanelContainer.Panel2.Controls.Add(this.tcBottom);
            this.bottomPanelContainer.Panel2Collapsed = true;
            this.bottomPanelContainer.Size = new System.Drawing.Size(764, 579);
            this.bottomPanelContainer.SplitterDistance = 520;
            this.bottomPanelContainer.TabIndex = 7;
            // 
            // tcBottom
            // 
            this.tcBottom.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.tcBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcBottom.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tcBottom.IgnoreFirst = false;
            this.tcBottom.Location = new System.Drawing.Point(0, 0);
            this.tcBottom.Name = "tcBottom";
            this.tcBottom.SelectedIndex = 0;
            this.tcBottom.Size = new System.Drawing.Size(146, 42);
            this.tcBottom.TabIndex = 1;
            this.tcBottom.ControlAdded += new System.Windows.Forms.ControlEventHandler(this.tcPanel2_ControlAdded);
            this.tcBottom.ControlRemoved += new System.Windows.Forms.ControlEventHandler(this.tcPanel2_ControlRemoved);
            // 
            // elementHost2
            // 
            this.elementHost2.Dock = System.Windows.Forms.DockStyle.Top;
            this.elementHost2.Location = new System.Drawing.Point(0, 24);
            this.elementHost2.Name = "elementHost2";
            this.elementHost2.Size = new System.Drawing.Size(764, 30);
            this.elementHost2.TabIndex = 10;
            this.elementHost2.Text = "elementHost2";
            this.elementHost2.Child = this.toolbarControl1;
            // 
            // MainGlueWindow
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(764, 633);
            this.Controls.Add(this.bottomPanelContainer);
            this.Controls.Add(this.elementHost2);
            this.Controls.Add(this.mMenu);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.mMenu;
            this.Name = "MainGlueWindow";
            this.Text = "FlatRedBall Glue";
            this.Activated += new System.EventHandler(this.Form1_Activated);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.mElementContextMenu.ResumeLayout(false);
            this.mMenu.ResumeLayout(false);
            this.mMenu.PerformLayout();
            this.rightPanelContainer.Panel1.ResumeLayout(false);
            this.rightPanelContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.rightPanelContainer)).EndInit();
            this.rightPanelContainer.ResumeLayout(false);
            this.MainTabControl.ResumeLayout(false);
            this.tcRight.ResumeLayout(false);
            this.topPanelContainer.Panel1.ResumeLayout(false);
            this.topPanelContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.topPanelContainer)).EndInit();
            this.topPanelContainer.ResumeLayout(false);
            this.leftPanelContainer.Panel1.ResumeLayout(false);
            this.leftPanelContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.leftPanelContainer)).EndInit();
            this.leftPanelContainer.ResumeLayout(false);
            this.bottomPanelContainer.Panel1.ResumeLayout(false);
            this.bottomPanelContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.bottomPanelContainer)).EndInit();
            this.bottomPanelContainer.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

        private System.Windows.Forms.MenuStrip mMenu;
        public System.Windows.Forms.ImageList ElementImages;
        internal System.Windows.Forms.ContextMenuStrip mElementContextMenu;

        public System.Windows.Forms.SplitContainer rightPanelContainer;
        private System.Windows.Forms.SplitContainer topPanelContainer;
        private System.Windows.Forms.SplitContainer bottomPanelContainer;
        private System.Windows.Forms.SplitContainer leftPanelContainer;
        private FlatRedBall.Glue.Controls.TabControlEx tcRight;
        private FlatRedBall.Glue.Controls.TabControlEx tcTop;
        private FlatRedBall.Glue.Controls.TabControlEx tcLeft;
        private FlatRedBall.Glue.Controls.TabControlEx tcBottom;
        internal FlatRedBall.Glue.Controls.TabControlEx MainTabControl;
        private System.Windows.Forms.Integration.ElementHost elementHost2;
        private FlatRedBall.Glue.Controls.ToolbarControl toolbarControl1;
    }
}

