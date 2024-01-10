using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Controls
{
    public enum AboveOrBelow
    {
        Above,
        Below
    }

    [Obsolete("Use CustomizableTextInputWindow which is WPF and more flexible")]
    public partial class TextInputWindow : Form
	{
		#region Properties


        public string Message
        {
			get
			{
				return this.mDisplayText.Text;
			}
			set
			{
				this.mDisplayText.Text = value;
			}
        }

        [Obsolete("Use Message")]
		public string DisplayText
		{
            get { return Message; }
            set { Message = value; }
		}

		public string Result
		{
			get { return textBox1.Text; }
            set { textBox1.Text = value; }
		}

		#endregion
        
        public TextInputWindow()
		{
			InitializeComponent();
			DialogResult = DialogResult.Cancel;

			StartPosition = FormStartPosition.Manual;
            Location = new Point(TextInputWindow.MousePosition.X - Width / 2, TextInputWindow.MousePosition.Y - Height / 2);
            this.EnsureOnScreen();
        }

        /// <summary>
        /// Adds a control to the TextInputWindow.  The control will automatically
        /// be positioned below the previously-added control.  The spacing can be controlled
        /// through the control's Margin property.
        /// </summary>
        /// <remarks>
        /// If you are adding a control after a label, you may need to adjust
        /// the label's height.  By default it's bigger than usually desired.
        /// </remarks>
        /// <param name="control">The control to add</param>
        public void AddControl(Control control, AboveOrBelow aboveOrBelow = AboveOrBelow.Below)
        {
            bool isFirst = (aboveOrBelow == AboveOrBelow.Below && this.ExtraControlsPanel.Controls.Count == 0)
                ||
                (aboveOrBelow == AboveOrBelow.Above && this.ExtraControlsPanelAbove.Controls.Count == 0)
                ;
            if (isFirst)
            {
                this.Height += 5;
            }

            if (aboveOrBelow == AboveOrBelow.Above)
            {
                this.ExtraControlsPanelAbove.Controls.Add(control);
            }
            else
            {
                // below
                this.ExtraControlsPanel.Controls.Add(control);

            }
            this.Height += control.Height;
            this.textBox1.Focus();
            this.DefaultControlPanel.Location = new Point(0, ExtraControlsPanelAbove.Height);
        }

        public void ClickOk()
        {
            this.mOkWindow.PerformClick();
        }

        protected override void OnActivated(EventArgs e) {
            base.OnActivated(e);
            textBox1.Focus();
        }
    }
}
