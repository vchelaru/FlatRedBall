using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GlueCsvEditor.Controls
{
    public partial class EntityDesigner : UserControl
    {
        public EntityDesigner()
        {
            InitializeComponent();
        }

        private void EntityDesigner_Load(object sender, EventArgs e)
        {
            this.Dock = DockStyle.Fill;
        }
    }
}
