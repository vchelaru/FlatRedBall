using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.OutputPlugin;
using L = Localization;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins
{
    [Export(typeof(PluginBase))]
    public class OutputPrintPlugin : EmbeddedPlugin
    {
        OutputControl outputControl; // This is the control we created

        public override void StartUp()
        {
            outputControl = new OutputControl();
            var tab = base.CreateAndAddTab(outputControl, L.Texts.Output, TabLocation.Bottom);

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
