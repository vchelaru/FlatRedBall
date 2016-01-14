using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBallProfiler
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();

            this.mainControl1.AfterLoad += HandleAfterLoad;
        }

        private void HandleAfterLoad(object sender, EventArgs e)
        {
            this.Text = ProjectManager.Self.LastFile;
        }
    }
}
