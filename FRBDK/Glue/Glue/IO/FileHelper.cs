using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EditorObjects.Parsing;
using FlatRedBall.Glue.Managers;
using FlatRedBall.IO;
using Microsoft.VisualBasic.FileIO;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.IO
{
    public static class FileHelper
    {
        public static bool DoesFileReferenceContent(string file)
        {
            bool toReturn = false;

            string extension = FileManager.GetExtension(file);

            switch (extension)
            {
                case "scnx":
                case "srgx":
                case "achx":
                case "emix":
                case "bmfc":
                case "wme":
                case "fnt":
                    toReturn = true;
                    break;

            }
            
            // but wait!  This may reference content because
            // of plugins!
            if (!toReturn)
            {
                toReturn = PluginManager.CanFileReferenceContent(file);
            }
            
            return toReturn;
        }

        public static bool IsCodeFile(string fileName)
        {
            return FileManager.GetExtension(fileName) == "cs";
        }

        public static void RecursivelyCopyContentTo(string sourceFile, string sourceDirectoryRelativeTo, string directoryThatFileShouldBeRelativeTo, string destinationFileName = null)
        {
            string fileWithoutPath = FileManager.MakeRelative(sourceFile, sourceDirectoryRelativeTo);

            string targetFile;

            if (string.IsNullOrEmpty(destinationFileName))
            {
                targetFile = directoryThatFileShouldBeRelativeTo + fileWithoutPath;
            }
            else
            {
                targetFile = directoryThatFileShouldBeRelativeTo + destinationFileName;
            }
            

            string sourceDirectory = FileManager.GetDirectory(sourceFile);


            if (!string.IsNullOrEmpty(FileManager.GetDirectory(fileWithoutPath)))
            {
                directoryThatFileShouldBeRelativeTo =
                    FileManager.GetDirectory(directoryThatFileShouldBeRelativeTo + fileWithoutPath);
            }

            // We need to copy the file over to the new location.
            if (!Directory.Exists(FileManager.GetDirectory(targetFile)))
            {
                Directory.CreateDirectory(FileManager.GetDirectory(targetFile));
            }

            if (File.Exists(sourceFile.Replace("/", "\\")))
            {
                File.Copy(sourceFile, targetFile, true);
            }
            else
            {
                // DO NOT SHOW A MESSAGE BOX HERE!!!!
                //MessageBox.Show("Glue is attempting to copy file " + sourceFile + " which is referenced by game content, but it cannot find this file.");
            }


            // We also need to copy all of the other content files.
            var referencedFiles = new List<FilePath>();

            referencedFiles = FileReferenceManager.Self.GetFilesReferencedBy(sourceFile);

            for (int i = 0; i < referencedFiles.Count; i++)
            {
                var file = referencedFiles[i];

                RecursivelyCopyContentTo(file.FullPath, sourceDirectory, directoryThatFileShouldBeRelativeTo);

            }
        }

        /// <summary>
        /// Moves a file to the recycle bin. Attempts multiple times in case the file is in use
        /// </summary>
        /// <param name="fileName"></param>
        public static void MoveToRecycleBin(string fileName)
        {
            GlueCommands.Self.TryMultipleTimes(() =>
            {
                // Let's move stuff to the recycle bin:
                //System.IO.File.Delete(fileName);
                FileSystem.DeleteFile(fileName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            });

        }
    }
}
