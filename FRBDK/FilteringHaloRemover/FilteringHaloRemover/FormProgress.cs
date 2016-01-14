using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FilteringHaloRemover
{
    public partial class FormProgress : Form
    {
        public bool EnableOK
        {
            get
            {
                return buttonOK.Enabled;
            }
            set
            {
                buttonOK.Enabled = value;
            }
        }

        public ProgressBar ProgressBar
        {
            get
            {
                return progressBar1;
            }
        }

        public ProgressBar ProgressBarTotal
        {
            get
            {
                return progressBarTotal;
            }
        }

        public void DoProgress()
        {
            if(progressBar1.Value < progressBar1.Maximum)
            {
                progressBar1.Value++;
            }

        }

        public void DoTotalProgress()
        {
            if (progressBarTotal.Value < progressBarTotal.Maximum)
            {
                progressBarTotal.Value++;
            }

        }

        public FormProgress()
        {
            InitializeComponent();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}