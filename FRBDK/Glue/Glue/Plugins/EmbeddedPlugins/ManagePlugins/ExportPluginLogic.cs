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

        public string CreatePluginFromDirectory(string sourceDirectory, string destinationFileName, bool includeAllFiles)
        {
            string response = "Unknown Error";




            if (File.Exists(destinationFileName))
            {
                FileHelper.DeleteFile(destinationFileName);
            }
            //Create plugin file
            using (var zip = new ZipFile())
            {
                var directory = new DirectoryInfo(sourceDirectory);

                //Add files in directory
                foreach (var fileToAdd in Directory.GetFiles(sourceDirectory, "*.*", SearchOption.AllDirectories))
                {
                    if (includeAllFiles || mWhatIsConsideredCode.Contains(FileManager.GetExtension(fileToAdd).ToLower()))
                    {
                        string relativeDirectory = null;

                        relativeDirectory = FileManager.MakeRelative(FileManager.GetDirectory(fileToAdd), directory.Parent.FullName);

                        if (relativeDirectory.EndsWith("/"))
                        {
                            relativeDirectory = relativeDirectory.Substring(0, relativeDirectory.Length - 1);
                        }
                        zip.AddFile(fileToAdd, relativeDirectory);
                    }
                }

                //Add compatibility file
                var time = new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime;


                if (zip.Entries.Any(item => item.FileName == directory.Name + "/" + "Compatibility.txt"))
                {
                    zip.RemoveEntry(directory.Name + "/" + "Compatibility.txt");
                }

                try
                {

                    zip.AddFile("Compatibility.txt", directory.Name);
                    response = "Successfully Created";
                }
                catch (Exception)
                {
                    response = "The directory already contains a Compatibility.txt file name.  The plugin will still be created but it may not properly include compatibility information.  Consider removing this file and re-creating the plugin.";
                }
                zip.Save(destinationFileName);

            }

            return response;
        }
    }
}
