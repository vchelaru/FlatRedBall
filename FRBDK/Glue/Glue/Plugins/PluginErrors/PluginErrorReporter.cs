using FlatRedBall.Glue.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.PluginErrors
{
    public class PluginErrorReporter : IErrorReporter
    {
        public ErrorViewModel[] GetAllErrors()
        {
            List<ErrorViewModel> toReturn = new List<ErrorViewModel>();
            foreach(var container in PluginManager.AllPluginContainers)
            {
                if(container.FailureException != null)
                {
                    var error = new PluginError();
                    error.PluginContainer = container;
                    toReturn.Add(error);
                }
            }

            return toReturn.ToArray();
        }
    }
}
