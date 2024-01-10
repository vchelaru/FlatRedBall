using L = Localization;
namespace FlatRedBall.Glue.Controls;

partial class InstallPluginWindow
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
        this.cbInstallType = new System.Windows.Forms.ComboBox();
        this.label1 = new System.Windows.Forms.Label();
        this.label2 = new System.Windows.Forms.Label();
        this.tbPath = new System.Windows.Forms.TextBox();
        this.btnPath = new System.Windows.Forms.Button();
        this.ofdPlugin = new System.Windows.Forms.OpenFileDialog();
        this.btnOk = new System.Windows.Forms.Button();
        this.btnCancel = new System.Windows.Forms.Button();
        this.SuspendLayout();
        // 
        // cbInstallType
        // 
        this.cbInstallType.FormattingEnabled = true;
        this.cbInstallType.Items.AddRange(new object[] {
            L.Texts.ProjectForCurrent,
            L.Texts.UserFor
        });
        this.cbInstallType.Location = new System.Drawing.Point(84, 6);
        this.cbInstallType.Name = "cbInstallType";
        this.cbInstallType.Size = new System.Drawing.Size(219, 21);
        this.cbInstallType.TabIndex = 0;
        this.cbInstallType.Text = L.Texts.ProjectForCurrent;
        // 
        // label1
        // 
        this.label1.AutoSize = true;
        this.label1.Location = new System.Drawing.Point(12, 9);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(64, 13);
        this.label1.TabIndex = 1;
        this.label1.Text = L.Texts.TypeInstall;
        // 
        // label2
        // 
        this.label2.AutoSize = true;
        this.label2.Location = new System.Drawing.Point(46, 36);
        this.label2.Name = "label2";
        this.label2.Size = new System.Drawing.Size(32, 13);
        this.label2.TabIndex = 2;
        this.label2.Text = L.Texts.Path;
        // 
        // tbPath
        // 
        this.tbPath.Location = new System.Drawing.Point(84, 33);
        this.tbPath.Name = "tbPath";
        this.tbPath.Size = new System.Drawing.Size(187, 20);
        this.tbPath.TabIndex = 3;
        // 
        // btnPath
        // 
        this.btnPath.Location = new System.Drawing.Point(277, 33);
        this.btnPath.Name = "btnPath";
        this.btnPath.Size = new System.Drawing.Size(26, 20);
        this.btnPath.TabIndex = 4;
        this.btnPath.Text = "...";
        this.btnPath.UseVisualStyleBackColor = true;
        this.btnPath.Click += new System.EventHandler(this.btnPath_Click);
        // 
        // ofdPlugin
        // 
        this.ofdPlugin.Filter = $"{L.Texts.PluginFiles}|*.plug";
        // 
        // btnOk
        // 
        this.btnOk.Location = new System.Drawing.Point(12, 59);
        this.btnOk.Name = "btnOk";
        this.btnOk.Size = new System.Drawing.Size(139, 23);
        this.btnOk.TabIndex = 5;
        this.btnOk.Text = L.Texts.Ok;
        this.btnOk.UseVisualStyleBackColor = true;
        this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
        // 
        // btnCancel
        // 
        this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnCancel.Location = new System.Drawing.Point(164, 59);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(139, 23);
        this.btnCancel.TabIndex = 6;
        this.btnCancel.Text = L.Texts.Cancel;
        this.btnCancel.UseVisualStyleBackColor = true;
        this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
        // 
        // InstallPluginWindow
        // 
        this.AcceptButton = this.btnOk;
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.CancelButton = this.btnCancel;
        this.ClientSize = new System.Drawing.Size(311, 91);
        this.Controls.Add(this.btnCancel);
        this.Controls.Add(this.btnOk);
        this.Controls.Add(this.btnPath);
        this.Controls.Add(this.tbPath);
        this.Controls.Add(this.label2);
        this.Controls.Add(this.label1);
        this.Controls.Add(this.cbInstallType);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "InstallPluginWindow";
        this.ShowIcon = false;
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = L.Texts.PluginInstall;
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.ComboBox cbInstallType;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.TextBox tbPath;
    private System.Windows.Forms.Button btnPath;
    private System.Windows.Forms.OpenFileDialog ofdPlugin;
    private System.Windows.Forms.Button btnOk;
    private System.Windows.Forms.Button btnCancel;
}