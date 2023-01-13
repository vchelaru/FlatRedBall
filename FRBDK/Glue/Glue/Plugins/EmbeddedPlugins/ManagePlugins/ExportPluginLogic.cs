using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.IO;
using FlatRedBall.IO;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.ManagePlugins
{
    public class ExportPluginLogic
    {
        List<string> mWhatIsConsideredCode = new List<string>();
        public ExportPluginLogic()
        {
            mWhatIsConsideredCode.Add("cs");
            mWhatIsConsideredCode.Add("resx");
        }

        public void CreatePluginFromDirectory(string sourceDirectory, string destinationFileName, bool includeAllFiles)
        {
            if (File.Exists(destinationFileName))
            {
                FileHelper.MoveToRecycleBin(destinationFileName);
            }
            System.IO.Compression.ZipFile.CreateFromDirectory(sourceDirectory, destinationFileName,
                System.IO.Compression.CompressionLevel.Fastest, includeBaseDirectory:true);
        }
    }
}
