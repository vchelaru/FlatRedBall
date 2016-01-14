using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace MasterInstaller.Components.InstallableComponents.FRBDK
{
    public partial class FileAssociationControl : UserControl
    {


        public bool ShouldInstall
        {
            get
            {
                return checkBox1.Checked;
            }
            set
            {
                checkBox1.Checked = value;
            }
        }

        public FileAssociationControl()
        {
            InitializeComponent();
        }
    }
}
