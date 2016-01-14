using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Plugins.Interfaces
{
    public interface IOutputReceiver : IPlugin
    {
        void OnOutput(string output);
        void OnErrorOutput(string output);
    }
}
