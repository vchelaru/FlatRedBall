//using System;
//using System.Drawing;
//using System.Collections;
//using System.ComponentModel;
//using System.Windows.Forms;
//using Alsing.SourceCode;
//
//namespace Alsing.Windows.Forms.SyntaxBox
//{
//	/// <summary>
//	/// Summary description for Settings.
//	/// </summary>
//	public class SettingsForm : System.Windows.Forms.Form
//	{
//		/// <summary>
//		/// Required designer variable.
//		/// </summary>
//		/// 
//		private Hashtable						SpanDefinitions=new Hashtable ();
//		private Hashtable						Styles=new Hashtable ();
//
//		private System.ComponentModel.Container components = null;
//		private System.Windows.Forms.Panel panel1;
//		private System.Windows.Forms.Label label1;
//		private System.Windows.Forms.Label label2;
//		private System.Windows.Forms.ComboBox cboFonts;
//		private System.Windows.Forms.ComboBox cboSize;
//		private System.Windows.Forms.Label label3;
//		private System.Windows.Forms.ComboBox comboBox1;
//		private System.Windows.Forms.Label label4;
//		private System.Windows.Forms.Button button1;
//		private System.Windows.Forms.Button button2;
//		private System.Windows.Forms.Label label5;
//		private System.Windows.Forms.ComboBox comboBox2;
//		private System.Windows.Forms.CheckBox checkBox1;
//		private System.Windows.Forms.CheckBox checkBox2;
//		private System.Windows.Forms.CheckBox checkBox3;
//		private System.Windows.Forms.ListBox lstBlocks;
//		private SyntaxBoxControl				mOwner=null;
//
//		public SettingsForm()
//		{
//			//
//			// Required for Windows Form Designer support
//			//
//			InitializeComponent();
//
//			//
//			// TODO: Add any constructor code after InitializeComponent call
//			//
//		}
//
//		public SettingsForm(SyntaxBoxControl Owner)
//		{
//			InitializeComponent();
//			mOwner=Owner;
//			lstBlocks.Items.Clear ();
//
//			FillTree(Owner.Document.Parser.SyntaxDefinition.mainSpanDefinition);
//		}
//
//		public void FillTree(spanDefinition Block)
//		{
//			if (SpanDefinitions[Block]!=null)
//				return;
//
//			SpanDefinitions.Add (Block,Block);	
//			AddStyle(Block.Style);
//			foreach (PatternList pl in Block.KeywordsList)
//			{
//				AddStyle(pl.Style);
//			}
//			foreach (PatternList pl in Block.OperatorsList)
//			{
//				AddStyle(pl.Style);
//			}
//		
//
//			foreach (spanDefinition ChildBlock in Block.childSpanDefinitions)
//			{
//				FillTree(ChildBlock);
//			}
//			
//		}
//
//		private void AddStyle(TextStyle style)
//		{
//			if (Styles[style]!=null)
//				return;
//
//			Styles.Add (style,style);
//			lstBlocks.Items.Add (style.Name);
//		}
//
//
//
//
//		/// <summary>
//		/// Clean up any resources being used.
//		/// </summary>
//		protected override void Dispose( bool disposing )
//		{
//			if( disposing )
//			{
//				if(components != null)
//				{
//					components.Dispose();
//				}
//			}
//			base.Dispose( disposing );
//		}
//
//		#region Windows Form Designer generated code
//		/// <summary>
//		/// Required method for Designer support - do not modify
//		/// the contents of this method with the code editor.
//		/// </summary>
//		private void InitializeComponent()
//		{
//			this.panel1 = new System.Windows.Forms.Panel();
//			this.checkBox3 = new System.Windows.Forms.CheckBox();
//			this.checkBox2 = new System.Windows.Forms.CheckBox();
//			this.checkBox1 = new System.Windows.Forms.CheckBox();
//			this.button2 = new System.Windows.Forms.Button();
//			this.label5 = new System.Windows.Forms.Label();
//			this.comboBox2 = new System.Windows.Forms.ComboBox();
//			this.button1 = new System.Windows.Forms.Button();
//			this.label4 = new System.Windows.Forms.Label();
//			this.comboBox1 = new System.Windows.Forms.ComboBox();
//			this.label3 = new System.Windows.Forms.Label();
//			this.lstBlocks = new System.Windows.Forms.ListBox();
//			this.cboSize = new System.Windows.Forms.ComboBox();
//			this.cboFonts = new System.Windows.Forms.ComboBox();
//			this.label2 = new System.Windows.Forms.Label();
//			this.label1 = new System.Windows.Forms.Label();
//			this.panel1.SuspendLayout();
//			this.SuspendLayout();
//			// 
//			// panel1
//			// 
//			this.panel1.Controls.AddRange(new System.Windows.Forms.Control[] {
//																				 this.checkBox3,
//																				 this.checkBox2,
//																				 this.checkBox1,
//																				 this.button2,
//																				 this.label5,
//																				 this.comboBox2,
//																				 this.button1,
//																				 this.label4,
//																				 this.comboBox1,
//																				 this.label3,
//																				 this.lstBlocks,
//																				 this.cboSize,
//																				 this.cboFonts,
//																				 this.label2,
//																				 this.label1});
//			this.panel1.Location = new System.Drawing.Point(8, 0);
//			this.panel1.Name = "panel1";
//			this.panel1.Size = new System.Drawing.Size(408, 344);
//			this.panel1.TabIndex = 0;
//			// 
//			// checkBox3
//			// 
//			this.checkBox3.Location = new System.Drawing.Point(184, 208);
//			this.checkBox3.Name = "checkBox3";
//			this.checkBox3.Size = new System.Drawing.Size(128, 16);
//			this.checkBox3.TabIndex = 14;
//			this.checkBox3.Text = "Underline";
//			// 
//			// checkBox2
//			// 
//			this.checkBox2.Location = new System.Drawing.Point(184, 184);
//			this.checkBox2.Name = "checkBox2";
//			this.checkBox2.Size = new System.Drawing.Size(128, 16);
//			this.checkBox2.TabIndex = 13;
//			this.checkBox2.Text = "Italic";
//			// 
//			// checkBox1
//			// 
//			this.checkBox1.Location = new System.Drawing.Point(184, 160);
//			this.checkBox1.Name = "checkBox1";
//			this.checkBox1.Size = new System.Drawing.Size(128, 16);
//			this.checkBox1.TabIndex = 12;
//			this.checkBox1.Text = "Bold";
//			// 
//			// button2
//			// 
//			this.button2.Location = new System.Drawing.Point(320, 128);
//			this.button2.Name = "button2";
//			this.button2.Size = new System.Drawing.Size(80, 21);
//			this.button2.TabIndex = 11;
//			this.button2.Text = "Custom...";
//			// 
//			// label5
//			// 
//			this.label5.Location = new System.Drawing.Point(184, 112);
//			this.label5.Name = "label5";
//			this.label5.Size = new System.Drawing.Size(100, 16);
//			this.label5.TabIndex = 10;
//			this.label5.Text = "Item foregound:";
//			// 
//			// comboBox2
//			// 
//			this.comboBox2.Location = new System.Drawing.Point(184, 128);
//			this.comboBox2.Name = "comboBox2";
//			this.comboBox2.Size = new System.Drawing.Size(128, 21);
//			this.comboBox2.TabIndex = 9;
//			this.comboBox2.Text = "comboBox2";
//			// 
//			// button1
//			// 
//			this.button1.Location = new System.Drawing.Point(320, 80);
//			this.button1.Name = "button1";
//			this.button1.Size = new System.Drawing.Size(80, 21);
//			this.button1.TabIndex = 8;
//			this.button1.Text = "Custom...";
//			// 
//			// label4
//			// 
//			this.label4.Location = new System.Drawing.Point(184, 64);
//			this.label4.Name = "label4";
//			this.label4.Size = new System.Drawing.Size(100, 16);
//			this.label4.TabIndex = 7;
//			this.label4.Text = "Item foregound:";
//			// 
//			// comboBox1
//			// 
//			this.comboBox1.Location = new System.Drawing.Point(184, 80);
//			this.comboBox1.Name = "comboBox1";
//			this.comboBox1.Size = new System.Drawing.Size(128, 21);
//			this.comboBox1.TabIndex = 6;
//			this.comboBox1.Text = "comboBox1";
//			// 
//			// label3
//			// 
//			this.label3.Location = new System.Drawing.Point(8, 64);
//			this.label3.Name = "label3";
//			this.label3.Size = new System.Drawing.Size(100, 16);
//			this.label3.TabIndex = 5;
//			this.label3.Text = "Display items:";
//			// 
//			// lstBlocks
//			// 
//			this.lstBlocks.Location = new System.Drawing.Point(8, 80);
//			this.lstBlocks.Name = "lstBlocks";
//			this.lstBlocks.Size = new System.Drawing.Size(168, 212);
//			this.lstBlocks.TabIndex = 4;
//			// 
//			// cboSize
//			// 
//			this.cboSize.Location = new System.Drawing.Point(320, 32);
//			this.cboSize.Name = "cboSize";
//			this.cboSize.Size = new System.Drawing.Size(80, 21);
//			this.cboSize.TabIndex = 3;
//			this.cboSize.Text = "comboBox1";
//			// 
//			// cboFonts
//			// 
//			this.cboFonts.Location = new System.Drawing.Point(8, 32);
//			this.cboFonts.Name = "cboFonts";
//			this.cboFonts.Size = new System.Drawing.Size(304, 21);
//			this.cboFonts.TabIndex = 2;
//			this.cboFonts.Text = "comboBox1";
//			// 
//			// label2
//			// 
//			this.label2.Location = new System.Drawing.Point(320, 16);
//			this.label2.Name = "label2";
//			this.label2.Size = new System.Drawing.Size(32, 16);
//			this.label2.TabIndex = 1;
//			this.label2.Text = "Size:";
//			// 
//			// label1
//			// 
//			this.label1.Location = new System.Drawing.Point(8, 16);
//			this.label1.Name = "label1";
//			this.label1.Size = new System.Drawing.Size(264, 16);
//			this.label1.TabIndex = 0;
//			this.label1.Text = "Font (bold type indicates fixed-width fonts):";
//			// 
//			// SettingsForm
//			// 
//			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
//			this.ClientSize = new System.Drawing.Size(424, 349);
//			this.Controls.AddRange(new System.Windows.Forms.Control[] {
//																		  this.panel1});
//			this.Name = "SettingsForm";
//			this.Text = "Settings";
//			this.panel1.ResumeLayout(false);
//			this.ResumeLayout(false);
//
//		}
//		#endregion
//	}
//}
