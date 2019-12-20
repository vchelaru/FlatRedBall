using FlatRedBall.Glue.Managers;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMXGlueLib;

namespace TileGraphicsPlugin.Managers
{
    class CopyFileManager : Singleton<CopyFileManager>
    {
        public void CopyTmx(string absoluteSourceFile, string absoluteDestinationFile)
        {
            TiledMapSave tms = TiledMapSave.FromFile(absoluteSourceFile);

            CopyFile(absoluteSourceFile, absoluteDestinationFile);

            string sourceDirectory = FileManager.GetDirectory(absoluteSourceFile);
            string destinationDirectory = FileManager.GetDirectory(absoluteDestinationFile);

            List<string> referencedFiles = tms.GetReferencedFiles();

            foreach (string relativeFile in referencedFiles)
            {
                string absoluteSourceReferencedFile = sourceDirectory + relativeFile;
                string absoluteDestinationReferencedFile = destinationDirectory + relativeFile;
                if (System.IO.File.Exists(absoluteSourceReferencedFile))
                {
                    CopyFile(absoluteSourceReferencedFile, absoluteDestinationReferencedFile);
                }
            }
        }

        private void CopyFile(string absoluteSourceFile, string absoluteDestinationFile)
        {
            if (!Directory.Exists(FileManager.GetDirectory(absoluteDestinationFile)))
            {
                Directory.CreateDirectory(FileManager.GetDirectory(absoluteDestinationFile));
            }


            File.Copy(absoluteSourceFile, absoluteDestinationFile, true);
            
        }




    }
}
