using L = Localization;

namespace FlatRedBall.Glue.Controls;

partial class VariablesToResetWindow
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
        this.CancelButton = new System.Windows.Forms.Button();
        this.textBox1 = new System.Windows.Forms.TextBox();
        this.SuspendLayout();
        // 
        // OkButton
        // 
        this.OkButton.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.OkButton.Location = new System.Drawing.Point(1, 317);
        this.OkButton.Name = "OkButton";
        this.OkButton.Size = new System.Drawing.Size(179, 27);
        this.OkButton.TabIndex = 1;
        this.OkButton.Text = L.Texts.Ok;
        this.OkButton.UseVisualStyleBackColor = true;
        this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
        // 
        // CancelButton
        // 
        this.CancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.CancelButton.Location = new System.Drawing.Point(200, 317);
        this.CancelButton.Name = "CancelButton";
        this.CancelButton.Size = new System.Drawing.Size(180, 27);
        this.CancelButton.TabIndex = 2;
        this.CancelButton.Text = L.Texts.Cancel;
        this.CancelButton.UseVisualStyleBackColor = true;
        this.CancelButton.Click += new System.EventHandler(this.CancelButton_Click);
        // 
        // textBox1
        // 
        this.textBox1.AcceptsReturn = true;
        this.textBox1.Location = new System.Drawing.Point(1, 2);
        this.textBox1.Multiline = true;
        this.textBox1.Name = "textBox1";
        this.textBox1.Size = new System.Drawing.Size(379, 309);
        this.textBox1.TabIndex = 3;
        // 
        // VariablesToResetWindow
        // 
        this.AcceptButton = this.OkButton;
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(381, 346);
        this.Controls.Add(this.textBox1);
        this.Controls.Add(this.CancelButton);
        this.Controls.Add(this.OkButton);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        this.Name = "VariablesToResetWindow";
        this.ShowIcon = false;
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "VariablesToResetWindow";
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Button OkButton;
    private System.Windows.Forms.Button CancelButton;
    private System.Windows.Forms.TextBox textBox1;
}