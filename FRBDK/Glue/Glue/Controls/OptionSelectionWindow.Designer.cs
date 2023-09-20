using L = Localization;
namespace FlatRedBall.Glue.Controls;

partial class OptionSelectionWindow
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
        this.mCancelButton = new System.Windows.Forms.Button();
        this.mOkWindow = new System.Windows.Forms.Button();
        this.mOptionsComboBox = new System.Windows.Forms.ComboBox();
        this.label1 = new System.Windows.Forms.Label();
        this.SuspendLayout();
        // 
        // mCancelButton
        // 
        this.mCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.mCancelButton.Location = new System.Drawing.Point(81, 57);
        this.mCancelButton.Name = "mCancelButton";
        this.mCancelButton.Size = new System.Drawing.Size(71, 34);
        this.mCancelButton.TabIndex = 5;
        this.mCancelButton.Text = L.Texts.Cancel;
        this.mCancelButton.UseVisualStyleBackColor = true;
        // 
        // mOkWindow
        // 
        this.mOkWindow.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.mOkWindow.Location = new System.Drawing.Point(4, 57);
        this.mOkWindow.Name = "mOkWindow";
        this.mOkWindow.Size = new System.Drawing.Size(71, 34);
        this.mOkWindow.TabIndex = 4;
        this.mOkWindow.Text = L.Texts.Ok;
        this.mOkWindow.UseVisualStyleBackColor = true;
        // 
        // mOptionsComboBox
        // 
        this.mOptionsComboBox.FormattingEnabled = true;
        this.mOptionsComboBox.Location = new System.Drawing.Point(7, 32);
        this.mOptionsComboBox.Name = "mOptionsComboBox";
        this.mOptionsComboBox.Size = new System.Drawing.Size(143, 21);
        this.mOptionsComboBox.TabIndex = 6;
        // 
        // label1
        // 
        this.label1.AutoSize = true;
        this.label1.Location = new System.Drawing.Point(5, 14);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(35, 13);
        this.label1.TabIndex = 7;
        this.label1.Text = "label1";
        // 
        // OptionSelectionWindow
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(157, 96);
        this.Controls.Add(this.label1);
        this.Controls.Add(this.mOptionsComboBox);
        this.Controls.Add(this.mCancelButton);
        this.Controls.Add(this.mOkWindow);
        this.Name = "OptionSelectionWindow";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "OptionSelectionWindow";
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Button mCancelButton;
    private System.Windows.Forms.Button mOkWindow;
    private System.Windows.Forms.ComboBox mOptionsComboBox;
    private System.Windows.Forms.Label label1;
}