using L = Localization;

namespace FlatRedBall.Glue.Controls;

partial class ElementReferenceListWindow
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
        this.listBox1 = new System.Windows.Forms.ListBox();
        this.OKButton = new System.Windows.Forms.Button();
        this.SuspendLayout();
        // 
        // listBox1
        // 
        this.listBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                                                                      | System.Windows.Forms.AnchorStyles.Left)
                                                                     | System.Windows.Forms.AnchorStyles.Right)));
        this.listBox1.FormattingEnabled = true;
        this.listBox1.Location = new System.Drawing.Point(0, 0);
        this.listBox1.Name = "listBox1";
        this.listBox1.Size = new System.Drawing.Size(292, 251);
        this.listBox1.TabIndex = 0;
        this.listBox1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listBox1_MouseDoubleClick);
        // 
        // OKButton
        // 
        this.OKButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                                                                     | System.Windows.Forms.AnchorStyles.Right)));
        this.OKButton.Location = new System.Drawing.Point(0, 257);
        this.OKButton.Name = "OKButton";
        this.OKButton.Size = new System.Drawing.Size(292, 23);
        this.OKButton.TabIndex = 1;
        this.OKButton.Text = L.Texts.Ok;
        this.OKButton.UseVisualStyleBackColor = true;
        this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
        // 
        // ElementReferenceListWindow
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(292, 285);
        this.Controls.Add(this.OKButton);
        this.Controls.Add(this.listBox1);
        this.Name = "ElementReferenceListWindow";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.ListBox listBox1;
    private System.Windows.Forms.Button OKButton;
}