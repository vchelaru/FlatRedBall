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
    [Export(typeof(IOutputReceiver))]
    public class OutputPrintPlugin : EmbeddedPlugin, IOutputReceiver
    {
        OutputWindow mOutputWindow; // This is the control we created

        public override void StartUp()
        {
            mOutputWindow = new OutputWindow();
            this.AddToTab(PluginManager.BottomTab, mOutputWindow, "Output");
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
