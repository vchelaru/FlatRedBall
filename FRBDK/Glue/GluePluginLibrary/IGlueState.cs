using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;

namespace GluePluginLibrary
{
    public interface IGlueState
    {
        IElement CurrentElement
        {
            get;
        }
    }
}
