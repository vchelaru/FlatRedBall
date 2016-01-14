using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using FlatRedBall.Glue.Plugins.Interfaces;

namespace PluginTestbed.SourceSetup
{
    [Export(typeof(IOpenVisualStudio))]
	public partial class SourceSetupPlugin : IOpenVisualStudio
	{
        public bool OpenSolution(string solution)
        {
            var settings = SourceSetupSettings.LoadSettings();

            if (settings.UseSource)
            {
                var debugSolution = solution.Substring(0, solution.Length - 4) + "_Debug.sln";

                if (File.Exists(debugSolution))
                {
                    Process.Start(debugSolution);

                    return true;
                }
            }

            return false;
        }

        public bool OpenProject(string project)
        {
            return false;
        }
	}
}
