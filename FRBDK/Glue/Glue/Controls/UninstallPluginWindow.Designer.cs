using L = Localization;

namespace FlatRedBall.Glue.Controls;

partial class UninstallPluginWindow
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
        this.btnUninstall = new System.Windows.Forms.Button();
        this.fbdPath = new System.Windows.Forms.FolderBrowserDialog();
        this.sfdPlugin = new System.Windows.Forms.SaveFileDialog();
        this.dgvPlugins = new System.Windows.Forms.DataGridView();
        ((System.ComponentModel.ISupportInitialize)(this.dgvPlugins)).BeginInit();
        this.SuspendLayout();
        // 
        // btnUninstall
        // 
        this.btnUninstall.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.btnUninstall.Location = new System.Drawing.Point(12, 254);
        this.btnUninstall.Name = "btnUninstall";
        this.btnUninstall.Size = new System.Drawing.Size(272, 26);
        this.btnUninstall.TabIndex = 6;
        this.btnUninstall.Text = L.Texts.Uninstall;
        this.btnUninstall.UseVisualStyleBackColor = true;
        this.btnUninstall.Click += new System.EventHandler(this.btnUninstall_Click);
        // 
        // sfdPlugin
        // 
        this.sfdPlugin.DefaultExt = "plug";
        this.sfdPlugin.Filter = $"{L.Texts.PluginFiles}|*.plug";
        // 
        // dgvPlugins
        // 
        this.dgvPlugins.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                                                                        | System.Windows.Forms.AnchorStyles.Left)
                                                                       | System.Windows.Forms.AnchorStyles.Right)));
        this.dgvPlugins.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dgvPlugins.Location = new System.Drawing.Point(12, 12);
        this.dgvPlugins.Name = "dgvPlugins";
        this.dgvPlugins.Size = new System.Drawing.Size(272, 236);
        this.dgvPlugins.TabIndex = 7;
        // 
        // UninstallPluginWindow
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(296, 292);
        this.Controls.Add(this.dgvPlugins);
        this.Controls.Add(this.btnUninstall);
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "UninstallPluginWindow";
        this.ShowIcon = false;
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = L.Texts.PluginUninstall;
        this.Load += new System.EventHandler(this.UninstallPluginWindow_Load);
        ((System.ComponentModel.ISupportInitialize)(this.dgvPlugins)).EndInit();
        this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Button btnUninstall;
    private System.Windows.Forms.FolderBrowserDialog fbdPath;
    private System.Windows.Forms.SaveFileDialog sfdPlugin;
    private System.Windows.Forms.DataGridView dgvPlugins;
}