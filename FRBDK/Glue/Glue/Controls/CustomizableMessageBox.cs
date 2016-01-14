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


    /*
     * usage:
     *      CustomizableMessageBox cmb = new CustomizableMessageBox();
            cmb.Show(this);
            cmb.Message = "Test test test";
            cmb.AddButton("Wimpy tooth");
            cmb.AddButton("Crazy Neck");
     * 
     * 
     */


    public partial class CustomizableMessageBox : Form
    {
        List<Button> mButtons = new List<Button>();

        public Button ClickedButton
        {
            get;
            private set;
        }

        public string Message
        {
            get
            {
                return MessageLabel.Text;
            }
            set
            {
                MessageLabel.Text = value;
            }
        }

        public CustomizableMessageBox()
        {
            InitializeComponent();
        }


        public Button AddButton(string text)
        {
            int spacingBetweenButtons = 40;
            int yPosition = 150 + spacingBetweenButtons * mButtons.Count;

            Button button = new Button();
            button.Size = new Size(this.Size.Width - 20, spacingBetweenButtons - 5);
            button.Location = new Point(10, yPosition);
            button.Text = text;
            this.Controls.Add(button);

            button.Click += new EventHandler(ButtonClick);

            mButtons.Add(button);

            return button;
        }

        void ButtonClick(object sender, EventArgs e)
        {
            ClickedButton = ((Button)sender);
            this.Close();
        }

    }
}
