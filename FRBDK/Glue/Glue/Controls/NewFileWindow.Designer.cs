using L = Localization;

namespace FlatRedBall.Glue.Controls;

partial class NewFileWindow
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
        this.ComboBoxLabel = new System.Windows.Forms.Label();
        this.label2 = new System.Windows.Forms.Label();
        this.textBox1 = new System.Windows.Forms.TextBox();
        this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
        this.DynamicUiPanel = new System.Windows.Forms.FlowLayoutPanel();
        this.OkCancelPanel = new System.Windows.Forms.FlowLayoutPanel();
        this.flowLayoutPanel1.SuspendLayout();
        this.OkCancelPanel.SuspendLayout();
        this.SuspendLayout();
        // 
        // mCancelButton
        // 
        this.mCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.mCancelButton.Location = new System.Drawing.Point(104, 3);
        this.mCancelButton.Name = "mCancelButton";
        this.mCancelButton.Size = new System.Drawing.Size(95, 34);
        this.mCancelButton.TabIndex = 5;
        this.mCancelButton.Text = L.Texts.Cancel;
        this.mCancelButton.UseVisualStyleBackColor = true;
        // 
        // mOkWindow
        // 
        this.mOkWindow.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.mOkWindow.Location = new System.Drawing.Point(3, 3);
        this.mOkWindow.Name = "mOkWindow";
        this.mOkWindow.Size = new System.Drawing.Size(95, 34);
        this.mOkWindow.TabIndex = 4;
        this.mOkWindow.Text = L.Texts.Ok;
        this.mOkWindow.UseVisualStyleBackColor = true;
        this.mOkWindow.Click += new System.EventHandler(this.mOkWindow_Click);
        // 
        // mOptionsComboBox
        // 
        this.mOptionsComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.mOptionsComboBox.FormattingEnabled = true;
        this.mOptionsComboBox.Location = new System.Drawing.Point(3, 16);
        this.mOptionsComboBox.Name = "mOptionsComboBox";
        this.mOptionsComboBox.Size = new System.Drawing.Size(203, 21);
        this.mOptionsComboBox.TabIndex = 6;
        this.mOptionsComboBox.SelectedIndexChanged += new System.EventHandler(this.OptionsComboBox_SelectedIndexChanged);
        // 
        // ComboBoxLabel
        // 
        this.ComboBoxLabel.AutoSize = true;
        this.ComboBoxLabel.Location = new System.Drawing.Point(3, 0);
        this.ComboBoxLabel.Name = "ComboBoxLabel";
        this.ComboBoxLabel.Size = new System.Drawing.Size(97, 13);
        this.ComboBoxLabel.TabIndex = 7;
        this.ComboBoxLabel.Text = "Select the file type:";
        // 
        // label2
        // 
        this.label2.AutoSize = true;
        this.label2.Location = new System.Drawing.Point(3, 40);
        this.label2.Name = "label2";
        this.label2.Size = new System.Drawing.Size(128, 13);
        this.label2.TabIndex = 8;
        this.label2.Text = L.Texts.EnterFile;
        // 
        // textBox1
        // 
        this.textBox1.Location = new System.Drawing.Point(3, 54);
        this.textBox1.Margin = new System.Windows.Forms.Padding(3, 1, 3, 3);
        this.textBox1.Name = "textBox1";
        this.textBox1.Size = new System.Drawing.Size(203, 20);
        this.textBox1.TabIndex = 9;
        // 
        // flowLayoutPanel1
        // 
        this.flowLayoutPanel1.AutoSize = true;
        this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this.flowLayoutPanel1.Controls.Add(this.ComboBoxLabel);
        this.flowLayoutPanel1.Controls.Add(this.mOptionsComboBox);
        this.flowLayoutPanel1.Controls.Add(this.label2);
        this.flowLayoutPanel1.Controls.Add(this.textBox1);
        this.flowLayoutPanel1.Controls.Add(this.DynamicUiPanel);
        this.flowLayoutPanel1.Controls.Add(this.OkCancelPanel);
        this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
        this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
        this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
        this.flowLayoutPanel1.Name = "flowLayoutPanel1";
        this.flowLayoutPanel1.Size = new System.Drawing.Size(256, 253);
        this.flowLayoutPanel1.TabIndex = 10;
        // 
        // DynamicUiPanel
        // 
        this.DynamicUiPanel.AutoSize = true;
        this.DynamicUiPanel.Location = new System.Drawing.Point(3, 80);
        this.DynamicUiPanel.Name = "DynamicUiPanel";
        this.DynamicUiPanel.Size = new System.Drawing.Size(0, 0);
        this.DynamicUiPanel.TabIndex = 11;
        // 
        // OkCancelPanel
        // 
        this.OkCancelPanel.Controls.Add(this.mOkWindow);
        this.OkCancelPanel.Controls.Add(this.mCancelButton);
        this.OkCancelPanel.Location = new System.Drawing.Point(3, 86);
        this.OkCancelPanel.Name = "OkCancelPanel";
        this.OkCancelPanel.Size = new System.Drawing.Size(203, 40);
        this.OkCancelPanel.TabIndex = 10;
        // 
        // NewFileWindow
        // 
        this.AcceptButton = this.mOkWindow;
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.AutoSize = true;
        this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this.CancelButton = this.mCancelButton;
        this.ClientSize = new System.Drawing.Size(256, 253);
        this.Controls.Add(this.flowLayoutPanel1);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.Name = "NewFileWindow";
        this.ShowIcon = false;
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.flowLayoutPanel1.ResumeLayout(false);
        this.flowLayoutPanel1.PerformLayout();
        this.OkCancelPanel.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Button mCancelButton;
    private System.Windows.Forms.Button mOkWindow;
    private System.Windows.Forms.ComboBox mOptionsComboBox;
    private System.Windows.Forms.Label ComboBoxLabel;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.TextBox textBox1;
    private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
    private System.Windows.Forms.FlowLayoutPanel OkCancelPanel;
    private System.Windows.Forms.FlowLayoutPanel DynamicUiPanel;
}
