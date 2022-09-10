using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OfficialPlugins.RuntimeFileCopier
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : EmbeddedPlugin
    {
        string[] copiedExtensions = new[]
        {
            "csv",
            "txt",
            "png",
            "tmx",
            "tsx",
            "bmp",
            "png",
            "achx",
            "emix",
            "json"
        };

        public bool ShouldCopyFile(string fileName)
        {
            string extension = FileManager.GetExtension(fileName);

            return copiedExtensions.Contains(extension);

        }

        public override void StartUp()
        {
        }

    }
}
