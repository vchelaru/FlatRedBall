using System;
using System.Drawing;
using System.Windows.Forms;

namespace NewProjectCreator
{
	public partial class Form1 : Form
	{


		public static void Main ()
		{


			Application.Run (new Form1 ());
		}

		public Form1 ()
		{
			InitializeComponent ();
		}

		private void Button_Click (object sender, EventArgs e)
		{
			MessageBox.Show ("Button Clicked!");
		}

		void ProjectTypeListBox_SelectedIndexChanged(object sender, EventArgs args)
		{

		}
	}
}

