using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;

namespace GluePluginLibrary
{
    public interface IGluePlugin
    {
        void ReceiveMessage(string message);
    }
}
