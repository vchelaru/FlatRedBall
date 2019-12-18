using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using EditorObjects.Parsing;
using Ionic.Zip;
using FlatRedBall.IO;
using FlatRedBall.Glue.Managers;

namespace FlatRedBall.Glue.IO.Zip
{
    public static class Zipper
    {
        public static string CreateZip(ReferencedFileSave rfs)
        {
            string absoluteFile = ProjectManager.MakeAbsolute(rfs.Name, true);

            List<string> allFiles = FileReferenceManager.Self.GetFilesReferencedBy(
                absoluteFile, TopLevelOrRecursive.Recursive);

            #region Check for relative files

            string directoryOfMainFile = FileManager.GetDirectory(absoluteFile);
            bool areAnyFilesOutsideOfMainDirectory = false;
            foreach (string referencedFile in allFiles)
            {
                if (!FileManager.IsRelativeTo(referencedFile, directoryOfMainFile))
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

                using (ZipFile zip = new ZipFile())
                {
                    foreach (string fileToAdd in allFiles)
                    {
                        string directory = FileManager.MakeRelative(FileManager.GetDirectory(fileToAdd), directoryOfMainFile);
                        if (directory.EndsWith("/"))
                        {
                            directory = directory.Substring(0, directory.Length - 1);
                        }

                        zip.AddFile(fileToAdd, directory);
                    }

                    zip.Save(outputFile);
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
                using (ZipFile zip1 = ZipFile.Read(fileName))
                {
                    // here, we extract every entry, but we could extract conditionally
                    // based on entry name, size, date, checkbox status, etc.  
                    foreach (ZipEntry zipEntry in zip1)
                    {
                        zipEntry.Extract(unpackDirectory, ExtractExistingFileAction.OverwriteSilently);
                    }
                }
                fileName = fileName.Substring(0, fileName.Length - 1) + 'x';
                //System.Windows.Forms.MessageBox.Show("Unzipped zip file to\n" + fileName);
            }

        }

        public static void UnzipScreenOrEntityImport(string fileName, out string unpackDirectory, out List<string> filesToAddToContent, out List<string> codeFiles)
        {
            codeFiles = new List<string>();
            unpackDirectory = FileManager.UserApplicationDataForThisApplication + "Unzip\\";
            if (System.IO.Directory.Exists(unpackDirectory))
            {
                FileManager.DeleteDirectory(unpackDirectory);
            }
            System.IO.Directory.CreateDirectory(unpackDirectory);

            filesToAddToContent = new List<string>();
            string csFile = null;
            string elementFile = null;

            using (ZipFile zip1 = ZipFile.Read(fileName))
            {
                foreach (ZipEntry zipEntry in zip1)
                {
                    string extension = FileManager.GetExtension(zipEntry.FileName);

                    if (extension == "cs")
                    {
                        codeFiles.Add(zipEntry.FileName);
                    }
                    else if (extension == "entx" || extension == "scrx")
                    {
                        elementFile = zipEntry.FileName;
                    }
                    else
                    {
                        filesToAddToContent.Add(zipEntry.FileName);
                    }

                    zipEntry.Extract(unpackDirectory, ExtractExistingFileAction.OverwriteSilently);
                }
            }


        }
    }
}
