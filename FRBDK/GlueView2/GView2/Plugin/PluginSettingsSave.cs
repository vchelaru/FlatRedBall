using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueView2.Plugin
{
    public class PluginSettingsSave
    {
        public List<string> DisabledPlugins
        {
            get;
            set;
        }




        public PluginSettingsSave()
        {
            DisabledPlugins = new List<string>();
        }

        public static PluginSettingsSave Load(string fileName)
        {
            return FileManager.XmlDeserialize<PluginSettingsSave>(fileName);
        }

        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);
        }
    }
}
