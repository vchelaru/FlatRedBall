using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Data
{
    public class PluginSettings
    {
        public const string SaveLocation = "GlueSettings/PluginSettings.xml";

        public List<string> PluginsToIgnore = new List<string>();

        public static bool FileExists(string glueProjectFolder)
        {
            return File.Exists(glueProjectFolder + SaveLocation);
        }

        public void Save(string glueProjectFolder)
        {
            FileManager.XmlSerialize(this, glueProjectFolder + SaveLocation);
        }

        public static PluginSettings Load(string glueProjectFolder)
        {
            return FileManager.XmlDeserialize<PluginSettings>(glueProjectFolder + SaveLocation);

        }


    }
}
