using L = Localization;
namespace FlatRedBall.Glue.Controls;

partial class ComboBoxMessageBox
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
        this.label1 = new System.Windows.Forms.Label();
        this.comboBox1 = new System.Windows.Forms.ComboBox();
        this.OkButton = new System.Windows.Forms.Button();
        this.CancelButton = new System.Windows.Forms.Button();
        this.SuspendLayout();
        // 
        // label1
        // 
        this.label1.Location = new System.Drawing.Point(12, 9);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(260, 86);
        this.label1.TabIndex = 0;
        this.label1.Text = "label1";
        // 
        // comboBox1
        // 
        this.comboBox1.FormattingEnabled = true;
        this.comboBox1.Location = new System.Drawing.Point(15, 98);
        this.comboBox1.Name = "comboBox1";
        this.comboBox1.Size = new System.Drawing.Size(257, 21);
        this.comboBox1.TabIndex = 1;
        // 
        // OkButton
        // 
        this.OkButton.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.OkButton.Location = new System.Drawing.Point(15, 125);
        this.OkButton.Name = "OkButton";
        this.OkButton.Size = new System.Drawing.Size(125, 23);
        this.OkButton.TabIndex = 2;
        this.OkButton.Text = L.Texts.Ok;
        this.OkButton.UseVisualStyleBackColor = true;
        // 
        // CancelButton
        // 
        this.CancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.CancelButton.Location = new System.Drawing.Point(147, 125);
        this.CancelButton.Name = "CancelButton";
        this.CancelButton.Size = new System.Drawing.Size(125, 23);
        this.CancelButton.TabIndex = 3;
        this.CancelButton.Text = L.Texts.Cancel;
        this.CancelButton.UseVisualStyleBackColor = true;
        // 
        // ComboBoxMessageBox
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(284, 157);
        this.Controls.Add(this.CancelButton);
        this.Controls.Add(this.OkButton);
        this.Controls.Add(this.comboBox1);
        this.Controls.Add(this.label1);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "ComboBoxMessageBox";
        this.ShowIcon = false;
        this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.ComboBox comboBox1;
    private System.Windows.Forms.Button OkButton;
    private System.Windows.Forms.Button CancelButton;
}