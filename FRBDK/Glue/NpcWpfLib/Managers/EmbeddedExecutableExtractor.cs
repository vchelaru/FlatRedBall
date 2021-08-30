using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using ToolsUtilities;

namespace Npc.Managers
{
    public class EmbeddedExecutableExtractor
    {
        static EmbeddedExecutableExtractor mSelf;

        public static EmbeddedExecutableExtractor Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new EmbeddedExecutableExtractor();
                }
                return mSelf;
            }
        }

        public string ExtractFile(string unqualifiedName)
        {

            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            string saveAsName = FileManager.UserApplicationDataForThisApplication + unqualifiedName;
            ExtractFileFromAssembly(currentAssembly, unqualifiedName, saveAsName);
            return saveAsName;
        }

        public bool ExtractFileFromAssembly(Assembly currentAssembly, string unqualifiedName, string saveAsName)
        {
            bool succeeded = true;

            string[] arrResources = currentAssembly.GetManifestResourceNames();

            string qualifiedName = null;

            foreach (string resourceName in arrResources)
            {
                if (resourceName.EndsWith("." + unqualifiedName))
                {
                    qualifiedName = resourceName;
                    break;
                }
            }


            FileInfo fileInfoOutputFile = new FileInfo(saveAsName);
            //CHECK IF FILE EXISTS AND DO SOMETHING DEPENDING ON YOUR NEEDS
            if (fileInfoOutputFile.Exists)
            {

            }

            if(string.IsNullOrWhiteSpace( qualifiedName))
            {
                throw new InvalidOperationException($"Could not find {unqualifiedName} as an embedded resource. Has this been added as an embedded resource to the project?");
            }

            using (FileStream streamToOutputFile = fileInfoOutputFile.OpenWrite())
            using (Stream streamToResourceFile = currentAssembly.GetManifestResourceStream(qualifiedName))
            {
                //---------------------------------
                //SAVE TO DISK OPERATION
                //---------------------------------
                const int size = 4096;
                byte[] bytes = new byte[4096];
                int numBytes;
                while ((numBytes = streamToResourceFile.Read(bytes, 0, size)) > 0)
                {
                    streamToOutputFile.Write(bytes, 0, numBytes);
                }
            }

            if (!File.Exists(saveAsName))
            {
                throw new Exception("The file wasn't extracted correctly");
                succeeded = false;
            }

            return succeeded;
        }

    }
}
