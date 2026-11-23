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
            this.OnOutputHandler += OnOutput;
            this.OnErrorOutputHandler += OnErrorOutput;
        }

        public override void StartUpUi()
        {
            outputControl = new OutputControl();
            base.CreateAndAddTab(outputControl, L.Texts.Output, TabLocation.Bottom);
        }

        public void OnOutput(string output)
        {
            if (!string.IsNullOrWhiteSpace(output))
            {
                outputControl?.OnOutput(output);
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
