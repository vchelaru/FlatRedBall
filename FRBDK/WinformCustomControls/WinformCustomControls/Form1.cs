using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WinformCustomControls
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void collapsibleControl1_Load(object sender, EventArgs e)
        {

        }

        private void collapsibleControl1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

            TestCollapsableContainers();
            //TestButtons();
        }

        private void TestButtons()
        {
            Random random = new Random();
            for (int i = 0; i < 14; i++)
            {
                int size = random.Next(50) + 20;
                collapsibleContainerStrip1.AddButton(size);
            }
        }

        private void TestCollapsableContainers()
        {
            SuspendLayout();
            this.Height = 300;

            for (int i = 0; i < 2; i++)
            {
                this.collapsibleContainerStrip1.AddCollapsibleControl();
            }

            Button button = new Button();
            button.Text = "Hello";
            collapsibleContainerStrip1.AddCollapsibleControlFor(button);


            ListBox listBox = new ListBox();
            for (int i = 0; i < 10; i++)
            {
                listBox.Items.Add(i.ToString());
            }
            listBox.Height = 150;
            collapsibleContainerStrip1.AddCollapsibleControlFor(listBox, 150);


            PropertyGrid propertyGrid = new PropertyGrid();
            propertyGrid.SelectedObject = button;
            collapsibleContainerStrip1.AddCollapsibleControlFor(propertyGrid, 200, "PropertyGrid Stuff");



            propertyGrid = new PropertyGrid();
            propertyGrid.SelectedObject = listBox;
            propertyGrid.Height = 230;
            collapsibleContainerStrip1.AddCollapsibleControlForReduced(propertyGrid, -1, "ListBox Properties");

            
            
            
            ResumeLayout();




            //collapsibleContainerStrip1.DisplayRectangle = rectangle;
            //collapsibleContainerStrip1.ClientRectangle = rectangle;
        }




    }
}
