using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.Interfaces
{
    public interface IGluxLoad : IPlugin
    {
        void ReactToGluxLoad(GlueProjectSave newGlux, string fileName);
        void ReactToGluxSave();
        void ReactToGluxUnload(bool isExiting);
        void RefreshGlux();
    }
}
