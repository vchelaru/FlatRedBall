using L = Localization;
namespace FlatRedBall.Glue.Controls;

partial class AddEventWindow
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
        this.label4 = new System.Windows.Forms.Label();
        this.TunnelingObjectComboBox = new System.Windows.Forms.ComboBox();
        this.label5 = new System.Windows.Forms.Label();
        this.TunnelingEventComboBox = new System.Windows.Forms.ComboBox();
        this.label6 = new System.Windows.Forms.Label();
        this.AlternativeNameTextBox = new System.Windows.Forms.TextBox();
        this.label7 = new System.Windows.Forms.Label();
        this.OverridingPropertyTypeComboBox = new System.Windows.Forms.ComboBox();
        this.label8 = new System.Windows.Forms.Label();
        this.TypeConverterComboBox = new System.Windows.Forms.ComboBox();
        this.AvailableEventsComboBox = new System.Windows.Forms.ComboBox();
        this.label2 = new System.Windows.Forms.Label();
        this.label1 = new System.Windows.Forms.Label();
        this.textBox1 = new System.Windows.Forms.TextBox();
        this.AvailableTypesComboBox = new System.Windows.Forms.ComboBox();
        this.radExistingEvent = new System.Windows.Forms.RadioButton();
        this.radTunnelEvent = new System.Windows.Forms.RadioButton();
        this.radCreateNewEvent = new System.Windows.Forms.RadioButton();
        this.panel1 = new System.Windows.Forms.Panel();
        this.pnlExistingEvent = new System.Windows.Forms.Panel();
        this.label9 = new System.Windows.Forms.Label();
        this.pnlTunnelEvent = new System.Windows.Forms.Panel();
        this.label10 = new System.Windows.Forms.Label();
        this.pnlNewEvent = new System.Windows.Forms.Panel();
        this.GenericTypeTextBox = new System.Windows.Forms.TextBox();
        this.GenericTypeLabel = new System.Windows.Forms.Label();
        this.mCancelButton = new System.Windows.Forms.Button();
        this.mOkWindow = new System.Windows.Forms.Button();
        this.pnlExistingEvent.SuspendLayout();
        this.pnlTunnelEvent.SuspendLayout();
        this.pnlNewEvent.SuspendLayout();
        this.SuspendLayout();
        // 
        // label4
        // 
        this.label4.AutoSize = true;
        this.label4.Location = new System.Drawing.Point(3, 0);
        this.label4.Name = "label4";
        this.label4.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
        this.label4.Size = new System.Drawing.Size(41, 18);
        this.label4.TabIndex = 1;
        this.label4.Text = L.Texts.Object;
        // 
        // TunnelingObjectComboBox
        // 
        this.TunnelingObjectComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.TunnelingObjectComboBox.FormattingEnabled = true;
        this.TunnelingObjectComboBox.Location = new System.Drawing.Point(53, 0);
        this.TunnelingObjectComboBox.Name = "TunnelingObjectComboBox";
        this.TunnelingObjectComboBox.Size = new System.Drawing.Size(213, 21);
        this.TunnelingObjectComboBox.TabIndex = 0;
        this.TunnelingObjectComboBox.SelectedIndexChanged += new System.EventHandler(this.TunnelingObjectComboBox_SelectedIndexChanged);
        // 
        // label5
        // 
        this.label5.AutoSize = true;
        this.label5.Location = new System.Drawing.Point(3, 27);
        this.label5.Name = "label5";
        this.label5.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
        this.label5.Size = new System.Drawing.Size(38, 18);
        this.label5.TabIndex = 2;
        this.label5.Text = L.Texts.Event;
        this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // TunnelingEventComboBox
        // 
        this.TunnelingEventComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.TunnelingEventComboBox.FormattingEnabled = true;
        this.TunnelingEventComboBox.Location = new System.Drawing.Point(53, 27);
        this.TunnelingEventComboBox.Name = "TunnelingEventComboBox";
        this.TunnelingEventComboBox.Size = new System.Drawing.Size(213, 21);
        this.TunnelingEventComboBox.TabIndex = 1;
        this.TunnelingEventComboBox.SelectedIndexChanged += new System.EventHandler(this.TunnelingVariableComboBox_SelectedIndexChanged);
        // 
        // label6
        // 
        this.label6.Location = new System.Drawing.Point(3, 84);
        this.label6.Name = "label6";
        this.label6.Size = new System.Drawing.Size(91, 13);
        this.label6.TabIndex = 4;
        this.label6.Text = L.Texts.AlternativeName;
        // 
        // AlternativeNameTextBox
        // 
        this.AlternativeNameTextBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
        this.AlternativeNameTextBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
        this.AlternativeNameTextBox.Location = new System.Drawing.Point(106, 81);
        this.AlternativeNameTextBox.Name = "AlternativeNameTextBox";
        this.AlternativeNameTextBox.Size = new System.Drawing.Size(160, 20);
        this.AlternativeNameTextBox.TabIndex = 2;
        // 
        // label7
        // 
        this.label7.Location = new System.Drawing.Point(3, 110);
        this.label7.Name = "label7";
        this.label7.Size = new System.Drawing.Size(91, 19);
        this.label7.TabIndex = 7;
        this.label7.Text = L.Texts.PropertyType;
        // 
        // OverridingPropertyTypeComboBox
        // 
        this.OverridingPropertyTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.OverridingPropertyTypeComboBox.FormattingEnabled = true;
        this.OverridingPropertyTypeComboBox.Location = new System.Drawing.Point(106, 107);
        this.OverridingPropertyTypeComboBox.Name = "OverridingPropertyTypeComboBox";
        this.OverridingPropertyTypeComboBox.Size = new System.Drawing.Size(160, 21);
        this.OverridingPropertyTypeComboBox.TabIndex = 3;
        // 
        // label8
        // 
        this.label8.Location = new System.Drawing.Point(3, 137);
        this.label8.Name = "label8";
        this.label8.Size = new System.Drawing.Size(91, 13);
        this.label8.TabIndex = 9;
        this.label8.Text = L.Texts.TypeConverter;
        // 
        // TypeConverterComboBox
        // 
        this.TypeConverterComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.TypeConverterComboBox.FormattingEnabled = true;
        this.TypeConverterComboBox.Location = new System.Drawing.Point(106, 134);
        this.TypeConverterComboBox.Name = "TypeConverterComboBox";
        this.TypeConverterComboBox.Size = new System.Drawing.Size(160, 21);
        this.TypeConverterComboBox.TabIndex = 4;
        // 
        // AvailableEventsComboBox
        // 
        this.AvailableEventsComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.AvailableEventsComboBox.FormattingEnabled = true;
        this.AvailableEventsComboBox.Location = new System.Drawing.Point(6, 16);
        this.AvailableEventsComboBox.Name = "AvailableEventsComboBox";
        this.AvailableEventsComboBox.Size = new System.Drawing.Size(260, 21);
        this.AvailableEventsComboBox.TabIndex = 0;
        // 
        // label2
        // 
        this.label2.AutoSize = true;
        this.label2.Location = new System.Drawing.Point(2, 53);
        this.label2.Name = "label2";
        this.label2.Size = new System.Drawing.Size(38, 13);
        this.label2.TabIndex = 19;
        this.label2.Text = L.Texts.Name;
        // 
        // label1
        // 
        this.label1.AutoSize = true;
        this.label1.Location = new System.Drawing.Point(2, 3);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(34, 13);
        this.label1.TabIndex = 18;
        this.label1.Text = L.Texts.Type;
        // 
        // textBox1
        // 
        this.textBox1.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
        this.textBox1.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
        this.textBox1.Location = new System.Drawing.Point(82, 50);
        this.textBox1.Name = "textBox1";
        this.textBox1.Size = new System.Drawing.Size(184, 20);
        this.textBox1.TabIndex = 1;
        // 
        // AvailableTypesComboBox
        // 
        this.AvailableTypesComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.AvailableTypesComboBox.FormattingEnabled = true;
        this.AvailableTypesComboBox.Location = new System.Drawing.Point(82, 0);
        this.AvailableTypesComboBox.Name = "AvailableTypesComboBox";
        this.AvailableTypesComboBox.Size = new System.Drawing.Size(183, 21);
        this.AvailableTypesComboBox.TabIndex = 0;
        this.AvailableTypesComboBox.SelectedIndexChanged += new System.EventHandler(this.AvailableTypesComboBox_SelectedIndexChanged);
        // 
        // radExistingEvent
        // 
        this.radExistingEvent.AutoSize = true;
        this.radExistingEvent.Checked = true;
        this.radExistingEvent.Location = new System.Drawing.Point(12, 12);
        this.radExistingEvent.Name = "radExistingEvent";
        this.radExistingEvent.Size = new System.Drawing.Size(143, 17);
        this.radExistingEvent.TabIndex = 5;
        this.radExistingEvent.TabStop = true;
        this.radExistingEvent.Text = L.Texts.EventExistingExpose;
        this.radExistingEvent.UseVisualStyleBackColor = true;
        this.radExistingEvent.CheckedChanged += new System.EventHandler(this.radCreateNewEvent_CheckedChanged);
        // 
        // radTunnelEvent
        // 
        this.radTunnelEvent.AutoSize = true;
        this.radTunnelEvent.Location = new System.Drawing.Point(12, 35);
        this.radTunnelEvent.Name = "radTunnelEvent";
        this.radTunnelEvent.Size = new System.Drawing.Size(197, 17);
        this.radTunnelEvent.TabIndex = 6;
        this.radTunnelEvent.Text = "Tunnel an event from another object";
        this.radTunnelEvent.UseVisualStyleBackColor = true;
        this.radTunnelEvent.CheckedChanged += new System.EventHandler(this.radCreateNewEvent_CheckedChanged);
        // 
        // radCreateNewEvent
        // 
        this.radCreateNewEvent.AutoSize = true;
        this.radCreateNewEvent.Location = new System.Drawing.Point(12, 58);
        this.radCreateNewEvent.Name = "radCreateNewEvent";
        this.radCreateNewEvent.Size = new System.Drawing.Size(118, 17);
        this.radCreateNewEvent.TabIndex = 7;
        this.radCreateNewEvent.Text = L.Texts.EventNewCreate;
        this.radCreateNewEvent.UseVisualStyleBackColor = true;
        this.radCreateNewEvent.CheckedChanged += new System.EventHandler(this.radCreateNewEvent_CheckedChanged);
        // 
        // panel1
        // 
        this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                                                                   | System.Windows.Forms.AnchorStyles.Right)));
        this.panel1.BackColor = System.Drawing.Color.Black;
        this.panel1.Location = new System.Drawing.Point(3, 81);
        this.panel1.Name = "panel1";
        this.panel1.Size = new System.Drawing.Size(276, 2);
        this.panel1.TabIndex = 19;
        // 
        // pnlExistingEvent
        // 
        this.pnlExistingEvent.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                                                                             | System.Windows.Forms.AnchorStyles.Right)));
        this.pnlExistingEvent.Controls.Add(this.AvailableEventsComboBox);
        this.pnlExistingEvent.Controls.Add(this.label9);
        this.pnlExistingEvent.Location = new System.Drawing.Point(3, 89);
        this.pnlExistingEvent.Name = "pnlExistingEvent";
        this.pnlExistingEvent.Size = new System.Drawing.Size(276, 60);
        this.pnlExistingEvent.TabIndex = 1;
        // 
        // label9
        // 
        this.label9.AutoSize = true;
        this.label9.Location = new System.Drawing.Point(3, 0);
        this.label9.Name = "label9";
        this.label9.Size = new System.Drawing.Size(123, 13);
        this.label9.TabIndex = 0;
        this.label9.Text = L.Texts.EventSelectExisting;
        // 
        // pnlTunnelEvent
        // 
        this.pnlTunnelEvent.Controls.Add(this.label10);
        this.pnlTunnelEvent.Controls.Add(this.label8);
        this.pnlTunnelEvent.Controls.Add(this.label7);
        this.pnlTunnelEvent.Controls.Add(this.TypeConverterComboBox);
        this.pnlTunnelEvent.Controls.Add(this.label6);
        this.pnlTunnelEvent.Controls.Add(this.OverridingPropertyTypeComboBox);
        this.pnlTunnelEvent.Controls.Add(this.AlternativeNameTextBox);
        this.pnlTunnelEvent.Controls.Add(this.TunnelingEventComboBox);
        this.pnlTunnelEvent.Controls.Add(this.label5);
        this.pnlTunnelEvent.Controls.Add(this.TunnelingObjectComboBox);
        this.pnlTunnelEvent.Controls.Add(this.label4);
        this.pnlTunnelEvent.Location = new System.Drawing.Point(3, 89);
        this.pnlTunnelEvent.Name = "pnlTunnelEvent";
        this.pnlTunnelEvent.Size = new System.Drawing.Size(271, 162);
        this.pnlTunnelEvent.TabIndex = 2;
        this.pnlTunnelEvent.Visible = false;
        // 
        // label10
        // 
        this.label10.AutoSize = true;
        this.label10.Location = new System.Drawing.Point(3, 63);
        this.label10.Name = "label10";
        this.label10.Size = new System.Drawing.Size(95, 13);
        this.label10.TabIndex = 12;
        this.label10.Text = L.Texts.AdvancedOptions;
        // 
        // pnlNewEvent
        // 
        this.pnlNewEvent.Controls.Add(this.GenericTypeTextBox);
        this.pnlNewEvent.Controls.Add(this.GenericTypeLabel);
        this.pnlNewEvent.Controls.Add(this.label2);
        this.pnlNewEvent.Controls.Add(this.AvailableTypesComboBox);
        this.pnlNewEvent.Controls.Add(this.label1);
        this.pnlNewEvent.Controls.Add(this.textBox1);
        this.pnlNewEvent.Location = new System.Drawing.Point(3, 89);
        this.pnlNewEvent.Name = "pnlNewEvent";
        this.pnlNewEvent.Size = new System.Drawing.Size(271, 75);
        this.pnlNewEvent.TabIndex = 0;
        this.pnlNewEvent.Visible = false;
        // 
        // GenericTypeTextBox
        // 
        this.GenericTypeTextBox.Location = new System.Drawing.Point(82, 25);
        this.GenericTypeTextBox.Name = "GenericTypeTextBox";
        this.GenericTypeTextBox.Size = new System.Drawing.Size(183, 20);
        this.GenericTypeTextBox.TabIndex = 21;
        this.GenericTypeTextBox.Text = "float";
        // 
        // GenericTypeLabel
        // 
        this.GenericTypeLabel.AutoSize = true;
        this.GenericTypeLabel.Location = new System.Drawing.Point(2, 28);
        this.GenericTypeLabel.Name = "GenericTypeLabel";
        this.GenericTypeLabel.Size = new System.Drawing.Size(74, 13);
        this.GenericTypeLabel.TabIndex = 20;
        this.GenericTypeLabel.Text = L.Texts.GenericType;
        // 
        // mCancelButton
        // 
        this.mCancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.mCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.mCancelButton.Location = new System.Drawing.Point(201, 260);
        this.mCancelButton.Name = "mCancelButton";
        this.mCancelButton.Size = new System.Drawing.Size(70, 23);
        this.mCancelButton.TabIndex = 4;
        this.mCancelButton.Text = L.Texts.Cancel;
        this.mCancelButton.UseVisualStyleBackColor = true;
        // 
        // mOkWindow
        // 
        this.mOkWindow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.mOkWindow.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.mOkWindow.Location = new System.Drawing.Point(125, 260);
        this.mOkWindow.Name = "mOkWindow";
        this.mOkWindow.Size = new System.Drawing.Size(70, 23);
        this.mOkWindow.TabIndex = 3;
        this.mOkWindow.Text = L.Texts.Ok;
        this.mOkWindow.UseVisualStyleBackColor = true;
        // 
        // AddEventWindow
        // 
        this.AcceptButton = this.mOkWindow;
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.CancelButton = this.mCancelButton;
        this.ClientSize = new System.Drawing.Size(283, 295);
        this.Controls.Add(this.mCancelButton);
        this.Controls.Add(this.mOkWindow);
        this.Controls.Add(this.panel1);
        this.Controls.Add(this.radCreateNewEvent);
        this.Controls.Add(this.radTunnelEvent);
        this.Controls.Add(this.radExistingEvent);
        this.Controls.Add(this.pnlNewEvent);
        this.Controls.Add(this.pnlExistingEvent);
        this.Controls.Add(this.pnlTunnelEvent);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "AddEventWindow";
        this.ShowIcon = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "New Event";
        this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AddVariableWindow_FormClosing);
        this.Load += new System.EventHandler(this.AddVariableWindow_Load);
        this.pnlExistingEvent.ResumeLayout(false);
        this.pnlExistingEvent.PerformLayout();
        this.pnlTunnelEvent.ResumeLayout(false);
        this.pnlTunnelEvent.PerformLayout();
        this.pnlNewEvent.ResumeLayout(false);
        this.pnlNewEvent.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.TextBox AlternativeNameTextBox;
    private System.Windows.Forms.Label label6;
    private System.Windows.Forms.ComboBox TunnelingEventComboBox;
    private System.Windows.Forms.Label label5;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.ComboBox TunnelingObjectComboBox;
    private System.Windows.Forms.ComboBox AvailableEventsComboBox;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.TextBox textBox1;
    private System.Windows.Forms.ComboBox AvailableTypesComboBox;
    private System.Windows.Forms.Label label8;
    private System.Windows.Forms.ComboBox TypeConverterComboBox;
    private System.Windows.Forms.Label label7;
    private System.Windows.Forms.ComboBox OverridingPropertyTypeComboBox;
    private System.Windows.Forms.RadioButton radExistingEvent;
    private System.Windows.Forms.RadioButton radTunnelEvent;
    private System.Windows.Forms.RadioButton radCreateNewEvent;
    private System.Windows.Forms.Panel panel1;
    private System.Windows.Forms.Panel pnlExistingEvent;
    private System.Windows.Forms.Label label9;
    private System.Windows.Forms.Panel pnlTunnelEvent;
    private System.Windows.Forms.Label label10;
    private System.Windows.Forms.Panel pnlNewEvent;
    private System.Windows.Forms.Button mCancelButton;
    private System.Windows.Forms.Button mOkWindow;
    private System.Windows.Forms.TextBox GenericTypeTextBox;
    private System.Windows.Forms.Label GenericTypeLabel;

}