using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Plugins
{
    public interface IGlueCommands
    {
        void RefreshUiForSelectedElement();

        void SaveGlux();
    }
}
