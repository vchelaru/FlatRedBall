using L = Localization;

namespace FlatRedBall.Glue.Controls;

partial class PerformanceSettingsWindow
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
        this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
        this.DoneButton = new System.Windows.Forms.Button();
        this.SuspendLayout();
        // 
        // propertyGrid1
        // 
        this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
        this.propertyGrid1.HelpVisible = false;
        this.propertyGrid1.Location = new System.Drawing.Point(0, 0);
        this.propertyGrid1.Name = "propertyGrid1";
        this.propertyGrid1.Size = new System.Drawing.Size(292, 236);
        this.propertyGrid1.TabIndex = 0;
        this.propertyGrid1.ToolbarVisible = false;
        this.propertyGrid1.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propertyGrid1_PropertyValueChanged);
        this.propertyGrid1.Click += new System.EventHandler(this.propertyGrid1_Click);
        // 
        // DoneButton
        // 
        this.DoneButton.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.DoneButton.Location = new System.Drawing.Point(0, 236);
        this.DoneButton.Name = "DoneButton";
        this.DoneButton.Size = new System.Drawing.Size(292, 37);
        this.DoneButton.TabIndex = 1;
        this.DoneButton.Text = L.Texts.Done;
        this.DoneButton.UseVisualStyleBackColor = true;
        this.DoneButton.Click += new System.EventHandler(this.DoneButton_Click);
        // 
        // PerformanceSettingsWindow
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(292, 273);
        this.Controls.Add(this.propertyGrid1);
        this.Controls.Add(this.DoneButton);
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "PerformanceSettingsWindow";
        this.ShowIcon = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = L.Texts.PerformanceSettings;
        this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.PropertyGrid propertyGrid1;
    private System.Windows.Forms.Button DoneButton;
}