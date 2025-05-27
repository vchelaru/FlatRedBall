using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using EditorObjects.Parsing;
using FlatRedBall.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System.IO.Compression;
using System.IO;

namespace FlatRedBall.Glue.IO.Zip
{
    public static class Zipper
    {
        public static string CreateZip(ReferencedFileSave rfs)
        {
            string absoluteFile = GlueCommands.Self.GetAbsoluteFileName(rfs);

            var allFiles = FileReferenceManager.Self.GetFilesReferencedBy(
                absoluteFile, TopLevelOrRecursive.Recursive);

            #region Check for relative files

            string directoryOfMainFile = FileManager.GetDirectory(absoluteFile);
            bool areAnyFilesOutsideOfMainDirectory = false;
            foreach (var referencedFile in allFiles)
            {
                if (!FileManager.IsRelativeTo(referencedFile.FullPath, directoryOfMainFile))
                {
                    areAnyFilesOutsideOfMainDirectory = true;
                    break;
                }
            }


            #endregion
            string outputFile;

            if (areAnyFilesOutsideOfMainDirectory)
            {
                outputFile = null;    
            }
            else
            {
                allFiles.Add(absoluteFile);

                string extension = FileManager.GetExtension(rfs.Name);
                string newExtension = "zip";
                if (extension.Length == 4 && extension[3] == 'x')
                {
                    newExtension = extension.Substring(0, 3) + 'z';
                }

                outputFile = FileManager.RemoveExtension(absoluteFile) + "." + newExtension;

                using (FileStream zipToCreate = new FileStream(outputFile, FileMode.Create))
                using (ZipArchive archive = new ZipArchive(zipToCreate, ZipArchiveMode.Create))
                {
                    foreach (var fileToAdd in allFiles)
                    {
                        // Compute the relative directory path inside the zip
                        string directory = FileManager.MakeRelative(
                            fileToAdd.GetDirectoryContainingThis().FullPath,
                            directoryOfMainFile);

                        // Remove trailing slash if present
                        if (directory.EndsWith("/"))
                        {
                            directory = directory.Substring(0, directory.Length - 1);
                        }

                        // Build the full entry name (path inside the zip)
                        string entryName = Path.Combine(directory, Path.GetFileName(fileToAdd.FullPath)).Replace('\\', '/');

                        // Create the entry and copy the file into it
                        ZipArchiveEntry entry = archive.CreateEntry(entryName);
                        using var entryStream = entry.Open();
                        using var fileStream = new FileStream(fileToAdd.FullPath, FileMode.Open, FileAccess.Read);
                        fileStream.CopyTo(entryStream);
                    }
                }
            }
            return outputFile;
        }

        public static void UnzipAndModifyFileIfZip(ref string fileName)
        {
            string extension = FileManager.GetExtension(fileName);
            string unpackDirectory = FileManager.GetDirectory(fileName);

            if (extension.Length == 4 && extension[3] == 'z')
            {
                using (ZipArchive archive = ZipFile.OpenRead(fileName))
                {
                    foreach (ZipArchiveEntry zipEntry in archive.Entries)
                    {
                        string destinationPath = Path.Combine(unpackDirectory, zipEntry.FullName);

                        // Ensure the directory exists
                        string destinationDir = Path.GetDirectoryName(destinationPath);
                        if (!string.IsNullOrEmpty(destinationDir))
                        {
                            Directory.CreateDirectory(destinationDir);
                        }

                        // Only extract if it's a file (not a directory)
                        if (!string.IsNullOrEmpty(zipEntry.Name))
                        {
                            zipEntry.ExtractToFile(destinationPath, overwrite: true);
                        }
                    }
                }
                fileName = fileName.Substring(0, fileName.Length - 1) + 'x';
                //System.Windows.Forms.MessageBox.Show("Unzipped zip file to\n" + fileName);
            }

        }

        public static void UnzipScreenOrEntityImport(string fileName, out string unpackDirectory, out List<string> filesToAddToContent, out List<string> codeFilesInZip)
        {
            codeFilesInZip = new List<string>();
            unpackDirectory = FileManager.UserApplicationDataForThisApplication + "Unzip\\";
            if (System.IO.Directory.Exists(unpackDirectory))
            {
                FileManager.DeleteDirectory(unpackDirectory);
            }
            System.IO.Directory.CreateDirectory(unpackDirectory);

            filesToAddToContent = new List<string>();
            string csFile = null;
            string elementFile = null;

            using (ZipArchive archive = ZipFile.OpenRead(fileName))
            {
                foreach (ZipArchiveEntry zipEntry in archive.Entries)
                {
                    // Get the extension using your custom FileManager (assuming it takes the full path or name)
                    string extension = FileManager.GetExtension(zipEntry.FullName);

                    if (extension == "cs")
                    {
                        codeFilesInZip.Add(zipEntry.FullName);
                    }
                    else if (extension == "entx" || extension == "scrx")
                    {
                        elementFile = zipEntry.FullName;
                    }
                    else
                    {
                        filesToAddToContent.Add(zipEntry.FullName);
                    }

                    // Prepare the destination path
                    string destinationPath = Path.Combine(unpackDirectory, zipEntry.FullName);

                    // Ensure the destination directory exists
                    string destinationDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                    }

                    // Only extract actual files (skip folder entries)
                    if (!string.IsNullOrEmpty(zipEntry.Name))
                    {
                        zipEntry.ExtractToFile(destinationPath, overwrite: true);
                    }
                }
            }
        }
    }
}
