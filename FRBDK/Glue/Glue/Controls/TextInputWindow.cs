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

	public partial class TextInputWindow : Form
	{
		#region Properties

		public string DisplayText
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

		public string Result
		{
			get { return textBox1.Text; }
            set { textBox1.Text = value; }
		}

		#endregion

        #region Event Methods

        private void mOkWindow_Click(object sender, EventArgs e)
        {

        }

        #endregion


        public TextInputWindow()
		{
			InitializeComponent();
			DialogResult = DialogResult.Cancel;

			StartPosition = FormStartPosition.Manual;
            // This will be set in OnShow after all controls 
            // have been added because we want to center the control 
            // where the mouse is.
            //Location = new Point(TextInputWindow.MousePosition.X, TextInputWindow.MousePosition.Y);

            this.textBox1.Focus();
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

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Location = new Point(TextInputWindow.MousePosition.X - Width / 2, TextInputWindow.MousePosition.Y - Height / 2);

            this.EnsureOnScreen();

            var screen = Screen.FromControl(this);
            System.Drawing.Point newLocation = this.Location;

            if (this.Bounds.Top < 0)
                newLocation.Y = 0;

            this.Location = newLocation;




            textBox1.Focus();
        }
	}
}
