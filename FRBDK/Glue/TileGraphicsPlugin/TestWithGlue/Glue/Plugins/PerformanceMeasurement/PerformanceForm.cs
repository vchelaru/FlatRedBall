using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PluginTestbed.PerformanceMeasurement
{
    public partial class PerformanceForm : Form
    {
        PerformanceMeasurementPlugin mPerformanceMeasurementPlugin;

        public PerformanceForm()
        {
            InitializeComponent();
        }

        public PerformanceMeasurementPlugin PerformanceMeasurementPlugin
        {
            get { return mPerformanceMeasurementPlugin; }
            set 
            { 
                mPerformanceMeasurementPlugin = value;

                MeasureTimesCheckBox.Checked = mPerformanceMeasurementPlugin.Active;
            }
        }

        private void PerformanceForm_Load(object sender, EventArgs e)
        {
        }

        private void MeasureTimesCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            PerformanceMeasurementPlugin.Active = MeasureTimesCheckBox.Checked;


        }



    }
}
