using L = Localization;
namespace FlatRedBall.Glue.Controls;

partial class ReferencedFileFlatListWindow
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
        this.ListBox = new System.Windows.Forms.ListBox();
        this.CloseButton = new System.Windows.Forms.Button();
        this.SuspendLayout();
        // 
        // ListBox
        // 
        this.ListBox.AllowDrop = true;
        this.ListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                                                                     | System.Windows.Forms.AnchorStyles.Left)
                                                                    | System.Windows.Forms.AnchorStyles.Right)));
        this.ListBox.FormattingEnabled = true;
        this.ListBox.Location = new System.Drawing.Point(3, 3);
        this.ListBox.Name = "ListBox";
        this.ListBox.Size = new System.Drawing.Size(288, 238);
        this.ListBox.TabIndex = 0;
        this.ListBox.DragDrop += new System.Windows.Forms.DragEventHandler(this.ListBox_DragDrop);
        this.ListBox.DragOver += new System.Windows.Forms.DragEventHandler(this.ListBox_DragOver);
        this.ListBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ListBox_MouseDown);
        // 
        // CloseButton
        // 
        this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                                                                        | System.Windows.Forms.AnchorStyles.Right)));
        this.CloseButton.Location = new System.Drawing.Point(3, 241);
        this.CloseButton.Name = "CloseButton";
        this.CloseButton.Size = new System.Drawing.Size(288, 31);
        this.CloseButton.TabIndex = 1;
        this.CloseButton.Text = L.Texts.Close;
        this.CloseButton.UseVisualStyleBackColor = true;
        this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
        // 
        // ReferencedFileFlatListWindow
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(292, 273);
        this.Controls.Add(this.CloseButton);
        this.Controls.Add(this.ListBox);
        this.Name = "ReferencedFileFlatListWindow";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "ReferencedFileFlatListWindow";
        this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.ListBox ListBox;
    private System.Windows.Forms.Button CloseButton;
}