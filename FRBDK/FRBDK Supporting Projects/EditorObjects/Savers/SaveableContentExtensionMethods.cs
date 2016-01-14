using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content;
using FlatRedBall.IO;
using Ionic.Zip;

namespace EditorObjects.Savers
{
    public static class SaveableContentExtensionMethods
    {
        public static void SaveZipFile(this ISaveableContent saveable, string fileName)
        {
            // Make sure all files are relative
            List<string> allFiles = saveable.GetReferencedFiles(RelativeType.Absolute);

            string directory = FileManager.GetDirectory(fileName);

            foreach (string referencedFile in allFiles)
            {
                if (!FileManager.IsRelativeTo(referencedFile, directory))
                {
                    throw new InvalidOperationException("The file " +
                        referencedFile + " is not relative to the destination file " +
                        fileName + ".  All files must be relative to create a ZIP.");
                }
            }

            // First save the XML - we'll need this
            string xmlFileName = FileManager.RemoveExtension(fileName) + ".xmlInternal";
            FileManager.XmlSerialize(saveable, xmlFileName);

            using (ZipFile zip = new ZipFile())
            {
                // add this map file into the "images" directory in the zip archive
                zip.AddFile(xmlFileName);
                foreach (string referencedFile in allFiles)
                {
                    zip.AddFile(referencedFile);
                }
                zip.Save(fileName);
            }
        }

    }
}
