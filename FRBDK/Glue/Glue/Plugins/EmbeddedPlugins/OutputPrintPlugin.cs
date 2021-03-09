using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.Controls;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins
{
    [Export(typeof(PluginBase))]
    public class OutputPrintPlugin : EmbeddedPlugin
    {
        OutputWindow mOutputWindow; // This is the control we created
        PluginTab tab;
        public override void StartUp()
        {
            mOutputWindow = new OutputWindow();
            tab = base.CreateAndAddTab(mOutputWindow, "Output", TabLocation.Bottom);

            this.OnOutputHandler += OnOutput;
            this.OnErrorOutputHandler += OnErrorOutput;
        }


        public void OnOutput(string output)
        {
            if (mOutputWindow != null && !string.IsNullOrWhiteSpace(output))
            {
                mOutputWindow.OnOutput(output);
            }
        }

        public void OnErrorOutput(string output)
        {
            if (mOutputWindow != null && !string.IsNullOrWhiteSpace(output))
            {
                mOutputWindow.OnErrorOutput(output);
            }
        }
    }
}
