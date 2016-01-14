namespace FlatRedBall.Glue.Controls
{
    partial class CharacterListCreationWindow
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.AllFilesList = new System.Windows.Forms.TreeView();
            this.SelectedFileList = new System.Windows.Forms.TreeView();
            this.GetCharacterListButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.AllFilesList);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.SelectedFileList);
            this.splitContainer1.Size = new System.Drawing.Size(539, 323);
            this.splitContainer1.SplitterDistance = 243;
            this.splitContainer1.TabIndex = 0;
            // 
            // AllFilesList
            // 
            this.AllFilesList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AllFilesList.Location = new System.Drawing.Point(0, 0);
            this.AllFilesList.Name = "AllFilesList";
            this.AllFilesList.Size = new System.Drawing.Size(243, 323);
            this.AllFilesList.TabIndex = 0;
            this.AllFilesList.DoubleClick += new System.EventHandler(this.AllFilesList_DoubleClick);
            // 
            // SelectedFileList
            // 
            this.SelectedFileList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SelectedFileList.Location = new System.Drawing.Point(0, 0);
            this.SelectedFileList.Name = "SelectedFileList";
            this.SelectedFileList.Size = new System.Drawing.Size(292, 323);
            this.SelectedFileList.TabIndex = 0;
            this.SelectedFileList.DoubleClick += new System.EventHandler(this.SelectedFileList_DoubleClick);
            // 
            // GetCharacterListButton
            // 
            this.GetCharacterListButton.Location = new System.Drawing.Point(413, 329);
            this.GetCharacterListButton.Name = "GetCharacterListButton";
            this.GetCharacterListButton.Size = new System.Drawing.Size(125, 27);
            this.GetCharacterListButton.TabIndex = 1;
            this.GetCharacterListButton.Text = "GetCharacterList";
            this.GetCharacterListButton.UseVisualStyleBackColor = true;
            this.GetCharacterListButton.Click += new System.EventHandler(this.GetCharacterListButton_Click);
            // 
            // CharacterListCreationWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(539, 357);
            this.Controls.Add(this.GetCharacterListButton);
            this.Controls.Add(this.splitContainer1);
            this.Name = "CharacterListCreationWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "CharacterListCreationWindow";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView AllFilesList;
        private System.Windows.Forms.TreeView SelectedFileList;
        private System.Windows.Forms.Button GetCharacterListButton;

    }
}