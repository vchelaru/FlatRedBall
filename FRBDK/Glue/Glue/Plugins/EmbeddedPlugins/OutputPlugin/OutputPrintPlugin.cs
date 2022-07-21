using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.Controls;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.OutputPlugin;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins
{
    [Export(typeof(PluginBase))]
    public class OutputPrintPlugin : EmbeddedPlugin
    {
        OutputControl outputControl; // This is the control we created

        public override void StartUp()
        {
            outputControl = new OutputControl();
            var tab = base.CreateAndAddTab(outputControl, "Output", TabLocation.Bottom);

            this.OnOutputHandler += OnOutput;
            this.OnErrorOutputHandler += OnErrorOutput;
        }


        public void OnOutput(string output)
        {
            if (!string.IsNullOrWhiteSpace(output))
            {
                outputControl.OnOutput(output);
            }
        }

        public void OnErrorOutput(string output)
        {
            if (!string.IsNullOrWhiteSpace(output))
            {
                outputControl?.OnErrorOutput(output);
            }
        }
    }
}
