using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins
{
    public class PluginError : ErrorViewModel
    {
        PluginContainer pluginContainer;
        public PluginContainer PluginContainer
        {
            get
            {
                return pluginContainer;
            }
            set
            {
                pluginContainer = value;
                Details = $"{pluginContainer.Name} ({pluginContainer?.Plugin?.Version}):{pluginContainer.FailureDetails}\n" +
                    pluginContainer.AssemblyLocation + "\n" +
                    pluginContainer.FailureException.ToString();
            }
        }

        public override bool GetIfIsFixed()
        {
            return base.GetIfIsFixed();
        }

        public override bool ReactsToFileChange(FilePath filePath)
        {
            return false;
        }

        public override string ToString()
        {
            return "Plugin had an error:\n" + PluginContainer?.FailureDetails;
        }
    }
}
