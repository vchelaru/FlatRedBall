using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace FlatRedBall.Glue.Controls
{
    public partial class ProcessesWindow : Form
    {
        internal class ItemValue
        {
            public string Text { get; set; }
            public Process Value { get; set; }
        }

        public ProcessesWindow()
        {
            InitializeComponent();

            lbProcesses.DisplayMember = "Text";
            lbProcesses.ValueMember = "Value";

            foreach (Process process in Process.GetProcesses())
            {
                if (!String.IsNullOrEmpty(process.MainWindowTitle))
                {
                    ItemValue value = new ItemValue();

                    value.Text = process.MainWindowTitle;
                    value.Value = process;

                    lbProcesses.Items.Add(value);
                }
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if(lbProcesses.SelectedItem != null)
            {
                ProcessManager.OpenProcess(((ItemValue)lbProcesses.SelectedItem).Value);
            }

            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
