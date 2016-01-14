using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using FRB;
using FRB.Particle;

namespace ParticleEditor.Forms
{
    public partial class EmitterPropertyGridWindow : Form
    {
        #region Properties
        public object SelectedObject
        {
            get { return mPropertyGrid.SelectedObject; }
            set { mPropertyGrid.SelectedObject = value; }
        }
        #endregion

        public EmitterPropertyGridWindow()
        {
            InitializeComponent();
        }
    }
}