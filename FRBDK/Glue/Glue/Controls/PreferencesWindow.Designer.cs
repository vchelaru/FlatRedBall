using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using L = Localization;

namespace FlatRedBall.Glue.Controls;

partial class PreferencesWindow
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
        this.OkButton = new System.Windows.Forms.Button();
        this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
        this.cbChildExternalApplications = new System.Windows.Forms.CheckBox();
        this.GenerateTombstoningCodeCheckBox = new System.Windows.Forms.CheckBox();
        this.ShowHiddenNodesCheckBox = new System.Windows.Forms.CheckBox();
        this.LanguageComboxBox = new System.Windows.Forms.ComboBox();

        this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
        this.flowLayoutPanel1.SuspendLayout();
        this.flowLayoutPanel2.SuspendLayout();
        this.SuspendLayout();
        // 
        // OkButton
        // 
        this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
        this.OkButton.Location = new System.Drawing.Point(3, 78);
        this.OkButton.Name = "OkButton";
        this.OkButton.Size = new System.Drawing.Size(238, 22);
        this.OkButton.TabIndex = 1;
        this.OkButton.Text = "OK";
        this.OkButton.UseVisualStyleBackColor = true;
        this.OkButton.Click += new System.EventHandler(this.OkButtonClick);
        // 
        // flowLayoutPanel1
        // 
        this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                                                                              | System.Windows.Forms.AnchorStyles.Left)
                                                                             | System.Windows.Forms.AnchorStyles.Right)));
        this.flowLayoutPanel1.AutoSize = true;
        this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this.flowLayoutPanel1.Controls.Add(this.cbChildExternalApplications);
        this.flowLayoutPanel1.Controls.Add(this.GenerateTombstoningCodeCheckBox);
        this.flowLayoutPanel1.Controls.Add(this.ShowHiddenNodesCheckBox);
        this.flowLayoutPanel1.Controls.Add(this.LanguageComboxBox);
        this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
        this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 3);
        this.flowLayoutPanel1.Name = "flowLayoutPanel1";
        this.flowLayoutPanel1.Size = new System.Drawing.Size(238, 69);
        this.flowLayoutPanel1.TabIndex = 2;
        // 
        // cbChildExternalApplications
        // 
        this.cbChildExternalApplications.AutoSize = true;
        this.cbChildExternalApplications.Location = new System.Drawing.Point(3, 3);
        this.cbChildExternalApplications.Name = "cbChildExternalApplications";
        this.cbChildExternalApplications.Size = new System.Drawing.Size(232, 17);
        this.cbChildExternalApplications.TabIndex = 0;
        this.cbChildExternalApplications.Text = L.Texts.OpenChildNewWindow;
        this.cbChildExternalApplications.UseVisualStyleBackColor = true;
        this.cbChildExternalApplications.CheckedChanged += new System.EventHandler(this.CbChildExternalApplicationsCheckedChanged);
        // 
        // GenerateTombstoningCodeCheckBox
        // 
        this.GenerateTombstoningCodeCheckBox.AutoSize = true;
        this.GenerateTombstoningCodeCheckBox.Location = new System.Drawing.Point(3, 26);
        this.GenerateTombstoningCodeCheckBox.Name = "GenerateTombstoningCodeCheckBox";
        this.GenerateTombstoningCodeCheckBox.Size = new System.Drawing.Size(162, 17);
        this.GenerateTombstoningCodeCheckBox.TabIndex = 1;
        this.GenerateTombstoningCodeCheckBox.Text = L.Texts.GenerateTombstoningCode;
        this.GenerateTombstoningCodeCheckBox.UseVisualStyleBackColor = true;
        this.GenerateTombstoningCodeCheckBox.CheckedChanged += new System.EventHandler(this.GenerateTombstoningCodeCheckBoxCheckedChanged);
        //
        // LanguageComboxBox
        //
        this.LanguageComboxBox.Name = "LanguageComboxBox";
        this.LanguageComboxBox.Items.Insert(0, L.Texts.English);
        this.LanguageComboxBox.Items.Insert(1, L.Texts.French);
        this.LanguageComboxBox.Items.Insert(2, L.Texts.German);
        this.LanguageComboxBox.Items.Insert(3, L.Texts.Dutch);
        this.LanguageComboxBox.SelectedIndex = GlueState.Self.GlueSettingsSave.Culture switch
        {
            "fr" => 1,
            "de" => 2,
            "nl" => 3,
            _ => 0
        };
        this.LanguageComboxBox.Text = L.Texts.EditorLanguage;
        this.LanguageComboxBox.SelectedIndexChanged += LanguageComboxBox_SelectedIndexChanged;

        // 
        // ShowHiddenNodesCheckBox
        // 
        this.ShowHiddenNodesCheckBox.AutoSize = true;
        this.ShowHiddenNodesCheckBox.Location = new System.Drawing.Point(3, 49);
        this.ShowHiddenNodesCheckBox.Name = "ShowHiddenNodesCheckBox";
        this.ShowHiddenNodesCheckBox.Size = new System.Drawing.Size(124, 17);
        this.ShowHiddenNodesCheckBox.TabIndex = 2;
        this.ShowHiddenNodesCheckBox.Text = L.Texts.ShowHiddenNodes;
        this.ShowHiddenNodesCheckBox.UseVisualStyleBackColor = true;
        this.ShowHiddenNodesCheckBox.CheckedChanged += new System.EventHandler(this.ShowHiddenNodesCheckBoxCheckedChanged);
        // 
        // flowLayoutPanel2
        // 
        this.flowLayoutPanel2.AutoSize = true;
        this.flowLayoutPanel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this.flowLayoutPanel2.Controls.Add(this.flowLayoutPanel1);
        this.flowLayoutPanel2.Controls.Add(this.OkButton);
        this.flowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
        this.flowLayoutPanel2.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
        this.flowLayoutPanel2.Location = new System.Drawing.Point(0, 0);
        this.flowLayoutPanel2.Name = "flowLayoutPanel2";
        this.flowLayoutPanel2.Size = new System.Drawing.Size(244, 105);
        this.flowLayoutPanel2.TabIndex = 3;
        // 
        // PreferencesWindow
        // 
        this.AcceptButton = this.OkButton;
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.AutoSize = true;
        this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this.ClientSize = new System.Drawing.Size(244, 105);
        this.ControlBox = false;
        this.Controls.Add(this.flowLayoutPanel2);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "PreferencesWindow";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = L.Texts.Preferences;
        this.Shown += new System.EventHandler(this.PreferencesWindowShown);
        this.flowLayoutPanel1.ResumeLayout(false);
        this.flowLayoutPanel1.PerformLayout();
        this.flowLayoutPanel2.ResumeLayout(false);
        this.flowLayoutPanel2.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    private void LanguageComboxBox_SelectedIndexChanged(object sender, System.EventArgs e)
    { 
        var newCulture = this.LanguageComboxBox.SelectedIndex switch
        {
            1 => new CultureInfo("fr-FR"),
            2 => new CultureInfo("de-DE"),
            3 => new CultureInfo("nl-NL"),
            _ => new CultureInfo("en-US")
        };
        L.Texts.Culture = newCulture;
        GlueState.Self.GlueSettingsSave.CurrentCulture = newCulture;

        Thread.CurrentThread.CurrentCulture = newCulture;
        Thread.CurrentThread.CurrentUICulture = newCulture;
        GlueCommands.Self.GluxCommands.SaveSettings();
    }

    #endregion

    private System.Windows.Forms.Button OkButton;
    private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
    private System.Windows.Forms.CheckBox cbChildExternalApplications;
    private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
    private System.Windows.Forms.CheckBox GenerateTombstoningCodeCheckBox;
    private System.Windows.Forms.CheckBox ShowHiddenNodesCheckBox;
    private System.Windows.Forms.ComboBox LanguageComboxBox;
}