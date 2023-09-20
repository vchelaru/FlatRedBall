using L = Localization;

namespace FlatRedBall.Glue.Controls;

partial class ProjectSpecificFileCollectionEditorForm
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
        this.lbFiles = new System.Windows.Forms.ListBox();
        this.btnRemove = new System.Windows.Forms.Button();
        this.cboProjectType = new System.Windows.Forms.ComboBox();
        this.label1 = new System.Windows.Forms.Label();
        this.btnAddNewFile = new System.Windows.Forms.Button();
        this.btnAddExistingFile = new System.Windows.Forms.Button();
        this.TabControl = new System.Windows.Forms.TabControl();
        this.AddFileTab = new System.Windows.Forms.TabPage();
        this.RemoveFileTab = new System.Windows.Forms.TabPage();
        this.DoneButton = new System.Windows.Forms.Button();
        this.TabControl.SuspendLayout();
        this.AddFileTab.SuspendLayout();
        this.RemoveFileTab.SuspendLayout();
        this.SuspendLayout();
        // 
        // lbFiles
        // 
        this.lbFiles.Dock = System.Windows.Forms.DockStyle.Fill;
        this.lbFiles.FormattingEnabled = true;
        this.lbFiles.Location = new System.Drawing.Point(3, 3);
        this.lbFiles.Name = "lbFiles";
        this.lbFiles.Size = new System.Drawing.Size(253, 33);
        this.lbFiles.TabIndex = 0;
        this.lbFiles.SelectedIndexChanged += new System.EventHandler(this.LbFilesSelectedIndexChanged);
        // 
        // btnRemove
        // 
        this.btnRemove.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.btnRemove.Enabled = false;
        this.btnRemove.Location = new System.Drawing.Point(3, 36);
        this.btnRemove.Name = "btnRemove";
        this.btnRemove.Size = new System.Drawing.Size(253, 23);
        this.btnRemove.TabIndex = 2;
        this.btnRemove.Text = L.Texts.Remove;
        this.btnRemove.UseVisualStyleBackColor = true;
        this.btnRemove.Click += new System.EventHandler(this.BtnRemoveClick);
        // 
        // cboProjectType
        // 
        this.cboProjectType.FormattingEnabled = true;
        this.cboProjectType.Location = new System.Drawing.Point(82, 7);
        this.cboProjectType.Name = "cboProjectType";
        this.cboProjectType.Size = new System.Drawing.Size(171, 21);
        this.cboProjectType.TabIndex = 3;
        // 
        // label1
        // 
        this.label1.AutoSize = true;
        this.label1.Location = new System.Drawing.Point(9, 10);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(67, 13);
        this.label1.TabIndex = 4;
        this.label1.Text = L.Texts.ProjectType;
        // 
        // btnAddNewFile
        // 
        this.btnAddNewFile.Location = new System.Drawing.Point(3, 34);
        this.btnAddNewFile.Name = "btnAddNewFile";
        this.btnAddNewFile.Size = new System.Drawing.Size(122, 23);
        this.btnAddNewFile.TabIndex = 5;
        this.btnAddNewFile.Text = L.Texts.FileNewAdd;
        this.btnAddNewFile.UseVisualStyleBackColor = true;
        this.btnAddNewFile.Click += new System.EventHandler(this.BtnAddNewFileClick);
        // 
        // btnAddExistingFile
        // 
        this.btnAddExistingFile.Location = new System.Drawing.Point(129, 34);
        this.btnAddExistingFile.Name = "btnAddExistingFile";
        this.btnAddExistingFile.Size = new System.Drawing.Size(124, 23);
        this.btnAddExistingFile.TabIndex = 6;
        this.btnAddExistingFile.Text = L.Texts.FileExistingAdd;
        this.btnAddExistingFile.UseVisualStyleBackColor = true;
        this.btnAddExistingFile.Click += new System.EventHandler(this.BtnAddExistingFileClick);
        // 
        // TabControl
        // 
        this.TabControl.Controls.Add(this.AddFileTab);
        this.TabControl.Controls.Add(this.RemoveFileTab);
        this.TabControl.Dock = System.Windows.Forms.DockStyle.Fill;
        this.TabControl.Location = new System.Drawing.Point(0, 0);
        this.TabControl.Name = "TabControl";
        this.TabControl.SelectedIndex = 0;
        this.TabControl.Size = new System.Drawing.Size(267, 88);
        this.TabControl.TabIndex = 7;
        // 
        // AddFileTab
        // 
        this.AddFileTab.Controls.Add(this.cboProjectType);
        this.AddFileTab.Controls.Add(this.btnAddExistingFile);
        this.AddFileTab.Controls.Add(this.btnAddNewFile);
        this.AddFileTab.Controls.Add(this.label1);
        this.AddFileTab.Location = new System.Drawing.Point(4, 22);
        this.AddFileTab.Name = "AddFileTab";
        this.AddFileTab.Padding = new System.Windows.Forms.Padding(3);
        this.AddFileTab.Size = new System.Drawing.Size(259, 62);
        this.AddFileTab.TabIndex = 1;
        this.AddFileTab.Text = L.Texts.FileAdd;
        this.AddFileTab.UseVisualStyleBackColor = true;
        // 
        // RemoveFileTab
        // 
        this.RemoveFileTab.Controls.Add(this.lbFiles);
        this.RemoveFileTab.Controls.Add(this.btnRemove);
        this.RemoveFileTab.Location = new System.Drawing.Point(4, 22);
        this.RemoveFileTab.Name = "RemoveFileTab";
        this.RemoveFileTab.Padding = new System.Windows.Forms.Padding(3);
        this.RemoveFileTab.Size = new System.Drawing.Size(259, 62);
        this.RemoveFileTab.TabIndex = 0;
        this.RemoveFileTab.Text = L.Texts.FileRemove;
        this.RemoveFileTab.UseVisualStyleBackColor = true;
        // 
        // DoneButton
        // 
        this.DoneButton.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.DoneButton.Location = new System.Drawing.Point(0, 88);
        this.DoneButton.Name = "DoneButton";
        this.DoneButton.Size = new System.Drawing.Size(267, 23);
        this.DoneButton.TabIndex = 8;
        this.DoneButton.Text = L.Texts.Done;
        this.DoneButton.UseVisualStyleBackColor = true;
        this.DoneButton.Click += new System.EventHandler(this.DoneButton_Click);
        // 
        // ProjectSpecificFileCollectionEditorForm
        // 
        this.AcceptButton = this.DoneButton;
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(267, 111);
        this.Controls.Add(this.TabControl);
        this.Controls.Add(this.DoneButton);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.MinimumSize = new System.Drawing.Size(280, 120);
        this.Name = "ProjectSpecificFileCollectionEditorForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = L.Texts.ProjectSpecificFiles;
        this.Load += new System.EventHandler(this.ProjectSpecificFileCollectionEditorFormLoad);
        this.TabControl.ResumeLayout(false);
        this.AddFileTab.ResumeLayout(false);
        this.AddFileTab.PerformLayout();
        this.RemoveFileTab.ResumeLayout(false);
        this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.ListBox lbFiles;
    private System.Windows.Forms.Button btnRemove;
    private System.Windows.Forms.ComboBox cboProjectType;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Button btnAddNewFile;
    private System.Windows.Forms.Button btnAddExistingFile;
    private System.Windows.Forms.TabControl TabControl;
    private System.Windows.Forms.TabPage RemoveFileTab;
    private System.Windows.Forms.TabPage AddFileTab;
    private System.Windows.Forms.Button DoneButton;
}