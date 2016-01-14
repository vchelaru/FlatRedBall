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
    public partial class VariablesToResetWindow : Form
    {
        char[] splitCharacters = new char[] { '\n', ' ', '\t', ',', ';' };

        public string[] Results
        {
            get
            {
                return textBox1.Text.Split(splitCharacters, StringSplitOptions.RemoveEmptyEntries);
            }
        }





        public VariablesToResetWindow(List<string> variableNames)
        {
            InitializeComponent();
            


            foreach (string variable in variableNames)
            {
                textBox1.Text += variable + "\r\n";

            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {

        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
