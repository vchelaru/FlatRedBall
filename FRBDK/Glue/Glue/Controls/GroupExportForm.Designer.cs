using L = Localization;
namespace FlatRedBall.Glue.Controls;

partial class GroupExportForm
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
        this.AllElementsTreeView = new System.Windows.Forms.TreeView();
        this.ToExportTreeView = new System.Windows.Forms.TreeView();
        this.OkButton = new System.Windows.Forms.Button();
        this.CancelButton = new System.Windows.Forms.Button();
        this.label1 = new System.Windows.Forms.Label();
        this.label2 = new System.Windows.Forms.Label();
        this.SuspendLayout();
        // 
        // AllElementsTreeView
        // 
        this.AllElementsTreeView.Location = new System.Drawing.Point(3, 26);
        this.AllElementsTreeView.Name = "AllElementsTreeView";
        this.AllElementsTreeView.Size = new System.Drawing.Size(163, 285);
        this.AllElementsTreeView.TabIndex = 0;
        this.AllElementsTreeView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.AllElementsTreeView_MouseDoubleClick);
        // 
        // ToExportTreeView
        // 
        this.ToExportTreeView.Location = new System.Drawing.Point(207, 26);
        this.ToExportTreeView.Name = "ToExportTreeView";
        this.ToExportTreeView.Size = new System.Drawing.Size(178, 285);
        this.ToExportTreeView.TabIndex = 1;
        // 
        // OkButton
        // 
        this.OkButton.Location = new System.Drawing.Point(229, 317);
        this.OkButton.Name = "OkButton";
        this.OkButton.Size = new System.Drawing.Size(75, 23);
        this.OkButton.TabIndex = 2;
        this.OkButton.Text = L.Texts.Ok;
        this.OkButton.UseVisualStyleBackColor = true;
        this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
        // 
        // CancelButton
        // 
        this.CancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.CancelButton.Location = new System.Drawing.Point(310, 317);
        this.CancelButton.Name = "CancelButton";
        this.CancelButton.Size = new System.Drawing.Size(75, 23);
        this.CancelButton.TabIndex = 3;
        this.CancelButton.Text = L.Texts.Cancel;
        this.CancelButton.UseVisualStyleBackColor = true;
        // 
        // label1
        // 
        this.label1.AutoSize = true;
        this.label1.Location = new System.Drawing.Point(0, 9);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(66, 13);
        this.label1.TabIndex = 4;
        this.label1.Text = L.Texts.ElementsAll;
        // 
        // label2
        // 
        this.label2.AutoSize = true;
        this.label2.Location = new System.Drawing.Point(204, 9);
        this.label2.Name = "label2";
        this.label2.Size = new System.Drawing.Size(97, 13);
        this.label2.TabIndex = 5;
        this.label2.Text = L.Texts.ElementsToExport;
        // 
        // GroupExportForm
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.CancelButton = this.CancelButton;
        this.ClientSize = new System.Drawing.Size(386, 343);
        this.Controls.Add(this.label2);
        this.Controls.Add(this.label1);
        this.Controls.Add(this.CancelButton);
        this.Controls.Add(this.OkButton);
        this.Controls.Add(this.ToExportTreeView);
        this.Controls.Add(this.AllElementsTreeView);
        this.Name = "GroupExportForm";
        this.Text = L.Texts.ElementsToExportSelect;
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.TreeView AllElementsTreeView;
    private System.Windows.Forms.TreeView ToExportTreeView;
    private System.Windows.Forms.Button OkButton;
    private System.Windows.Forms.Button CancelButton;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
}