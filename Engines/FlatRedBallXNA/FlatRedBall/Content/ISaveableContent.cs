using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO;

namespace FlatRedBall.Content
{
    public interface ISaveableContent
    {
        List<string> GetReferencedFiles(RelativeType relativeType);


    }

    public static class ISaveableContentExtensionMethods
    {
        public static bool AreAllFilesRelativeTo(this ISaveableContent saveableContent,
            string fileName)
        {
            List<string> allFiles = saveableContent.GetReferencedFiles(RelativeType.Absolute);

            string directory = FileManager.GetDirectory(fileName);

            foreach (string referencedFile in allFiles)
            {
                if(!FileManager.IsRelativeTo(referencedFile, directory))
                {
                    return false;
                }
            }

            return true;

        }

    }

}
