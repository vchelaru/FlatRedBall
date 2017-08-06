using GlueSaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.SaveClasses
{
    
    public class GluxPluginData
    {
        public List<PluginRequirement> RequiredPlugins
        {
            get;
            set;
        } = new List<PluginRequirement>();
    }
}
