using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.Controls
{
    public partial class PerformanceSettingsWindow : Form
    {
        public PerformanceSettingsWindow()
        {
            InitializeComponent();

            propertyGrid1.SelectedObject =
                ProjectManager.GlueProjectSave?.PerformanceSettingsSave;
        }


        private void propertyGrid1_Click(object sender, EventArgs e)
        {

        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();
            GluxCommands.Self.SaveProjectAndElements();
        }

        private void DoneButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
