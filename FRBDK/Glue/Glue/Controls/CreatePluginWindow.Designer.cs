using L = Localization;

namespace FlatRedBall.Glue.Controls;

partial class CreatePluginWindow
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
        this.FromFolderLabel = new System.Windows.Forms.Label();
        this.FromFolderTextBox = new System.Windows.Forms.TextBox();
        this.FromFolderButton = new System.Windows.Forms.Button();
        this.btnOk = new System.Windows.Forms.Button();
        this.btnCancel = new System.Windows.Forms.Button();
        this.fbdPath = new System.Windows.Forms.FolderBrowserDialog();
        this.sfdPlugin = new System.Windows.Forms.SaveFileDialog();
        this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
        this.CodeFilesRadioButton = new System.Windows.Forms.RadioButton();
        this.AllFilesRadioButton = new System.Windows.Forms.RadioButton();
        this.groupBox1 = new System.Windows.Forms.GroupBox();
        this.InstalledPluginRadioButton = new System.Windows.Forms.RadioButton();
        this.FromFolderRadioButton = new System.Windows.Forms.RadioButton();
        this.FromInstalledPluginComboBox = new System.Windows.Forms.ComboBox();
        this.flowLayoutPanel1.SuspendLayout();
        this.groupBox1.SuspendLayout();
        this.SuspendLayout();
        // 
        // FromFolderLabel
        // 
        this.FromFolderLabel.AutoSize = true;
        this.FromFolderLabel.Location = new System.Drawing.Point(7, 84);
        this.FromFolderLabel.Name = "FromFolderLabel";
        this.FromFolderLabel.Size = new System.Drawing.Size(32, 13);
        this.FromFolderLabel.TabIndex = 2;
        this.FromFolderLabel.Text = L.Texts.Path;
        // 
        // FromFolderTextBox
        // 
        this.FromFolderTextBox.Location = new System.Drawing.Point(45, 81);
        this.FromFolderTextBox.Name = "FromFolderTextBox";
        this.FromFolderTextBox.Size = new System.Drawing.Size(132, 20);
        this.FromFolderTextBox.TabIndex = 3;
        // 
        // FromFolderButton
        // 
        this.FromFolderButton.Location = new System.Drawing.Point(183, 80);
        this.FromFolderButton.Name = "FromFolderButton";
        this.FromFolderButton.Size = new System.Drawing.Size(33, 23);
        this.FromFolderButton.TabIndex = 4;
        this.FromFolderButton.Text = "...";
        this.FromFolderButton.UseVisualStyleBackColor = true;
        this.FromFolderButton.Click += new System.EventHandler(this.btnPath_Click);
        // 
        // btnOk
        // 
        this.btnOk.Location = new System.Drawing.Point(10, 136);
        this.btnOk.Name = "btnOk";
        this.btnOk.Size = new System.Drawing.Size(100, 23);
        this.btnOk.TabIndex = 5;
        this.btnOk.Text = L.Texts.Ok;
        this.btnOk.UseVisualStyleBackColor = true;
        this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
        // 
        // btnCancel
        // 
        this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnCancel.Location = new System.Drawing.Point(116, 136);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(100, 23);
        this.btnCancel.TabIndex = 6;
        this.btnCancel.Text = "&Cancel";
        this.btnCancel.UseVisualStyleBackColor = true;
        this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
        // 
        // sfdPlugin
        // 
        this.sfdPlugin.DefaultExt = "plug";
        this.sfdPlugin.Filter = $"{L.Texts.PluginFiles}|*.plug";
        // 
        // flowLayoutPanel1
        // 
        this.flowLayoutPanel1.AutoSize = true;
        this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this.flowLayoutPanel1.Controls.Add(this.AllFilesRadioButton);
        this.flowLayoutPanel1.Controls.Add(this.CodeFilesRadioButton);
        this.flowLayoutPanel1.Location = new System.Drawing.Point(10, 107);
        this.flowLayoutPanel1.Name = "flowLayoutPanel1";
        this.flowLayoutPanel1.Size = new System.Drawing.Size(195, 23);
        this.flowLayoutPanel1.TabIndex = 7;
        // 
        // CodeFilesRadioButton
        // 
        this.CodeFilesRadioButton.AutoSize = true;
        this.CodeFilesRadioButton.Location = new System.Drawing.Point(96, 3);
        this.CodeFilesRadioButton.Margin = new System.Windows.Forms.Padding(30, 3, 3, 3);
        this.CodeFilesRadioButton.Name = "CodeFilesRadioButton";
        this.CodeFilesRadioButton.Size = new System.Drawing.Size(96, 17);
        this.CodeFilesRadioButton.TabIndex = 0;
        this.CodeFilesRadioButton.Text = L.Texts.OnlyCodeFiles;
        this.CodeFilesRadioButton.UseVisualStyleBackColor = true;
        // 
        // AllFilesRadioButton
        // 
        this.AllFilesRadioButton.AutoSize = true;
        this.AllFilesRadioButton.Checked = true;
        this.AllFilesRadioButton.Location = new System.Drawing.Point(3, 3);
        this.AllFilesRadioButton.Name = "AllFilesRadioButton";
        this.AllFilesRadioButton.Size = new System.Drawing.Size(60, 17);
        this.AllFilesRadioButton.TabIndex = 1;
        this.AllFilesRadioButton.TabStop = true;
        this.AllFilesRadioButton.Text = L.Texts.FilesAll;
        this.AllFilesRadioButton.UseVisualStyleBackColor = true;
        // 
        // groupBox1
        // 
        this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                                                                      | System.Windows.Forms.AnchorStyles.Right)));
        this.groupBox1.Controls.Add(this.FromFolderRadioButton);
        this.groupBox1.Controls.Add(this.InstalledPluginRadioButton);
        this.groupBox1.Location = new System.Drawing.Point(10, 3);
        this.groupBox1.Name = "groupBox1";
        this.groupBox1.Size = new System.Drawing.Size(206, 72);
        this.groupBox1.TabIndex = 9;
        this.groupBox1.TabStop = false;
        this.groupBox1.Text = L.Texts.PluginSource;
        // 
        // InstalledPluginRadioButton
        // 
        this.InstalledPluginRadioButton.AutoSize = true;
        this.InstalledPluginRadioButton.Checked = true;
        this.InstalledPluginRadioButton.Location = new System.Drawing.Point(6, 19);
        this.InstalledPluginRadioButton.Name = "InstalledPluginRadioButton";
        this.InstalledPluginRadioButton.Size = new System.Drawing.Size(122, 17);
        this.InstalledPluginRadioButton.TabIndex = 0;
        this.InstalledPluginRadioButton.TabStop = true;
        this.InstalledPluginRadioButton.Text = L.Texts.PluginFromInstalled;
        this.InstalledPluginRadioButton.UseVisualStyleBackColor = true;
        this.InstalledPluginRadioButton.CheckedChanged += new System.EventHandler(this.InstalledPluginRadioButton_CheckedChanged);
        // 
        // FromFolderRadioButton
        // 
        this.FromFolderRadioButton.AutoSize = true;
        this.FromFolderRadioButton.Location = new System.Drawing.Point(6, 42);
        this.FromFolderRadioButton.Name = "FromFolderRadioButton";
        this.FromFolderRadioButton.Size = new System.Drawing.Size(80, 17);
        this.FromFolderRadioButton.TabIndex = 1;
        this.FromFolderRadioButton.Text = L.Texts.FromFolder;
        this.FromFolderRadioButton.UseVisualStyleBackColor = true;
        this.FromFolderRadioButton.CheckedChanged += new System.EventHandler(this.FromFolderRadioButton_CheckedChanged);
        // 
        // FromInstalledPluginComboBox
        // 
        this.FromInstalledPluginComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.FromInstalledPluginComboBox.FormattingEnabled = true;
        this.FromInstalledPluginComboBox.Location = new System.Drawing.Point(10, 80);
        this.FromInstalledPluginComboBox.Name = "FromInstalledPluginComboBox";
        this.FromInstalledPluginComboBox.Size = new System.Drawing.Size(206, 21);
        this.FromInstalledPluginComboBox.TabIndex = 10;
        // 
        // CreatePluginWindow
        // 
        this.AcceptButton = this.btnOk;
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.AutoSize = true;
        this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this.CancelButton = this.btnCancel;
        this.ClientSize = new System.Drawing.Size(221, 170);
        this.Controls.Add(this.groupBox1);
        this.Controls.Add(this.flowLayoutPanel1);
        this.Controls.Add(this.FromFolderButton);
        this.Controls.Add(this.btnCancel);
        this.Controls.Add(this.btnOk);
        this.Controls.Add(this.FromFolderTextBox);
        this.Controls.Add(this.FromFolderLabel);
        this.Controls.Add(this.FromInstalledPluginComboBox);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "CreatePluginWindow";
        this.ShowIcon = false;
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = L.Texts.PluginCreate;
        this.flowLayoutPanel1.ResumeLayout(false);
        this.flowLayoutPanel1.PerformLayout();
        this.groupBox1.ResumeLayout(false);
        this.groupBox1.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Label FromFolderLabel;
    private System.Windows.Forms.TextBox FromFolderTextBox;
    private System.Windows.Forms.Button FromFolderButton;
    private System.Windows.Forms.Button btnOk;
    private System.Windows.Forms.Button btnCancel;
    private System.Windows.Forms.FolderBrowserDialog fbdPath;
    private System.Windows.Forms.SaveFileDialog sfdPlugin;
    private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
    private System.Windows.Forms.RadioButton CodeFilesRadioButton;
    private System.Windows.Forms.RadioButton AllFilesRadioButton;
    private System.Windows.Forms.GroupBox groupBox1;
    private System.Windows.Forms.RadioButton FromFolderRadioButton;
    private System.Windows.Forms.RadioButton InstalledPluginRadioButton;
    private System.Windows.Forms.ComboBox FromInstalledPluginComboBox;
}