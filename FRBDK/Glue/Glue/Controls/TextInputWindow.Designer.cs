using L = Localization;

namespace FlatRedBall.Glue.Controls;
partial class TextInputWindow
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
        this.mDisplayText = new System.Windows.Forms.Label();
        this.textBox1 = new System.Windows.Forms.TextBox();
        this.mOkWindow = new System.Windows.Forms.Button();
        this.mCancelButton = new System.Windows.Forms.Button();
        this.DefaultControlPanel = new System.Windows.Forms.Panel();
        this.ExtraControlsPanel = new System.Windows.Forms.FlowLayoutPanel();
        this.ExtraControlsPanelAbove = new System.Windows.Forms.FlowLayoutPanel();
        this.DefaultControlPanel.SuspendLayout();
        this.SuspendLayout();
        // 
        // mDisplayText
        // 
        this.mDisplayText.AutoSize = true;
        this.mDisplayText.Location = new System.Drawing.Point(0, 1);
        this.mDisplayText.Margin = new System.Windows.Forms.Padding(0, 0, 12, 2);
        this.mDisplayText.Name = "mDisplayText";
        this.mDisplayText.Size = new System.Drawing.Size(35, 13);
        this.mDisplayText.TabIndex = 5;
        this.mDisplayText.Text = "label1";
        // 
        // textBox1
        // 
        this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                                                                     | System.Windows.Forms.AnchorStyles.Right)));
        this.textBox1.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
        this.textBox1.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
        this.textBox1.Location = new System.Drawing.Point(3, 18);
        this.textBox1.Name = "textBox1";
        this.textBox1.Size = new System.Drawing.Size(287, 20);
        this.textBox1.TabIndex = 0;
        // 
        // mOkWindow
        // 
        this.mOkWindow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.mOkWindow.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.mOkWindow.Location = new System.Drawing.Point(144, 44);
        this.mOkWindow.Name = "mOkWindow";
        this.mOkWindow.Size = new System.Drawing.Size(70, 23);
        this.mOkWindow.TabIndex = 1;
        this.mOkWindow.Text = L.Texts.Ok;
        this.mOkWindow.UseVisualStyleBackColor = true;
        // 
        // mCancelButton
        // 
        this.mCancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.mCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.mCancelButton.Location = new System.Drawing.Point(220, 44);
        this.mCancelButton.Name = "mCancelButton";
        this.mCancelButton.Size = new System.Drawing.Size(70, 23);
        this.mCancelButton.TabIndex = 2;
        this.mCancelButton.Text = L.Texts.Cancel;
        this.mCancelButton.UseVisualStyleBackColor = true;
        // 
        // DefaultControlPanel
        // 
        this.DefaultControlPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
        this.DefaultControlPanel.BackColor = System.Drawing.SystemColors.Control;
        this.DefaultControlPanel.Controls.Add(this.textBox1);
        this.DefaultControlPanel.Controls.Add(this.mDisplayText);
        this.DefaultControlPanel.Location = new System.Drawing.Point(0, 1);
        this.DefaultControlPanel.Margin = new System.Windows.Forms.Padding(0);
        this.DefaultControlPanel.Name = "DefaultControlPanel";
        this.DefaultControlPanel.Size = new System.Drawing.Size(293, 40);
        this.DefaultControlPanel.TabIndex = 6;
        // 
        // ExtraControlsPanel
        // 
        this.ExtraControlsPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
                                                                               | System.Windows.Forms.AnchorStyles.Right)));
        this.ExtraControlsPanel.AutoSize = true;
        this.ExtraControlsPanel.BackColor = System.Drawing.SystemColors.Control;
        this.ExtraControlsPanel.Location = new System.Drawing.Point(0, 41);
        this.ExtraControlsPanel.Name = "ExtraControlsPanel";
        this.ExtraControlsPanel.Size = new System.Drawing.Size(293, 1);
        this.ExtraControlsPanel.TabIndex = 7;
        // 
        // ExtraControlsPanelAbove
        // 
        this.ExtraControlsPanelAbove.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
        this.ExtraControlsPanelAbove.AutoSize = true;
        this.ExtraControlsPanelAbove.BackColor = System.Drawing.SystemColors.Control;
        this.ExtraControlsPanelAbove.Location = new System.Drawing.Point(0, 0);
        this.ExtraControlsPanelAbove.Margin = new System.Windows.Forms.Padding(0);
        this.ExtraControlsPanelAbove.Name = "ExtraControlsPanelAbove";
        this.ExtraControlsPanelAbove.Size = new System.Drawing.Size(293, 0);
        this.ExtraControlsPanelAbove.TabIndex = 8;
        // 
        // TextInputWindow
        // 
        this.AcceptButton = this.mOkWindow;
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.BackColor = System.Drawing.SystemColors.Control;
        this.CancelButton = this.mCancelButton;
        this.ClientSize = new System.Drawing.Size(293, 69);
        this.Controls.Add(this.ExtraControlsPanelAbove);
        this.Controls.Add(this.ExtraControlsPanel);
        this.Controls.Add(this.DefaultControlPanel);
        this.Controls.Add(this.mCancelButton);
        this.Controls.Add(this.mOkWindow);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "TextInputWindow";
        this.ShowIcon = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.DefaultControlPanel.ResumeLayout(false);
        this.DefaultControlPanel.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Label mDisplayText;
    private System.Windows.Forms.TextBox textBox1;
    private System.Windows.Forms.Button mOkWindow;
    private System.Windows.Forms.Button mCancelButton;
    private System.Windows.Forms.Panel DefaultControlPanel;
    private System.Windows.Forms.FlowLayoutPanel ExtraControlsPanel;
    private System.Windows.Forms.FlowLayoutPanel ExtraControlsPanelAbove;

}