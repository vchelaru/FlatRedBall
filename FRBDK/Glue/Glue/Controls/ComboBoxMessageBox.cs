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
    public partial class ComboBoxMessageBox : Form
    {

        class ObjectWithDisplayText
        {
            public string DisplayText;
            public object ObjectReference;

            public override string ToString()
            {
                return DisplayText;
            }
        }


        public string Message
        {

            set
            {
                label1.Text = value;
            }
        }

        public string SelectedText
        {
            get
            {
                return comboBox1.SelectedText;
            }
        }

        public object SelectedItem
        {
            get
            {
                return ((ObjectWithDisplayText)comboBox1.SelectedItem).ObjectReference;
            }
        }

        public ComboBoxMessageBox()
        {
            InitializeComponent();
        }

        public void Add(object item)
        {
            Add(item, item.ToString());
        }

        public void Add(object item, string displayText)
        {
            ObjectWithDisplayText owdt = new ObjectWithDisplayText() { DisplayText = displayText, ObjectReference = item };
            comboBox1.Items.Add(owdt);

            if (comboBox1.Items.Count == 1)
            {
                comboBox1.SelectedItem = item;
            }
        }
    }
}
