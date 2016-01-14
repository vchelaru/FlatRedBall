using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.Interfaces
{
    public interface IGlueSettings : IPlugin
    {
        void ReactToGlueSettingsLoad(GlueSettingsSave glueSettings, string fileName);
        void ReactToGlueSettingsSave();
    }
}
